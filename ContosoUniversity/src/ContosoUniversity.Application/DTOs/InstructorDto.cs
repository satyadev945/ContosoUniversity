using System.ComponentModel.DataAnnotations;

namespace ContosoUniversity.Application.DTOs;

/// <summary>
/// Data Transfer Object for Instructor data.
/// </summary>
public class InstructorDto
{
    public int ID { get; set; }

    [Required]
    [Display(Name = "Last Name")]
    [StringLength(50)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "First Name")]
    [StringLength(50)]
    public string FirstMidName { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy}", ApplyFormatInEditMode = true)]
    [Display(Name = "Hire Date")]
    public DateTime HireDate { get; set; }

    [Display(Name = "Full Name")]
    public string FullName => $"{LastName}, {FirstMidName}";

    public string? OfficeLocation { get; set; }

    public IEnumerable<CourseDto> Courses { get; set; } = new List<CourseDto>();
}
