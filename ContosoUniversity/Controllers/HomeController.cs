using ContosoUniversity.Caching;
using ContosoUniversity.DAL;
using ContosoUniversity.ViewModels;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace ContosoUniversity.Controllers
{
    public class HomeController : Controller
    {

        private SchoolContext db = new SchoolContext();

        public Task<ActionResult> Index()
        {
            return Task.FromResult<ActionResult>(View());
        }

        public async Task<ActionResult> About()
        {
            // Aggregated statistics are read through AWS ElastiCache (Redis) without blocking the request thread.
            var data = await ReferenceDataCache.GetOrAddAsync(CacheKeys.EnrollmentStatistics, () =>
                (from student in db.Students.AsNoTracking()
                 group student by student.EnrollmentDate into dataGroup
                 select new EnrollmentDataGroup()
                 {
                     EnrollmentDate = dataGroup.Key,
                     StudentCount = dataGroup.Count()
                 }).ToListAsync());

            return View(data);
        }

        public Task<ActionResult> Contact()
        {
            ViewBag.Message = "Your contact page.";

            return Task.FromResult<ActionResult>(View());
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}