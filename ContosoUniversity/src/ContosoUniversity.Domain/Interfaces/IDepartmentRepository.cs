using ContosoUniversity.Domain.Entities;

namespace ContosoUniversity.Domain.Interfaces;

/// <summary>
/// Repository interface for Department entity operations.
/// </summary>
public interface IDepartmentRepository
{
    Task<IEnumerable<Department>> GetAllAsync();
    Task<Department?> GetByIdAsync(int id);
    Task AddAsync(Department department);
    Task UpdateAsync(Department department);
    Task DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
}
