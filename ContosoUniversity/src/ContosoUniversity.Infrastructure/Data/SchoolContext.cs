using ContosoUniversity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ContosoUniversity.Infrastructure.Data;

/// <summary>
/// EF Core DbContext for Contoso University.
/// Replaces the legacy EF6 SchoolContext (System.Data.Entity.DbContext).
/// </summary>
public class SchoolContext : DbContext
{
    public SchoolContext(DbContextOptions<SchoolContext> options) : base(options)
    {
    }

    public DbSet<Course> Courses { get; set; } = null!;
    public DbSet<Department> Departments { get; set; } = null!;
    public DbSet<Enrollment> Enrollments { get; set; } = null!;
    public DbSet<Instructor> Instructors { get; set; } = null!;
    public DbSet<Student> Students { get; set; } = null!;
    public DbSet<OfficeAssignment> OfficeAssignments { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Remove pluralizing table name convention (matches legacy behavior)
        modelBuilder.Entity<Course>().ToTable("Course");
        modelBuilder.Entity<Department>().ToTable("Department");
        modelBuilder.Entity<Enrollment>().ToTable("Enrollment");
        modelBuilder.Entity<Instructor>().ToTable("Instructor");
        modelBuilder.Entity<Student>().ToTable("Student");
        modelBuilder.Entity<OfficeAssignment>().ToTable("OfficeAssignment");

        // Many-to-many: Course <-> Instructor via CourseInstructor join table
        modelBuilder.Entity<Course>()
            .HasMany(c => c.Instructors)
            .WithMany(i => i.Courses)
            .UsingEntity<Dictionary<string, object>>(
                "CourseInstructor",
                j => j.HasOne<Instructor>().WithMany().HasForeignKey("InstructorID"),
                j => j.HasOne<Course>().WithMany().HasForeignKey("CourseID"));
    }
}
