using ContosoUniversity.Domain.Entities;
using ContosoUniversity.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ContosoUniversity.Infrastructure.Data;

/// <summary>
/// Seeds the database with initial data.
/// Replaces the legacy EF6 DbMigrationsConfiguration Seed method.
/// </summary>
public static class DbInitializer
{
    public static void Initialize(SchoolContext context)
    {
        // Apply any pending migrations
        context.Database.EnsureCreated();

        // Return if data already exists
        if (context.Students.Any()) return;

        var students = new Student[]
        {
            new() { FirstMidName = "Carson",   LastName = "Alexander",  EnrollmentDate = DateTime.Parse("2010-09-01") },
            new() { FirstMidName = "Meredith", LastName = "Alonso",     EnrollmentDate = DateTime.Parse("2012-09-01") },
            new() { FirstMidName = "Arturo",   LastName = "Anand",      EnrollmentDate = DateTime.Parse("2013-09-01") },
            new() { FirstMidName = "Gytis",    LastName = "Barzdukas",  EnrollmentDate = DateTime.Parse("2012-09-01") },
            new() { FirstMidName = "Yan",      LastName = "Li",         EnrollmentDate = DateTime.Parse("2012-09-01") },
            new() { FirstMidName = "Peggy",    LastName = "Justice",    EnrollmentDate = DateTime.Parse("2011-09-01") },
            new() { FirstMidName = "Laura",    LastName = "Norman",     EnrollmentDate = DateTime.Parse("2013-09-01") },
            new() { FirstMidName = "Nino",     LastName = "Olivetto",   EnrollmentDate = DateTime.Parse("2005-09-01") }
        };
        context.Students.AddRange(students);
        context.SaveChanges();

        var instructors = new Instructor[]
        {
            new() { FirstMidName = "Kim",     LastName = "Abercrombie", HireDate = DateTime.Parse("1995-03-11") },
            new() { FirstMidName = "Fadi",    LastName = "Fakhouri",    HireDate = DateTime.Parse("2002-07-06") },
            new() { FirstMidName = "Roger",   LastName = "Harui",       HireDate = DateTime.Parse("1998-07-01") },
            new() { FirstMidName = "Candace", LastName = "Kapoor",      HireDate = DateTime.Parse("2001-01-15") },
            new() { FirstMidName = "Roger",   LastName = "Zheng",       HireDate = DateTime.Parse("2004-02-12") }
        };
        context.Instructors.AddRange(instructors);
        context.SaveChanges();

        var departments = new Department[]
        {
            new() { Name = "English",     Budget = 350000, StartDate = DateTime.Parse("2007-09-01"), InstructorID = instructors.Single(i => i.LastName == "Abercrombie").ID },
            new() { Name = "Mathematics", Budget = 100000, StartDate = DateTime.Parse("2007-09-01"), InstructorID = instructors.Single(i => i.LastName == "Fakhouri").ID },
            new() { Name = "Engineering", Budget = 350000, StartDate = DateTime.Parse("2007-09-01"), InstructorID = instructors.Single(i => i.LastName == "Harui").ID },
            new() { Name = "Economics",   Budget = 100000, StartDate = DateTime.Parse("2007-09-01"), InstructorID = instructors.Single(i => i.LastName == "Kapoor").ID }
        };
        context.Departments.AddRange(departments);
        context.SaveChanges();

        var courses = new Course[]
        {
            new() { CourseID = 1050, Title = "Chemistry",      Credits = 3, DepartmentID = departments.Single(s => s.Name == "Engineering").DepartmentID },
            new() { CourseID = 4022, Title = "Microeconomics", Credits = 3, DepartmentID = departments.Single(s => s.Name == "Economics").DepartmentID },
            new() { CourseID = 4041, Title = "Macroeconomics", Credits = 3, DepartmentID = departments.Single(s => s.Name == "Economics").DepartmentID },
            new() { CourseID = 1045, Title = "Calculus",       Credits = 4, DepartmentID = departments.Single(s => s.Name == "Mathematics").DepartmentID },
            new() { CourseID = 3141, Title = "Trigonometry",   Credits = 4, DepartmentID = departments.Single(s => s.Name == "Mathematics").DepartmentID },
            new() { CourseID = 2021, Title = "Composition",    Credits = 3, DepartmentID = departments.Single(s => s.Name == "English").DepartmentID },
            new() { CourseID = 2042, Title = "Literature",     Credits = 4, DepartmentID = departments.Single(s => s.Name == "English").DepartmentID }
        };
        context.Courses.AddRange(courses);
        context.SaveChanges();

        var officeAssignments = new OfficeAssignment[]
        {
            new() { InstructorID = instructors.Single(i => i.LastName == "Fakhouri").ID, Location = "Smith 17" },
            new() { InstructorID = instructors.Single(i => i.LastName == "Harui").ID,    Location = "Gowan 27" },
            new() { InstructorID = instructors.Single(i => i.LastName == "Kapoor").ID,   Location = "Thompson 304" }
        };
        context.OfficeAssignments.AddRange(officeAssignments);
        context.SaveChanges();

        // Assign instructors to courses
        var chemistry = courses.Single(c => c.Title == "Chemistry");
        chemistry.Instructors.Add(instructors.Single(i => i.LastName == "Kapoor"));
        chemistry.Instructors.Add(instructors.Single(i => i.LastName == "Harui"));

        var microeconomics = courses.Single(c => c.Title == "Microeconomics");
        microeconomics.Instructors.Add(instructors.Single(i => i.LastName == "Zheng"));

        var macroeconomics = courses.Single(c => c.Title == "Macroeconomics");
        macroeconomics.Instructors.Add(instructors.Single(i => i.LastName == "Zheng"));

        var calculus = courses.Single(c => c.Title == "Calculus");
        calculus.Instructors.Add(instructors.Single(i => i.LastName == "Fakhouri"));

        var trigonometry = courses.Single(c => c.Title == "Trigonometry");
        trigonometry.Instructors.Add(instructors.Single(i => i.LastName == "Harui"));

        var composition = courses.Single(c => c.Title == "Composition");
        composition.Instructors.Add(instructors.Single(i => i.LastName == "Abercrombie"));

        var literature = courses.Single(c => c.Title == "Literature");
        literature.Instructors.Add(instructors.Single(i => i.LastName == "Abercrombie"));

        context.SaveChanges();

        var enrollments = new Enrollment[]
        {
            new() { StudentID = students.Single(s => s.LastName == "Alexander").ID, CourseID = courses.Single(c => c.Title == "Chemistry").CourseID,      Grade = Grade.A },
            new() { StudentID = students.Single(s => s.LastName == "Alexander").ID, CourseID = courses.Single(c => c.Title == "Microeconomics").CourseID,  Grade = Grade.C },
            new() { StudentID = students.Single(s => s.LastName == "Alexander").ID, CourseID = courses.Single(c => c.Title == "Macroeconomics").CourseID,  Grade = Grade.B },
            new() { StudentID = students.Single(s => s.LastName == "Alonso").ID,    CourseID = courses.Single(c => c.Title == "Calculus").CourseID,        Grade = Grade.B },
            new() { StudentID = students.Single(s => s.LastName == "Alonso").ID,    CourseID = courses.Single(c => c.Title == "Trigonometry").CourseID,    Grade = Grade.B },
            new() { StudentID = students.Single(s => s.LastName == "Alonso").ID,    CourseID = courses.Single(c => c.Title == "Composition").CourseID,     Grade = Grade.B },
            new() { StudentID = students.Single(s => s.LastName == "Anand").ID,     CourseID = courses.Single(c => c.Title == "Chemistry").CourseID },
            new() { StudentID = students.Single(s => s.LastName == "Anand").ID,     CourseID = courses.Single(c => c.Title == "Microeconomics").CourseID,  Grade = Grade.B },
            new() { StudentID = students.Single(s => s.LastName == "Barzdukas").ID, CourseID = courses.Single(c => c.Title == "Chemistry").CourseID,       Grade = Grade.B },
            new() { StudentID = students.Single(s => s.LastName == "Li").ID,        CourseID = courses.Single(c => c.Title == "Composition").CourseID,     Grade = Grade.B },
            new() { StudentID = students.Single(s => s.LastName == "Justice").ID,   CourseID = courses.Single(c => c.Title == "Literature").CourseID,      Grade = Grade.B }
        };
        context.Enrollments.AddRange(enrollments);
        context.SaveChanges();
    }
}
