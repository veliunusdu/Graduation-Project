using System.Net;
using System.Text.RegularExpressions;
using HomeworkPlatform.Web.Data;
using HomeworkPlatform.Web.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HomeworkPlatform.Web.Tests;

public class CourseRosterTests
{
    [Fact]
    public async Task DetailsShowRealCourseCountAndOnlyUnenrolledStudentsInSelector()
    {
        using var app = new AuthWebApplicationFactory { EnableInitialData = true };
        using var teacher = app.NewClient();
        await FormClient.LoginAsync(teacher, "teacher@edupilot.local", "Teacher-Start8!");
        int id;
        using (var scope = app.Services.CreateScope()) id = await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Courses.Where(c => c.Name == "Mathematics").Select(c => c.Id).SingleAsync();
        var html = WebUtility.HtmlDecode(await teacher.GetStringAsync("/Courses/Details/" + id));
        var select = Regex.Match(html, "<select[^>]*id=\"Email\"[\\s\\S]*?</select>").Value;
        var roster = Regex.Match(html, "<table[^>]*id=\"enrolled-students\"[\\s\\S]*?</table>").Value;
        Assert.Contains("data-count=\"1\"", html);
        Assert.Contains("data-count=\"2\"", html);
        Assert.Contains("Elif Aydın", roster);
        Assert.Contains("student3@edupilot.local", roster);
        Assert.DoesNotContain("student3@edupilot.local", select);
        Assert.Contains("student1@edupilot.local", select);
        Assert.Contains("student2@edupilot.local", select);
        Assert.Equal(2, Regex.Matches(select, "<option value=\"student").Count);
    }

    [Fact]
    public async Task EnrollmentUpdatesCountRemovesTheStudentFromChoicesAndIsIdempotent()
    {
        using var app = new AuthWebApplicationFactory { EnableInitialData = true };
        using var teacher = app.NewClient();
        await FormClient.LoginAsync(teacher, "teacher@edupilot.local", "Teacher-Start8!");
        int id;
        using (var scope = app.Services.CreateScope()) id = await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Courses.Where(c => c.Name == "Mathematics").Select(c => c.Id).SingleAsync();
        var route = "/Courses/Details/" + id;
        using var added = await FormClient.PostAsync(teacher, "/Courses/Enroll/" + id, new() { ["Email"] = "student1@edupilot.local" }, route);
        Assert.Equal(HttpStatusCode.Redirect, added.StatusCode);
        var html = await teacher.GetStringAsync(route);
        Assert.Contains("id=\"enrolled-count\" data-count=\"2\"", html);
        var select = Regex.Match(html, "<select[^>]*id=\"Email\"[\\s\\S]*?</select>").Value;
        Assert.DoesNotContain("student1@edupilot.local", select);
        Assert.Contains("student2@edupilot.local", select);
        using var duplicate = await FormClient.PostAsync(teacher, "/Courses/Enroll/" + id, new() { ["Email"] = "student1@edupilot.local" }, route);
        Assert.Equal(HttpStatusCode.Redirect, duplicate.StatusCode);
        Assert.Contains("already enrolled", await teacher.GetStringAsync(route));
        using var check = app.Services.CreateScope();
        Assert.Equal(2, await check.ServiceProvider.GetRequiredService<ApplicationDbContext>().StudentCourses.CountAsync(e => e.CourseId == id));
        foreach (var page in new[] { "/Teacher", "/Courses" })
            Assert.Contains($"data-course-id=\"{id}\" data-enrolled-count=\"2\"", await teacher.GetStringAsync(page));
    }

    [Fact]
    public async Task FullyEnrolledCourseHasNoAddSelectorAndAnAccurateRoster()
    {
        using var fixture = await OwnershipFixture.CreateAsync();
        using var teacher = await fixture.TeacherAsync();
        var html = await teacher.GetStringAsync("/Courses/Details/" + fixture.CourseAId);
        Assert.Contains("No additional students are available", html);
        Assert.DoesNotContain("id=\"enroll-form\"", html);
        Assert.Contains("id=\"enrolled-count\" data-count=\"2\"", html);
        using var response = await FormClient.PostAsync(teacher, "/Courses/Enroll/" + fixture.CourseAId, new() { ["Email"] = "student-a@example.test" }, "/Courses/Details/" + fixture.CourseAId);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(2, await fixture.ReadAsync(db => db.StudentCourses.CountAsync(e => e.CourseId == fixture.CourseAId)));
    }

    [Fact]
    public async Task EmptyCourseShowsZeroStudentsAndAllEligibleChoices()
    {
        using var fixture = await OwnershipFixture.CreateAsync();
        using var teacher = await fixture.TeacherAsync(other: true);
        var html = await teacher.GetStringAsync("/Courses/Details/" + fixture.CourseBId);
        Assert.Contains("id=\"enrolled-count\" data-count=\"0\"", html);
        Assert.Contains("No students enrolled yet", html);
        Assert.Contains("student-a@example.test", html);
        Assert.Contains("student-b@example.test", html);
    }
}
