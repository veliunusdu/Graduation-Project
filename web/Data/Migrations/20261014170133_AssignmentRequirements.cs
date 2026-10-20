using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeworkPlatform.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AssignmentRequirements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CreatedByAI",
                table: "Assignments",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Difficulty",
                table: "Assignments",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "DueDate",
                table: "Assignments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AssignmentRequirements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AssignmentId = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 10000, nullable: false),
                    Weight = table.Column<int>(type: "INTEGER", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssignmentRequirements", x => x.Id);
                    table.CheckConstraint("CK_Requirements_Order", "\"Order\" BETWEEN 1 AND 100");
                    table.CheckConstraint("CK_Requirements_Weight", "Weight BETWEEN 1 AND 100");
                    table.ForeignKey(
                        name: "FK_AssignmentRequirements_Assignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "Assignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Assignments_Difficulty",
                table: "Assignments",
                sql: "Difficulty BETWEEN 0 AND 2");

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentRequirements_AssignmentId_Order",
                table: "AssignmentRequirements",
                columns: new[] { "AssignmentId", "Order" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssignmentRequirements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Assignments_Difficulty",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "CreatedByAI",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "Difficulty",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "DueDate",
                table: "Assignments");
        }
    }
}
