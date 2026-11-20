using HomeworkPlatform.Web.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HomeworkPlatform.Web.Tests;

public class EnglishDataMigrationTests
{
    [Fact]
    public async Task TranslatesExistingCourseDataWithoutChangingIdsAccountsOrEnrollments()
    {
        using var fixture = await OwnershipFixture.CreateAsync();
        await fixture.ReadAsync(async db =>
        {
            var migrator = db.GetService<IMigrator>();
            var migrations = db.Database.GetMigrations().ToArray();
            var previous = migrations[Array.FindIndex(migrations, m => m.EndsWith("_EnglishCourseData")) - 1];
            await migrator.MigrateAsync(previous);
            var course = await db.Courses.SingleAsync(c => c.Id == fixture.CourseAId);
            course.Name = "Programlamaya Giriş";
            course.Description = "Hazır başlangıç dersi: algoritmalar ve programlamanın temelleri.";
            await db.SaveChangesAsync();
            var usersBefore = await db.Users.AsNoTracking().OrderBy(u => u.Id).Select(u => new { u.Id, u.Email, u.PasswordHash }).ToListAsync();
            var enrollmentsBefore = await db.StudentCourses.CountAsync();
            var coursesBefore = await db.Courses.CountAsync();
            await migrator.MigrateAsync();
            db.ChangeTracker.Clear();
            var updated = await db.Courses.SingleAsync(c => c.Id == fixture.CourseAId);
            Assert.Equal("Introduction to Programming", updated.Name);
            Assert.Equal("Introductory course in algorithms and programming fundamentals.", updated.Description);
            Assert.Equal(fixture.TeacherAId, updated.TeacherId);
            Assert.Equal(coursesBefore, await db.Courses.CountAsync());
            Assert.Equal(enrollmentsBefore, await db.StudentCourses.CountAsync());
            Assert.Equal(usersBefore, await db.Users.AsNoTracking().OrderBy(u => u.Id).Select(u => new { u.Id, u.Email, u.PasswordHash }).ToListAsync());
            Assert.Equal("Beta-private-course", (await db.Courses.SingleAsync(c => c.Id == fixture.CourseBId)).Name);
            return true;
        });
    }
}
