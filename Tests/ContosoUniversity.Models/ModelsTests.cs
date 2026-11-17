using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Xunit;

namespace ContosoUniversity.Models
{
    public class StudentTests
    {
        [Fact]
        public void FullName_CombinesLastAndFirstName()
        {
            var s = new Student { LastName = "Alexander", FirstMidName = "Carson" };
            Assert.Equal("Alexander, Carson", s.FullName);
        }

        [Fact]
        public void Properties_RoundTrip()
        {
            var date = new DateTime(2010, 9, 1);
            var s = new Student { ID = 5, LastName = "L", FirstMidName = "F", EnrollmentDate = date, Enrollments = new List<Enrollment>() };
            Assert.Equal(5, s.ID);
            Assert.Equal(date, s.EnrollmentDate);
            Assert.Empty(s.Enrollments);
        }

        [Fact]
        public void Validation_MissingRequiredFields_Fails()
        {
            var results = new List<ValidationResult>();
            var student = new Student();
            var ok = Validator.TryValidateObject(student, new ValidationContext(student), results, true);
            Assert.False(ok);
            Assert.Equal(2, results.Count);
        }

        [Fact]
        public void Validation_ValidStudent_Passes()
        {
            var s = new Student { LastName = "A", FirstMidName = "B" };
            Assert.True(Validator.TryValidateObject(s, new ValidationContext(s), new List<ValidationResult>(), true));
        }

        [Fact]
        public void Validation_TooLongFirstName_Fails()
        {
            var s = new Student { LastName = "A", FirstMidName = new string('x', 51) };
            var results = new List<ValidationResult>();
            Assert.False(Validator.TryValidateObject(s, new ValidationContext(s), results, true));
            Assert.Contains(results, r => r.ErrorMessage.Contains("less than 50"));
        }
    }

    public class InstructorTests
    {
        [Fact]
        public void FullName_CombinesLastAndFirstName()
        {
            var i = new Instructor { LastName = "Abercrombie", FirstMidName = "Kim" };
            Assert.Equal("Abercrombie, Kim", i.FullName);
        }

        [Fact]
        public void Properties_RoundTrip()
        {
            var i = new Instructor
            {
                ID = 3,
                HireDate = new DateTime(2000, 1, 1),
                Courses = new List<Course>(),
                OfficeAssignment = new OfficeAssignment { Location = "X" }
            };
            Assert.Equal(3, i.ID);
            Assert.Equal(2000, i.HireDate.Year);
            Assert.Empty(i.Courses);
            Assert.Equal("X", i.OfficeAssignment.Location);
        }

        [Fact]
        public void Validation_MissingNames_Fails()
        {
            var i = new Instructor();
            Assert.False(Validator.TryValidateObject(i, new ValidationContext(i), new List<ValidationResult>(), true));
        }
    }

    public class CourseTests
    {
        [Fact]
        public void Properties_RoundTrip()
        {
            var c = new Course
            {
                CourseID = 1050,
                Title = "Chemistry",
                Credits = 3,
                DepartmentID = 2,
                Department = new Department(),
                Enrollments = new List<Enrollment>(),
                Instructors = new List<Instructor>()
            };
            Assert.Equal(1050, c.CourseID);
            Assert.Equal("Chemistry", c.Title);
            Assert.Equal(3, c.Credits);
            Assert.Equal(2, c.DepartmentID);
            Assert.NotNull(c.Department);
            Assert.Empty(c.Enrollments);
            Assert.Empty(c.Instructors);
        }

        [Theory]
        [InlineData(0, true)]
        [InlineData(5, true)]
        [InlineData(6, false)]
        [InlineData(-1, false)]
        public void Credits_RangeValidation(int credits, bool valid)
        {
            var c = new Course { Title = "Valid title", Credits = credits };
            var ok = Validator.TryValidateObject(c, new ValidationContext(c), new List<ValidationResult>(), true);
            Assert.Equal(valid, ok);
        }

        [Theory]
        [InlineData("ab", false)]
        [InlineData("abc", true)]
        [InlineData("This title is fine", true)]
        public void Title_LengthValidation(string title, bool valid)
        {
            var c = new Course { Title = title, Credits = 3 };
            var ok = Validator.TryValidateObject(c, new ValidationContext(c), new List<ValidationResult>(), true);
            Assert.Equal(valid, ok);
        }
    }

    public class DepartmentTests
    {
        [Fact]
        public void Properties_RoundTrip()
        {
            var d = new Department
            {
                DepartmentID = 1,
                Name = "English",
                Budget = 123.45m,
                StartDate = new DateTime(2007, 9, 1),
                InstructorID = 4,
                Administrator = new Instructor(),
                Courses = new List<Course>()
            };
            Assert.Equal(1, d.DepartmentID);
            Assert.Equal("English", d.Name);
            Assert.Equal(123.45m, d.Budget);
            Assert.Equal(4, d.InstructorID);
            Assert.NotNull(d.Administrator);
            Assert.Empty(d.Courses);
        }

        [Fact]
        public void InstructorID_DefaultsToNull()
        {
            Assert.Null(new Department().InstructorID);
        }
    }

    public class EnrollmentTests
    {
        [Fact]
        public void Grade_DefaultsToNull()
        {
            Assert.Null(new Enrollment().Grade);
        }

        [Fact]
        public void Properties_RoundTrip()
        {
            var e = new Enrollment
            {
                EnrollmentID = 1,
                CourseID = 2,
                StudentID = 3,
                Grade = Grade.B,
                Course = new Course(),
                Student = new Student()
            };
            Assert.Equal(1, e.EnrollmentID);
            Assert.Equal(2, e.CourseID);
            Assert.Equal(3, e.StudentID);
            Assert.Equal(Grade.B, e.Grade);
            Assert.NotNull(e.Course);
            Assert.NotNull(e.Student);
        }

        [Fact]
        public void GradeEnum_HasExpectedValues()
        {
            Assert.Equal(new[] { "A", "B", "C", "D", "F" }, Enum.GetNames(typeof(Grade)));
            Assert.Equal(0, (int)Grade.A);
            Assert.Equal(4, (int)Grade.F);
        }
    }

    public class OfficeAssignmentTests
    {
        [Fact]
        public void Properties_RoundTrip()
        {
            var o = new OfficeAssignment { InstructorID = 7, Location = "Smith 17", Instructor = new Instructor() };
            Assert.Equal(7, o.InstructorID);
            Assert.Equal("Smith 17", o.Location);
            Assert.NotNull(o.Instructor);
        }

        [Fact]
        public void Location_TooLong_FailsValidation()
        {
            var o = new OfficeAssignment { Location = new string('x', 51) };
            Assert.False(Validator.TryValidateObject(o, new ValidationContext(o), new List<ValidationResult>(), true));
        }
    }

    public class ErrorViewModelTests
    {
        [Theory]
        [InlineData(null, false)]
        [InlineData("", false)]
        [InlineData("abc", true)]
        public void ShowRequestId_DependsOnRequestId(string requestId, bool expected)
        {
            var vm = new ErrorViewModel { RequestId = requestId };
            Assert.Equal(expected, vm.ShowRequestId);
        }

        [Fact]
        public void StatusCode_RoundTrips()
        {
            var vm = new ErrorViewModel { StatusCode = 404 };
            Assert.Equal(404, vm.StatusCode);
            Assert.Null(new ErrorViewModel().StatusCode);
        }
    }
}
