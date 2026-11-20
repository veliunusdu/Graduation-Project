using System.Net;
using HomeworkPlatform.Web.Tests.Infrastructure;
namespace HomeworkPlatform.Web.Tests;
public class StudentPanelPolishTests
{
    [Theory]
    [InlineData("teacher@edupilot.local","Teacher-Start8!","/Teacher")]
    [InlineData("student1@edupilot.local","Student-Start8!","/Student")]
    public async Task LogoutOpensSignInAndClearsSessionForBothRoles(string email,string password,string page)
    {
        using var app=new AuthWebApplicationFactory{EnableInitialData=true};using var client=app.NewClient();
        await FormClient.LoginAsync(client,email,password);
        var response=await FormClient.PostAsync(client,"/Account/Logout",new(),page);
        Assert.Equal(HttpStatusCode.Redirect,response.StatusCode);Assert.Equal("/Account/Login",response.Headers.Location!.OriginalString);
        var login=await client.GetStringAsync("/Account/Login");Assert.Contains("Welcome back",login);Assert.DoesNotContain("onboardCarousel",login);
        Assert.Equal(HttpStatusCode.Redirect,(await client.GetAsync(page)).StatusCode);
        Assert.Contains("onboardCarousel",await client.GetStringAsync("/"));
    }
    [Fact]
    public async Task NotSubmittedLabelStillFiltersUnsubmittedAssignmentsAndUsesTeacherCardStyles()
    {
        using var f=await OwnershipFixture.CreateAsync();using var student=await f.StudentAsync();
        var filtered=await student.GetStringAsync("/StudentWork?status=Pending");
        Assert.Contains(">Not Submitted</option>",filtered);
        Assert.Contains("Fresh assignment",filtered);Assert.DoesNotContain("Alpha-private-assignment",filtered);
        Assert.Contains(">Not Submitted</span>",filtered);
        var courses=await student.GetStringAsync("/StudentCourses");Assert.Contains("ep-course-panel ep-course-card",courses);Assert.Contains("ep-course-card-bottom",courses);Assert.Contains("ep-course-tools",courses);Assert.DoesNotContain("Edit course",courses);
        var dashboard=await student.GetStringAsync("/Student");Assert.Contains("Your AI learning tools",dashboard);Assert.Contains("/AIStatus",dashboard);Assert.Contains("edupilot-teacher.css",dashboard);
    }
}
