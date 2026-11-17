using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ContosoUniversity.Models;
using ContosoUniversity.Tests.Helpers;
using Xunit;

namespace ContosoUniversity.ViewModels
{
    public class PaginatedListTests
    {
        [Fact]
        public void Constructor_ComputesPagingProperties()
        {
            var list = new PaginatedList<int>(new List<int> { 1, 2, 3 }, 10, 2, 3);
            Assert.Equal(2, list.PageIndex);
            Assert.Equal(10, list.TotalCount);
            Assert.Equal(4, list.TotalPages);
            Assert.Equal(3, list.Count);
            Assert.True(list.HasPreviousPage);
            Assert.True(list.HasNextPage);
        }

        [Fact]
        public void FirstPage_HasNoPreviousPage()
        {
            var list = new PaginatedList<int>(new List<int> { 1 }, 5, 1, 3);
            Assert.False(list.HasPreviousPage);
            Assert.True(list.HasNextPage);
        }

        [Fact]
        public void LastPage_HasNoNextPage()
        {
            var list = new PaginatedList<int>(new List<int> { 1 }, 4, 2, 3);
            Assert.True(list.HasPreviousPage);
            Assert.False(list.HasNextPage);
        }

        [Fact]
        public void EmptySource_HasZeroPages()
        {
            var list = new PaginatedList<int>(new List<int>(), 0, 1, 3);
            Assert.Equal(0, list.TotalPages);
            Assert.Empty(list);
            Assert.False(list.HasNextPage);
        }

        [Fact]
        public async Task CreateAsync_ReturnsRequestedPage()
        {
            using var ctx = TestHelpers.SeededContext();
            var result = await PaginatedList<Student>.CreateAsync(ctx.Students.OrderBy(s => s.ID), 2, 3);
            Assert.Single(result);
            Assert.Equal(4, result[0].ID);
            Assert.Equal(4, result.TotalCount);
            Assert.Equal(2, result.TotalPages);
            Assert.Equal(2, result.PageIndex);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public async Task CreateAsync_PageIndexBelowOne_IsClampedToFirstPage(int pageIndex)
        {
            using var ctx = TestHelpers.SeededContext();
            var result = await PaginatedList<Student>.CreateAsync(ctx.Students.OrderBy(s => s.ID), pageIndex, 3);
            Assert.Equal(1, result.PageIndex);
            Assert.Equal(3, result.Count);
            Assert.Equal(1, result[0].ID);
        }

        [Fact]
        public async Task CreateAsync_PageBeyondEnd_ReturnsEmpty()
        {
            using var ctx = TestHelpers.SeededContext();
            var result = await PaginatedList<Student>.CreateAsync(ctx.Students.OrderBy(s => s.ID), 10, 3);
            Assert.Empty(result);
        }
    }

    public class EnrollmentDataGroupTests
    {
        [Fact]
        public void Properties_RoundTrip()
        {
            var g = new EnrollmentDataGroup { EnrollmentDate = new DateTime(2012, 9, 1), StudentCount = 4 };
            Assert.Equal(new DateTime(2012, 9, 1), g.EnrollmentDate);
            Assert.Equal(4, g.StudentCount);
        }

        [Fact]
        public void Defaults_AreEmpty()
        {
            var g = new EnrollmentDataGroup();
            Assert.Null(g.EnrollmentDate);
            Assert.Equal(0, g.StudentCount);
        }
    }

    public class InstructorIndexDataTests
    {
        [Fact]
        public void Defaults_AreNull()
        {
            var d = new InstructorIndexData();
            Assert.Null(d.Instructors);
            Assert.Null(d.Courses);
            Assert.Null(d.Enrollments);
        }

        [Fact]
        public void Properties_RoundTrip()
        {
            var d = new InstructorIndexData
            {
                Instructors = new List<Instructor> { new Instructor() },
                Courses = new List<Course> { new Course(), new Course() },
                Enrollments = new List<Enrollment>()
            };
            Assert.Single(d.Instructors);
            Assert.Equal(2, d.Courses.Count());
            Assert.Empty(d.Enrollments);
        }
    }
}
