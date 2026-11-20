using System.Net;
using HomeworkPlatform.Web.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
namespace HomeworkPlatform.Web.Tests;
public class Week4AssignmentTests
{
    private static Dictionary<string,string> Fields() => new() {
        ["Title"] = "Assignment with requirements", ["Description"] = "Design and implement the solution.", ["DueDate"] = "2026-12-01",
        ["Difficulty"] = "99", ["CreatedByAI"] = "true", ["Requirements[0].Weight"] = "999"
    };
    [Theory]
    [InlineData("Title", " ")]
    [InlineData("DueDate", "")]
    public async Task InvalidInputDoesNotPersist(string key,string value)
    {
        using var f=await OwnershipFixture.CreateAsync();using var teacher=await f.TeacherAsync();
        var fields=Fields();fields[key]=value;
        var result=await FormClient.PostAsync(teacher,$"/Assignments/Create?courseId={f.CourseAId}",fields);
        Assert.Equal(HttpStatusCode.OK,result.StatusCode);
        Assert.False(await f.ReadAsync(db=>db.Assignments.AnyAsync(a=>a.Title=="Assignment with requirements")));
    }
    [Fact]
    public async Task SimplifiedFormSavesRequirementsAndReviewFlowPreservesOwnership()
    {
        using var f=await OwnershipFixture.CreateAsync();using var teacher=await f.TeacherAsync();
        var route=$"/Assignments/Create?courseId={f.CourseAId}";
        var html=await teacher.GetStringAsync(route);
        Assert.Contains(">Requirements</label>",html);
        Assert.DoesNotContain("name=\"Difficulty\"",html);
        Assert.DoesNotContain("Requirements[0]",html);
        Assert.Contains("multipart/form-data",html);
        Assert.Equal(HttpStatusCode.Redirect,(await FormClient.PostAsync(teacher,route,Fields())).StatusCode);
        var a=await f.ReadAsync(db=>db.Assignments.Include(a=>a.Requirements).SingleAsync(a=>a.Title=="Assignment with requirements"));
        Assert.False(a.CreatedByAI);Assert.Empty(a.Requirements);Assert.Equal("Design and implement the solution.",a.Description);
        var fields=Fields();fields["Description"]="Updated requirements";
        Assert.Equal(HttpStatusCode.Redirect,(await FormClient.PostAsync(teacher,$"/Assignments/Edit/{a.Id}",fields)).StatusCode);
        using var student=await f.StudentAsync();
        Assert.Contains("Updated requirements",await student.GetStringAsync($"/StudentWork/Details/{a.Id}"));
        Assert.Equal(HttpStatusCode.Redirect,(await FormClient.PostAsync(student,$"/Submissions/Create?assignmentId={a.Id}",new(){["Content"]="My work"})).StatusCode);
        var submission=await f.ReadAsync(db=>db.Submissions.SingleAsync(s=>s.AssignmentId==a.Id));
        using var other=await f.TeacherAsync(true);
        var review=new Dictionary<string,string>{["Feedback"]="Good design",["Grade"]="85"};
        Assert.Equal(HttpStatusCode.NotFound,(await FormClient.PostAsync(other,$"/Assignments/Review/{submission.Id}",review,"/Teacher")).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect,(await FormClient.PostAsync(teacher,$"/Assignments/Review/{submission.Id}",review,$"/Assignments/Submission/{submission.Id}")).StatusCode);
        Assert.Contains("Good design",await student.GetStringAsync($"/Submissions/Details/{submission.Id}"));
        Assert.Equal(HttpStatusCode.Redirect,(await FormClient.PostAsync(student,$"/Submissions/Edit/{submission.Id}",new(){["Content"]="Revised"})).StatusCode);
        Assert.Null(await f.ReadAsync(db=>db.Submissions.Where(s=>s.Id==submission.Id).Select(s=>s.ReviewedAt).SingleAsync()));
    }
    [Fact]
    public async Task InvalidReviewStillDoesNotPersist()
    {
        using var f=await OwnershipFixture.CreateAsync();using var teacher=await f.TeacherAsync();
        Assert.Equal(HttpStatusCode.OK,(await FormClient.PostAsync(teacher,$"/Assignments/Review/{f.SubmissionAId}",new(){["Feedback"]="Test",["Grade"]="101"},$"/Assignments/Submission/{f.SubmissionAId}")).StatusCode);
        Assert.Null(await f.ReadAsync(db=>db.Submissions.Where(s=>s.Id==f.SubmissionAId).Select(s=>s.ReviewedAt).SingleAsync()));
    }
}
