using System.ComponentModel.DataAnnotations;
using ContosoUniversity.Domain.Enums;

namespace ContosoUniversity.Application.DTOs;

/// <summary>
/// Data Transfer Object for Enrollment data.
/// </summary>
public class EnrollmentDto
{
    public int EnrollmentID { get; set; }
    public int CourseID { get; set; }
    public int StudentID { get; set; }

    [DisplayFormat(NullDisplayText = "No Grade")]
    public Grade? Grade { get; set; }

    public string CourseTitle { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
}
