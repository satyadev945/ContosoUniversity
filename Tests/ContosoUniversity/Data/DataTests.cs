using ContosoUniversity.Helpers;
using ContosoUniversity.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ContosoUniversity.Data
{
    public class DbInitializerTests
    {
        [Fact]
        public async Task InitializeAsync_EmptyDatabase_SeedsAllEntities()
        {
            using var ctx = TestHelpers.CreateContext();
            await DbInitializer.InitializeAsync(ctx);

            Assert.Equal(8, await ctx.Students.CountAsync());
            Assert.Equal(5, await ctx.Instructors.CountAsync());
            Assert.Equal(4, await ctx.Departments.CountAsync());
            Assert.Equal(7, await ctx.Courses.CountAsync());
            Assert.Equal(3, await ctx.OfficeAssignments.CountAsync());
            Assert.Equal(11, await ctx.Enrollments.CountAsync());
        }

        [Fact]
        public async Task InitializeAsync_CalledTwice_IsIdempotent()
        {
            using var ctx = TestHelpers.CreateContext();
            await DbInitializer.InitializeAsync(ctx);
            await DbInitializer.InitializeAsync(ctx);
            Assert.Equal(8, await ctx.Students.CountAsync());
            Assert.Equal(11, await ctx.Enrollments.CountAsync());
        }

        [Fact]
        public async Task InitializeAsync_ExistingStudents_DoesNotSeed()
        {
            using var ctx = TestHelpers.CreateContext();
            ctx.Students.Add(new Student { LastName = "A", FirstMidName = "B", EnrollmentDate = DateTime.Today });
            await ctx.SaveChangesAsync();
            await DbInitializer.InitializeAsync(ctx);
            Assert.Equal(1, await ctx.Students.CountAsync());
            Assert.Equal(0, await ctx.Courses.CountAsync());
        }

        [Fact]
        public async Task InitializeAsync_AssignsInstructorsToCourses()
        {
            using var ctx = TestHelpers.CreateContext();
            await DbInitializer.InitializeAsync(ctx);
            var chem = await ctx.Courses.Include(c => c.Instructors).SingleAsync(c => c.Title == "Chemistry");
            Assert.Equal(2, chem.Instructors.Count);
        }

        [Fact]
        public async Task InitializeAsync_AnandHasUngradedEnrollment()
        {
            using var ctx = TestHelpers.CreateContext();
            await DbInitializer.InitializeAsync(ctx);
            Assert.Equal(1, await ctx.Enrollments.CountAsync(e => e.Grade == null));
        }

        [Fact]
        public async Task InitializeAsync_CancelledToken_Throws()
        {
            using var ctx = TestHelpers.CreateContext();
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => DbInitializer.InitializeAsync(ctx, cts.Token));
        }
    }

    public class SchoolContextTests
    {
        [Fact]
        public void Constructor_CreatesContextWithDbSets()
        {
            using var ctx = TestHelpers.CreateContext();
            Assert.NotNull(ctx.Courses);
            Assert.NotNull(ctx.Departments);
            Assert.NotNull(ctx.Enrollments);
            Assert.NotNull(ctx.Instructors);
            Assert.NotNull(ctx.Students);
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
            using var ctx = TestHelpers.CreateContext();
            var et = ctx.Model.FindEntityType(entity);
            Assert.NotNull(et);
            Assert.Equal(table, et.GetTableName());
        }

        [Fact]
        public void Model_HasCourseInstructorJoinTable()
        {
            using var ctx = TestHelpers.CreateContext();
            var join = ctx.Model.GetEntityTypes().SingleOrDefault(e => e.GetTableName() == "CourseInstructor");
            Assert.NotNull(join);
        }

        [Fact]
        public async Task CanAddAndQueryStudent()
        {
            using var ctx = TestHelpers.CreateContext();
            ctx.Students.Add(new Student { LastName = "X", FirstMidName = "Y", EnrollmentDate = DateTime.Today });
            await ctx.SaveChangesAsync();
            Assert.Equal("X, Y", (await ctx.Students.SingleAsync()).FullName);
        }
    }
}
