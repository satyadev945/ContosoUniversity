using ContosoUniversity.Domain.Entities;
using ContosoUniversity.Domain.Interfaces;
using ContosoUniversity.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ContosoUniversity.Infrastructure.Repositories;

/// <summary>
/// EF Core repository implementation for Course entity.
/// </summary>
public class CourseRepository : ICourseRepository
{
    private readonly SchoolContext _context;

    public CourseRepository(SchoolContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Course>> GetAllAsync()
    {
        return await _context.Courses.AsNoTracking().ToListAsync();
    }

    public async Task<IEnumerable<Course>> GetAllWithDepartmentAsync()
    {
        return await _context.Courses
            .Include(c => c.Department)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Course?> GetByIdAsync(int id)
    {
        return await _context.Courses.FindAsync(id);
    }

    public async Task<Course?> GetByIdWithDepartmentAsync(int id)
    {
        return await _context.Courses
            .Include(c => c.Department)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CourseID == id);
    }

    public async Task AddAsync(Course course)
    {
        _context.Courses.Add(course);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Course course)
    {
        _context.Entry(course).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var course = await _context.Courses.FindAsync(id);
        if (course != null)
        {
            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Courses.AnyAsync(c => c.CourseID == id);
    }
}
