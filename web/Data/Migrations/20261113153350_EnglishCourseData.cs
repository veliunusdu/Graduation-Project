using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeworkPlatform.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class EnglishCourseData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE Courses
                SET Name = CASE Name
                    WHEN 'Programlamaya Giriş' THEN 'Introduction to Programming'
                    WHEN 'Veritabanı Yönetimi' THEN 'Database Management'
                    WHEN 'Matematik' THEN 'Mathematics'
                    ELSE Name END,
                    Description = CASE Description
                    WHEN 'Hazır başlangıç dersi: algoritmalar ve programlamanın temelleri.' THEN 'Introductory course in algorithms and programming fundamentals.'
                    WHEN 'Hazır başlangıç dersi: veri modelleme ve SQL.' THEN 'Introductory course in data modeling and SQL.'
                    WHEN 'Hazır başlangıç dersi: lise matematiği.' THEN 'Introductory course in high school mathematics.'
                    ELSE Description END
                WHERE Name IN ('Programlamaya Giriş', 'Veritabanı Yönetimi', 'Matematik')
                   OR Description IN (
                    'Hazır başlangıç dersi: algoritmalar ve programlamanın temelleri.',
                    'Hazır başlangıç dersi: veri modelleme ve SQL.',
                    'Hazır başlangıç dersi: lise matematiği.');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Retain translated content when schema migrations are rolled back.
        }
    }
}
