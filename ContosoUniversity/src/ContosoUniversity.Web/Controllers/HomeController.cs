using ContosoUniversity.Application.Interfaces;
using ContosoUniversity.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ContosoUniversity.Web.Controllers;

/// <summary>
/// Home controller - migrated from legacy HomeController.
/// Uses IStudentService instead of direct DbContext access.
/// </summary>
public class HomeController : Controller
{
    private readonly IStudentService _studentService;
    private readonly ILogger<HomeController> _logger;

    public HomeController(IStudentService studentService, ILogger<HomeController> logger)
    {
        _studentService = studentService;
        _logger = logger;
    }

    // GET: /Home/Index
    public IActionResult Index()
    {
        return View();
    }

    // GET: /Home/About
    // Replaces: synchronous LINQ query against DbContext
    public async Task<IActionResult> About()
    {
        var data = await _studentService.GetEnrollmentStatisticsAsync();
        return View(data);
    }

    // GET: /Home/Contact
    public IActionResult Contact()
    {
        ViewBag.Message = "Your contact page.";
        return View();
    }

    // GET: /Home/Error
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId = HttpContext.TraceIdentifier
        });
    }
}
