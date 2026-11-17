using ContosoUniversity.Controllers;
using ContosoUniversity.DAL;
using ContosoUniversity.Models;
using ContosoUniversity.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ContosoUniversity.Tests
{
    public class StudentsControllerTests
    {
        private static SchoolContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<SchoolContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var context = new SchoolContext(options);
            context.Students.AddRange(
                new Student { LastName = "Alexander", FirstMidName = "Carson", EnrollmentDate = new DateTime(2010, 9, 1) },
                new Student { LastName = "Alonso", FirstMidName = "Meredith", EnrollmentDate = new DateTime(2012, 9, 1) },
                new Student { LastName = "Anand", FirstMidName = "Arturo", EnrollmentDate = new DateTime(2013, 9, 1) },
                new Student { LastName = "Barzdukas", FirstMidName = "Gytis", EnrollmentDate = new DateTime(2011, 9, 1) });
            context.SaveChanges();
            return context;
        }

        private static StudentsController CreateController(SchoolContext context) =>
            new StudentsController(context, NullLogger<StudentsController>.Instance);

        [Fact]
        public async Task Index_ReturnsFirstPageSortedByLastName()
        {
            using var context = CreateContext();
            var result = await CreateController(context).Index(null, null, null, null);

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<PaginatedList<Student>>(view.Model);
            Assert.Equal(3, model.Count);
            Assert.Equal("Alexander", model[0].LastName);
            Assert.Equal(2, model.TotalPages);
            Assert.True(model.HasNextPage);
            Assert.False(model.HasPreviousPage);
        }

        [Fact]
        public async Task Index_SortsByDateDescendingAndPreservesCurrentSort()
        {
            using var context = CreateContext();
            var controller = CreateController(context);
            var result = await controller.Index("date_desc", null, null, 1);

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<PaginatedList<Student>>(view.Model);
            Assert.Equal("Anand", model[0].LastName);
            Assert.Equal("date_desc", controller.ViewBag.CurrentSort);
        }

        [Fact]
        public async Task Index_FiltersBySearchString()
        {
            using var context = CreateContext();
            var result = await CreateController(context).Index(null, null, "Meredith", null);

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<PaginatedList<Student>>(view.Model);
            Assert.Single(model);
            Assert.Equal("Alonso", model[0].LastName);
        }

        [Fact]
        public async Task Details_WithoutId_ReturnsBadRequest()
        {
            using var context = CreateContext();
            Assert.IsType<BadRequestResult>(await CreateController(context).Details(null));
        }

        [Fact]
        public async Task Details_UnknownId_ReturnsNotFound()
        {
            using var context = CreateContext();
            Assert.IsType<NotFoundResult>(await CreateController(context).Details(9999));
        }
    }
}
