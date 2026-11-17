using System;
using System.Linq;
using System.Threading.Tasks;
using ContosoUniversity.DAL;
using ContosoUniversity.Models;
using ContosoUniversity.Tests.Helpers;
using ContosoUniversity.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ContosoUniversity.Controllers
{
    public class StudentsControllerTests
    {
        private static StudentsController Create(SchoolContext ctx, ILogger<StudentsController> logger = null) =>
            new StudentsController(ctx, logger ?? NullLogger<StudentsController>.Instance);

        private static StudentsController CreateWithMvc(SchoolContext ctx)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddMvc();
            var provider = services.BuildServiceProvider();
            var controller = Create(ctx);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { RequestServices = provider },
                RouteData = new Microsoft.AspNetCore.Routing.RouteData()
            };
            return controller;
        }

        [Fact]
        public async Task Index_DefaultSort_ByLastNameAscending()
        {
            using var ctx = TestHelpers.SeededContext();
            var c = Create(ctx);
            var view = Assert.IsType<ViewResult>(await c.Index(null, null, null, null));
            var model = Assert.IsType<PaginatedList<Student>>(view.Model);
            Assert.Equal(new[] { "Alexander", "Alonso", "Anand" }, model.Select(s => s.LastName));
            Assert.Equal("name_desc", c.ViewBag.NameSortParm);
            Assert.Equal("Date", c.ViewBag.DateSortParm);
        }

        [Fact]
        public async Task Index_NameDesc_SortsDescending()
        {
            using var ctx = TestHelpers.SeededContext();
            var c = Create(ctx);
            var view = Assert.IsType<ViewResult>(await c.Index("name_desc", null, null, null));
            var model = Assert.IsType<PaginatedList<Student>>(view.Model);
            Assert.Equal("Barzdukas", model[0].LastName);
            Assert.Equal("", c.ViewBag.NameSortParm);
        }

        [Fact]
        public async Task Index_DateSort_SortsByDateAscending()
        {
            using var ctx = TestHelpers.SeededContext();
            var c = Create(ctx);
            var view = Assert.IsType<ViewResult>(await c.Index("Date", null, null, null));
            var model = Assert.IsType<PaginatedList<Student>>(view.Model);
            Assert.Equal("Alexander", model[0].LastName);
            Assert.Equal("date_desc", c.ViewBag.DateSortParm);
        }

        [Fact]
        public async Task Index_DateDesc_SortsByDateDescending()
        {
            using var ctx = TestHelpers.SeededContext();
            var c = Create(ctx);
            var view = Assert.IsType<ViewResult>(await c.Index("date_desc", null, null, null));
            var model = Assert.IsType<PaginatedList<Student>>(view.Model);
            Assert.Equal(new DateTime(2012, 9, 1), model[0].EnrollmentDate);
        }

        [Fact]
        public async Task Index_SearchString_FiltersAndResetsPage()
        {
            using var ctx = TestHelpers.SeededContext();
            var c = Create(ctx);
            var view = Assert.IsType<ViewResult>(await c.Index(null, null, "Alo", 2));
            var model = Assert.IsType<PaginatedList<Student>>(view.Model);
            Assert.Single(model);
            Assert.Equal(1, model.PageIndex);
            Assert.Equal("Alo", c.ViewBag.CurrentFilter);
        }

        [Fact]
        public async Task Index_NoSearch_UsesCurrentFilter()
        {
            using var ctx = TestHelpers.SeededContext();
            var c = Create(ctx);
            var view = Assert.IsType<ViewResult>(await c.Index(null, "Barz", null, 1));
            var model = Assert.IsType<PaginatedList<Student>>(view.Model);
            Assert.Single(model);
            Assert.Equal("Barz", c.ViewBag.CurrentFilter);
        }

        [Fact]
        public async Task Index_SecondPage_ReturnsRemainingItems()
        {
            using var ctx = TestHelpers.SeededContext();
            var view = Assert.IsType<ViewResult>(await Create(ctx).Index(null, null, null, 2));
            var model = Assert.IsType<PaginatedList<Student>>(view.Model);
            Assert.Single(model);
            Assert.True(model.HasPreviousPage);
        }

        [Fact]
        public async Task Details_NullId_ReturnsBadRequest()
        {
            using var ctx = TestHelpers.SeededContext();
            Assert.IsType<BadRequestResult>(await Create(ctx).Details(null));
        }

        [Fact]
        public async Task Details_Unknown_ReturnsNotFound()
        {
            using var ctx = TestHelpers.SeededContext();
            Assert.IsType<NotFoundResult>(await Create(ctx).Details(999));
        }

        [Fact]
        public async Task Details_Existing_ReturnsStudentWithEnrollments()
        {
            using var ctx = TestHelpers.SeededContext();
            var view = Assert.IsType<ViewResult>(await Create(ctx).Details(1));
            var student = Assert.IsType<Student>(view.Model);
            Assert.Equal(2, student.Enrollments.Count);
            Assert.All(student.Enrollments, e => Assert.NotNull(e.Course));
        }

        [Fact]
        public void Create_Get_ReturnsView()
        {
            using var ctx = TestHelpers.NewContext();
            Assert.IsType<ViewResult>(Create(ctx).Create());
        }

        [Fact]
        public async Task Create_Post_Valid_AddsAndRedirects()
        {
            using var ctx = TestHelpers.NewContext();
            var result = await Create(ctx).Create(new Student { LastName = "New", FirstMidName = "Stu", EnrollmentDate = DateTime.Today });
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Equal(1, await ctx.Students.CountAsync());
        }

        [Fact]
        public async Task Create_Post_InvalidModel_ReturnsViewWithoutSaving()
        {
            using var ctx = TestHelpers.NewContext();
            var c = Create(ctx);
            c.ModelState.AddModelError("LastName", "required");
            var student = new Student();
            var view = Assert.IsType<ViewResult>(await c.Create(student));
            Assert.Same(student, view.Model);
            Assert.Equal(0, await ctx.Students.CountAsync());
        }

        [Fact]
        public async Task Create_Post_DbUpdateException_LogsAndReturnsView()
        {
            using var ctx = TestHelpers.NewThrowingContext();
            ctx.ThrowOnSave = true;
            var logger = new ListLogger<StudentsController>();
            var c = Create(ctx, logger);
            var view = Assert.IsType<ViewResult>(await c.Create(new Student { LastName = "A", FirstMidName = "B" }));
            Assert.False(c.ModelState.IsValid);
            Assert.Contains(logger.Entries, e => e.Level == LogLevel.Error);
        }

        [Fact]
        public async Task Edit_Get_NullId_ReturnsBadRequest()
        {
            using var ctx = TestHelpers.SeededContext();
            Assert.IsType<BadRequestResult>(await Create(ctx).Edit(null));
        }

        [Fact]
        public async Task Edit_Get_Unknown_ReturnsNotFound()
        {
            using var ctx = TestHelpers.SeededContext();
            Assert.IsType<NotFoundResult>(await Create(ctx).Edit(999));
        }

        [Fact]
        public async Task Edit_Get_Existing_ReturnsView()
        {
            using var ctx = TestHelpers.SeededContext();
            var view = Assert.IsType<ViewResult>(await Create(ctx).Edit(2));
            Assert.Equal("Alonso", Assert.IsType<Student>(view.Model).LastName);
        }

        [Fact]
        public async Task EditPost_NullId_ReturnsBadRequest()
        {
            using var ctx = TestHelpers.SeededContext();
            Assert.IsType<BadRequestResult>(await Create(ctx).EditPost(null));
        }

        [Fact]
        public async Task EditPost_Unknown_ReturnsNotFound()
        {
            using var ctx = TestHelpers.SeededContext();
            Assert.IsType<NotFoundResult>(await Create(ctx).EditPost(999));
        }

        [Fact]
        public async Task EditPost_Existing_UpdatesAndRedirects()
        {
            using var ctx = TestHelpers.SeededContext();
            var c = CreateWithMvc(ctx);
            var result = await c.EditPost(1);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
        }

        [Fact]
        public async Task EditPost_SaveFails_ReturnsViewWithError()
        {
            using var ctx = TestHelpers.SeededThrowingContext();
            ctx.ThrowOnSave = true;
            var c = CreateWithMvc(ctx);
            var view = Assert.IsType<ViewResult>(await c.EditPost(1));
            Assert.IsType<Student>(view.Model);
            Assert.False(c.ModelState.IsValid);
        }

        [Fact]
        public async Task Delete_Get_NullId_ReturnsBadRequest()
        {
            using var ctx = TestHelpers.SeededContext();
            Assert.IsType<BadRequestResult>(await Create(ctx).Delete(null));
        }

        [Fact]
        public async Task Delete_Get_Unknown_ReturnsNotFound()
        {
            using var ctx = TestHelpers.SeededContext();
            Assert.IsType<NotFoundResult>(await Create(ctx).Delete((int?)999));
        }

        [Fact]
        public async Task Delete_Get_Existing_ReturnsView()
        {
            using var ctx = TestHelpers.SeededContext();
            var c = Create(ctx);
            var view = Assert.IsType<ViewResult>(await c.Delete((int?)1));
            Assert.IsType<Student>(view.Model);
            Assert.Null(c.ViewBag.ErrorMessage);
        }

        [Fact]
        public async Task Delete_Get_WithSaveChangesError_SetsErrorMessage()
        {
            using var ctx = TestHelpers.SeededContext();
            var c = Create(ctx);
            Assert.IsType<ViewResult>(await c.Delete(1, true));
            Assert.Contains("Delete failed", (string)c.ViewBag.ErrorMessage);
        }

        [Fact]
        public async Task Delete_Post_Existing_RemovesAndRedirects()
        {
            using var ctx = TestHelpers.SeededContext();
            var redirect = Assert.IsType<RedirectToActionResult>(await Create(ctx).Delete(4));
            Assert.Equal("Index", redirect.ActionName);
            Assert.Null(await ctx.Students.FindAsync(4));
        }

        [Fact]
        public async Task Delete_Post_Unknown_StillRedirectsToIndex()
        {
            using var ctx = TestHelpers.SeededContext();
            var redirect = Assert.IsType<RedirectToActionResult>(await Create(ctx).Delete(999));
            Assert.Equal("Index", redirect.ActionName);
        }

        [Fact]
        public async Task Delete_Post_Failure_RedirectsToDeleteWithError()
        {
            using var ctx = TestHelpers.SeededThrowingContext();
            ctx.ThrowOnSave = true;
            var logger = new ListLogger<StudentsController>();
            var redirect = Assert.IsType<RedirectToActionResult>(await Create(ctx, logger).Delete(1));
            Assert.Equal("Delete", redirect.ActionName);
            Assert.Equal(true, redirect.RouteValues["saveChangesError"]);
            Assert.Contains(logger.Entries, e => e.Level == LogLevel.Error);
        }
    }

    public class HomeControllerTests
    {
        private static HomeController Create(SchoolContext ctx, ILogger<HomeController> logger = null)
        {
            var c = new HomeController(ctx, logger ?? NullLogger<HomeController>.Instance);
            c.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { TraceIdentifier = "trace-1" } };
            return c;
        }

        [Fact]
        public void Index_ReturnsView()
        {
            using var ctx = TestHelpers.NewContext();
            Assert.IsType<ViewResult>(Create(ctx).Index());
        }

        [Fact]
        public async Task About_GroupsStudentsByEnrollmentDate()
        {
            using var ctx = TestHelpers.SeededContext();
            var view = Assert.IsType<ViewResult>(await Create(ctx).About());
            var data = Assert.IsAssignableFrom<System.Collections.Generic.List<EnrollmentDataGroup>>(view.Model);
            Assert.Equal(3, data.Count);
            Assert.Equal(4, data.Sum(g => g.StudentCount));
            Assert.Equal(2, data.Single(g => g.EnrollmentDate == new DateTime(2012, 9, 1)).StudentCount);
        }

        [Fact]
        public async Task About_EmptyDatabase_ReturnsEmptyList()
        {
            using var ctx = TestHelpers.NewContext();
            var view = Assert.IsType<ViewResult>(await Create(ctx).About());
            Assert.Empty(Assert.IsAssignableFrom<System.Collections.Generic.List<EnrollmentDataGroup>>(view.Model));
        }

        [Fact]
        public void Contact_SetsMessage()
        {
            using var ctx = TestHelpers.NewContext();
            var c = Create(ctx);
            Assert.IsType<ViewResult>(c.Contact());
            Assert.Equal("Your contact page.", c.ViewBag.Message);
        }

        [Fact]
        public void Error_ReturnsViewModelWithRequestIdAndStatusCode()
        {
            using var ctx = TestHelpers.NewContext();
            var logger = new ListLogger<HomeController>();
            var view = Assert.IsType<ViewResult>(Create(ctx, logger).Error(404));
            var vm = Assert.IsType<ErrorViewModel>(view.Model);
            Assert.False(string.IsNullOrEmpty(vm.RequestId));
            Assert.Equal(404, vm.StatusCode);
            Assert.True(vm.ShowRequestId);
            Assert.Contains(logger.Entries, e => e.Level == LogLevel.Warning);
        }

        [Fact]
        public void Error_WithoutStatusCode_HasNullStatusCode()
        {
            using var ctx = TestHelpers.NewContext();
            var view = Assert.IsType<ViewResult>(Create(ctx).Error());
            Assert.Null(Assert.IsType<ErrorViewModel>(view.Model).StatusCode);
        }
    }

    public class CourseControllerTests
    {
        private static CourseController Create(SchoolContext ctx, ILogger<CourseController> logger = null) =>
            new CourseController(ctx, logger ?? NullLogger<CourseController>.Instance);

        [Fact]
        public async Task Index_ReturnsAllCoursesWithDepartments()
        {
            using var ctx = TestHelpers.SeededContext();
            var view = Assert.IsType<ViewResult>(await Create(ctx).Index());
            var model = Assert.IsAssignableFrom<System.Collections.Generic.List<Course>>(view.Model);
            Assert.Equal(2, model.Count);
            Assert.All(model, c => Assert.NotNull(c.Department));
        }

        [Fact]
        public async Task Details_NullId_ReturnsBadRequest()
        {
            using var ctx = TestHelpers.SeededContext();
            Assert.IsType<BadRequestResult>(await Create(ctx).Details(null));
        }

        [Fact]
        public async Task Details_Unknown_ReturnsNotFound()
        {
            using var ctx = TestHelpers.SeededContext();
            Assert.IsType<NotFoundResult>(await Create(ctx).Details(1));
        }

        [Fact]
        public async Task Details_Existing_ReturnsCourse()
        {
            using var ctx = TestHelpers.SeededContext();
            var view = Assert.IsType<ViewResult>(await Create(ctx).Details(2021));
            var course = Assert.IsType<Course>(view.Model);
            Assert.Equal("Composition", course.Title);
            Assert.Equal("English", course.Department.Name);
        }

        [Fact]
        public async Task Create_Get_PopulatesDepartments()
        {
            using var ctx = TestHelpers.SeededContext();
            var c = Create(ctx);
            Assert.IsType<ViewResult>(await c.Create());
            var list = Assert.IsType<SelectList>(c.ViewBag.DepartmentID);
            Assert.Equal(new[] { "English", "Mathematics" }, ((System.Collections.Generic.IEnumerable<SelectListItem>)list).Select(i => i.Text));
        }

        [Fact]
        public async Task Create_Post_Valid_RedirectsAndSaves()
        {
            using var ctx = TestHelpers.SeededContext();
            var result = await Create(ctx).Create(new Course { CourseID = 9999, Title = "New Course", Credits = 2, DepartmentID = 1 });
            Assert.IsType<RedirectToActionResult>(result);
            Assert.NotNull(await ctx.Courses.FindAsync(9999));
        }

        [Fact]
        public async Task Create_Post_InvalidModel_ReturnsViewAndPopulatesDepartments()
        {
            using var ctx = TestHelpers.SeededContext();
            var c = Create(ctx);
            c.ModelState.AddModelError("Title", "bad");
            var course = new Course { DepartmentID = 2 };
            var view = Assert.IsType<ViewResult>(await c.Create(course));
            Assert.Same(course, view.Model);
            var list = Assert.IsType<SelectList>(c.ViewBag.DepartmentID);
            Assert.Equal(2, list.SelectedValue);
        }

        [Fact]
        public async Task Create_Post_SaveFails_ReturnsViewWithError()
        {
            using var ctx = TestHelpers.SeededThrowingContext();
            ctx.ThrowOnSave = true;
            var logger = new ListLogger<CourseController>();
            var c = Create(ctx, logger);
            var view = Assert.IsType<ViewResult>(await c.Create(new Course { CourseID = 8888, Title = "Err", Credits = 1, DepartmentID = 1 }));
            Assert.False(c.ModelState.IsValid);
            Assert.Contains(logger.Entries, e => e.Level == LogLevel.Error);
        }

        [Fact]
        public async Task Edit_Get_NullId_ReturnsBadRequest()
        {
            using var ctx = TestHelpers.SeededContext();
            Assert.IsType<BadRequestResult>(await Create(ctx).Edit((int?)null));
        }

        [Fact]
        public async Task Edit_Get_Unknown_ReturnsNotFound()
        {
            using var ctx = TestHelpers.SeededContext();
            Assert.IsType<NotFoundResult>(await Create(ctx).Edit((int?)5));
        }

        [Fact]
        public async Task Edit_Get_Existing_ReturnsViewWithDepartments()
        {
            using var ctx = TestHelpers.SeededContext();
            var c = Create(ctx);
            var view = Assert.IsType<ViewResult>(await c.Edit((int?)1045));
            Assert.IsType<Course>(view.Model);
            Assert.IsType<SelectList>(c.ViewBag.DepartmentID);
        }

        [Fact]
        public async Task Edit_Post_Valid_UpdatesAndRedirects()
        {
            using var ctx = TestHelpers.SeededContext();
            var result = await Create(ctx).Edit(new Course { CourseID = 1045, Title = "Calculus II", Credits = 5, DepartmentID = 1 });
            Assert.IsType<RedirectToActionResult>(result);
            var updated = await ctx.Courses.FindAsync(1045);
            Assert.Equal("Calculus II", updated.Title);
            Assert.Equal(5, updated.Credits);
            Assert.Equal(1, updated.DepartmentID);
        }

        [Fact]
        public async Task Edit_Post_Unknown_ReturnsNotFound()
        {
            using var ctx = TestHelpers.SeededContext();
            Assert.IsType<NotFoundResult>(await Create(ctx).Edit(new Course { CourseID = 12345 }));
        }

        [Fact]
        public async Task Edit_Post_InvalidModel_ReturnsView()
        {
            using var ctx = TestHelpers.SeededContext();
            var c = Create(ctx);
            c.ModelState.AddModelError("Title", "bad");
            Assert.IsType<ViewResult>(await c.Edit(new Course { CourseID = 1045, DepartmentID = 1 }));
            Assert.IsType<SelectList>(c.ViewBag.DepartmentID);
        }

        [Fact]
        public async Task Edit_Post_SaveFails_ReturnsViewWithError()
        {
            using var ctx = TestHelpers.SeededThrowingContext();
            ctx.ThrowOnSave = true;
            var c = Create(ctx);
            Assert.IsType<ViewResult>(await c.Edit(new Course { CourseID = 1045, Title = "X", Credits = 1, DepartmentID = 1 }));
            Assert.False(c.ModelState.IsValid);
        }

        [Fact]
        public async Task Delete_Get_NullId_ReturnsBadRequest()
        {
            using var ctx = TestHelpers.SeededContext();
            Assert.IsType<BadRequestResult>(await Create(ctx).Delete(null));
        }

        [Fact]
        public async Task Delete_Get_Unknown_ReturnsNotFound()
        {
            using var ctx = TestHelpers.SeededContext();
            Assert.IsType<NotFoundResult>(await Create(ctx).Delete(77));
        }

        [Fact]
        public async Task Delete_Get_Existing_ReturnsView()
        {
            using var ctx = TestHelpers.SeededContext();
            var view = Assert.IsType<ViewResult>(await Create(ctx).Delete(2021));
            Assert.Equal(2021, Assert.IsType<Course>(view.Model).CourseID);
        }

        [Fact]
        public async Task DeleteConfirmed_Existing_RemovesCourse()
        {
            using var ctx = TestHelpers.SeededContext();
            var result = await Create(ctx).DeleteConfirmed(1045);
            Assert.IsType<RedirectToActionResult>(result);
            Assert.Null(await ctx.Courses.FindAsync(1045));
        }

        [Fact]
        public async Task DeleteConfirmed_Unknown_StillRedirects()
        {
            using var ctx = TestHelpers.SeededContext();
            Assert.IsType<RedirectToActionResult>(await Create(ctx).DeleteConfirmed(4242));
            Assert.Equal(2, await ctx.Courses.CountAsync());
        }
    }

    public class InstructorControllerTests
    {
        private static InstructorController Create(SchoolContext ctx, ILogger<InstructorController> logger = null) =>
            new InstructorController(ctx, logger ?? NullLogger<InstructorController>.Instance);

        [Fact]
        public async Task Index_NoSelection_ReturnsInstructorsOrderedByLastName()
        {
            using var ctx = TestHelpers.SeededContext();
            var view = Assert.IsType<ViewResult>(await Create(ctx).Index(null, null));
            var vm = Assert.IsType<InstructorIndexData>(view.Model);
            Assert.Equal(new[] { "Abercrombie", "Fakhouri" }, vm.Instructors.Select(i => i.LastName));
            Assert.Null(vm.Courses);
            Assert.Null(vm.Enrollments);
        }

        [Fact]
        public async Task Index_UnknownInstructor_ReturnsNotFound()
        {
            using var ctx = TestHelpers.SeededContext();
            Assert.IsType<NotFoundResult>(await Create(ctx).Index(99, null));
        }

        [Fact]
        public async Task Index_SelectedInstructor_ReturnsCourses()
        {
            using var ctx = TestHelpers.SeededContext();
            var c = Create(ctx);
            var view = Assert.IsType<ViewResult>(await c.Index(1, null));
            var vm = Assert.IsType<InstructorIndexData>(view.Model);
            Assert.Single(vm.Courses);
            Assert.Equal(1, c.ViewBag.InstructorID);
        }

        [Fact]
        public async Task Index_SelectedCourse_LoadsEnrollmentsWithStudents()
        {
            using var ctx = TestHelpers.SeededContext();
            var c = Create(ctx);
            var view = Assert.IsType<ViewResult>(await c.Index(1, 2021));
            var vm = Assert.IsType<InstructorIndexData>(view.Model);
            Assert.Equal(2, vm.Enrollments.Count());
            Assert.All(vm.Enrollments, e => Assert.NotNull(e.Student));
            Assert.Equal(2021, c.ViewBag.CourseID);
        }

        [Fact]
        public async Task Index_CourseNotTaughtByInstructor_ReturnsNotFound()
        {
            using var ctx = TestHelpers.SeededContext();
            Assert.IsType<NotFoundResult>(await Create(ctx).Index(1, 1045));
        }

        [Fact]
        public async Task Details_NullId_ReturnsBadRequest()
        {
            using var ctx = TestHelpers.SeededContext();
            Assert.IsType<BadRequestResult>(await Create(ctx).Details(null));
        }

        [Fact]
        public async Task Details_Unknown_ReturnsNotFound()
        {
            using var ctx = TestHelpers.SeededContext();
            Assert.IsType<NotFoundResult>(await Create(ctx).Details(99));
        }

        [Fact]
        public async Task Details_Existing_ReturnsInstructorWithOffice()
        {
            using var ctx = TestHelpers.SeededContext();
            var view = Assert.IsType<ViewResult>(await Create(ctx).Details(2));
            var i = Assert.IsType<Instructor>(view.Model);
            Assert.Equal("Smith 17", i.OfficeAssignment.Location);
        }

        [Fact]
        public void Create_Get_ReturnsView()
        {
            using var ctx = TestHelpers.NewContext();
            Assert.IsType<ViewResult>(Create(ctx).Create());
        }

        [Fact]
        public async Task Create_Post_Valid_AddsAndRedirects()
        {
            using var ctx = TestHelpers.NewContext();
            var result = await Create(ctx).Create(new Instructor { LastName = "L", FirstMidName = "F", HireDate = DateTime.Today });
            Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(1, await ctx.Instructors.CountAsync());
        }

        [Fact]
        public async Task Create_Post_Invalid_ReturnsView()
        {
            using var ctx = TestHelpers.NewContext();
            var c = Create(ctx);
            c.ModelState.AddModelError("LastName", "bad");
            Assert.IsType<ViewResult>(await c.Create(new Instructor()));
            Assert.Equal(0, await ctx.Instructors.CountAsync());
        }

        [Fact]
        public async Task Edit_Get_NullId_ReturnsBadRequest()
        {
            using var ctx = TestHelpers.SeededContext();
            Assert.IsType<BadRequestResult>(await Create(ctx).Edit((int?)null));
        }

        [Fact]
        public async Task Edit_Get_Unknown_ReturnsNotFound()
        {
            using var ctx = TestHelpers.SeededContext();
            Assert.IsType<NotFoundResult>(await Create(ctx).Edit((int?)99));
        }

        [Fact]
        public async Task Edit_Get_Existing_ReturnsView()
        {
            using var ctx = TestHelpers.SeededContext();
            var view = Assert.IsType<ViewResult>(await Create(ctx).Edit((int?)1));
            Assert.IsType<Instructor>(view.Model);
        }

        [Fact]
        public async Task Edit_Post_Valid_UpdatesAndRedirects()
        {
            using var ctx = TestHelpers.SeededContext();
            var result = await Create(ctx).Edit(new Instructor { ID = 1, LastName = "Changed", FirstMidName = "Name", HireDate = new DateTime(2020, 1, 1) });
            Assert.IsType<RedirectToActionResult>(result);
            var i = await ctx.Instructors.FindAsync(1);
            Assert.Equal("Changed", i.LastName);
            Assert.Equal(2020, i.HireDate.Year);
        }

        [Fact]
        public async Task Edit_Post_Unknown_ReturnsNotFound()
        {
            using var ctx = TestHelpers.SeededContext();
            Assert.IsType<NotFoundResult>(await Create(ctx).Edit(new Instructor { ID = 99 }));
        }

        [Fact]
        public async Task Edit_Post_InvalidModel_ReturnsView()
        {
            using var ctx = TestHelpers.SeededContext();
            var c = Create(ctx);
            c.ModelState.AddModelError("LastName", "bad");
            Assert.IsType<ViewResult>(await c.Edit(new Instructor { ID = 1 }));
        }

        [Fact]
        public async Task Edit_Post_SaveFails_ReturnsViewWithError()
        {
            using var ctx = TestHelpers.SeededThrowingContext();
            ctx.ThrowOnSave = true;
            var logger = new ListLogger<InstructorController>();
            var c = Create(ctx, logger);
            Assert.IsType<ViewResult>(await c.Edit(new Instructor { ID = 1, LastName = "A", FirstMidName = "B" }));
            Assert.False(c.ModelState.IsValid);
            Assert.Contains(logger.Entries, e => e.Level == LogLevel.Error);
        }

        [Fact]
        public async Task Delete_Get_NullId_ReturnsBadRequest()
        {
            using var ctx = TestHelpers.SeededContext();
            Assert.IsType<BadRequestResult>(await Create(ctx).Delete(null));
        }

        [Fact]
        public async Task Delete_Get_Unknown_ReturnsNotFound()
        {
            using var ctx = TestHelpers.SeededContext();
            Assert.IsType<NotFoundResult>(await Create(ctx).Delete(99));
        }

        [Fact]
        public async Task Delete_Get_Existing_ReturnsView()
        {
            using var ctx = TestHelpers.SeededContext();
            var view = Assert.IsType<ViewResult>(await Create(ctx).Delete(2));
            Assert.IsType<Instructor>(view.Model);
        }

        [Fact]
        public async Task DeleteConfirmed_Unknown_RedirectsWithoutChanges()
        {
            using var ctx = TestHelpers.SeededContext();
            Assert.IsType<RedirectToActionResult>(await Create(ctx).DeleteConfirmed(99));
            Assert.Equal(2, await ctx.Instructors.CountAsync());
        }

        [Fact]
        public async Task DeleteConfirmed_Existing_RemovesInstructorAndClearsDepartmentAdministrator()
        {
            using var ctx = TestHelpers.SeededContext();
            Assert.IsType<RedirectToActionResult>(await Create(ctx).DeleteConfirmed(2));
            Assert.Null(await ctx.Instructors.FindAsync(2));
            var math = await ctx.Departments.FindAsync(2);
            Assert.Null(math.InstructorID);
            Assert.Empty(await ctx.OfficeAssignments.ToListAsync());
        }
    }
}
