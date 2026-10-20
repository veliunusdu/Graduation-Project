using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeworkPlatform.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class SubmissionFiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "FileSize",
                table: "Submissions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FileUploadedAt",
                table: "Submissions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginalFileName",
                table: "Submissions",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StoredFileName",
                table: "Submissions",
                type: "TEXT",
                maxLength: 39,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileSize",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "FileUploadedAt",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "OriginalFileName",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "StoredFileName",
                table: "Submissions");
        }
    }
}
