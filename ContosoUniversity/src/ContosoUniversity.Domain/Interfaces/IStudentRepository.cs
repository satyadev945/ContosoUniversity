using ContosoUniversity.Domain.Entities;

namespace ContosoUniversity.Domain.Interfaces;

/// <summary>
/// Repository interface for Student entity operations.
/// </summary>
public interface IStudentRepository
{
    Task<IEnumerable<Student>> GetAllAsync();
    Task<Student?> GetByIdAsync(int id);
    Task<Student?> GetByIdWithEnrollmentsAsync(int id);
    Task<IEnumerable<Student>> SearchAsync(string searchString);
    Task AddAsync(Student student);
    Task UpdateAsync(Student student);
    Task DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
}
