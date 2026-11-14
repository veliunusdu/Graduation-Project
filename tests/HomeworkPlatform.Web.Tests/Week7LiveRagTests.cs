using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using HomeworkPlatform.Web.Services.AI;
using HomeworkPlatform.Web.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
namespace HomeworkPlatform.Web.Tests;

public class Week7LiveRagTests
{
    [Week7LiveFact]
    public async Task RealPythonAndBgeReturnOnlyTheEnrolledCoursesSources()
    {
        var url = Environment.GetEnvironmentVariable("WEEK7_PYTHON_URL")!;
        Assert.True(new Uri(url).IsLoopback);
        using var f = await OwnershipFixture.CreateAsync(app => app.ConfigureServices = services =>
            services.Configure<AIServiceOptions>(options => options.BaseUrl = url));
        using var teacherA = await f.TeacherAsync(); using var teacherB = await f.TeacherAsync(true); using var student = await f.StudentAsync();
        await f.ReadAsync(db => db.Courses.Where(c => c.Id == f.CourseBId).ExecuteUpdateAsync(s => s.SetProperty(c => c.EducationLevel, HomeworkPlatform.Web.Models.Domain.EducationLevel.University).SetProperty(c => c.GradeLevel, 2)));
        await Upload(teacherA, f.CourseAId, "course-a-notes.txt", "A primary key uniquely identifies each row in a relational database table. This course's verification phrase is ALPHA-COURSE-ONLY. A foreign key links two tables.");
        await Upload(teacherB, f.CourseBId, "course-b-notes.txt", "A primary key uniquely identifies each row in a relational database table. This course's verification phrase is BETA-COURSE-ONLY. A foreign key links two tables.");
        var materials = await f.ReadAsync(db => db.CourseMaterials.AsNoTracking().ToListAsync());
        Assert.All(materials, material => Assert.Equal(HomeworkPlatform.Web.Models.Domain.MaterialStatus.Ready, material.Status));
        var question = "What does a primary key do in this course?";
        using var api = new HttpClient { BaseAddress = new Uri(url), Timeout = TimeSpan.FromSeconds(10) };
        using var direct = await System.Net.Http.Json.HttpClientJsonExtensions.PostAsJsonAsync(api, "rag/retrieve", new { question, course_id = f.CourseAId, top_k = 3 });
        direct.EnsureSuccessStatusCode(); using var raw = JsonDocument.Parse(await direct.Content.ReadAsStringAsync());
        var hits = raw.RootElement.GetProperty("results").EnumerateArray().ToArray(); Assert.NotEmpty(hits);
        Assert.All(hits, hit => Assert.Equal(f.CourseAId, hit.GetProperty("metadata").GetProperty("course_id").GetInt32()));
        using var response = await FormClient.PostAsync(student, $"/StudentCourses/SearchSources/{f.CourseAId}", new() { ["question"] = question }, $"/StudentCourses/Details/{f.CourseAId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode); var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("ALPHA-COURSE-ONLY", html); Assert.Contains("course-a-notes.txt", html); Assert.DoesNotContain("BETA-COURSE-ONLY", html);
        using var denied = await FormClient.PostAsync(student, $"/StudentCourses/SearchSources/{f.CourseBId}", new() { ["question"] = question }, "/Student");
        Assert.Equal(HttpStatusCode.NotFound, denied.StatusCode);
        var evidence = Environment.GetEnvironmentVariable("WEEK7_EVIDENCE_PATH");
        if (!string.IsNullOrEmpty(evidence)) await File.WriteAllTextAsync(evidence, JsonSerializer.Serialize(new {
            status = "passed", checked_at_utc = DateTime.UtcNow, real_python = true, real_bge_m3 = true,
            timeout_seconds = 10, source_course_id = f.CourseAId, foreign_course_id = f.CourseBId,
            returned_chunks = hits.Length, material_ids = materials.Select(m => m.Id),
            checks = new[] { "automatic material processing", "real embedding and retrieval", "course-scoped Python results", "ASP.NET source display", "foreign course denied" }
        }, new JsonSerializerOptions { WriteIndented = true }));
    }
    private static async Task Upload(HttpClient teacher, int course, string name, string content)
    {
        var page = await teacher.GetStringAsync($"/CourseMaterials/Index?courseId={course}");
        var token = Regex.Match(page, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        using var form = new MultipartFormDataContent(); form.Add(new StringContent(WebUtility.HtmlDecode(token.Groups[1].Value)), "__RequestVerificationToken");
        form.Add(new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes(content)), "upload", name);
        Assert.Equal(HttpStatusCode.Redirect, (await teacher.PostAsync($"/CourseMaterials/Upload?courseId={course}", form)).StatusCode);
    }
}
public sealed class Week7LiveFactAttribute : FactAttribute
{
    public Week7LiveFactAttribute() { if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("WEEK7_PYTHON_URL"))) Skip = "Requires isolated Python and real BGE-M3; use scripts/verify_week7_rag_live.py."; }
}
