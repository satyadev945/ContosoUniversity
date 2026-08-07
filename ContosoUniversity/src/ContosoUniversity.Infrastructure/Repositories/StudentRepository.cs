using ContosoUniversity.Domain.Entities;
using ContosoUniversity.Domain.Interfaces;
using ContosoUniversity.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ContosoUniversity.Infrastructure.Repositories;

/// <summary>
/// EF Core repository implementation for Student entity.
/// Replaces direct DbContext usage in legacy controllers.
/// </summary>
public class StudentRepository : IStudentRepository
{
    private readonly SchoolContext _context;

    public StudentRepository(SchoolContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Student>> GetAllAsync()
    {
        return await _context.Students.AsNoTracking().ToListAsync();
    }

    public async Task<Student?> GetByIdAsync(int id)
    {
        return await _context.Students.FindAsync(id);
    }

    public async Task<Student?> GetByIdWithEnrollmentsAsync(int id)
    {
        return await _context.Students
            .Include(s => s.Enrollments)
                .ThenInclude(e => e.Course)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.ID == id);
    }

    public async Task<IEnumerable<Student>> SearchAsync(string searchString)
    {
        return await _context.Students
            .Where(s => s.LastName.Contains(searchString) || s.FirstMidName.Contains(searchString))
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task AddAsync(Student student)
    {
        _context.Students.Add(student);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Student student)
    {
        _context.Entry(student).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var student = await _context.Students.FindAsync(id);
        if (student != null)
        {
            _context.Students.Remove(student);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Students.AnyAsync(s => s.ID == id);
    }
}
