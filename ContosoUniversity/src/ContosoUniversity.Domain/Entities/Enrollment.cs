using System.ComponentModel.DataAnnotations;
using ContosoUniversity.Domain.Enums;

namespace ContosoUniversity.Domain.Entities;

/// <summary>
/// Represents a student's enrollment in a course.
/// </summary>
public class Enrollment
{
    public int EnrollmentID { get; set; }
    public int CourseID { get; set; }
    public int StudentID { get; set; }

    [DisplayFormat(NullDisplayText = "No Grade")]
    public Grade? Grade { get; set; }

    public virtual Course Course { get; set; } = null!;
    public virtual Student Student { get; set; } = null!;
}
