using System.ComponentModel.DataAnnotations;

namespace ContosoUniversity.Application.DTOs;

/// <summary>
/// Data Transfer Object for Course data.
/// </summary>
public class CourseDto
{
    [Display(Name = "Number")]
    public int CourseID { get; set; }

    [StringLength(50, MinimumLength = 3)]
    public string Title { get; set; } = string.Empty;

    [Range(0, 5)]
    public int Credits { get; set; }

    public int DepartmentID { get; set; }

    public string DepartmentName { get; set; } = string.Empty;
}
