using ContosoUniversity.Application.DTOs;
using ContosoUniversity.Application.Interfaces;
using ContosoUniversity.Domain.Entities;
using ContosoUniversity.Domain.Interfaces;

namespace ContosoUniversity.Application.Services;

/// <summary>
/// Service implementation for Instructor business operations.
/// </summary>
public class InstructorService : IInstructorService
{
    private readonly IInstructorRepository _instructorRepository;

    public InstructorService(IInstructorRepository instructorRepository)
    {
        _instructorRepository = instructorRepository;
    }

    public async Task<IEnumerable<InstructorDto>> GetAllInstructorsAsync()
    {
        var instructors = await _instructorRepository.GetAllWithDetailsAsync();
        return instructors.Select(MapToDto);
    }

    public async Task<InstructorDto?> GetInstructorByIdAsync(int id)
    {
        var instructor = await _instructorRepository.GetByIdAsync(id);
        return instructor == null ? null : MapToDto(instructor);
    }

    public async Task<InstructorDto?> GetInstructorWithDetailsAsync(int id)
    {
        var instructor = await _instructorRepository.GetByIdWithDetailsAsync(id);
        return instructor == null ? null : MapToDtoWithDetails(instructor);
    }

    public async Task<InstructorDto> CreateInstructorAsync(InstructorDto instructorDto)
    {
        var instructor = MapToEntity(instructorDto);
        await _instructorRepository.AddAsync(instructor);
        return MapToDto(instructor);
    }

    public async Task<bool> UpdateInstructorAsync(InstructorDto instructorDto)
    {
        var instructor = await _instructorRepository.GetByIdAsync(instructorDto.ID);
        if (instructor == null) return false;

        instructor.LastName = instructorDto.LastName;
        instructor.FirstMidName = instructorDto.FirstMidName;
        instructor.HireDate = instructorDto.HireDate;

        await _instructorRepository.UpdateAsync(instructor);
        return true;
    }

    public async Task<bool> DeleteInstructorAsync(int id)
    {
        if (!await _instructorRepository.ExistsAsync(id)) return false;
        await _instructorRepository.DeleteAsync(id);
        return true;
    }

    private static InstructorDto MapToDto(Instructor instructor) => new()
    {
        ID = instructor.ID,
        LastName = instructor.LastName,
        FirstMidName = instructor.FirstMidName,
        HireDate = instructor.HireDate,
        OfficeLocation = instructor.OfficeAssignment?.Location
    };

    private static InstructorDto MapToDtoWithDetails(Instructor instructor) => new()
    {
        ID = instructor.ID,
        LastName = instructor.LastName,
        FirstMidName = instructor.FirstMidName,
        HireDate = instructor.HireDate,
        OfficeLocation = instructor.OfficeAssignment?.Location,
        Courses = instructor.Courses.Select(c => new CourseDto
        {
            CourseID = c.CourseID,
            Title = c.Title,
            Credits = c.Credits,
            DepartmentID = c.DepartmentID,
            DepartmentName = c.Department?.Name ?? string.Empty
        })
    };

    private static Instructor MapToEntity(InstructorDto dto) => new()
    {
        ID = dto.ID,
        LastName = dto.LastName,
        FirstMidName = dto.FirstMidName,
        HireDate = dto.HireDate
    };
}
