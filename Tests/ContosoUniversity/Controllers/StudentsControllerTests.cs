using ContosoUniversity.Data;
using ContosoUniversity.Helpers;
using ContosoUniversity.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using X.PagedList;
using Xunit;

namespace ContosoUniversity.Controllers
{
    public class StudentsControllerTests
    {
        private static async Task<(StudentsController, SchoolContext, ListLogger<StudentsController>)> CreateAsync(Dictionary<string, string> form = null)
        {
            var ctx = await TestHelpers.CreateSeededContextAsync();
            var logger = new ListLogger<StudentsController>();
            var c = new StudentsController(ctx, logger);
            TestHelpers.SetupController(c, form);
            return (c, ctx, logger);
        }

        private static IPagedList<Student> Model(IActionResult r) =>
            Assert.IsAssignableFrom<IPagedList<Student>>(Assert.IsType<ViewResult>(r).Model);

        [Fact]
        public async Task Index_Default_SortsByLastNameAndPages()
        {
            var (c, ctx, _) = await CreateAsync();
            using (ctx)
            {
                var m = Model(await c.Index(null, null, null, null));
                Assert.Equal(3, m.Count);
                Assert.Equal(8, m.TotalItemCount);
                Assert.Equal("Alexander", m.First().LastName);
                Assert.Equal("name_desc", c.ViewBag.NameSortParm);
                Assert.Equal("Date", c.ViewBag.DateSortParm);
            }
        }

        [Fact]
        public async Task Index_NameDesc_SortsDescending()
        {
            var (c, ctx, _) = await CreateAsync();
            using (ctx)
            {
                var m = Model(await c.Index("name_desc", null, null, null));
                Assert.Equal("Olivetto", m.First().LastName);
                Assert.Equal("", c.ViewBag.NameSortParm);
            }
        }

        [Fact]
        public async Task Index_DateSort_SortsByDateAscending()
        {
            var (c, ctx, _) = await CreateAsync();
            using (ctx)
            {
                var m = Model(await c.Index("Date", null, null, null));
                Assert.Equal("Olivetto", m.First().LastName);
                Assert.Equal("date_desc", c.ViewBag.DateSortParm);
            }
        }

        [Fact]
        public async Task Index_DateDesc_SortsByDateDescending()
        {
            var (c, ctx, _) = await CreateAsync();
            using (ctx)
            {
                var m = Model(await c.Index("date_desc", null, null, null));
                Assert.Equal(2013, m.First().EnrollmentDate.Year);
            }
        }

        [Fact]
        public async Task Index_SearchString_FiltersAndResetsPage()
        {
            var (c, ctx, _) = await CreateAsync();
            using (ctx)
            {
                var m = Model(await c.Index(null, null, "Al", 3));
                Assert.Equal(2, m.TotalItemCount);
                Assert.Equal(1, m.PageNumber);
                Assert.Equal("Al", c.ViewBag.CurrentFilter);
            }
        }

        [Fact]
        public async Task Index_CurrentFilterUsedWhenNoSearch()
        {
            var (c, ctx, _) = await CreateAsync();
            using (ctx)
            {
                var m = Model(await c.Index(null, "Li", null, 1));
                Assert.Equal(1, m.TotalItemCount);
                Assert.Equal("Li", c.ViewBag.CurrentFilter);
            }
        }

        [Fact]
        public async Task Index_SecondPage_ReturnsNextItems()
        {
            var (c, ctx, _) = await CreateAsync();
            using (ctx)
            {
                var m = Model(await c.Index(null, null, null, 2));
                Assert.Equal(2, m.PageNumber);
                Assert.Equal(3, m.Count);
            }
        }

        [Fact]
        public async Task Details_NullId_ReturnsBadRequest()
        {
            var (c, ctx, _) = await CreateAsync();
            using (ctx) Assert.IsType<BadRequestResult>(await c.Details(null));
        }

        [Fact]
        public async Task Details_Missing_ReturnsNotFound()
        {
            var (c, ctx, _) = await CreateAsync();
            using (ctx) Assert.IsType<NotFoundResult>(await c.Details(99999));
        }

        [Fact]
        public async Task Details_Existing_ReturnsStudentWithEnrollments()
        {
            var (c, ctx, _) = await CreateAsync();
            using (ctx)
            {
                var id = (await ctx.Students.FirstAsync(s => s.LastName == "Alexander")).ID;
                var s = Assert.IsType<Student>(Assert.IsType<ViewResult>(await c.Details(id)).Model);
                Assert.Equal(3, s.Enrollments.Count);
            }
        }

        [Fact]
        public async Task CreateGet_ReturnsView()
        {
            var (c, ctx, _) = await CreateAsync();
            using (ctx) Assert.IsType<ViewResult>(c.Create());
        }

        [Fact]
        public async Task CreatePost_Valid_SavesAndRedirects()
        {
            var (c, ctx, _) = await CreateAsync();
            using (ctx)
            {
                var r = await c.Create(new Student { LastName = "New", FirstMidName = "Student", EnrollmentDate = DateTime.Today });
                Assert.Equal("Index", Assert.IsType<RedirectToActionResult>(r).ActionName);
                Assert.Equal(9, await ctx.Students.CountAsync());
            }
        }

