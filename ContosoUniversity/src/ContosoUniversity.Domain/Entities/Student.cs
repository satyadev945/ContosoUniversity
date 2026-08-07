using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContosoUniversity.Domain.Entities;

/// <summary>
/// Represents a student enrolled at Contoso University.
/// </summary>
public class Student
{
    public int ID { get; set; }

    [Required]
    [StringLength(50)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [StringLength(50, ErrorMessage = "First Name must be less than 50 characters")]
    [Column("FirstName")]
    [Display(Name = "First Name")]
    public string FirstMidName { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy}", ApplyFormatInEditMode = true)]
    [Display(Name = "Enrollment Date")]
    public DateTime EnrollmentDate { get; set; }

    [Display(Name = "Full Name")]
    public string FullName => $"{LastName}, {FirstMidName}";

    public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}
