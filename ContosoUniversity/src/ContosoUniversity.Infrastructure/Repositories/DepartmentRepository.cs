using ContosoUniversity.Domain.Entities;
using ContosoUniversity.Domain.Interfaces;
using ContosoUniversity.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ContosoUniversity.Infrastructure.Repositories;

/// <summary>
/// EF Core repository implementation for Department entity.
/// </summary>
public class DepartmentRepository : IDepartmentRepository
{
    private readonly SchoolContext _context;

    public DepartmentRepository(SchoolContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Department>> GetAllAsync()
    {
        return await _context.Departments.AsNoTracking().ToListAsync();
    }

    public async Task<Department?> GetByIdAsync(int id)
    {
        return await _context.Departments.FindAsync(id);
    }

    public async Task AddAsync(Department department)
    {
        _context.Departments.Add(department);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Department department)
    {
        _context.Entry(department).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var department = await _context.Departments.FindAsync(id);
        if (department != null)
        {
            _context.Departments.Remove(department);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Departments.AnyAsync(d => d.DepartmentID == id);
    }
}
