using ContosoUniversity.Application.DTOs;
using ContosoUniversity.Application.Interfaces;
using ContosoUniversity.Domain.Entities;
using ContosoUniversity.Domain.Interfaces;

namespace ContosoUniversity.Application.Services;

/// <summary>
/// Service implementation for Student business operations.
/// Handles mapping between domain entities and DTOs.
/// </summary>
public class StudentService : IStudentService
{
    private readonly IStudentRepository _studentRepository;

    public StudentService(IStudentRepository studentRepository)
    {
        _studentRepository = studentRepository;
    }

    public async Task<IEnumerable<StudentDto>> GetAllStudentsAsync(string? sortOrder = null, string? searchString = null)
    {
        var students = await _studentRepository.GetAllAsync();

        if (!string.IsNullOrEmpty(searchString))
        {
            students = students.Where(s =>
                s.LastName.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
                s.FirstMidName.Contains(searchString, StringComparison.OrdinalIgnoreCase));
        }

        students = sortOrder switch
        {
            "name_desc" => students.OrderByDescending(s => s.LastName),
            "Date" => students.OrderBy(s => s.EnrollmentDate),
            "date_desc" => students.OrderByDescending(s => s.EnrollmentDate),
            _ => students.OrderBy(s => s.LastName)
        };

        return students.Select(MapToDto);
    }

    public async Task<StudentDto?> GetStudentByIdAsync(int id)
    {
        var student = await _studentRepository.GetByIdAsync(id);
        return student == null ? null : MapToDto(student);
    }

    public async Task<StudentDto?> GetStudentWithEnrollmentsAsync(int id)
    {
        var student = await _studentRepository.GetByIdWithEnrollmentsAsync(id);
        return student == null ? null : MapToDtoWithEnrollments(student);
    }

    public async Task<StudentDto> CreateStudentAsync(StudentDto studentDto)
    {
        var student = MapToEntity(studentDto);
        await _studentRepository.AddAsync(student);
        return MapToDto(student);
    }

    public async Task<bool> UpdateStudentAsync(StudentDto studentDto)
    {
        var student = await _studentRepository.GetByIdAsync(studentDto.ID);
        if (student == null) return false;

        student.LastName = studentDto.LastName;
        student.FirstMidName = studentDto.FirstMidName;
        student.EnrollmentDate = studentDto.EnrollmentDate;

        await _studentRepository.UpdateAsync(student);
        return true;
    }

    public async Task<bool> DeleteStudentAsync(int id)
    {
        if (!await _studentRepository.ExistsAsync(id)) return false;
        await _studentRepository.DeleteAsync(id);
        return true;
    }

    public async Task<IEnumerable<EnrollmentDateGroupDto>> GetEnrollmentStatisticsAsync()
    {
        var students = await _studentRepository.GetAllAsync();
        return students
            .GroupBy(s => s.EnrollmentDate)
            .Select(g => new EnrollmentDateGroupDto
            {
                EnrollmentDate = g.Key,
                StudentCount = g.Count()
            })
            .OrderBy(g => g.EnrollmentDate);
    }

    private static StudentDto MapToDto(Student student) => new()
    {
        ID = student.ID,
        LastName = student.LastName,
        FirstMidName = student.FirstMidName,
        EnrollmentDate = student.EnrollmentDate
    };

    private static StudentDto MapToDtoWithEnrollments(Student student) => new()
    {
        ID = student.ID,
        LastName = student.LastName,
        FirstMidName = student.FirstMidName,
        EnrollmentDate = student.EnrollmentDate,
        Enrollments = student.Enrollments.Select(e => new EnrollmentDto
        {
            EnrollmentID = e.EnrollmentID,
            CourseID = e.CourseID,
            StudentID = e.StudentID,
            Grade = e.Grade,
            CourseTitle = e.Course?.Title ?? string.Empty
        })
    };

    private static Student MapToEntity(StudentDto dto) => new()
    {
        ID = dto.ID,
        LastName = dto.LastName,
        FirstMidName = dto.FirstMidName,
        EnrollmentDate = dto.EnrollmentDate
    };
}
