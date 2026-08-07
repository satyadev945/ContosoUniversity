using ContosoUniversity.Domain.Entities;

namespace ContosoUniversity.Domain.Interfaces;

/// <summary>
/// Repository interface for Course entity operations.
/// </summary>
public interface ICourseRepository
{
    Task<IEnumerable<Course>> GetAllAsync();
    Task<IEnumerable<Course>> GetAllWithDepartmentAsync();
    Task<Course?> GetByIdAsync(int id);
    Task<Course?> GetByIdWithDepartmentAsync(int id);
    Task AddAsync(Course course);
    Task UpdateAsync(Course course);
    Task DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
}
