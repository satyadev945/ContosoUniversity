using System.Globalization;
using ContosoUniversity.Data;
using ContosoUniversity.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ContosoUniversity.Helpers
{
    public sealed class ListLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message, Exception Exception)> Entries { get; } = new();

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            Entries.Add((logLevel, formatter(state, exception), exception));
        }
    }

    public static class TestHelpers
    {
        public static SchoolContext CreateContext(string name = null)
        {
            var options = new DbContextOptionsBuilder<SchoolContext>()
                .UseInMemoryDatabase(name ?? Guid.NewGuid().ToString())
                .Options;
            return new SchoolContext(options);
        }

        public static async Task<SchoolContext> CreateSeededContextAsync()
        {
            var name = Guid.NewGuid().ToString();
            using (var seed = CreateContext(name))
            {
                await DbInitializer.InitializeAsync(seed);
            }
            // Return a fresh context (empty change tracker) over the same seeded in-memory store.
            return CreateContext(name);
        }

        public static void SetupController(Controller controller, Dictionary<string, string> form = null)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddMvc();
            var provider = services.BuildServiceProvider();

            var httpContext = new DefaultHttpContext { RequestServices = provider };
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
            controller.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                httpContext, new FakeTempDataProvider());

            controller.Url = new FakeUrlHelper(controller.ControllerContext);

            var values = new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>();
            if (form != null)
            {
                foreach (var kv in form) values[kv.Key] = kv.Value;
            }
            controller.ControllerContext.ValueProviderFactories.Add(new FixedValueProviderFactory(
                new FormValueProvider(BindingSource.Form, new FormCollection(values), CultureInfo.InvariantCulture)));
        }

        private sealed class FixedValueProviderFactory : Microsoft.AspNetCore.Mvc.ModelBinding.IValueProviderFactory
        {
            private readonly IValueProvider _provider;
            public FixedValueProviderFactory(IValueProvider provider) { _provider = provider; }
            public Task CreateValueProviderAsync(ValueProviderFactoryContext context)
            {
                context.ValueProviders.Add(_provider);
                return Task.CompletedTask;
            }
        }

        private sealed class FakeUrlHelper : IUrlHelper
        {
            public FakeUrlHelper(ActionContext ctx) { ActionContext = ctx; }
            public ActionContext ActionContext { get; }
            public string Action(UrlActionContext actionContext) => "/fake";
            public string Content(string contentPath) => contentPath;
            public bool IsLocalUrl(string url) => true;
            public string Link(string routeName, object values) => "/fake";
            public string RouteUrl(UrlRouteContext routeContext) => "/fake";
        }

        private sealed class FakeTempDataProvider : Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider
        {
            public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
            public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
        }
    }
}
