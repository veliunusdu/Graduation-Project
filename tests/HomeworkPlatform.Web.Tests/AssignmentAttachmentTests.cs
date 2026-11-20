using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using HomeworkPlatform.Web.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
namespace HomeworkPlatform.Web.Tests;
public class AssignmentAttachmentTests
{
    private static async Task<HttpResponseMessage> Upload(HttpClient client,string url,(string Name,string Body)[] uploads,string? tokenPage=null,bool withToken=true)
    {
        using var form=new MultipartFormDataContent();
        if(withToken){var html=await client.GetStringAsync(tokenPage??url);var token=Regex.Match(html,"name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");Assert.True(token.Success);form.Add(new StringContent(WebUtility.HtmlDecode(token.Groups[1].Value)),"__RequestVerificationToken");}
        form.Add(new StringContent("File requirements assignment"),"Title");form.Add(new StringContent("Complete the attached task."),"Description");form.Add(new StringContent("2026-12-01"),"DueDate");
        foreach(var file in uploads)form.Add(new ByteArrayContent(Encoding.UTF8.GetBytes(file.Body)),"Attachments",file.Name);
        return await client.PostAsync(url,form);
    }
    private static int Files(OwnershipFixture f){var dir=Path.Combine(f.App.DirectoryPath,"AssignmentFiles");return Directory.Exists(dir)?Directory.GetFiles(dir).Length:0;}
    [Fact]
    public async Task MultipleAttachmentsAreVisibleToEnrolledStudentsAndOnlyOwnerCanRemove()
    {
        using var f=await OwnershipFixture.CreateAsync();using var teacher=await f.TeacherAsync();
        Assert.Equal(HttpStatusCode.Redirect,(await Upload(teacher,$"/Assignments/Create?courseId={f.CourseAId}",[("task.pdf","%PDF-1.7 requirements"),("readme.txt","Example inputs")])).StatusCode);
        var a=await f.ReadAsync(db=>db.Assignments.Include(a=>a.Attachments).SingleAsync(a=>a.Title=="File requirements assignment"));Assert.Equal(2,a.Attachments.Count);Assert.Equal(2,Files(f));
        using var student=await f.StudentAsync();using var other=await f.TeacherAsync(true);using var anonymous=f.App.NewClient();
        var file=a.Attachments.Single(x=>x.OriginalFileName=="task.pdf");var path=$"/AssignmentFiles/Download/{file.Id}";
        var studentPage=await student.GetStringAsync($"/StudentWork/Details/{a.Id}");Assert.Contains(path,studentPage);Assert.Contains("Complete the attached task.",studentPage);
        using var download=await student.GetAsync(path);Assert.Equal(HttpStatusCode.OK,download.StatusCode);Assert.Equal("%PDF-1.7 requirements",await download.Content.ReadAsStringAsync());Assert.Equal("attachment",download.Content.Headers.ContentDisposition!.DispositionType);
        Assert.Equal(HttpStatusCode.OK,(await teacher.GetAsync(path)).StatusCode);Assert.Equal(HttpStatusCode.NotFound,(await other.GetAsync(path)).StatusCode);Assert.Equal(HttpStatusCode.Redirect,(await anonymous.GetAsync(path)).StatusCode);
        await f.ReadAsync(db=>db.StudentCourses.Where(e=>e.StudentId==f.StudentAId && e.CourseId==f.CourseAId).ExecuteDeleteAsync());Assert.Equal(HttpStatusCode.NotFound,(await student.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await FormClient.PostAsync(other,$"/Assignments/RemoveAttachment/{file.Id}",new(),"/Teacher")).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect,(await Upload(teacher,$"/Assignments/Edit/{a.Id}",[])).StatusCode);Assert.Equal(2,Files(f));
        Assert.Equal(HttpStatusCode.Redirect,(await FormClient.PostAsync(teacher,$"/Assignments/RemoveAttachment/{file.Id}",new(),$"/Assignments/Edit/{a.Id}")).StatusCode);Assert.Equal(1,Files(f));Assert.Equal(HttpStatusCode.NotFound,(await teacher.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect,(await FormClient.PostAsync(teacher,$"/Assignments/Delete/{a.Id}",new())).StatusCode);Assert.Equal(0,Files(f));
    }
    [Theory]
    [InlineData("invalid.pdf","not a PDF")]
    [InlineData("bad.exe","binary")]
    [InlineData("empty.txt","")]
    public async Task FailedSecondFileRollsBackTheEntireUpload(string name,string body)
    {
        using var f=await OwnershipFixture.CreateAsync();using var teacher=await f.TeacherAsync();
        var result=await Upload(teacher,$"/Assignments/Create?courseId={f.CourseAId}",[("valid.txt","Good"),(name,body)]);Assert.Equal(HttpStatusCode.OK,result.StatusCode);Assert.Contains("validation-summary-errors",await result.Content.ReadAsStringAsync());Assert.Equal(0,Files(f));Assert.False(await f.ReadAsync(db=>db.Assignments.AnyAsync(a=>a.Title=="File requirements assignment")));
    }
    [Fact]
    public async Task UploadChecksOwnershipAntiforgeryCountAndDatabaseFailures()
    {
        using var f=await OwnershipFixture.CreateAsync();using var teacher=await f.TeacherAsync();
        var url=$"/Assignments/Create?courseId={f.CourseAId}";
        Assert.Equal(HttpStatusCode.BadRequest,(await Upload(teacher,url,[("a.txt","A")],withToken:false)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await Upload(teacher,$"/Assignments/Create?courseId={f.CourseBId}",[("a.txt","A")],"/Teacher")).StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await Upload(teacher,url,Enumerable.Range(1,6).Select(i=>($"a{i}.txt","A")).ToArray())).StatusCode);Assert.Equal(0,Files(f));
        await f.ReadAsync(db=>db.Database.ExecuteSqlRawAsync("CREATE TRIGGER RejectAssignment BEFORE INSERT ON Assignments BEGIN SELECT RAISE(ABORT, 'test failure'); END;"));
        Assert.Equal(HttpStatusCode.OK,(await Upload(teacher,url,[("a.txt","A")])).StatusCode);Assert.Equal(0,Files(f));Assert.False(await f.ReadAsync(db=>db.AssignmentAttachments.AnyAsync()));
    }
    [Fact]
    public async Task PendingReviewsCountsOnlyOwnedUnreviewedSubmissionsAndUpdatesAfterReview()
    {
        using var f=await OwnershipFixture.CreateAsync();using var teacher=await f.TeacherAsync();using var other=await f.TeacherAsync(true);
        var html=await teacher.GetStringAsync("/Teacher");Assert.Contains("<strong>2</strong><span>Pending Reviews</span>",html);Assert.DoesNotContain("YOUR TEACHING SPACE",html);Assert.DoesNotContain("assignmentsâ",html);
        Assert.Contains("<strong>0</strong><span>Pending Reviews</span>",await other.GetStringAsync("/Teacher"));
        await FormClient.PostAsync(teacher,$"/Assignments/Review/{f.SubmissionAId}",new(){["Feedback"]="Reviewed",["Grade"]="90"},$"/Assignments/Submission/{f.SubmissionAId}");Assert.Contains("<strong>1</strong><span>Pending Reviews</span>",await teacher.GetStringAsync("/Teacher"));
    }
}
