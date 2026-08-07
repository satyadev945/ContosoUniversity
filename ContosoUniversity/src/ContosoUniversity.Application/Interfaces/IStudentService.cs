using ContosoUniversity.Application.DTOs;

namespace ContosoUniversity.Application.Interfaces;

/// <summary>
/// Service interface for Student business operations.
/// </summary>
public interface IStudentService
{
    Task<IEnumerable<StudentDto>> GetAllStudentsAsync(string? sortOrder = null, string? searchString = null);
    Task<StudentDto?> GetStudentByIdAsync(int id);
    Task<StudentDto?> GetStudentWithEnrollmentsAsync(int id);
    Task<StudentDto> CreateStudentAsync(StudentDto studentDto);
    Task<bool> UpdateStudentAsync(StudentDto studentDto);
    Task<bool> DeleteStudentAsync(int id);
    Task<IEnumerable<EnrollmentDateGroupDto>> GetEnrollmentStatisticsAsync();
}
