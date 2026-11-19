using ContosoUniversity.Data;
using ContosoUniversity.Helpers;
using ContosoUniversity.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ContosoUniversity.Controllers
{
    public class CourseControllerTests
    {
        private static async Task<(CourseController, SchoolContext)> CreateAsync()
        {
            var ctx = await TestHelpers.CreateSeededContextAsync();
            var c = new CourseController(ctx, new ListLogger<CourseController>());
            TestHelpers.SetupController(c);
            return (c, ctx);
        }

        [Fact]
        public async Task Index_ReturnsAllCourses()
        {
            var (c, ctx) = await CreateAsync();
            using (ctx)
            {
                var r = Assert.IsType<ViewResult>(await c.Index());
                Assert.Equal(7, Assert.IsAssignableFrom<IEnumerable<Course>>(r.Model).Count());
            }
        }

        [Fact]
        public async Task Details_NullId_ReturnsBadRequest()
        {
            var (c, ctx) = await CreateAsync();
            using (ctx) Assert.IsType<BadRequestResult>(await c.Details(null));
        }

        [Fact]
        public async Task Details_Missing_ReturnsNotFound()
        {
            var (c, ctx) = await CreateAsync();
            using (ctx) Assert.IsType<NotFoundResult>(await c.Details(99999));
        }

        [Fact]
        public async Task Details_Existing_ReturnsCourse()
        {
            var (c, ctx) = await CreateAsync();
            using (ctx)
            {
                var r = Assert.IsType<ViewResult>(await c.Details(1050));
                Assert.Equal("Chemistry", Assert.IsType<Course>(r.Model).Title);
            }
        }

        [Fact]
        public async Task CreateGet_PopulatesDepartments()
        {
            var (c, ctx) = await CreateAsync();
            using (ctx)
            {
                Assert.IsType<ViewResult>(await c.Create());
                Assert.IsType<SelectList>(c.ViewBag.DepartmentID);
            }
        }

        [Fact]
        public async Task CreatePost_Valid_RedirectsAndSaves()
        {
            var (c, ctx) = await CreateAsync();
            using (ctx)
            {
                var r = await c.Create(new Course { CourseID = 9999, Title = "New Course", Credits = 3, DepartmentID = 1 });
                var red = Assert.IsType<RedirectToActionResult>(r);
                Assert.Equal("Index", red.ActionName);
                Assert.NotNull(await ctx.Courses.FindAsync(9999));
            }
        }

        [Fact]
        public async Task CreatePost_InvalidModel_ReturnsViewWithDepartments()
        {
            var (c, ctx) = await CreateAsync();
            using (ctx)
            {
                c.ModelState.AddModelError("Title", "bad");
                var course = new Course { CourseID = 8888, DepartmentID = 1 };
                var r = Assert.IsType<ViewResult>(await c.Create(course));
                Assert.Same(course, r.Model);
                Assert.NotNull(c.ViewBag.DepartmentID);
                Assert.Null(await ctx.Courses.FindAsync(8888));
            }
        }

        [Fact]
        public async Task CreatePost_DuplicateKey_DoesNotRedirectOrAddSecondCourse()
        {
            var (c, ctx) = await CreateAsync();
            using (ctx)
            {
                IActionResult result = null;
                var ex = await Record.ExceptionAsync(async () =>
                    result = await c.Create(new Course { CourseID = 1050, Title = "Dup", Credits = 1, DepartmentID = 1 }));
                Assert.True(ex != null || result is ViewResult);
                Assert.Equal(1, await ctx.Courses.AsNoTracking().CountAsync(x => x.CourseID == 1050));
            }
        }

        [Fact]
        public async Task EditGet_NullId_ReturnsBadRequest()
        {
            var (c, ctx) = await CreateAsync();
            using (ctx) Assert.IsType<BadRequestResult>(await c.Edit((int?)null));
        }

        [Fact]
        public async Task EditGet_Missing_ReturnsNotFound()
        {
            var (c, ctx) = await CreateAsync();
            using (ctx) Assert.IsType<NotFoundResult>(await c.Edit((int?)12345));
        }

        [Fact]
        public async Task EditGet_Existing_ReturnsView()
        {
            var (c, ctx) = await CreateAsync();
            using (ctx)
            {
                var r = Assert.IsType<ViewResult>(await c.Edit((int?)1050));
                Assert.IsType<Course>(r.Model);
                Assert.NotNull(c.ViewBag.DepartmentID);
            }
        }

        [Fact]
        public async Task EditPost_Valid_UpdatesAndRedirects()
        {
            var (c, ctx) = await CreateAsync();
            using (ctx)
            {
                var existing = await ctx.Courses.AsNoTracking().FirstAsync(x => x.CourseID == 1050);
                var edited = new Course { CourseID = 1050, Title = "Chemistry 2", Credits = 4, DepartmentID = existing.DepartmentID };
                var r = await c.Edit(edited);
                Assert.IsType<RedirectToActionResult>(r);
                Assert.Equal("Chemistry 2", (await ctx.Courses.AsNoTracking().FirstAsync(x => x.CourseID == 1050)).Title);
            }
        }

        [Fact]
        public async Task EditPost_InvalidModel_ReturnsView()
        {
            var (c, ctx) = await CreateAsync();
            using (ctx)
            {
                c.ModelState.AddModelError("Title", "bad");
                var r = Assert.IsType<ViewResult>(await c.Edit(new Course { CourseID = 1050, DepartmentID = 1 }));
                Assert.NotNull(r.Model);
            }
        }

        [Fact]
        public async Task DeleteGet_NullId_ReturnsBadRequest()
        {
            var (c, ctx) = await CreateAsync();
            using (ctx) Assert.IsType<BadRequestResult>(await c.Delete(null));
        }

        [Fact]
        public async Task DeleteGet_Missing_ReturnsNotFound()
        {
            var (c, ctx) = await CreateAsync();
            using (ctx) Assert.IsType<NotFoundResult>(await c.Delete(424242));
        }

        [Fact]
        public async Task DeleteGet_Existing_ReturnsView()
        {
            var (c, ctx) = await CreateAsync();
            using (ctx) Assert.IsType<Course>(Assert.IsType<ViewResult>(await c.Delete(1050)).Model);
        }

        [Fact]
        public async Task DeleteConfirmed_Existing_RemovesCourse()
        {
            var (c, ctx) = await CreateAsync();
            using (ctx)
            {
                var r = await c.DeleteConfirmed(2042);
                Assert.IsType<RedirectToActionResult>(r);
                Assert.Null(await ctx.Courses.FindAsync(2042));
            }
        }

        [Fact]
        public async Task DeleteConfirmed_Missing_StillRedirects()
        {
            var (c, ctx) = await CreateAsync();
            using (ctx)
            {
                var r = Assert.IsType<RedirectToActionResult>(await c.DeleteConfirmed(777777));
                Assert.Equal("Index", r.ActionName);
                Assert.Equal(7, await ctx.Courses.CountAsync());
            }
        }
    }
}
