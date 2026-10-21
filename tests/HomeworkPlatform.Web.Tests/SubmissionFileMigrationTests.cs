using HomeworkPlatform.Web.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HomeworkPlatform.Web.Tests;

public class SubmissionFileMigrationTests
{
    [Fact]
    public async Task UpgradeFromWeek4PreservesSubmissionAndTeacherReview()
    {
        using var fixture = await OwnershipFixture.CreateAsync();
        await fixture.ReadAsync(async db =>
        {
            var migrator = db.GetService<IMigrator>();
            await migrator.MigrateAsync("20261014170338_TeacherReview");
            await db.Database.ExecuteSqlRawAsync("UPDATE Submissions SET TeacherFeedback = 'Existing review', TeacherGrade = 90 WHERE Id = {0}", fixture.SubmissionAId);
            await migrator.MigrateAsync();
            var submission = await db.Submissions.AsNoTracking().SingleAsync(s => s.Id == fixture.SubmissionAId);
            Assert.Equal("Alice-private-content", submission.Content);
            Assert.Equal("Existing review", submission.TeacherFeedback); Assert.Equal(90, submission.TeacherGrade);
            Assert.Null(submission.StoredFileName);
            return true;
        });
    }
}
