using System.ComponentModel.DataAnnotations;

namespace ContosoUniversity.Application.DTOs;

/// <summary>
/// Data Transfer Object for Student data.
/// </summary>
public class StudentDto
{
    public int ID { get; set; }

    [Required]
    [StringLength(50)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [StringLength(50, ErrorMessage = "First Name must be less than 50 characters")]
    [Display(Name = "First Name")]
    public string FirstMidName { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy}", ApplyFormatInEditMode = true)]
    [Display(Name = "Enrollment Date")]
    public DateTime EnrollmentDate { get; set; }

    [Display(Name = "Full Name")]
    public string FullName => $"{LastName}, {FirstMidName}";

    public IEnumerable<EnrollmentDto> Enrollments { get; set; } = new List<EnrollmentDto>();
}
