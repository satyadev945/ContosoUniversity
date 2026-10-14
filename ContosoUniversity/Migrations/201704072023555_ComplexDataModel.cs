using Microsoft.EntityFrameworkCore.Migrations;

namespace ContosoUniversity.Migrations
{
    public partial class ComplexDataModel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "InstructorCourse",
                newName: "CourseInstructor");

            migrationBuilder.RenameColumn(
                name: "Instructor_ID",
                table: "CourseInstructor",
                newName: "InstructorID");

            migrationBuilder.RenameColumn(
                name: "Course_CourseID",
                table: "CourseInstructor",
                newName: "CourseID");

            migrationBuilder.RenameIndex(
                name: "IX_Course_CourseID",
                table: "CourseInstructor",
                newName: "IX_CourseID");

            migrationBuilder.RenameIndex(
                name: "IX_Instructor_ID",
                table: "CourseInstructor",
                newName: "IX_InstructorID");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CourseInstructor",
                table: "CourseInstructor");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CourseInstructor",
                table: "CourseInstructor",
                columns: new[] { "CourseID", "InstructorID" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_CourseInstructor",
                table: "CourseInstructor");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CourseInstructor",
                table: "CourseInstructor",
                columns: new[] { "InstructorID", "CourseID" });

            migrationBuilder.RenameIndex(
                name: "IX_InstructorID",
                table: "CourseInstructor",
                newName: "IX_Instructor_ID");

            migrationBuilder.RenameIndex(
                name: "IX_CourseID",
                table: "CourseInstructor",
                newName: "IX_Course_CourseID");

            migrationBuilder.RenameColumn(
                name: "CourseID",
                table: "CourseInstructor",
                newName: "Course_CourseID");

            migrationBuilder.RenameColumn(
                name: "InstructorID",
                table: "CourseInstructor",
                newName: "Instructor_ID");

            migrationBuilder.RenameTable(
                name: "CourseInstructor",
                newName: "InstructorCourse");
        }
    }
}
