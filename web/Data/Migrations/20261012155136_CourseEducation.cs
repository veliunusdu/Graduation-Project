using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeworkPlatform.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class CourseEducation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EducationLevel",
                table: "Courses",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GradeLevel",
                table: "Courses",
                type: "INTEGER",
                nullable: false,
                defaultValue: 9);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Courses",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Courses_EducationGrade",
                table: "Courses",
                sql: "(EducationLevel = 0 AND GradeLevel BETWEEN 9 AND 12) OR (EducationLevel = 1 AND GradeLevel BETWEEN 1 AND 4)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Courses_EducationGrade",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "EducationLevel",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "GradeLevel",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Courses");
        }
    }
}
