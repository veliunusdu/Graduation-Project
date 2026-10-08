using System.Net;
using HomeworkPlatform.Web.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
namespace HomeworkPlatform.Web.Tests;

public class TeacherOwnershipTests
{
    [Theory]
    [InlineData("Courses", "Details")]
    [InlineData("Courses", "Edit")]
    [InlineData("Courses", "Delete")]
    [InlineData("Assignments", "Details")]
    [InlineData("Assignments", "Edit")]
    [InlineData("Assignments", "Delete")]
    [InlineData("Assignments", "Submissions")]
    public async Task ForeignTeacherRecordIsNotReadable(string controller, string action)
    {
        using var fixture = await OwnershipFixture.CreateAsync();
        using var client = await fixture.TeacherAsync();
        var foreign = controller == "Courses" ? fixture.CourseBId : fixture.AssignmentBId;
        var own = controller == "Courses" ? fixture.CourseAId : fixture.AssignmentAId;
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/{controller}/{action}/{own}")).StatusCode);
        var response = await client.GetAsync($"/{controller}/{action}/{foreign}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.DoesNotContain("Beta-private", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("Courses", "Edit")]
    [InlineData("Courses", "Delete")]
    [InlineData("Assignments", "Edit")]
    [InlineData("Assignments", "Delete")]
    public async Task ForeignTeacherMutationDoesNotChangeData(string controller, string action)
    {
        using var fixture = await OwnershipFixture.CreateAsync();
        using var client = await fixture.TeacherAsync();
        var foreign = controller == "Courses" ? fixture.CourseBId : fixture.AssignmentBId;
        var own = controller == "Courses" ? fixture.CourseAId : fixture.AssignmentAId;
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/{controller}/Edit/{own}")).StatusCode);
        var result = await FormClient.PostAsync(client, $"/{controller}/{action}/{foreign}", new() { ["Name"] = "Stolen", ["Title"] = "Stolen" }, "/Teacher");
        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
        Assert.Equal("Beta-private-course", await fixture.ReadAsync(db => db.Courses.Where(c => c.Id == fixture.CourseBId).Select(c => c.Name).SingleAsync()));
        Assert.Equal("Beta-private-assignment", await fixture.ReadAsync(db => db.Assignments.Where(a => a.Id == fixture.AssignmentBId).Select(a => a.Title).SingleAsync()));
    }

    [Fact]
    public async Task OwnCrudIgnoresForgedOwnerParentAndBodyId()
    {
        using var fixture = await OwnershipFixture.CreateAsync();
        using var client = await fixture.TeacherAsync();
        var created = await FormClient.PostAsync(client, "/Courses/Create", new() { ["Name"] = "Created course", ["Description"] = "Local", ["TeacherId"] = fixture.TeacherBId, ["Id"] = fixture.CourseBId.ToString() });
        Assert.Equal(HttpStatusCode.Redirect, created.StatusCode);
        var course = await fixture.ReadAsync(db => db.Courses.SingleAsync(c => c.Name == "Created course"));
        Assert.Equal(fixture.TeacherAId, course.TeacherId);
        var edited = await FormClient.PostAsync(client, $"/Courses/Edit/{course.Id}", new() { ["Name"] = "Edited course", ["TeacherId"] = fixture.TeacherBId, ["Id"] = fixture.CourseBId.ToString() });
        Assert.Equal(HttpStatusCode.Redirect, edited.StatusCode);
        Assert.Equal(fixture.TeacherAId, await fixture.ReadAsync(db => db.Courses.Where(c => c.Id == course.Id).Select(c => c.TeacherId).SingleAsync()));
        var assignment = await FormClient.PostAsync(client, $"/Assignments/Create?courseId={course.Id}", new() { ["Title"] = "Created assignment" });
        Assert.Equal(HttpStatusCode.Redirect, assignment.StatusCode);
        var assignmentId = await fixture.ReadAsync(db => db.Assignments.Where(a => a.Title == "Created assignment").Select(a => a.Id).SingleAsync());
        var update = await FormClient.PostAsync(client, $"/Assignments/Edit/{assignmentId}", new() { ["Title"] = "Edited assignment", ["CourseId"] = fixture.CourseBId.ToString(), ["Id"] = fixture.AssignmentBId.ToString() });
        Assert.Equal(HttpStatusCode.Redirect, update.StatusCode);
        Assert.Equal(course.Id, await fixture.ReadAsync(db => db.Assignments.Where(a => a.Id == assignmentId).Select(a => a.CourseId).SingleAsync()));
        Assert.Equal(HttpStatusCode.Redirect, (await FormClient.PostAsync(client, $"/Assignments/Delete/{assignmentId}", new())).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await FormClient.PostAsync(client, $"/Courses/Delete/{course.Id}", new())).StatusCode);
        Assert.False(await fixture.ReadAsync(db => db.Courses.AnyAsync(c => c.Id == course.Id)));
        Assert.Equal("Beta-private-course", await fixture.ReadAsync(db => db.Courses.Where(c => c.Id == fixture.CourseBId).Select(c => c.Name).SingleAsync()));
    }

    [Fact]
    public async Task ListsAndAssignmentCreationStayUnderOwnedCourses()
    {
        using var fixture = await OwnershipFixture.CreateAsync();
        using var client = await fixture.TeacherAsync();
        var courses = await client.GetAsync("/Courses");
        Assert.Equal(HttpStatusCode.OK, courses.StatusCode);
        Assert.Contains("Alpha-private-course", await courses.Content.ReadAsStringAsync());
        Assert.DoesNotContain("Beta-private-course", await courses.Content.ReadAsStringAsync());
        var assignments = await client.GetAsync("/Assignments");
        Assert.Equal(HttpStatusCode.OK, assignments.StatusCode);
        Assert.DoesNotContain("Beta-private-assignment", await assignments.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/Assignments?courseId={fixture.CourseBId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/Assignments/Create?courseId={fixture.CourseBId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await FormClient.PostAsync(client, $"/Assignments/Create?courseId={fixture.CourseBId}", new() { ["Title"] = "Intrusion" }, "/Teacher")).StatusCode);
        Assert.False(await fixture.ReadAsync(db => db.Assignments.AnyAsync(a => a.Title == "Intrusion")));
    }

    [Fact]
    public async Task TeacherCannotDeleteParentsContainingStudentWork()
    {
        using var fixture = await OwnershipFixture.CreateAsync();
        using var client = await fixture.TeacherAsync();
        Assert.Equal(HttpStatusCode.Conflict, (await FormClient.PostAsync(client, $"/Courses/Delete/{fixture.CourseAId}", new())).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await FormClient.PostAsync(client, $"/Assignments/Delete/{fixture.AssignmentAId}", new())).StatusCode);
        Assert.Equal(2, await fixture.ReadAsync(db => db.Submissions.CountAsync()));
    }
}
