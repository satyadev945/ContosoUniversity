using ContosoUniversity.Application.DTOs;

namespace ContosoUniversity.Application.Interfaces;

/// <summary>
/// Service interface for Course business operations.
/// </summary>
public interface ICourseService
{
    Task<IEnumerable<CourseDto>> GetAllCoursesAsync();
    Task<CourseDto?> GetCourseByIdAsync(int id);
    Task<CourseDto> CreateCourseAsync(CourseDto courseDto);
    Task<bool> UpdateCourseAsync(CourseDto courseDto);
    Task<bool> DeleteCourseAsync(int id);
}
