using ContosoUniversity.Application.DTOs;

namespace ContosoUniversity.Web.ViewModels;

/// <summary>
/// ViewModel for the Instructor Index view.
/// Replaces legacy InstructorIndexData ViewModel.
/// </summary>
public class InstructorIndexViewModel
{
    public IEnumerable<InstructorDto> Instructors { get; set; } = new List<InstructorDto>();
    public IEnumerable<CourseDto> Courses { get; set; } = new List<CourseDto>();
    public IEnumerable<EnrollmentDto> Enrollments { get; set; } = new List<EnrollmentDto>();
}
