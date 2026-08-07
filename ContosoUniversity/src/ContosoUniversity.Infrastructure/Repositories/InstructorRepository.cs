using ContosoUniversity.Domain.Entities;
using ContosoUniversity.Domain.Interfaces;
using ContosoUniversity.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ContosoUniversity.Infrastructure.Repositories;

/// <summary>
/// EF Core repository implementation for Instructor entity.
/// </summary>
public class InstructorRepository : IInstructorRepository
{
    private readonly SchoolContext _context;

    public InstructorRepository(SchoolContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Instructor>> GetAllAsync()
    {
        return await _context.Instructors.AsNoTracking().ToListAsync();
    }

    public async Task<IEnumerable<Instructor>> GetAllWithDetailsAsync()
    {
        return await _context.Instructors
            .Include(i => i.OfficeAssignment)
            .Include(i => i.Courses)
                .ThenInclude(c => c.Department)
            .OrderBy(i => i.LastName)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Instructor?> GetByIdAsync(int id)
    {
        return await _context.Instructors.FindAsync(id);
    }

    public async Task<Instructor?> GetByIdWithDetailsAsync(int id)
    {
        return await _context.Instructors
            .Include(i => i.OfficeAssignment)
            .Include(i => i.Courses)
                .ThenInclude(c => c.Department)
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.ID == id);
    }

    public async Task AddAsync(Instructor instructor)
    {
        _context.Instructors.Add(instructor);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Instructor instructor)
    {
        _context.Entry(instructor).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var instructor = await _context.Instructors.FindAsync(id);
        if (instructor != null)
        {
            _context.Instructors.Remove(instructor);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Instructors.AnyAsync(i => i.ID == id);
    }
}
