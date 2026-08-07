using System.ComponentModel.DataAnnotations;

namespace ContosoUniversity.Application.DTOs;

/// <summary>
/// Data Transfer Object for enrollment statistics grouped by date.
/// </summary>
public class EnrollmentDateGroupDto
{
    [DataType(DataType.Date)]
    public DateTime? EnrollmentDate { get; set; }

    public int StudentCount { get; set; }
}
