using System.Net;
using HomeworkPlatform.Web.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
namespace HomeworkPlatform.Web.Tests;
public class Week4AssignmentTests
{
    private static Dictionary<string,string> Fields() => new() {
        ["Title"] = "Week4 assignment", ["Difficulty"] = "2", ["DueDate"] = "2026-12-01",
        ["Requirements[0].Title"] = "Design", ["Requirements[0].Weight"] = "40", ["Requirements[0].Order"] = "1",
        ["Requirements[1].Title"] = "Implement", ["Requirements[1].Weight"] = "60", ["Requirements[1].Order"] = "2",
        ["CreatedByAI"] = "true"
    };
    [Theory]
    [InlineData("Difficulty", "99")]
    [InlineData("DueDate", "")]
    [InlineData("Requirements[0].Weight", "50")]
    [InlineData("Requirements[1].Order", "1")]
    [InlineData("Requirements[0].Title", " ")]
    public async Task InvalidInputDoesNotPersist(string key,string value)
    {
        using var f = await OwnershipFixture.CreateAsync(); using var teacher = await f.TeacherAsync();
        var fields = Fields(); fields[key] = value;
        var result = await FormClient.PostAsync(teacher, $"/Assignments/Create?courseId={f.CourseAId}", fields);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.False(await f.ReadAsync(db => db.Assignments.AnyAsync(a => a.Title == "Week4 assignment")));
    }
    [Fact]
    public async Task CoreFlowAndRequirementReplacementPreserveOwnership()
    {
        using var f = await OwnershipFixture.CreateAsync(); using var teacher = await f.TeacherAsync();
        var result = await FormClient.PostAsync(teacher, $"/Assignments/Create?courseId={f.CourseAId}", Fields());
        Assert.Equal(HttpStatusCode.Redirect,result.StatusCode);
        var a = await f.ReadAsync(db => db.Assignments.Include(a => a.Requirements).SingleAsync(a => a.Title == "Week4 assignment"));
        Assert.False(a.CreatedByAI); Assert.Equal(100,a.Requirements.Sum(r => r.Weight));
        var fields = Fields(); fields.Remove("Requirements[1].Title"); fields.Remove("Requirements[1].Weight"); fields.Remove("Requirements[1].Order"); fields["Requirements[0].Weight"] = "100";
        Assert.Equal(HttpStatusCode.Redirect,(await FormClient.PostAsync(teacher,$"/Assignments/Edit/{a.Id}",fields)).StatusCode);
        Assert.Equal(1,await f.ReadAsync(db => db.AssignmentRequirements.CountAsync(r => r.AssignmentId == a.Id)));
        using var student = await f.StudentAsync();
        Assert.Contains("Design",await (await student.GetAsync($"/StudentWork/Details/{a.Id}")).Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.Redirect,(await FormClient.PostAsync(student,$"/Submissions/Create?assignmentId={a.Id}",new() { ["Content"]="My work" })).StatusCode);
        var submission = await f.ReadAsync(db => db.Submissions.SingleAsync(s => s.AssignmentId == a.Id));
        using var other = await f.TeacherAsync(true);
        var review = new Dictionary<string,string> { ["Feedback"]="Good design", ["Grade"]="85" };
        Assert.Equal(HttpStatusCode.NotFound,(await FormClient.PostAsync(other,$"/Assignments/Review/{submission.Id}",review,"/Teacher")).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect,(await FormClient.PostAsync(teacher,$"/Assignments/Review/{submission.Id}",review,$"/Assignments/Submission/{submission.Id}")).StatusCode);
        Assert.Contains("Good design",await (await student.GetAsync($"/Submissions/Details/{submission.Id}")).Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.Redirect,(await FormClient.PostAsync(student,$"/Submissions/Edit/{submission.Id}",new() { ["Content"]="Revised" })).StatusCode);
        Assert.Null(await f.ReadAsync(db => db.Submissions.Where(s=>s.Id==submission.Id).Select(s=>s.ReviewedAt).SingleAsync()));
    }
    [Fact]
    public async Task EmptyRequirementsAndInvalidReviewAreRejected()
    {
        using var f = await OwnershipFixture.CreateAsync(); using var teacher = await f.TeacherAsync();
        Assert.Equal(HttpStatusCode.OK,(await FormClient.PostAsync(teacher,$"/Assignments/Create?courseId={f.CourseAId}",new() { ["Title"]="Empty", ["DueDate"]="2026-12-01" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await FormClient.PostAsync(teacher,$"/Assignments/Review/{f.SubmissionAId}",new() { ["Feedback"]="Test", ["Grade"]="101" },$"/Assignments/Submission/{f.SubmissionAId}")).StatusCode);
        Assert.Null(await f.ReadAsync(db=>db.Submissions.Where(s=>s.Id==f.SubmissionAId).Select(s=>s.ReviewedAt).SingleAsync()));
    }
}
