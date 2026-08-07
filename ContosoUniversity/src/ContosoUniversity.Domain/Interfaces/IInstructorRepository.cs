using ContosoUniversity.Domain.Entities;

namespace ContosoUniversity.Domain.Interfaces;

/// <summary>
/// Repository interface for Instructor entity operations.
/// </summary>
public interface IInstructorRepository
{
    Task<IEnumerable<Instructor>> GetAllAsync();
    Task<IEnumerable<Instructor>> GetAllWithDetailsAsync();
    Task<Instructor?> GetByIdAsync(int id);
    Task<Instructor?> GetByIdWithDetailsAsync(int id);
    Task AddAsync(Instructor instructor);
    Task UpdateAsync(Instructor instructor);
    Task DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
}
