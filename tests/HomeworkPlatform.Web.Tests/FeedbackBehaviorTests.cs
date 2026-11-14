using System.Net;
using HomeworkPlatform.Web.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
namespace HomeworkPlatform.Web.Tests;
public class FeedbackBehaviorTests
{
    [Fact]
    public async Task ReviewButtonShowsSavedOnlyAfterValidPersistence()
    {
        using var f=await OwnershipFixture.CreateAsync();using var teacher=await f.TeacherAsync();
        var route=$"/Assignments/Submission/{f.SubmissionAId}";
        var initial=await teacher.GetStringAsync(route);Assert.Contains("Grade (0-100)",initial);Assert.Contains("data-saved=\"false\"",initial);Assert.Contains("Save grade and feedback",initial);
        Assert.Equal(HttpStatusCode.Redirect,(await FormClient.PostAsync(teacher,$"/Assignments/Review/{f.SubmissionAId}",new(){["Grade"]="95",["Feedback"]="Clear and correct solution"},route)).StatusCode);
        var saved=await teacher.GetStringAsync(route);Assert.Contains("data-saved=\"true\"",saved);Assert.Contains(">Saved</button>",saved);
        var invalid=await FormClient.PostAsync(teacher,$"/Assignments/Review/{f.SubmissionAId}",new(){["Grade"]="101",["Feedback"]="Invalid score"},route);
        Assert.Equal(HttpStatusCode.OK,invalid.StatusCode);var invalidHtml=await invalid.Content.ReadAsStringAsync();Assert.Contains("data-saved=\"false\"",invalidHtml);Assert.DoesNotContain(">Saved</button>",invalidHtml);
        Assert.Equal(95,await f.ReadAsync(db=>db.Submissions.Where(s=>s.Id==f.SubmissionAId).Select(s=>s.TeacherGrade).SingleAsync()));
    }
    [Fact]
    public async Task SavedSubmissionHasWithdrawActionAndConfirmationReturnsToResubmission()
    {
        using var f=await OwnershipFixture.CreateAsync();using var student=await f.StudentAsync();using var other=await f.StudentAsync(true);
        var route=$"/Submissions/Details/{f.SubmissionAId}";
        Assert.Contains("Withdraw submission",await student.GetStringAsync(route));Assert.Contains("Withdraw submission",await student.GetStringAsync($"/StudentWork/Details/{f.AssignmentAId}"));
        var create=await student.GetAsync($"/Submissions/Create?assignmentId={f.AssignmentAId}");Assert.Equal(HttpStatusCode.Redirect,create.StatusCode);Assert.Equal(route,create.Headers.Location!.OriginalString);
        var confirmation=await student.GetStringAsync($"/Submissions/Delete/{f.SubmissionAId}");Assert.Contains("Keep submission",confirmation);Assert.Contains("__RequestVerificationToken",confirmation);
        Assert.Equal(HttpStatusCode.BadRequest,(await student.PostAsync($"/Submissions/Delete/{f.SubmissionAId}",new FormUrlEncodedContent(new Dictionary<string,string>()))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await FormClient.PostAsync(other,$"/Submissions/Delete/{f.SubmissionAId}",new(),"/Student")).StatusCode);
        var withdrawn=await FormClient.PostAsync(student,$"/Submissions/Delete/{f.SubmissionAId}",new());Assert.Equal(HttpStatusCode.Redirect,withdrawn.StatusCode);Assert.Equal($"/StudentWork/Details/{f.AssignmentAId}",withdrawn.Headers.Location!.OriginalString);
        Assert.False(await f.ReadAsync(db=>db.Submissions.AnyAsync(s=>s.Id==f.SubmissionAId)));Assert.Contains("Create submission",await student.GetStringAsync($"/StudentWork/Details/{f.AssignmentAId}"));Assert.Equal(HttpStatusCode.OK,(await student.GetAsync($"/Submissions/Create?assignmentId={f.AssignmentAId}")).StatusCode);
    }
    [Fact]
    public async Task SecondaryPagesHaveStyledBackLinksToTheirOwnPanel()
    {
        using var f=await OwnershipFixture.CreateAsync();using var teacher=await f.TeacherAsync();using var student=await f.StudentAsync();
        foreach(var route in new[]{"/Courses","/Courses/Create",$"/Courses/Details/{f.CourseAId}",$"/Courses/Edit/{f.CourseAId}",$"/Assignments/Create?courseId={f.CourseAId}",$"/Assignments/Edit/{f.AssignmentAId}",$"/Assignments/Submissions/{f.AssignmentAId}",$"/Assignments/Submission/{f.SubmissionAId}","/Teacher/Submissions","/Teacher/Search?q=private",$"/CourseMaterials/Index?courseId={f.CourseAId}"})
            Assert.Contains("aria-label=\"Back navigation\"",await teacher.GetStringAsync(route));
        foreach(var route in new[]{"/StudentCourses",$"/StudentCourses/Details/{f.CourseAId}","/StudentWork",$"/StudentWork/Details/{f.AssignmentAId}","/Submissions",$"/Submissions/Details/{f.SubmissionAId}",$"/Submissions/Edit/{f.SubmissionAId}",$"/Submissions/Delete/{f.SubmissionAId}",$"/Submissions/Create?assignmentId={f.FreshAssignmentId}","/Student/Search?q=private",$"/CourseMaterials/Index?courseId={f.CourseAId}"})
            Assert.Contains("aria-label=\"Back navigation\"",await student.GetStringAsync(route));
        Assert.DoesNotContain("aria-label=\"Back navigation\"",await teacher.GetStringAsync("/Teacher"));Assert.DoesNotContain("aria-label=\"Back navigation\"",await student.GetStringAsync("/Student"));
        var details=await teacher.GetStringAsync($"/Assignments/Details/{f.AssignmentAId}");Assert.Contains("ep-danger-link",details);Assert.Contains("ep-back-link",details);
    }
}
