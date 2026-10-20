using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeworkPlatform.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class TeacherReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAt",
                table: "Submissions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TeacherFeedback",
                table: "Submissions",
                type: "TEXT",
                maxLength: 10000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TeacherGrade",
                table: "Submissions",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "TeacherFeedback",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "TeacherGrade",
                table: "Submissions");
        }
    }
}
