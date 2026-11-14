using System.Net;
using HomeworkPlatform.Web.Tests.Infrastructure;
namespace HomeworkPlatform.Web.Tests;
public class TeacherPanelTests
{
    [Fact]
    public async Task ShellKeepsLogoutAndAIConnectionAndDoesNotChangeStudentOrEntryPages()
    {
        using var app=new AuthWebApplicationFactory{EnableInitialData=true};
        using var teacher=app.NewClient();
        await FormClient.LoginAsync(teacher,"teacher@edupilot.local","Teacher-Start8!");
        foreach(var route in new[]{"/Teacher","/Courses","/Assignments","/Teacher/Submissions","/Teacher/Search?q=programming"})
        {
            var html=await teacher.GetStringAsync(route);
            Assert.Contains("id=\"teacher-sidebar\"",html);
            Assert.Contains("Deniz Kaya",html);
            Assert.Contains("Check AI service connection",html);
            Assert.Contains("action=\"/Account/Logout\"",html);
            Assert.Contains("__RequestVerificationToken",html);
            Assert.DoesNotContain("/Profile/Edit",html);
            Assert.DoesNotContain("/AiAgent/",html);
        }
        var entry=await teacher.GetStringAsync("/");
        Assert.Contains("onboardCarousel",entry);
        Assert.DoesNotContain("teacher-sidebar",entry);
        using var student=app.NewClient();
        await FormClient.LoginAsync(student,"student1@edupilot.local","Student-Start8!");
        Assert.DoesNotContain("teacher-sidebar",await student.GetStringAsync("/Student"));
        using var logout=await FormClient.PostAsync(teacher,"/Account/Logout",new(),"/Teacher");
        Assert.Equal(HttpStatusCode.Redirect,logout.StatusCode);
        Assert.Equal(HttpStatusCode.Redirect,(await teacher.GetAsync("/Teacher")).StatusCode);
    }

    [Fact]
    public async Task SearchAndSubmissionListOnlyExposeTheSignedInTeachersCourses()
    {
        using var fixture=await OwnershipFixture.CreateAsync();
        using var teacher=await fixture.TeacherAsync();
        var search=await teacher.GetStringAsync("/Teacher/Search?q=PRIVATE");
        Assert.Contains("Alpha-private-course",search);
        Assert.Contains("Alpha-private-assignment",search);
        Assert.DoesNotContain("Beta-private",search);
        var rows=await teacher.GetStringAsync("/Teacher/Submissions");
        Assert.Contains("student-a@example.test",rows);
        Assert.Contains("student-b@example.test",rows);
        Assert.Contains("/Assignments/Submission/"+fixture.SubmissionAId,rows);
        using var other=await fixture.TeacherAsync(other:true);
        Assert.DoesNotContain("student-a@example.test",await other.GetStringAsync("/Teacher/Submissions"));
        using var student=await fixture.StudentAsync();
        foreach(var route in new[]{"/Teacher/Search?q=private","/Teacher/Submissions"})
        {
            var response=await student.GetAsync(route);
            Assert.Equal(HttpStatusCode.Redirect,response.StatusCode);
            Assert.Contains("AccessDenied",response.Headers.Location!.ToString());
        }
    }
}
