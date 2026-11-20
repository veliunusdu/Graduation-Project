using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeworkPlatform.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AccountEducationLevels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EducationLevel",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(@"
                UPDATE AspNetUsers SET EducationLevel = 1 WHERE StudentClass LIKE 'University%';
                UPDATE AspNetUsers SET EducationLevel = 1
                WHERE Id IN (SELECT TeacherId FROM Courses WHERE EducationLevel = 1);
                UPDATE AspNetUsers SET EducationLevel = 1
                WHERE NormalizedEmail IN ('TEACHER@EDUPILOT.LOCAL', 'STUDENT1@EDUPILOT.LOCAL', 'STUDENT2@EDUPILOT.LOCAL', 'STUDENT3@EDUPILOT.LOCAL');
                UPDATE AspNetUsers SET StudentClass = 'University2'
                WHERE NormalizedEmail = 'STUDENT3@EDUPILOT.LOCAL';
                UPDATE Courses SET EducationLevel = 1,
                    GradeLevel = CASE WHEN GradeLevel BETWEEN 9 AND 12 THEN GradeLevel - 8
                                      WHEN GradeLevel BETWEEN 1 AND 4 THEN GradeLevel ELSE 1 END,
                    Description = CASE WHEN Description = 'Introductory course in high school mathematics.'
                                       THEN 'Introductory course in university mathematics.' ELSE Description END
                WHERE EducationLevel = 0 AND TeacherId IN
                    (SELECT Id FROM AspNetUsers WHERE NormalizedEmail = 'TEACHER@EDUPILOT.LOCAL');
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EducationLevel",
                table: "AspNetUsers");
        }
    }
}
