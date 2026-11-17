using System;
using System.Data.Common;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ContosoUniversity.Models;
using ContosoUniversity.Tests.Helpers;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Xunit;

namespace ContosoUniversity.DAL
{
    public class SchoolContextTests
    {
        [Fact]
        public void Constructor_CreatesContext()
        {
            using var ctx = TestHelpers.NewContext();
            Assert.NotNull(ctx);
            Assert.NotNull(ctx.Students);
            Assert.NotNull(ctx.Courses);
            Assert.NotNull(ctx.Departments);
            Assert.NotNull(ctx.Enrollments);
            Assert.NotNull(ctx.Instructors);
            Assert.NotNull(ctx.OfficeAssignments);
        }

        [Theory]
        [InlineData(typeof(Course), "Course")]
        [InlineData(typeof(Department), "Department")]
        [InlineData(typeof(Enrollment), "Enrollment")]
        [InlineData(typeof(Instructor), "Instructor")]
        [InlineData(typeof(Student), "Student")]
        [InlineData(typeof(OfficeAssignment), "OfficeAssignment")]
        public void Model_UsesSingularTableNames(Type entity, string table)
        {
            using var ctx = TestHelpers.NewContext();
            var et = ctx.Model.FindEntityType(entity);
            Assert.NotNull(et);
            Assert.Equal(table, et.GetTableName());
        }

        [Fact]
        public void Model_ConfiguresCourseInstructorManyToMany()
        {
            using var ctx = TestHelpers.NewContext();
            var nav = ctx.Model.FindEntityType(typeof(Course)).FindSkipNavigation("Instructors");
            Assert.NotNull(nav);
            Assert.Equal("CourseInstructor", nav.JoinEntityType.GetTableName());
        }

        [Fact]
        public void Model_DepartmentAdministratorUsesClientSetNull()
        {
            using var ctx = TestHelpers.NewContext();
            var fk = ctx.Model.FindEntityType(typeof(Department)).GetForeignKeys().Single();
            Assert.Equal(DeleteBehavior.ClientSetNull, fk.DeleteBehavior);
        }

        [Fact]
        public async Task SaveAndQuery_Works()
        {
            using var ctx = TestHelpers.NewContext();
            ctx.Students.Add(new Student { LastName = "A", FirstMidName = "B", EnrollmentDate = DateTime.Today });
            await ctx.SaveChangesAsync();
            Assert.Equal(1, await ctx.Students.CountAsync());
        }
    }

    public class DbInitializerTests
    {
        [Fact]
        public async Task InitializeAsync_SeedsEmptyDatabase()
        {
            using var ctx = TestHelpers.NewContext();
            await DbInitializer.InitializeAsync(ctx);

            Assert.Equal(8, await ctx.Students.CountAsync());
            Assert.Equal(5, await ctx.Instructors.CountAsync());
            Assert.Equal(4, await ctx.Departments.CountAsync());
            Assert.Equal(7, await ctx.Courses.CountAsync());
            Assert.Equal(3, await ctx.OfficeAssignments.CountAsync());
            Assert.Equal(11, await ctx.Enrollments.CountAsync());
        }

        [Fact]
        public async Task InitializeAsync_IsIdempotent()
        {
            using var ctx = TestHelpers.NewContext();
            await DbInitializer.InitializeAsync(ctx);
            await DbInitializer.InitializeAsync(ctx);

            Assert.Equal(8, await ctx.Students.CountAsync());
            Assert.Equal(11, await ctx.Enrollments.CountAsync());
        }

        [Fact]
        public async Task InitializeAsync_WhenStudentsExist_DoesNothing()
        {
            using var ctx = TestHelpers.NewContext();
            ctx.Students.Add(new Student { LastName = "A", FirstMidName = "B", EnrollmentDate = DateTime.Today });
            await ctx.SaveChangesAsync();

            await DbInitializer.InitializeAsync(ctx);

            Assert.Equal(1, await ctx.Students.CountAsync());
            Assert.Equal(0, await ctx.Courses.CountAsync());
        }

