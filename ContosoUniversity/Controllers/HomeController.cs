using ContosoUniversity.DAL;
using ContosoUniversity.Security;
using ContosoUniversity.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using ContosoUniversity.Caching;

namespace ContosoUniversity.Controllers
{
    public class HomeController : Controller
    {

        public const string EnrollmentStatsCacheKey = "home:about";
        private static readonly TimeSpan EnrollmentStatsTtl = TimeSpan.FromMinutes(5);

        private SchoolContext db = new SchoolContext();
        private readonly IAsyncCache cache = RedisAsyncCache.Instance;

        public Task<ActionResult> Index()
        {
            return Task.FromResult<ActionResult>(View());
        }

        public async Task<ActionResult> About()
        {
            var data = await cache.GetOrCreateAsync(EnrollmentStatsCacheKey, () =>
            {
                IQueryable<EnrollmentDataGroup> query = from student in db.Students.AsNoTracking()
                                                        group student by student.EnrollmentDate into dataGroup
                                                        select new EnrollmentDataGroup()
                                                        {
                                                            EnrollmentDate = dataGroup.Key,
                                                            StudentCount = dataGroup.Count()
                                                        };
                return query.ToListAsync();
            }, EnrollmentStatsTtl);

            return View(data);
        }

        public Task<ActionResult> Contact()
        {
            ViewBag.Message = HtmlSanitizerService.Sanitize("Your contact page.");

            return Task.FromResult<ActionResult>(View());
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}