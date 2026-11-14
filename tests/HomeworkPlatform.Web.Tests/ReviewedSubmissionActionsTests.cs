using System.Net;
using System.Text.RegularExpressions;
using HomeworkPlatform.Web.Tests.Infrastructure;
namespace HomeworkPlatform.Web.Tests;
public class ReviewedSubmissionActionsTests
{
    private static string Actions(string html) => Regex.Match(html,"<div class=\"ep-student-actions\">[\\s\\S]*?</div>").Value;
    [Fact]
    public async Task ReviewedSubmissionOnlyOffersViewWhileUnreviewedWorkKeepsEditingAndWithdrawal()
    {
        using var f=await OwnershipFixture.CreateAsync();using var student=await f.StudentAsync();using var teacher=await f.TeacherAsync();
        var route=$"/Submissions/Details/{f.SubmissionAId}";
        var pending=Actions(await student.GetStringAsync(route));Assert.Contains("Edit submission",pending);Assert.Contains("Withdraw submission",pending);
        var reviewed=await FormClient.PostAsync(teacher,$"/Assignments/Review/{f.SubmissionAId}",new(){["Grade"]="95",["Feedback"]="Reviewed and complete."},$"/Assignments/Submission/{f.SubmissionAId}");Assert.Equal(HttpStatusCode.Redirect,reviewed.StatusCode);
        foreach(var path in new[]{route,"/Submissions"})
        {
            var html=await student.GetStringAsync(path);Assert.Contains("Teacher feedback",html);Assert.Contains("Reviewed and complete.",html);
            var actions=Actions(html);Assert.Contains("View submission",actions);Assert.DoesNotContain("Edit submission",actions);Assert.DoesNotContain("Withdraw submission",actions);Assert.Equal(1,Regex.Matches(actions,"<a ").Count);
        }
    }
}
