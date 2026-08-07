using ContosoUniversity.Application.DTOs;
using ContosoUniversity.Application.Interfaces;
using ContosoUniversity.Domain.Entities;
using ContosoUniversity.Domain.Interfaces;

namespace ContosoUniversity.Application.Services;

/// <summary>
/// Service implementation for Course business operations.
/// </summary>
public class CourseService : ICourseService
{
    private readonly ICourseRepository _courseRepository;

    public CourseService(ICourseRepository courseRepository)
    {
        _courseRepository = courseRepository;
    }

    public async Task<IEnumerable<CourseDto>> GetAllCoursesAsync()
    {
        var courses = await _courseRepository.GetAllWithDepartmentAsync();
        return courses.Select(MapToDto);
    }

    public async Task<CourseDto?> GetCourseByIdAsync(int id)
    {
        var course = await _courseRepository.GetByIdWithDepartmentAsync(id);
        return course == null ? null : MapToDto(course);
    }

    public async Task<CourseDto> CreateCourseAsync(CourseDto courseDto)
    {
        var course = MapToEntity(courseDto);
        await _courseRepository.AddAsync(course);
        return MapToDto(course);
    }

    public async Task<bool> UpdateCourseAsync(CourseDto courseDto)
    {
        var course = await _courseRepository.GetByIdAsync(courseDto.CourseID);
        if (course == null) return false;

        course.Title = courseDto.Title;
        course.Credits = courseDto.Credits;
        course.DepartmentID = courseDto.DepartmentID;

        await _courseRepository.UpdateAsync(course);
        return true;
    }

    public async Task<bool> DeleteCourseAsync(int id)
    {
        if (!await _courseRepository.ExistsAsync(id)) return false;
        await _courseRepository.DeleteAsync(id);
        return true;
    }

    private static CourseDto MapToDto(Course course) => new()
    {
        CourseID = course.CourseID,
        Title = course.Title,
        Credits = course.Credits,
        DepartmentID = course.DepartmentID,
        DepartmentName = course.Department?.Name ?? string.Empty
    };

    private static Course MapToEntity(CourseDto dto) => new()
    {
        CourseID = dto.CourseID,
        Title = dto.Title,
        Credits = dto.Credits,
        DepartmentID = dto.DepartmentID
    };
}
