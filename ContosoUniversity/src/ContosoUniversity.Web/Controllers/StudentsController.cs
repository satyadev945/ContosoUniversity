using ContosoUniversity.Application.DTOs;
using ContosoUniversity.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using X.PagedList;

namespace ContosoUniversity.Web.Controllers;

/// <summary>
/// Students controller - migrated from legacy StudentsController.
/// Key changes:
/// - Inherits from Controller (ASP.NET Core) instead of System.Web.Mvc.Controller
/// - Uses IStudentService via DI instead of direct SchoolContext
/// - Returns IActionResult (same interface, different namespace)
/// - HttpStatusCodeResult replaced with StatusCode() / BadRequest()
/// - HttpNotFound() replaced with NotFound()
/// - Async/await pattern throughout
/// - RetryLimitExceededException removed (EF Core handles retries via resilience strategies)
/// </summary>
public class StudentsController : Controller
{
    private readonly IStudentService _studentService;
    private readonly ILogger<StudentsController> _logger;

    public StudentsController(IStudentService studentService, ILogger<StudentsController> logger)
    {
        _studentService = studentService;
        _logger = logger;
    }

    // GET: Students
    public async Task<IActionResult> Index(string sortOrder, string currentFilter, string searchString, int? page)
    {
        ViewBag.CurrentSort = sortOrder;
        ViewBag.NameSortParm = string.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
        ViewBag.DateSortParm = sortOrder == "Date" ? "date_desc" : "Date";

        if (searchString != null)
        {
            page = 1;
        }
        else
        {
            searchString = currentFilter;
        }

        ViewBag.CurrentFilter = searchString;

        var students = await _studentService.GetAllStudentsAsync(sortOrder, searchString);

        int pageSize = 3;
        int pageNumber = page ?? 1;

        // X.PagedList replaces legacy PagedList.Mvc (not compatible with .NET 8)
        var pagedStudents = students.ToPagedList(pageNumber, pageSize);
        return View(pagedStudents);
    }

    // GET: Students/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return BadRequest();
        }

        var student = await _studentService.GetStudentWithEnrollmentsAsync(id.Value);
        if (student == null)
        {
            return NotFound();
        }

        return View(student);
    }

    // GET: Students/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: Students/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("LastName,FirstMidName,EnrollmentDate")] StudentDto studentDto)
    {
        try
        {
            if (ModelState.IsValid)
            {
                await _studentService.CreateStudentAsync(studentDto);
                return RedirectToAction(nameof(Index));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating student");
            ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists see your system administrator.");
        }

        return View(studentDto);
    }

    // GET: Students/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return BadRequest();
        }

        var student = await _studentService.GetStudentByIdAsync(id.Value);
        if (student == null)
        {
            return NotFound();
        }

        return View(student);
    }

    // POST: Students/Edit/5
    [HttpPost, ActionName("Edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditPost(int? id)
    {
        if (id == null)
        {
            return BadRequest();
        }

        var studentToUpdate = await _studentService.GetStudentByIdAsync(id.Value);
        if (studentToUpdate == null)
        {
            return NotFound();
        }

        if (await TryUpdateModelAsync(studentToUpdate, "",
            s => s.LastName, s => s.FirstMidName, s => s.EnrollmentDate))
        {
            try
            {
                await _studentService.UpdateStudentAsync(studentToUpdate);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating student {StudentId}", id);
                ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
            }
        }

        return View(studentToUpdate);
    }

    // GET: Students/Delete/5
    public async Task<IActionResult> Delete(int? id, bool? saveChangesError = false)
    {
        if (id == null)
        {
            return BadRequest();
        }

        if (saveChangesError.GetValueOrDefault())
        {
            ViewBag.ErrorMessage = "Delete failed. Try again, and if the problem persists see your system administrator.";
        }

        var student = await _studentService.GetStudentByIdAsync(id.Value);
        if (student == null)
        {
            return NotFound();
        }

        return View(student);
    }

    // POST: Students/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _studentService.DeleteStudentAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting student {StudentId}", id);
            return RedirectToAction(nameof(Delete), new { id, saveChangesError = true });
        }

        return RedirectToAction(nameof(Index));
    }
}
