using System.Net;
using HomeworkPlatform.Web.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
namespace HomeworkPlatform.Web.Tests;

public class AuthorizationMatrixTests
{
    [Theory]
    [InlineData("/Courses")]
    [InlineData("/Assignments")]
    [InlineData("/StudentWork")]
    [InlineData("/Submissions")]
    public async Task AnonymousResourceRequestsLeadToLogin(string route)
    {
        using var app = new AuthWebApplicationFactory();
        using var client = app.NewClient();
        var result = await client.GetAsync(route);
        Assert.Equal(HttpStatusCode.Redirect, result.StatusCode);
        Assert.StartsWith("/Account/Login", result.Headers.Location?.AbsolutePath);
    }
    [Theory]
    [InlineData(true, "/StudentWork")]
    [InlineData(true, "/Submissions")]
    [InlineData(false, "/Courses")]
    [InlineData(false, "/Assignments")]
    public async Task WrongRoleCannotReadResourceLists(bool teacher, string route)
    {
        using var fixture = await OwnershipFixture.CreateAsync();
        using var client = teacher ? await fixture.TeacherAsync() : await fixture.StudentAsync();
        var denied = await client.GetAsync(route);
        Assert.Equal(HttpStatusCode.Redirect, denied.StatusCode);
        Assert.StartsWith("/Account/AccessDenied", denied.Headers.Location?.AbsolutePath);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(denied.Headers.Location)).StatusCode);
    }
    [Theory]
    [InlineData("Courses", "Create")]
    [InlineData("Courses", "Edit")]
    [InlineData("Courses", "Delete")]
    [InlineData("Courses", "Enroll")]
    [InlineData("Assignments", "Create")]
    [InlineData("Assignments", "Edit")]
    [InlineData("Assignments", "Delete")]
    [InlineData("Submissions", "Create")]
    [InlineData("Submissions", "Edit")]
    [InlineData("Submissions", "Delete")]
    public async Task ResourcePostWithoutTokenIsRejectedWithoutMutation(string controller, string action)
    {
        using var fixture = await OwnershipFixture.CreateAsync();
        using var client = controller == "Submissions" ? await fixture.StudentAsync() : await fixture.TeacherAsync();
        var id = controller == "Courses" ? fixture.CourseAId : controller == "Assignments" ? fixture.AssignmentAId : fixture.SubmissionAId;
        var route = action == "Create" ? $"/{controller}/Create?courseId={fixture.CourseAId}&assignmentId={fixture.FreshAssignmentId}" : $"/{controller}/{action}/{id}";
        var denied = await client.PostAsync(route, new FormUrlEncodedContent(new Dictionary<string,string> { ["Name"] = "Changed", ["Title"] = "Changed", ["Content"] = "Changed", ["Email"] = "student-a@example.test" }));
        Assert.Equal(HttpStatusCode.BadRequest, denied.StatusCode);
        Assert.Equal("Alpha-private-course", await fixture.ReadAsync(db => db.Courses.Where(c => c.Id == fixture.CourseAId).Select(c => c.Name).SingleAsync()));
        Assert.Equal("Alice-private-content", await fixture.ReadAsync(db => db.Submissions.Where(s => s.Id == fixture.SubmissionAId).Select(s => s.Content).SingleAsync()));
        Assert.Equal(2, await fixture.ReadAsync(db => db.Submissions.CountAsync()));
    }
    [Theory]
    [InlineData("Courses")]
    [InlineData("Assignments")]
    [InlineData("Submissions")]
    public async Task ForeignInvalidBodyAndForgedRouteTargetRemainDenied(string controller)
    {
        using var fixture = await OwnershipFixture.CreateAsync();
        using var client = controller == "Submissions" ? await fixture.StudentAsync() : await fixture.TeacherAsync();
        var foreign = controller == "Courses" ? fixture.CourseBId : controller == "Assignments" ? fixture.AssignmentBId : fixture.SubmissionBId;
        var own = controller == "Courses" ? fixture.CourseAId : controller == "Assignments" ? fixture.AssignmentAId : fixture.SubmissionAId;
        var tokenPage = controller == "Submissions" ? "/Student" : "/Teacher";
        Assert.Equal(HttpStatusCode.NotFound, (await FormClient.PostAsync(client, $"/{controller}/Edit/{foreign}", new(), tokenPage)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await FormClient.PostAsync(client, $"/{controller}/Edit/{foreign}", new() { ["Id"] = own.ToString(), ["Name"] = "Intrusion", ["Title"] = "Intrusion", ["Content"] = "Intrusion" }, tokenPage)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/{controller}/Details/2147483647")).StatusCode);
        Assert.Equal("Alpha-private-course", await fixture.ReadAsync(db => db.Courses.Where(c => c.Id == fixture.CourseAId).Select(c => c.Name).SingleAsync()));
        Assert.Equal("Beta-private-assignment", await fixture.ReadAsync(db => db.Assignments.Where(a => a.Id == fixture.AssignmentBId).Select(a => a.Title).SingleAsync()));
        Assert.Equal("Alice-private-content", await fixture.ReadAsync(db => db.Submissions.Where(s => s.Id == fixture.SubmissionAId).Select(s => s.Content).SingleAsync()));
        Assert.Equal("Bob-private-content", await fixture.ReadAsync(db => db.Submissions.Where(s => s.Id == fixture.SubmissionBId).Select(s => s.Content).SingleAsync()));
    }
    [Theory]
    [InlineData("Courses")]
    [InlineData("Assignments")]
    [InlineData("Submissions")]
    public async Task GetDeleteOnlyShowsConfirmation(string controller)
    {
        using var fixture = await OwnershipFixture.CreateAsync();
        using var client = controller == "Submissions" ? await fixture.StudentAsync() : await fixture.TeacherAsync();
        var id = controller == "Courses" ? fixture.CourseAId : controller == "Assignments" ? fixture.AssignmentAId : fixture.SubmissionAId;
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/{controller}/Delete/{id}")).StatusCode);
        Assert.Equal(2, await fixture.ReadAsync(db => db.Courses.CountAsync()));
        Assert.Equal(3, await fixture.ReadAsync(db => db.Assignments.CountAsync()));
        Assert.Equal(2, await fixture.ReadAsync(db => db.Submissions.CountAsync()));
    }
    [Theory]
    [InlineData(true, "Submissions", "Create")]
    [InlineData(true, "Submissions", "Edit")]
    [InlineData(true, "Submissions", "Delete")]
    [InlineData(false, "Courses", "Create")]
    [InlineData(false, "Courses", "Edit")]
    [InlineData(false, "Courses", "Delete")]
    [InlineData(false, "Courses", "Enroll")]
    [InlineData(false, "Assignments", "Create")]
    [InlineData(false, "Assignments", "Edit")]
    [InlineData(false, "Assignments", "Delete")]
    public async Task WrongRoleCannotMutateResourcesEvenWithValidToken(bool teacher, string controller, string action)
    {
        using var fixture = await OwnershipFixture.CreateAsync();
        using var client = teacher ? await fixture.TeacherAsync() : await fixture.StudentAsync();
        var id = controller == "Courses" ? fixture.CourseAId : controller == "Assignments" ? fixture.AssignmentAId : fixture.SubmissionAId;
        var route = action == "Create" ? $"/{controller}/Create?courseId={fixture.CourseAId}&assignmentId={fixture.FreshAssignmentId}" : $"/{controller}/{action}/{id}";
        var denied = await FormClient.PostAsync(client, route, new() { ["Name"] = "Changed", ["Title"] = "Changed", ["Content"] = "Changed", ["Email"] = "student-a@example.test" }, teacher ? "/Teacher" : "/Student");
        Assert.Equal(HttpStatusCode.Redirect, denied.StatusCode);
        Assert.StartsWith("/Account/AccessDenied", denied.Headers.Location?.AbsolutePath);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(denied.Headers.Location)).StatusCode);
        Assert.Equal("Alpha-private-course", await fixture.ReadAsync(db => db.Courses.Where(c => c.Id == fixture.CourseAId).Select(c => c.Name).SingleAsync()));
        Assert.Equal("Alpha-private-assignment", await fixture.ReadAsync(db => db.Assignments.Where(a => a.Id == fixture.AssignmentAId).Select(a => a.Title).SingleAsync()));
        Assert.Equal("Alice-private-content", await fixture.ReadAsync(db => db.Submissions.Where(s => s.Id == fixture.SubmissionAId).Select(s => s.Content).SingleAsync()));
        Assert.Equal(2, await fixture.ReadAsync(db => db.Submissions.CountAsync()));
    }
    [Fact]
    public async Task EnrollmentCannotTargetForeignCourseOrNonStudentAndDuplicateIsSafe()
    {
        using var fixture = await OwnershipFixture.CreateAsync();
        using var teacher = await fixture.TeacherAsync();
        Assert.Equal(HttpStatusCode.NotFound, (await FormClient.PostAsync(teacher, $"/Courses/Enroll/{fixture.CourseBId}", new() { ["Email"] = "student-a@example.test" }, "/Teacher")).StatusCode);
        foreach (var email in new[] { "teacher-b@example.test", "missing@example.test" })
            Assert.Equal(HttpStatusCode.OK, (await FormClient.PostAsync(teacher, $"/Courses/Enroll/{fixture.CourseAId}", new() { ["Email"] = email }, $"/Courses/Details/{fixture.CourseAId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await FormClient.PostAsync(teacher, $"/Courses/Enroll/{fixture.CourseAId}", new() { ["Email"] = "student-a@example.test", ["Id"] = fixture.CourseBId.ToString() }, $"/Courses/Details/{fixture.CourseAId}")).StatusCode);
        Assert.Equal(2, await fixture.ReadAsync(db => db.StudentCourses.CountAsync()));
        Assert.False(await fixture.ReadAsync(db => db.StudentCourses.AnyAsync(e => e.CourseId == fixture.CourseBId)));
    }
    [Fact]
    public async Task SubmissionContentIsEncodedForBothStudentAndTeacher()
    {
        using var fixture = await OwnershipFixture.CreateAsync();
        using var student = await fixture.StudentAsync();
        using var teacher = await fixture.TeacherAsync();
        const string content = "<script>alert('test-only')</script>";
        Assert.Equal(HttpStatusCode.Redirect, (await FormClient.PostAsync(student, $"/Submissions/Edit/{fixture.SubmissionAId}", new() { ["Content"] = content })).StatusCode);
        foreach (var response in new[] { await student.GetAsync($"/Submissions/Details/{fixture.SubmissionAId}"), await teacher.GetAsync($"/Assignments/Submission/{fixture.SubmissionAId}") })
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var html = await response.Content.ReadAsStringAsync();
            Assert.DoesNotContain(content, html);
            Assert.Contains("&lt;script&gt;", html);
        }
        Assert.Equal(content, await fixture.ReadAsync(db => db.Submissions.Where(s => s.Id == fixture.SubmissionAId).Select(s => s.Content).SingleAsync()));
    }
    [Fact]
    public async Task InvalidResourceInputDoesNotCreateOrChangeRecords()
    {
        using var fixture = await OwnershipFixture.CreateAsync();
        using var teacher = await fixture.TeacherAsync();
        using var student = await fixture.StudentAsync();
        Assert.Equal(HttpStatusCode.OK, (await FormClient.PostAsync(teacher, "/Courses/Create", new() { ["Name"] = new string('x',121) })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await FormClient.PostAsync(teacher, $"/Assignments/Edit/{fixture.AssignmentAId}", new() { ["Title"] = "" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await FormClient.PostAsync(student, $"/Submissions/Create?assignmentId={fixture.FreshAssignmentId}", new() { ["Content"] = new string('x',100001) })).StatusCode);
        Assert.Equal(2, await fixture.ReadAsync(db => db.Courses.CountAsync()));
        Assert.Equal("Alpha-private-assignment", await fixture.ReadAsync(db => db.Assignments.Where(a => a.Id == fixture.AssignmentAId).Select(a => a.Title).SingleAsync()));
        Assert.Equal(2, await fixture.ReadAsync(db => db.Submissions.CountAsync()));
    }
}
