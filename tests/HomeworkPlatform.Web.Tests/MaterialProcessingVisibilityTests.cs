using System.Net;
using System.Text.RegularExpressions;
using HomeworkPlatform.Web.Models.Domain;
using HomeworkPlatform.Web.Services.AI;
using HomeworkPlatform.Web.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
namespace HomeworkPlatform.Web.Tests;

public class MaterialProcessingVisibilityTests
{
    private sealed class PausedProcessor : IMaterialProcessor
    {
        public TaskCompletionSource<bool> Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<MaterialProcessingResult> Finish { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public async Task<MaterialProcessingResult> ProcessAsync(CourseMaterial material, Stream input)
        {
            Started.TrySetResult(true);
            return await Finish.Task;
        }
    }

    private static async Task<HttpResponseMessage> Upload(HttpClient teacher, int courseId)
    {
        var page = await teacher.GetStringAsync($"/CourseMaterials/Index?courseId={courseId}");
        var token = Regex.Match(page, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(WebUtility.HtmlDecode(token.Groups[1].Value)), "__RequestVerificationToken");
        form.Add(new ByteArrayContent("Shared lesson notes"u8.ToArray()), "upload", "lesson.txt");
        return await teacher.PostAsync($"/CourseMaterials/Upload?courseId={courseId}", form);
    }

    [Theory]
    [InlineData(true, "Ready")]
    [InlineData(false, "Failed")]
    public async Task StudentsSeeAndDownloadCourseMaterialsDuringProcessingAndAfterItsOutcome(bool success, string status)
    {
        var processor = new PausedProcessor();
        using var fixture = await OwnershipFixture.CreateAsync(app => app.ConfigureServices = services => services.AddSingleton<IMaterialProcessor>(processor));
        using var teacher = await fixture.TeacherAsync();
        using var student = await fixture.StudentAsync();
        var pending = Upload(teacher, fixture.CourseAId);
        try
        {
            await processor.Started.Task.WaitAsync(TimeSpan.FromSeconds(15));
            var material = await fixture.ReadAsync(db => db.CourseMaterials.SingleAsync());
            Assert.Equal(MaterialStatus.Processing, material.Status);
            var page = await student.GetStringAsync($"/CourseMaterials/Index?courseId={fixture.CourseAId}");
            Assert.Contains("lesson.txt", page);
            Assert.Contains("AI processing status: Processing", page);
            Assert.Contains("data-material-auto-refresh=\"true\"", page);
            Assert.DoesNotContain("Retry processing", page);
            Assert.Equal("Shared lesson notes", await student.GetStringAsync($"/CourseMaterials/Download/{material.Id}"));
            processor.Finish.TrySetResult(new(success, success ? 3 : 0, success ? null : "private upstream details"));
            using var uploaded = await pending;
            Assert.Equal(HttpStatusCode.Redirect, uploaded.StatusCode);
            var completed = await student.GetStringAsync($"/CourseMaterials/Index?courseId={fixture.CourseAId}");
            Assert.Contains("AI processing status: " + status, completed);
            Assert.DoesNotContain("private upstream details", completed);
            Assert.Contains("lesson.txt", await student.GetStringAsync($"/StudentCourses/Details/{fixture.CourseAId}"));
            Assert.Equal(HttpStatusCode.OK, (await student.GetAsync($"/CourseMaterials/Download/{material.Id}")).StatusCode);
            var ownerPage = await teacher.GetStringAsync($"/CourseMaterials/Index?courseId={fixture.CourseAId}");
            Assert.Equal(!success, ownerPage.Contains("Retry processing"));
        }
        finally
        {
            processor.Finish.TrySetResult(new(false, 0, "Test cleanup"));
            await pending;
        }
    }
}