        [Fact]
        public async Task CreatePost_InvalidModel_ReturnsView()
        {
            var (c, ctx, _) = await CreateAsync();
            using (ctx)
            {
                c.ModelState.AddModelError("LastName", "required");
                var s = new Student();
                Assert.Same(s, Assert.IsType<ViewResult>(await c.Create(s)).Model);
                Assert.Equal(8, await ctx.Students.CountAsync());
            }
        }

        [Fact]
        public async Task EditGet_NullId_ReturnsBadRequest()
        {
            var (c, ctx, _) = await CreateAsync();
            using (ctx) Assert.IsType<BadRequestResult>(await c.Edit(null));
        }

        [Fact]
        public async Task EditGet_Missing_ReturnsNotFound()
        {
            var (c, ctx, _) = await CreateAsync();
            using (ctx) Assert.IsType<NotFoundResult>(await c.Edit(99999));
        }

        [Fact]
        public async Task EditGet_Existing_ReturnsView()
        {
            var (c, ctx, _) = await CreateAsync();
            using (ctx)
            {
                var id = (await ctx.Students.FirstAsync()).ID;
                Assert.IsType<Student>(Assert.IsType<ViewResult>(await c.Edit(id)).Model);
            }
        }

        [Fact]
        public async Task EditPost_NullId_ReturnsBadRequest()
        {
            var (c, ctx, _) = await CreateAsync();
            using (ctx) Assert.IsType<BadRequestResult>(await c.EditPost(null));
        }

        [Fact]
        public async Task EditPost_Missing_ReturnsNotFound()
        {
            var (c, ctx, _) = await CreateAsync();
            using (ctx) Assert.IsType<NotFoundResult>(await c.EditPost(99999));
        }

        [Fact]
        public async Task EditPost_ValidForm_UpdatesStudent()
        {
            var form = new Dictionary<string, string>
            {
                ["LastName"] = "Changed",
                ["FirstMidName"] = "Name",
                ["EnrollmentDate"] = "2011-01-02"
            };
            var (c, ctx, _) = await CreateAsync(form);
            using (ctx)
            {
                var id = (await ctx.Students.FirstAsync()).ID;
                var r = await c.EditPost(id);
                Assert.IsType<RedirectToActionResult>(r);
                Assert.Equal("Changed", (await ctx.Students.FindAsync(id)).LastName);
            }
        }

        [Fact]
        public async Task EditPost_InvalidForm_ReturnsView()
        {
            var form = new Dictionary<string, string>
            {
                ["LastName"] = "Changed",
                ["FirstMidName"] = "Name",
                ["EnrollmentDate"] = "not-a-date"
            };
            var (c, ctx, _) = await CreateAsync(form);
            using (ctx)
            {
                var id = (await ctx.Students.FirstAsync()).ID;
                Assert.IsType<ViewResult>(await c.EditPost(id));
            }
        }

        [Fact]
        public async Task DeleteGet_NullId_ReturnsBadRequest()
        {
            var (c, ctx, _) = await CreateAsync();
            using (ctx) Assert.IsType<BadRequestResult>(await c.Delete(null));
        }

        [Fact]
        public async Task DeleteGet_Missing_ReturnsNotFound()
        {
            var (c, ctx, _) = await CreateAsync();
            using (ctx) Assert.IsType<NotFoundResult>(await c.Delete(99999));
        }

        [Fact]
        public async Task DeleteGet_Existing_ReturnsView()
        {
            var (c, ctx, _) = await CreateAsync();
            using (ctx)
            {
                var id = (await ctx.Students.FirstAsync()).ID;
                Assert.IsType<Student>(Assert.IsType<ViewResult>(await c.Delete(id)).Model);
                Assert.Null(c.ViewBag.ErrorMessage);
            }
        }

        [Fact]
        public async Task DeleteGet_SaveChangesError_SetsErrorMessage()
        {
            var (c, ctx, _) = await CreateAsync();
            using (ctx)
            {
                var id = (await ctx.Students.FirstAsync()).ID;
                Assert.IsType<ViewResult>(await c.Delete(id, true));
                Assert.Contains("Delete failed", (string)c.ViewBag.ErrorMessage);
            }
        }

        [Fact]
        public async Task DeleteConfirmed_Existing_RemovesStudent()
        {
            var (c, ctx, _) = await CreateAsync();
            using (ctx)
            {
                var id = (await ctx.Students.FirstAsync(s => s.LastName == "Olivetto")).ID;
                Assert.Equal("Index", Assert.IsType<RedirectToActionResult>(await c.DeleteConfirmed(id)).ActionName);
                Assert.Null(await ctx.Students.FindAsync(id));
            }
        }

        [Fact]
        public async Task DeleteConfirmed_Missing_StillRedirects()
        {
            var (c, ctx, _) = await CreateAsync();
            using (ctx)
            {
                Assert.Equal("Index", Assert.IsType<RedirectToActionResult>(await c.DeleteConfirmed(99999)).ActionName);
                Assert.Equal(8, await ctx.Students.CountAsync());
            }
        }

        [Fact]
        public async Task DeleteConfirmed_DbError_RedirectsToDeleteWithError()
        {
            var (c, ctx, logger) = await CreateAsync();
            var id = (await ctx.Students.FirstAsync()).ID;
            ctx.Dispose();   // force failure
            var r = await Record.ExceptionAsync(() => c.DeleteConfirmed(id));
            // ObjectDisposedException is not a DbUpdateException -> propagates
            Assert.IsType<ObjectDisposedException>(r);
        }
    }
}
