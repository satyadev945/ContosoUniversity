using ContosoUniversity.Application.DTOs;

namespace ContosoUniversity.Application.Interfaces;

/// <summary>
/// Service interface for Instructor business operations.
/// </summary>
public interface IInstructorService
{
    Task<IEnumerable<InstructorDto>> GetAllInstructorsAsync();
    Task<InstructorDto?> GetInstructorByIdAsync(int id);
    Task<InstructorDto?> GetInstructorWithDetailsAsync(int id);
    Task<InstructorDto> CreateInstructorAsync(InstructorDto instructorDto);
    Task<bool> UpdateInstructorAsync(InstructorDto instructorDto);
    Task<bool> DeleteInstructorAsync(int id);
}
