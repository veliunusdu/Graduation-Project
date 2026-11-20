using System.Net;
using System.Text;
using System.Text.Json;
using HomeworkPlatform.Web.Services.AI;
using HomeworkPlatform.Web.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HomeworkPlatform.Web.Tests;

public class AssignmentAgentTests
{
    private sealed class Handler : HttpMessageHandler
    {
        public List<(string Path, string Body)> Requests { get; } = [];
        public Func<string, string, Task<HttpResponseMessage>> Reply { get; set; } = (_, _) => Task.FromResult(Json(new { status = "no_source" }));
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            var path = request.RequestUri!.AbsolutePath;
            var body = request.Content == null ? "" : await request.Content.ReadAsStringAsync(token);
            Requests.Add((path, body));
            return await Reply(path, body);
        }
    }

    private static HttpResponseMessage Json(object data) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json")
    };

    private static Task<OwnershipFixture> Fixture(Handler handler) => OwnershipFixture.CreateAsync(app =>
        app.ConfigureServices = services => services.AddHttpClient<AIServiceClient>().ConfigurePrimaryHttpMessageHandler(() => handler));

    [Fact]
    public async Task TeacherDraftUsesOwnCourseAndSaveMarksOnlyTheReturnedDraft()
    {
        var handler = new Handler();
        using var f = await Fixture(handler);
        using var teacher = await f.TeacherAsync();
        handler.Reply = (path, _) => Task.FromResult(Json(new
        {
            status = "drafted",
            title = "Primary keys",
            description = "Explain how a primary key identifies a row.",
            difficulty = "Intermediate",
            source_ids = new[] { "c1" }
        }));
        var draft = await FormClient.PostAsync(teacher, $"/Assignments/Draft?courseId={f.CourseAId}", new() { ["Topic"] = " Keys " }, $"/Assignments/Create?courseId={f.CourseAId}");
        Assert.Equal(HttpStatusCode.OK, draft.StatusCode);
        var html = await draft.Content.ReadAsStringAsync();
        Assert.Contains("Primary keys", html);
        Assert.Contains("Explain how a primary key identifies a row.", html);
        var request = Assert.Single(handler.Requests);
        Assert.Equal("/assignments/draft", request.Path);
        using var payload = JsonDocument.Parse(request.Body);
        Assert.Equal("Keys", payload.RootElement.GetProperty("topic").GetString());
        Assert.Equal(f.CourseAId, payload.RootElement.GetProperty("context").GetProperty("course_id").GetInt32());
        Assert.DoesNotContain("qwen", request.Body);
        Assert.DoesNotContain("bge", request.Body);

        var saved = await FormClient.PostAsync(teacher, $"/Assignments/Create?courseId={f.CourseAId}", new()
        {
            ["Title"] = "Primary keys",
            ["Description"] = "Explain how a primary key identifies a row.",
            ["DueDate"] = "2026-12-01"
        }, $"/Assignments/Create?courseId={f.CourseAId}");
        Assert.Equal(HttpStatusCode.Redirect, saved.StatusCode);
        var assignment = await f.ReadAsync(db => db.Assignments.SingleAsync(a => a.Title == "Primary keys"));
        Assert.True(assignment.CreatedByAI);
        Assert.Equal(HomeworkPlatform.Web.Models.Domain.Difficulty.Intermediate, assignment.Difficulty);
    }

    [Fact]
    public async Task AnotherTeacherCannotDraftOrTestTheAssignment()
    {
        var handler = new Handler();
        using var f = await Fixture(handler);
        using var other = await f.TeacherAsync(true);
        Assert.Equal(HttpStatusCode.NotFound, (await FormClient.PostAsync(other, $"/Assignments/Draft?courseId={f.CourseAId}", new() { ["Topic"] = "Keys" }, "/Teacher")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await FormClient.PostAsync(other, $"/Assignments/Check/{f.AssignmentAId}", new(), "/Teacher")).StatusCode);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task SavedAssignmentIsCheckedWithoutSendingABrowserCourseId()
    {
        var handler = new Handler();
        using var f = await Fixture(handler);
        using var teacher = await f.TeacherAsync();
        handler.Reply = (_, _) => Task.FromResult(Json(new
        {
            status = "revise",
            summary = "The task should mention a primary key.",
            findings = new[] { "Ask for one example from the notes." },
            source_ids = new[] { "c1" }
        }));
        await f.ReadAsync(async db =>
        {
            var assignment = await db.Assignments.SingleAsync(a => a.Id == f.AssignmentAId);
            assignment.Description = "Explain a primary key.";
            await db.SaveChangesAsync();
            return true;
        });
        var response = await FormClient.PostAsync(teacher, $"/Assignments/Check/{f.AssignmentAId}", new() { ["course_id"] = f.CourseBId.ToString() }, $"/Assignments/Details/{f.AssignmentAId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("The task should mention a primary key.", html);
        Assert.Contains("Ask for one example from the notes.", html);
        var request = Assert.Single(handler.Requests);
        Assert.Equal("/assignments/review", request.Path);
        using var payload = JsonDocument.Parse(request.Body);
        Assert.Equal(f.CourseAId, payload.RootElement.GetProperty("context").GetProperty("course_id").GetInt32());
        Assert.False(payload.RootElement.TryGetProperty("course_id", out _));
    }
}
