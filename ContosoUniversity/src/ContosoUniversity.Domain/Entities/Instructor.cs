using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContosoUniversity.Domain.Entities;

/// <summary>
/// Represents an instructor at Contoso University.
/// </summary>
public class Instructor
{
    public int ID { get; set; }

    [Required]
    [Display(Name = "Last Name")]
    [StringLength(50)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [Column("FirstName")]
    [Display(Name = "First Name")]
    [StringLength(50)]
    public string FirstMidName { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy}", ApplyFormatInEditMode = true)]
    [Display(Name = "Hire Date")]
    public DateTime HireDate { get; set; }

    [Display(Name = "Full Name")]
    public string FullName => $"{LastName}, {FirstMidName}";

    public virtual ICollection<Course> Courses { get; set; } = new List<Course>();
    public virtual OfficeAssignment? OfficeAssignment { get; set; }
}
