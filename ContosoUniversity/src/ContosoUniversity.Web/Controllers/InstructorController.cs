using ContosoUniversity.Application.DTOs;
using ContosoUniversity.Application.Interfaces;
using ContosoUniversity.Domain.Interfaces;
using ContosoUniversity.Infrastructure.Data;
using ContosoUniversity.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContosoUniversity.Web.Controllers;

/// <summary>
/// Instructor controller - migrated from legacy InstructorController.
/// Key changes:
/// - Uses IInstructorService via DI
/// - Async/await throughout
/// - Explicit loading replaced with EF Core Include/ThenInclude
/// - InstructorIndexData ViewModel preserved as InstructorIndexViewModel
/// </summary>
public class InstructorController : Controller
{
    private readonly IInstructorService _instructorService;
    private readonly SchoolContext _context;
    private readonly ILogger<InstructorController> _logger;

    public InstructorController(
        IInstructorService instructorService,
        SchoolContext context,
        ILogger<InstructorController> logger)
    {
        _instructorService = instructorService;
        _context = context;
        _logger = logger;
    }

    // GET: Instructor
    public async Task<IActionResult> Index(int? id, int? courseID)
    {
        var viewModel = new InstructorIndexViewModel();

        viewModel.Instructors = await _instructorService.GetAllInstructorsAsync();

        if (id != null)
        {
            ViewBag.InstructorID = id.Value;
            var selectedInstructor = viewModel.Instructors.FirstOrDefault(i => i.ID == id.Value);
            if (selectedInstructor != null)
            {
                viewModel.Courses = selectedInstructor.Courses;
            }
        }

        if (courseID != null)
        {
            ViewBag.CourseID = courseID.Value;
            // Load enrollments for the selected course with student data
            var enrollments = await _context.Enrollments
                .Include(e => e.Student)
                .Where(e => e.CourseID == courseID.Value)
                .AsNoTracking()
                .ToListAsync();

            viewModel.Enrollments = enrollments.Select(e => new EnrollmentDto
            {
                EnrollmentID = e.EnrollmentID,
                CourseID = e.CourseID,
                StudentID = e.StudentID,
                Grade = e.Grade,
                StudentName = e.Student?.FullName ?? string.Empty
            });
        }

        return View(viewModel);
    }

    // GET: Instructor/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return BadRequest();

        var instructor = await _instructorService.GetInstructorByIdAsync(id.Value);
        if (instructor == null) return NotFound();

        return View(instructor);
    }

    // GET: Instructor/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: Instructor/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("ID,LastName,FirstMidName,HireDate")] InstructorDto instructorDto)
    {
        if (ModelState.IsValid)
        {
            await _instructorService.CreateInstructorAsync(instructorDto);
            return RedirectToAction(nameof(Index));
        }

        return View(instructorDto);
    }

    // GET: Instructor/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return BadRequest();

        var instructor = await _instructorService.GetInstructorByIdAsync(id.Value);
        if (instructor == null) return NotFound();

        return View(instructor);
    }

    // POST: Instructor/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit([Bind("ID,LastName,FirstMidName,HireDate")] InstructorDto instructorDto)
    {
        if (ModelState.IsValid)
        {
            await _instructorService.UpdateInstructorAsync(instructorDto);
            return RedirectToAction(nameof(Index));
        }

        return View(instructorDto);
    }

    // GET: Instructor/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return BadRequest();

        var instructor = await _instructorService.GetInstructorByIdAsync(id.Value);
        if (instructor == null) return NotFound();

        return View(instructor);
    }

    // POST: Instructor/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await _instructorService.DeleteInstructorAsync(id);
        return RedirectToAction(nameof(Index));
    }
}
