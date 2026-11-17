using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ContosoUniversity.DAL;
using ContosoUniversity.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ContosoUniversity.Tests.Helpers
{
    /// <summary>SchoolContext whose SaveChanges always fails with a DbUpdateException.</summary>
    public class ThrowingSchoolContext : SchoolContext
    {
        public bool ThrowOnSave { get; set; }
        public Exception ExceptionToThrow { get; set; } = new DbUpdateException("boom");

        public ThrowingSchoolContext(DbContextOptions<SchoolContext> options) : base(options)
        {
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            if (ThrowOnSave)
            {
                throw ExceptionToThrow;
            }
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
    }

    /// <summary>Simple in-memory logger capturing log entries.</summary>
    public class ListLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message, Exception Exception)> Entries { get; } = new();

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            Entries.Add((logLevel, formatter(state, exception), exception));
        }
    }

    public static class TestHelpers
    {
        public static DbContextOptions<SchoolContext> NewOptions() =>
            new DbContextOptionsBuilder<SchoolContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

        public static SchoolContext NewContext() => new SchoolContext(NewOptions());

        public static ThrowingSchoolContext NewThrowingContext() => new ThrowingSchoolContext(NewOptions());

        /// <summary>Creates a context seeded with a small, deterministic data set.</summary>
        public static T Seed<T>(T context) where T : SchoolContext
        {
            var kim = new Instructor { ID = 1, FirstMidName = "Kim", LastName = "Abercrombie", HireDate = new DateTime(1995, 3, 11) };
            var fadi = new Instructor { ID = 2, FirstMidName = "Fadi", LastName = "Fakhouri", HireDate = new DateTime(2002, 7, 6) };
            context.Instructors.AddRange(kim, fadi);

            var english = new Department { DepartmentID = 1, Name = "English", Budget = 1000, StartDate = new DateTime(2007, 9, 1), InstructorID = 1 };
            var math = new Department { DepartmentID = 2, Name = "Mathematics", Budget = 2000, StartDate = new DateTime(2007, 9, 1), InstructorID = 2 };
            context.Departments.AddRange(english, math);

            var composition = new Course { CourseID = 2021, Title = "Composition", Credits = 3, DepartmentID = 1, Instructors = new List<Instructor> { kim } };
            var calculus = new Course { CourseID = 1045, Title = "Calculus", Credits = 4, DepartmentID = 2, Instructors = new List<Instructor> { fadi } };
            context.Courses.AddRange(composition, calculus);

            context.OfficeAssignments.Add(new OfficeAssignment { InstructorID = 2, Location = "Smith 17" });

            context.Students.AddRange(
                new Student { ID = 1, LastName = "Alexander", FirstMidName = "Carson", EnrollmentDate = new DateTime(2010, 9, 1) },
                new Student { ID = 2, LastName = "Alonso", FirstMidName = "Meredith", EnrollmentDate = new DateTime(2012, 9, 1) },
                new Student { ID = 3, LastName = "Anand", FirstMidName = "Arturo", EnrollmentDate = new DateTime(2012, 9, 1) },
                new Student { ID = 4, LastName = "Barzdukas", FirstMidName = "Gytis", EnrollmentDate = new DateTime(2011, 9, 1) });

            context.Enrollments.AddRange(
                new Enrollment { EnrollmentID = 1, StudentID = 1, CourseID = 2021, Grade = Grade.A },
                new Enrollment { EnrollmentID = 2, StudentID = 2, CourseID = 2021, Grade = Grade.B },
                new Enrollment { EnrollmentID = 3, StudentID = 1, CourseID = 1045 });

            context.SaveChanges();
            return context;
        }

        public static SchoolContext SeededContext() => Seed(NewContext());

        public static ThrowingSchoolContext SeededThrowingContext() => Seed(NewThrowingContext());
    }
}
