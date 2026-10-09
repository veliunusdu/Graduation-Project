using System.Net;
using HomeworkPlatform.Web.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
namespace HomeworkPlatform.Web.Tests;

public class StudentOwnershipTests
{
    [Theory]
    [InlineData("Details")]
    [InlineData("Edit")]
    [InlineData("Delete")]
    public async Task OtherStudentSubmissionIsNotReadable(string action)
    {
        using var fixture = await OwnershipFixture.CreateAsync();
        using var client = await fixture.StudentAsync();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/Submissions/{action}/{fixture.SubmissionAId}")).StatusCode);
        var denied = await client.GetAsync($"/Submissions/{action}/{fixture.SubmissionBId}");
        Assert.Equal(HttpStatusCode.NotFound, denied.StatusCode);
        Assert.DoesNotContain("Bob-private", await denied.Content.ReadAsStringAsync());
    }
    [Theory]
    [InlineData("Edit")]
    [InlineData("Delete")]
    public async Task OtherStudentEditOrDeleteLeavesSubmissionUnchanged(string action)
    {
        using var fixture = await OwnershipFixture.CreateAsync();
        using var client = await fixture.StudentAsync();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/Submissions/Edit/{fixture.SubmissionAId}")).StatusCode);
        var denied = await FormClient.PostAsync(client, $"/Submissions/{action}/{fixture.SubmissionBId}", new() { ["Content"] = "Stolen" }, "/Student");
        Assert.Equal(HttpStatusCode.NotFound, denied.StatusCode);
        Assert.Equal("Bob-private-content", await fixture.ReadAsync(db => db.Submissions.Where(s => s.Id == fixture.SubmissionBId).Select(s => s.Content).SingleAsync()));
    }
    [Fact]
    public async Task OwnSubmissionCrudIgnoresForgedStudentParentAndBodyId()
    {
        using var fixture = await OwnershipFixture.CreateAsync();
        using var client = await fixture.StudentAsync();
        var created = await FormClient.PostAsync(client, $"/Submissions/Create?assignmentId={fixture.FreshAssignmentId}", new() { ["Content"] = "Created text", ["StudentId"] = fixture.StudentBId, ["AssignmentId"] = fixture.AssignmentBId.ToString(), ["Id"] = fixture.SubmissionBId.ToString() });
        Assert.Equal(HttpStatusCode.Redirect, created.StatusCode);
        var submission = await fixture.ReadAsync(db => db.Submissions.SingleAsync(s => s.Content == "Created text"));
        Assert.Equal(fixture.StudentAId, submission.StudentId);
        Assert.Equal(fixture.FreshAssignmentId, submission.AssignmentId);
        var edited = await FormClient.PostAsync(client, $"/Submissions/Edit/{submission.Id}", new() { ["Content"] = "Edited text", ["StudentId"] = fixture.StudentBId, ["AssignmentId"] = fixture.AssignmentBId.ToString(), ["Id"] = fixture.SubmissionBId.ToString() });
        Assert.Equal(HttpStatusCode.Redirect, edited.StatusCode);
        var stored = await fixture.ReadAsync(db => db.Submissions.SingleAsync(s => s.Id == submission.Id));
        Assert.Equal("Edited text", stored.Content);
        Assert.Equal(fixture.StudentAId, stored.StudentId);
        Assert.Equal(fixture.FreshAssignmentId, stored.AssignmentId);
        Assert.Equal(HttpStatusCode.Redirect, (await FormClient.PostAsync(client, $"/Submissions/Delete/{submission.Id}", new())).StatusCode);
        Assert.False(await fixture.ReadAsync(db => db.Submissions.AnyAsync(s => s.Id == submission.Id)));
        Assert.Equal("Bob-private-content", await fixture.ReadAsync(db => db.Submissions.Where(s => s.Id == fixture.SubmissionBId).Select(s => s.Content).SingleAsync()));
    }
    [Fact]
    public async Task ListsAndCreationRequireEnrollmentAndDuplicateDoesNotOverwrite()
    {
        using var fixture = await OwnershipFixture.CreateAsync();
        using var client = await fixture.StudentAsync();
        var list = await client.GetAsync("/Submissions");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.Contains($"/Submissions/Details/{fixture.SubmissionAId}", await list.Content.ReadAsStringAsync());
        Assert.DoesNotContain($"/Submissions/Details/{fixture.SubmissionBId}", await list.Content.ReadAsStringAsync());
        var work = await client.GetAsync("/StudentWork");
        Assert.Equal(HttpStatusCode.OK, work.StatusCode);
        Assert.Contains("Alpha-private-assignment", await work.Content.ReadAsStringAsync());
        Assert.DoesNotContain("Beta-private-assignment", await work.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/StudentWork/Details/{fixture.AssignmentBId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/Submissions/Create?assignmentId={fixture.AssignmentBId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await FormClient.PostAsync(client, $"/Submissions/Create?assignmentId={fixture.AssignmentBId}", new() { ["Content"] = "Unenrolled" }, "/Student")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await FormClient.PostAsync(client, $"/Submissions/Create?assignmentId={fixture.AssignmentAId}", new() { ["Content"] = "Overwrite" }, "/Student")).StatusCode);
        Assert.Equal(2, await fixture.ReadAsync(db => db.Submissions.CountAsync()));
        Assert.Equal("Alice-private-content", await fixture.ReadAsync(db => db.Submissions.Where(s => s.Id == fixture.SubmissionAId).Select(s => s.Content).SingleAsync()));
    }
    [Fact]
    public async Task EnrollmentEnablesStudentWorkAndTeacherReadStaysUnderOwnedCourse()
    {
        using var fixture = await OwnershipFixture.CreateAsync();
        using var teacherB = await fixture.TeacherAsync(other:true);
        using var teacherA = await fixture.TeacherAsync();
        using var student = await fixture.StudentAsync();
        var enrollment = await FormClient.PostAsync(teacherB, $"/Courses/Enroll/{fixture.CourseBId}", new() { ["Email"] = "student-a@example.test", ["StudentId"] = fixture.StudentBId }, $"/Courses/Details/{fixture.CourseBId}");
        Assert.Equal(HttpStatusCode.Redirect, enrollment.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await student.GetAsync($"/StudentWork/Details/{fixture.AssignmentBId}")).StatusCode);
        var created = await FormClient.PostAsync(student, $"/Submissions/Create?assignmentId={fixture.AssignmentBId}", new() { ["Content"] = "Beta submission" });
        Assert.Equal(HttpStatusCode.Redirect, created.StatusCode);
        var id = await fixture.ReadAsync(db => db.Submissions.Where(s => s.Content == "Beta submission").Select(s => s.Id).SingleAsync());
        Assert.Equal(HttpStatusCode.OK, (await teacherB.GetAsync($"/Assignments/Submission/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await teacherA.GetAsync($"/Assignments/Submission/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await teacherA.GetAsync($"/Assignments/Submission/{fixture.SubmissionBId}")).StatusCode);
    }
}
