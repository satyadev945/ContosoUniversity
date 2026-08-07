using ContosoUniversity.Application.DTOs;
using ContosoUniversity.Application.Interfaces;
using ContosoUniversity.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ContosoUniversity.Web.Controllers;

/// <summary>
/// Course controller - migrated from legacy CourseController.
/// Key changes:
/// - Uses ICourseService and IDepartmentRepository via DI
/// - Async/await throughout
/// - SelectList populated from IDepartmentRepository
/// - HttpStatusCodeResult replaced with BadRequest()/NotFound()
/// </summary>
public class CourseController : Controller
{
    private readonly ICourseService _courseService;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly ILogger<CourseController> _logger;

    public CourseController(
        ICourseService courseService,
        IDepartmentRepository departmentRepository,
        ILogger<CourseController> logger)
    {
        _courseService = courseService;
        _departmentRepository = departmentRepository;
        _logger = logger;
    }

    // GET: Course
    public async Task<IActionResult> Index()
    {
        var courses = await _courseService.GetAllCoursesAsync();
        return View(courses);
    }

    // GET: Course/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return BadRequest();

        var course = await _courseService.GetCourseByIdAsync(id.Value);
        if (course == null) return NotFound();

        return View(course);
    }

    // GET: Course/Create
    public async Task<IActionResult> Create()
    {
        await PopulateDepartmentsDropDownList();
        return View();
    }

    // POST: Course/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("CourseID,Title,Credits,DepartmentID")] CourseDto courseDto)
    {
        if (ModelState.IsValid)
        {
            await _courseService.CreateCourseAsync(courseDto);
            return RedirectToAction(nameof(Index));
        }

        await PopulateDepartmentsDropDownList(courseDto.DepartmentID);
        return View(courseDto);
    }

    // GET: Course/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return BadRequest();

        var course = await _courseService.GetCourseByIdAsync(id.Value);
        if (course == null) return NotFound();

        await PopulateDepartmentsDropDownList(course.DepartmentID);
        return View(course);
    }

    // POST: Course/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit([Bind("CourseID,Title,Credits,DepartmentID")] CourseDto courseDto)
    {
        if (ModelState.IsValid)
        {
            await _courseService.UpdateCourseAsync(courseDto);
            return RedirectToAction(nameof(Index));
        }

        await PopulateDepartmentsDropDownList(courseDto.DepartmentID);
        return View(courseDto);
    }

    // GET: Course/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return BadRequest();

        var course = await _courseService.GetCourseByIdAsync(id.Value);
        if (course == null) return NotFound();

        return View(course);
    }

    // POST: Course/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await _courseService.DeleteCourseAsync(id);
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateDepartmentsDropDownList(object? selectedDepartment = null)
    {
        var departments = await _departmentRepository.GetAllAsync();
        ViewBag.DepartmentID = new SelectList(departments, "DepartmentID", "Name", selectedDepartment);
    }
}
