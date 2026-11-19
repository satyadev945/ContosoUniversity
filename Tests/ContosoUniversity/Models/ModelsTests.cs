using System.ComponentModel.DataAnnotations;
using ContosoUniversity.Models;
using Xunit;

namespace ContosoUniversity.Models
{
    public class StudentTests
    {
        [Fact]
        public void FullName_ReturnsLastNameCommaFirstName()
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
            Assert.NotNull(s.Enrollments);
        }

        [Fact]
        public void Validation_MissingRequiredFields_Fails()
        {
            var s = new Student();
            var results = new List<ValidationResult>();
            var ok = Validator.TryValidateObject(s, new ValidationContext(s), results, true);
            Assert.False(ok);
            Assert.Equal(2, results.Count);
        }

        [Fact]
        public void Validation_TooLongName_Fails()
        {
            var s = new Student { LastName = new string('a', 51), FirstMidName = "F" };
            Assert.False(Validator.TryValidateObject(s, new ValidationContext(s), new List<ValidationResult>(), true));
        }

        [Fact]
        public void Validation_ValidStudent_Passes()
        {
            var s = new Student { LastName = "L", FirstMidName = "F", EnrollmentDate = DateTime.Today };
            Assert.True(Validator.TryValidateObject(s, new ValidationContext(s), new List<ValidationResult>(), true));
        }
    }

    public class InstructorTests
    {
        [Fact]
        public void FullName_ReturnsLastNameCommaFirstName()
        {
            var i = new Instructor { LastName = "Kapoor", FirstMidName = "Candace" };
            Assert.Equal("Kapoor, Candace", i.FullName);
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
        public void Validation_MissingRequired_Fails()
        {
            var i = new Instructor();
            Assert.False(Validator.TryValidateObject(i, new ValidationContext(i), new List<ValidationResult>(), true));
        }

        [Fact]
        public void Validation_Valid_Passes()
        {
            var i = new Instructor { LastName = "L", FirstMidName = "F" };
            Assert.True(Validator.TryValidateObject(i, new ValidationContext(i), new List<ValidationResult>(), true));
        }
    }

    public class CourseTests
    {
        [Theory]
        [InlineData(0, true)]
        [InlineData(5, true)]
        [InlineData(6, false)]
        [InlineData(-1, false)]
        public void Credits_RangeValidation(int credits, bool expected)
        {
            var c = new Course { CourseID = 1, Title = "Valid", Credits = credits };
            Assert.Equal(expected, Validator.TryValidateObject(c, new ValidationContext(c), new List<ValidationResult>(), true));
        }

        [Theory]
        [InlineData("ab", false)]
        [InlineData("abc", true)]
        public void Title_LengthValidation(string title, bool expected)
        {
            var c = new Course { Title = title, Credits = 3 };
            Assert.Equal(expected, Validator.TryValidateObject(c, new ValidationContext(c), new List<ValidationResult>(), true));
        }

        [Fact]
        public void Properties_RoundTrip()
        {
            var c = new Course
            {
                CourseID = 10, DepartmentID = 2, Department = new Department(),
                Enrollments = new List<Enrollment>(), Instructors = new List<Instructor>()
            };
            Assert.Equal(10, c.CourseID);
            Assert.Equal(2, c.DepartmentID);
            Assert.NotNull(c.Department);
            Assert.Empty(c.Enrollments);
            Assert.Empty(c.Instructors);
        }
    }

    public class DepartmentTests
    {
        [Fact]
        public void Properties_RoundTrip()
        {
            var d = new Department
            {
                DepartmentID = 1, Name = "English", Budget = 123.45m, StartDate = new DateTime(2007, 9, 1),
                InstructorID = 4, Administrator = new Instructor(), Courses = new List<Course>()
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

        [Fact]
        public void Name_TooShort_FailsValidation()
        {
            var d = new Department { Name = "ab" };
            Assert.False(Validator.TryValidateObject(d, new ValidationContext(d), new List<ValidationResult>(), true));
        }
    }

    public class EnrollmentTests
    {
        [Fact]
        public void Grade_DefaultsToNull()
        {
            Assert.Null(new Enrollment().Grade);
        }

        [Theory]
        [InlineData(Grade.A)]
        [InlineData(Grade.B)]
        [InlineData(Grade.C)]
        [InlineData(Grade.D)]
        [InlineData(Grade.F)]
        public void Grade_CanBeAssigned(Grade grade)
        {
            var e = new Enrollment { Grade = grade };
            Assert.Equal(grade, e.Grade);
        }

        [Fact]
        public void Properties_RoundTrip()
        {
            var e = new Enrollment { EnrollmentID = 1, CourseID = 2, StudentID = 3, Course = new Course(), Student = new Student() };
            Assert.Equal(1, e.EnrollmentID);
            Assert.Equal(2, e.CourseID);
            Assert.Equal(3, e.StudentID);
            Assert.NotNull(e.Course);
            Assert.NotNull(e.Student);
        }

        [Fact]
        public void GradeEnum_HasFiveValues()
        {
            Assert.Equal(5, Enum.GetValues(typeof(Grade)).Length);
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
}
