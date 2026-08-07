using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContosoUniversity.Domain.Entities;

/// <summary>
/// Represents an office assignment for an instructor.
/// </summary>
public class OfficeAssignment
{
    [Key]
    [ForeignKey("Instructor")]
    public int InstructorID { get; set; }

    [StringLength(50)]
    [Display(Name = "Office Location")]
    public string Location { get; set; } = string.Empty;

    public virtual Instructor Instructor { get; set; } = null!;
}