        [Fact]
        public async Task InitializeAsync_LinksCoursesToInstructorsAndDepartments()
        {
            using var ctx = TestHelpers.NewContext();
            await DbInitializer.InitializeAsync(ctx);

            var chemistry = await ctx.Courses.Include(c => c.Instructors).Include(c => c.Department)
                .SingleAsync(c => c.CourseID == 1050);
            Assert.Equal(2, chemistry.Instructors.Count);
            Assert.Equal("Engineering", chemistry.Department.Name);

            var anandChemistry = await ctx.Enrollments.SingleAsync(e => e.Student.LastName == "Anand" && e.CourseID == 1050);
            Assert.Null(anandChemistry.Grade);
        }
    }

    public class SchoolInterceptorLoggingTests
    {
        private static CommandExecutedEventData ExecutedData() =>
            (CommandExecutedEventData)RuntimeHelpers.GetUninitializedObject(typeof(CommandExecutedEventData));

        private static CommandErrorEventData ErrorData() =>
            (CommandErrorEventData)RuntimeHelpers.GetUninitializedObject(typeof(CommandErrorEventData));

        private static (SchoolInterceptorLogging, ListLogger<SchoolInterceptorLogging>) Create()
        {
            var logger = new ListLogger<SchoolInterceptorLogging>();
            return (new SchoolInterceptorLogging(logger), logger);
        }

        private static SqlCommand Command() => new SqlCommand("SELECT 1");

        [Fact]
        public void Constructor_CreatesInstance()
        {
            var (interceptor, _) = Create();
            Assert.NotNull(interceptor);
            Assert.IsAssignableFrom<DbCommandInterceptor>(interceptor);
        }

        [Fact]
        public void NonQueryExecuted_LogsAndReturnsResult()
        {
            var (i, logger) = Create();
            var r = i.NonQueryExecuted(Command(), ExecutedData(), 7);
            Assert.Equal(7, r);
            Assert.Contains(logger.Entries, e => e.Level == LogLevel.Debug && e.Message.Contains("NonQueryExecuted") && e.Message.Contains("SELECT 1"));
        }

        [Fact]
        public async Task NonQueryExecutedAsync_LogsAndReturnsResult()
        {
            var (i, logger) = Create();
            var r = await i.NonQueryExecutedAsync(Command(), ExecutedData(), 3);
            Assert.Equal(3, r);
            Assert.Single(logger.Entries);
        }

        [Fact]
        public void ScalarExecuted_LogsAndReturnsResult()
        {
            var (i, logger) = Create();
            var r = i.ScalarExecuted(Command(), ExecutedData(), 42);
            Assert.Equal(42, r);
            Assert.Contains(logger.Entries, e => e.Message.Contains("ScalarExecuted"));
        }

        [Fact]
        public async Task ScalarExecutedAsync_LogsAndReturnsResult()
        {
            var (i, logger) = Create();
            var r = await i.ScalarExecutedAsync(Command(), ExecutedData(), "x");
            Assert.Equal("x", r);
            Assert.Contains(logger.Entries, e => e.Message.Contains("ScalarExecuted"));
        }

        [Fact]
        public void ReaderExecuted_LogsAndReturnsResult()
        {
            var (i, logger) = Create();
            DbDataReader reader = null;
            var r = i.ReaderExecuted(Command(), ExecutedData(), reader);
            Assert.Null(r);
            Assert.Contains(logger.Entries, e => e.Message.Contains("ReaderExecuted"));
        }

        [Fact]
        public async Task ReaderExecutedAsync_LogsAndReturnsResult()
        {
            var (i, logger) = Create();
            var r = await i.ReaderExecutedAsync(Command(), ExecutedData(), null);
            Assert.Null(r);
            Assert.Contains(logger.Entries, e => e.Message.Contains("ReaderExecuted"));
        }

        [Fact]
        public void CommandFailed_LogsError()
        {
            var (i, logger) = Create();
            i.CommandFailed(Command(), ErrorData());
            Assert.Contains(logger.Entries, e => e.Level == LogLevel.Error && e.Message.Contains("SELECT 1"));
        }

        [Fact]
        public async Task CommandFailedAsync_LogsError()
        {
            var (i, logger) = Create();
            await i.CommandFailedAsync(Command(), ErrorData());
            Assert.Contains(logger.Entries, e => e.Level == LogLevel.Error);
        }
    }
}
