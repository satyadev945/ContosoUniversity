using ContosoUniversity.Helpers;
using ContosoUniversity.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace ContosoUniversity.Controllers
{
    public class HomeControllerTests
    {
        [Fact]
        public void Index_ReturnsView()
        {
            using var ctx = TestHelpers.CreateContext();
            var c = new HomeController(ctx, new ListLogger<HomeController>());
            Assert.IsType<ViewResult>(c.Index());
        }

        [Fact]
        public void Contact_SetsMessage()
        {
            using var ctx = TestHelpers.CreateContext();
            var c = new HomeController(ctx, new ListLogger<HomeController>());
            var r = c.Contact();
            Assert.IsType<ViewResult>(r);
            Assert.Equal("Your contact page.", c.ViewBag.Message);
        }

        [Fact]
        public async Task About_ReturnsGroupedEnrollmentData()
        {
            using var ctx = await TestHelpers.CreateSeededContextAsync();
            var c = new HomeController(ctx, new ListLogger<HomeController>());
            var r = Assert.IsType<ViewResult>(await c.About());
            var model = Assert.IsAssignableFrom<IEnumerable<EnrollmentDataGroup>>(r.Model).ToList();
            Assert.Equal(5, model.Count);
            Assert.Equal(8, model.Sum(m => m.StudentCount));
        }

        [Fact]
        public async Task About_EmptyDb_ReturnsEmptyList()
        {
            using var ctx = TestHelpers.CreateContext();
            var c = new HomeController(ctx, new ListLogger<HomeController>());
            var r = Assert.IsType<ViewResult>(await c.About());
            Assert.Empty(Assert.IsAssignableFrom<IEnumerable<EnrollmentDataGroup>>(r.Model));
        }

        [Theory]
        [InlineData(null)]
        [InlineData(404)]
        [InlineData(500)]
        public void Error_ReturnsViewModelWithRequestIdAndLogs(int? statusCode)
        {
            using var ctx = TestHelpers.CreateContext();
            var logger = new ListLogger<HomeController>();
            var c = new HomeController(ctx, logger);
            TestHelpers.SetupController(c);
            c.HttpContext.TraceIdentifier = "trace-1";
            var r = Assert.IsType<ViewResult>(c.Error(statusCode));
            var vm = Assert.IsType<ErrorViewModel>(r.Model);
            Assert.False(string.IsNullOrEmpty(vm.RequestId));
            Assert.True(vm.ShowRequestId);
            Assert.Equal(statusCode, vm.StatusCode);
            Assert.Single(logger.Entries);
        }
    }
}
