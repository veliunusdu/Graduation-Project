using System.Net;
using HomeworkPlatform.Web.Tests.Infrastructure;
using HomeworkPlatform.Web.Services.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
namespace HomeworkPlatform.Web.Tests;
public class StudentPanelTests
{
    private sealed class OfflineHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable){Content=new StringContent("private traceback")});
    }
    [Fact]
    public async Task StudentShellUsesRealProfileCorrectRoutesAndKeepsOnboardingAndLogout()
    {
        using var app=new AuthWebApplicationFactory{EnableInitialData=true};using var student=app.NewClient();
        await FormClient.LoginAsync(student,"student1@edupilot.local","Student-Start8!");
        foreach(var route in new[]{"/Student","/StudentCourses","/StudentWork","/Submissions","/Student/Search?q=programming"})
        {
            var html=WebUtility.HtmlDecode(await student.GetStringAsync(route));Assert.Contains("id=\"student-sidebar\"",html);Assert.Contains("Ayşe Yılmaz",html);Assert.Contains("University",html);Assert.Contains("Year 2",html);Assert.Contains("/Account/Logout",html);Assert.DoesNotContain("href=\"/Courses",html);Assert.DoesNotContain("href=\"/Assignments",html);Assert.DoesNotContain("/Profile/Edit",html);Assert.DoesNotContain("/AiAgent/",html);Assert.Contains("/AIStatus",html);
        }
        var entry=await student.GetStringAsync("/");Assert.Contains("onboardCarousel",entry);Assert.DoesNotContain("student-sidebar",entry);
        Assert.Equal(HttpStatusCode.Redirect,(await FormClient.PostAsync(student,"/Account/Logout",new(),"/Student")).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect,(await student.GetAsync("/Student")).StatusCode);
    }
    [Fact]
    public async Task CountsFiltersAndSearchOnlyUseCurrentStudentsActiveEnrollments()
    {
        using var f=await OwnershipFixture.CreateAsync();using var student=await f.StudentAsync();
        var html=await student.GetStringAsync("/Student");Assert.Contains("id=\"student-course-count\">1",html);Assert.Contains("id=\"student-submitted-count\">1",html);Assert.Contains("id=\"student-pending-count\">1",html);
        var pending=await student.GetStringAsync("/StudentWork?status=Pending");Assert.Contains("Fresh assignment",pending);Assert.DoesNotContain("Alpha-private-assignment",pending);
        var submitted=await student.GetStringAsync("/StudentWork?status=Submitted");Assert.Contains("Alpha-private-assignment",submitted);Assert.DoesNotContain("Fresh assignment",submitted);Assert.Contains($"/Submissions/Details/{f.SubmissionAId}",submitted);Assert.DoesNotContain($"/Submissions/Details/{f.SubmissionBId}",submitted);
        var search=await student.GetStringAsync("/Student/Search?q=PRIVATE");Assert.Contains("Alpha-private-course",search);Assert.Contains("Alpha-private-assignment",search);Assert.DoesNotContain("Beta-private",search);
        Assert.Equal(HttpStatusCode.NotFound,(await student.GetAsync($"/StudentWork?courseId={f.CourseBId}")).StatusCode);
        await f.ReadAsync(db=>db.Courses.Where(c=>c.Id==f.CourseAId).ExecuteUpdateAsync(s=>s.SetProperty(c=>c.IsActive,false)));
        Assert.DoesNotContain("Alpha-private",await student.GetStringAsync("/Student/Search?q=private"));Assert.Contains("id=\"student-course-count\">0",await student.GetStringAsync("/Student"));
    }
    [Fact]
    public async Task ReviewUpdatesStudentsGradeAndStatusAndRevisionReturnsToAwaitingReview()
    {
        using var f=await OwnershipFixture.CreateAsync();using var student=await f.StudentAsync();using var teacher=await f.TeacherAsync();
        await FormClient.PostAsync(teacher,$"/Assignments/Review/{f.SubmissionAId}",new(){["Feedback"]="Panel review feedback",["Grade"]="91"},$"/Assignments/Submission/{f.SubmissionAId}");
        var list=await student.GetStringAsync("/Submissions");Assert.Contains("Panel review feedback",list);Assert.Contains("<b>91</b>",list);Assert.DoesNotContain("Bob-private-content",list);
        var graded=await student.GetStringAsync("/StudentWork?status=Graded");Assert.Contains("Alpha-private-assignment",graded);Assert.Contains(">Graded</span>",graded);
        await FormClient.PostAsync(student,$"/Submissions/Edit/{f.SubmissionAId}",new(){["Content"]="Revised for review"});
        var updated=await student.GetStringAsync($"/Submissions/Details/{f.SubmissionAId}");Assert.Contains("Awaiting review",updated);Assert.DoesNotContain("Panel review feedback",updated);
    }
    [Fact]
    public async Task RealLearningToolRoutesRemainAvailableAndOfflineAIShowsHelpfulPage()
    {
        using var f=await OwnershipFixture.CreateAsync(app=>app.ConfigureServices=services=>services.AddHttpClient<AIServiceClient>().ConfigurePrimaryHttpMessageHandler(()=>new OfflineHandler()));using var student=await f.StudentAsync();
        await f.ReadAsync(async db=>{db.CourseMaterials.Add(new HomeworkPlatform.Web.Models.Domain.CourseMaterial{CourseId=f.CourseAId,OriginalFileName="ready-notes.txt",Status=HomeworkPlatform.Web.Models.Domain.MaterialStatus.Ready});await db.SaveChangesAsync();return true;});
        var route=$"/StudentCourses/Details/{f.CourseAId}";var html=await student.GetStringAsync(route);Assert.Contains($"/StudentCourses/SearchSources/{f.CourseAId}",html);Assert.Contains("Ask AI Tutor",html);Assert.Contains("Code Agent - Coming soon",html);
        var response=await FormClient.PostAsync(student,$"/StudentCourses/SearchSources/{f.CourseAId}",new(){["question"]="Explain this topic",["tutor"]="true"},route);Assert.Equal(HttpStatusCode.BadGateway,response.StatusCode);var error=await response.Content.ReadAsStringAsync();Assert.Contains("student-sidebar",error);Assert.Contains("AI service",error);Assert.DoesNotContain("private traceback",error);
        using var teacher=await f.TeacherAsync();Assert.Equal(HttpStatusCode.Redirect,(await teacher.GetAsync("/Student/Search?q=private")).StatusCode);
    }
}
