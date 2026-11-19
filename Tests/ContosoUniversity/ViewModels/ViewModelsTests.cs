using ContosoUniversity.Models;
using Xunit;

namespace ContosoUniversity.ViewModels
{
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
        public void StatusCode_RoundTrip()
        {
            var vm = new ErrorViewModel { StatusCode = 404 };
            Assert.Equal(404, vm.StatusCode);
            Assert.Null(new ErrorViewModel().StatusCode);
        }
    }

    public class EnrollmentDataGroupTests
    {
        [Fact]
        public void Properties_RoundTrip()
        {
            var d = new DateTime(2012, 9, 1);
            var g = new EnrollmentDataGroup { EnrollmentDate = d, StudentCount = 3 };
            Assert.Equal(d, g.EnrollmentDate);
            Assert.Equal(3, g.StudentCount);
        }

        [Fact]
        public void Defaults_AreNullAndZero()
        {
            var g = new EnrollmentDataGroup();
            Assert.Null(g.EnrollmentDate);
            Assert.Equal(0, g.StudentCount);
        }
    }

    public class InstructorIndexDataTests
    {
        [Fact]
        public void Properties_RoundTrip()
        {
            var vm = new InstructorIndexData
            {
                Instructors = new List<Instructor> { new Instructor() },
                Courses = new List<Course>(),
                Enrollments = new List<Enrollment>()
            };
            Assert.Single(vm.Instructors);
            Assert.Empty(vm.Courses);
            Assert.Empty(vm.Enrollments);
        }

        [Fact]
        public void Defaults_AreNull()
        {
            var vm = new InstructorIndexData();
            Assert.Null(vm.Instructors);
            Assert.Null(vm.Courses);
            Assert.Null(vm.Enrollments);
        }
    }
}
