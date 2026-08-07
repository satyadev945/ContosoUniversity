using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContosoUniversity.Domain.Entities;

/// <summary>
/// Represents a course offered at Contoso University.
/// </summary>
public class Course
{
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    [Display(Name = "Number")]
    public int CourseID { get; set; }

    [StringLength(50, MinimumLength = 3)]
    public string Title { get; set; } = string.Empty;

    [Range(0, 5)]
    public int Credits { get; set; }

    public int DepartmentID { get; set; }

    public virtual Department Department { get; set; } = null!;
    public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public virtual ICollection<Instructor> Instructors { get; set; } = new List<Instructor>();
}
