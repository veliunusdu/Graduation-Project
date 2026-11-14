using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeworkPlatform.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AssignmentAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AssignmentAttachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AssignmentId = table.Column<int>(type: "INTEGER", nullable: false),
                    StoredFileName = table.Column<string>(type: "TEXT", maxLength: 39, nullable: false),
                    OriginalFileName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    FileSize = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssignmentAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssignmentAttachments_Assignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "Assignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentAttachments_AssignmentId",
                table: "AssignmentAttachments",
                column: "AssignmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssignmentAttachments");
        }
    }
}
