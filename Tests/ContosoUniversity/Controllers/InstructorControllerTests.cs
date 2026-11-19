using ContosoUniversity.Data;
using ContosoUniversity.Helpers;
using ContosoUniversity.Models;
using ContosoUniversity.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ContosoUniversity.Controllers
{
    public class InstructorControllerTests
    {
        private static async Task<(InstructorController, SchoolContext)> CreateAsync()
        {
            var ctx = await TestHelpers.CreateSeededContextAsync();
            var c = new InstructorController(ctx, new ListLogger<InstructorController>());
            TestHelpers.SetupController(c);
            return (c, ctx);
        }

        [Fact]
        public async Task Index_NoParams_ReturnsAllInstructorsOrdered()
        {
            var (c, ctx) = await CreateAsync();
            using (ctx)
            {
                var r = Assert.IsType<ViewResult>(await c.Index(null, null));
                var vm = Assert.IsType<InstructorIndexData>(r.Model);
                var list = vm.Instructors.ToList();
                Assert.Equal(5, list.Count);
                Assert.Equal("Abercrombie", list[0].LastName);
                Assert.Null(vm.Courses);
            }
        }

        [Fact]
        public async Task Index_WithInstructorId_ReturnsCourses()
        {
            var (c, ctx) = await CreateAsync();
            using (ctx)
            {
                var id = (await ctx.Instructors.FirstAsync(i => i.LastName == "Abercrombie")).ID;
                var vm = Assert.IsType<InstructorIndexData>(Assert.IsType<ViewResult>(await c.Index(id, null)).Model);
                Assert.Equal(2, vm.Courses.Count());
                Assert.Equal(id, c.ViewBag.InstructorID);
            }
        }

        [Fact]
        public async Task Index_UnknownInstructor_ReturnsNotFound()
        {
            var (c, ctx) = await CreateAsync();
            using (ctx) Assert.IsType<NotFoundResult>(await c.Index(99999, null));
        }

        [Fact]
        public async Task Index_WithInstructorAndCourse_ReturnsEnrollments()
        {
            var (c, ctx) = await CreateAsync();
            using (ctx)
            {
                var id = (await ctx.Instructors.FirstAsync(i => i.LastName == "Kapoor")).ID;
                var vm = Assert.IsType<InstructorIndexData>(Assert.IsType<ViewResult>(await c.Index(id, 1050)).Model);
                Assert.Equal(3, vm.Enrollments.Count());
                Assert.Equal(1050, c.ViewBag.CourseID);
            }
        }

        [Fact]
        public async Task Index_CourseNotTaughtByInstructor_ReturnsNotFound()
        {
            var (c, ctx) = await CreateAsync();
            using (ctx)
            {
                var id = (await ctx.Instructors.FirstAsync(i => i.LastName == "Kapoor")).ID;
                Assert.IsType<NotFoundResult>(await c.Index(id, 2042));
            }
        }

        [Fact]
        public async Task Index_CourseWithoutInstructor_IgnoresCourse()
        {
            var (c, ctx) = await CreateAsync();
            using (ctx)
            {
                var vm = Assert.IsType<InstructorIndexData>(Assert.IsType<ViewResult>(await c.Index(null, 1050)).Model);
                Assert.Null(vm.Enrollments);
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
        public async Task Details_Existing_ReturnsInstructor()
        {
            var (c, ctx) = await CreateAsync();
            using (ctx)
            {
                var id = (await ctx.Instructors.FirstAsync(i => i.LastName == "Harui")).ID;
                var inst = Assert.IsType<Instructor>(Assert.IsType<ViewResult>(await c.Details(id)).Model);
                Assert.Equal("Gowan 27", inst.OfficeAssignment.Location);
            }
        }

        [Fact]
        public async Task CreateGet_PopulatesOffices()
        {
            var (c, ctx) = await CreateAsync();
            using (ctx)
            {
                Assert.IsType<ViewResult>(await c.Create());
                Assert.IsType<SelectList>(c.ViewBag.ID);
            }
        }

        [Fact]
        public async Task CreatePost_Valid_Redirects()
        {
            var (c, ctx) = await CreateAsync();
            using (ctx)
            {
                var r = await c.Create(new Instructor { LastName = "New", FirstMidName = "Person", HireDate = DateTime.Today });
                Assert.IsType<RedirectToActionResult>(r);
                Assert.Equal(6, await ctx.Instructors.CountAsync());
            }
        }

        [Fact]
        public async Task CreatePost_InvalidModel_ReturnsView()
        {
            var (c, ctx) = await CreateAsync();
            using (ctx)
            {
                c.ModelState.AddModelError("LastName", "required");
                var inst = new Instructor();
                var r = Assert.IsType<ViewResult>(await c.Create(inst));
                Assert.Same(inst, r.Model);
                Assert.Equal(5, await ctx.Instructors.CountAsync());
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
            using (ctx) Assert.IsType<NotFoundResult>(await c.Edit((int?)99999));
        }

        [Fact]
        public async Task EditGet_Existing_ReturnsView()
        {
            var (c, ctx) = await CreateAsync();
            using (ctx)
            {
                var id = (await ctx.Instructors.FirstAsync()).ID;
                Assert.IsType<Instructor>(Assert.IsType<ViewResult>(await c.Edit((int?)id)).Model);
            }
        }

        [Fact]
        public async Task EditPost_Valid_Updates()
        {
            var (c, ctx) = await CreateAsync();
            using (ctx)
            {
                var orig = await ctx.Instructors.AsNoTracking().FirstAsync(i => i.LastName == "Zheng");
                var r = await c.Edit(new Instructor { ID = orig.ID, LastName = "Zheng2", FirstMidName = "Roger", HireDate = orig.HireDate });
                Assert.IsType<RedirectToActionResult>(r);
                Assert.Equal("Zheng2", (await ctx.Instructors.AsNoTracking().FirstAsync(i => i.ID == orig.ID)).LastName);
            }
        }

        [Fact]
        public async Task EditPost_InvalidModel_ReturnsView()
        {
            var (c, ctx) = await CreateAsync();
            using (ctx)
            {
                c.ModelState.AddModelError("LastName", "bad");
                Assert.IsType<ViewResult>(await c.Edit(new Instructor { ID = 1 }));
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
            using (ctx) Assert.IsType<NotFoundResult>(await c.Delete(99999));
        }

        [Fact]
        public async Task DeleteGet_Existing_ReturnsView()
        {
            var (c, ctx) = await CreateAsync();
            using (ctx)
            {
                var id = (await ctx.Instructors.FirstAsync()).ID;
                Assert.IsType<Instructor>(Assert.IsType<ViewResult>(await c.Delete(id)).Model);
            }
        }

        [Fact]
        public async Task DeleteConfirmed_Missing_Redirects()
        {
            var (c, ctx) = await CreateAsync();
            using (ctx)
            {
                Assert.IsType<RedirectToActionResult>(await c.DeleteConfirmed(99999));
                Assert.Equal(5, await ctx.Instructors.CountAsync());
            }
        }

        [Fact]
        public async Task DeleteConfirmed_Existing_RemovesAndDetachesDepartments()
        {
            var (c, ctx) = await CreateAsync();
            using (ctx)
            {
                var id = (await ctx.Instructors.FirstAsync(i => i.LastName == "Abercrombie")).ID;
                Assert.IsType<RedirectToActionResult>(await c.DeleteConfirmed(id));
                Assert.Equal(4, await ctx.Instructors.CountAsync());
                Assert.Equal(0, await ctx.Departments.CountAsync(d => d.InstructorID == id));
            }
        }
    }
}
