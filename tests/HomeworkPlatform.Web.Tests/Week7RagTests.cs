using System.Net;
using System.Text;
using System.Text.Json;
using HomeworkPlatform.Web.Data;
using HomeworkPlatform.Web.Models.Domain;
using HomeworkPlatform.Web.Services.AI;
using HomeworkPlatform.Web.Services.Files;
using HomeworkPlatform.Web.Tests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
namespace HomeworkPlatform.Web.Tests;

public class Week7RagTests
{
    private sealed class Handler : HttpMessageHandler
    {
        public List<(string Path, string Body)> Requests { get; } = [];
        public Func<string, string, CancellationToken, Task<HttpResponseMessage>> Reply { get; set; } = (_, _, _) => Task.FromResult(Json(new { course_id = 0, results = Array.Empty<object>() }));
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            var path = request.RequestUri!.AbsolutePath;
            var body = await request.Content!.ReadAsStringAsync(token);
            Requests.Add((path, body));
            return await Reply(path, body, token);
        }
    }
    private static HttpResponseMessage Json(object data) => new(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json") };
    private static object Hit(int course, int material, string id, string text = "Allowed source passage", double score = .9, int page = 2) => new {
        text, score, metadata = new { course_id = course, material_id = material, page_number = page, chunk_id = id }
    };
    private static Task<OwnershipFixture> Fixture(Handler handler, bool shortTimeout = false) => OwnershipFixture.CreateAsync(app => app.ConfigureServices = services => {
        services.AddHttpClient<AIServiceClient>().ConfigurePrimaryHttpMessageHandler(() => handler);
        if (shortTimeout) services.Configure<AIServiceOptions>(o => o.TimeoutSeconds = 1);
    });
    private static async Task<(int Ready, int Foreign, int Failed)> Materials(OwnershipFixture fixture)
    {
        using var scope = fixture.App.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<MaterialFileStore>();
        using var stream = new MemoryStream("Allowed course notes about primary keys and SQL joins."u8.ToArray());
        var file = await store.SaveAsync(new FormFile(stream, 0, stream.Length, "upload", "allowed-notes.txt"));
        var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var ready = new CourseMaterial { CourseId = fixture.CourseAId, OriginalFileName = file.OriginalName, StoredFileName = file.StoredName, FileSize = file.Size, Status = MaterialStatus.Ready };
        var foreign = new CourseMaterial { CourseId = fixture.CourseBId, OriginalFileName = "foreign-secret.txt", Status = MaterialStatus.Ready };
        var failed = new CourseMaterial { CourseId = fixture.CourseAId, OriginalFileName = "failed-source.txt", Status = MaterialStatus.Failed };
        database.AddRange(ready, foreign, failed); await database.SaveChangesAsync();
        return (ready.Id, foreign.Id, failed.Id);
    }
    private static Task<HttpResponseMessage> Ask(HttpClient client, int course, string question = "Explain primary keys", bool tutor = false, string? tokenPage = null, Dictionary<string, string>? extras = null)
    {
        var fields = extras ?? new(); fields["question"] = question; fields["tutor"] = tutor ? "true" : "false";
        return FormClient.PostAsync(client, $"/StudentCourses/SearchSources/{course}", fields, tokenPage ?? $"/StudentCourses/Details/{course}");
    }

    [Fact]
    public async Task CorrectQuestionAndRouteCourseAreSentAsSnakeCaseAndSourcesHaveAuthorizedDownloads()
    {
        var handler = new Handler(); using var f = await Fixture(handler); var ids = await Materials(f); using var student = await f.StudentAsync();
        handler.Reply = (_, _, _) => Task.FromResult(Json(new { course_id = f.CourseAId, results = new[] { Hit(f.CourseAId, ids.Ready, "chunk-1") } }));
        var response = await Ask(student, f.CourseAId, "  Explain primary keys  ", extras: new() { ["course_id"] = f.CourseBId.ToString(), ["courseId"] = f.CourseBId.ToString(), ["top_k"] = "999" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var request = Assert.Single(handler.Requests); Assert.Equal("/rag/retrieve", request.Path);
        using var payload = JsonDocument.Parse(request.Body); Assert.Equal("Explain primary keys", payload.RootElement.GetProperty("question").GetString()); Assert.Equal(f.CourseAId, payload.RootElement.GetProperty("course_id").GetInt32()); Assert.Equal(20, payload.RootElement.GetProperty("top_k").GetInt32());
        var html = await response.Content.ReadAsStringAsync(); Assert.Contains("allowed-notes.txt", html); Assert.Contains("Page 2", html); Assert.Contains("Allowed source passage", html); Assert.Contains("Explain primary keys", html); Assert.Contains($"/CourseMaterials/Download/{ids.Ready}", html);
        Assert.Equal(HttpStatusCode.OK, (await student.GetAsync($"/CourseMaterials/Download/{ids.Ready}")).StatusCode);
    }

    [Theory]
    [InlineData("foreign-course")]
    [InlineData("inactive-course")]
    [InlineData("unenrolled")]
    public async Task UnauthorizedCourseIsBlockedBeforeAnyAICall(string kind)
    {
        var handler = new Handler(); using var f = await Fixture(handler); await Materials(f); using var student = await f.StudentAsync();
        var id = kind == "foreign-course" ? f.CourseBId : f.CourseAId;
        if (kind == "inactive-course") await f.ReadAsync(db => db.Courses.Where(c => c.Id == id).ExecuteUpdateAsync(s => s.SetProperty(c => c.IsActive, false)));
        if (kind == "unenrolled") await f.ReadAsync(db => db.StudentCourses.Where(e => e.CourseId == id && e.StudentId == f.StudentAId).ExecuteDeleteAsync());
        Assert.Equal(HttpStatusCode.NotFound, (await Ask(student, id, tokenPage: "/Student")).StatusCode); Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task ForgedForeignFailedLowScoreAndDuplicateSourcesAreNotRendered()
    {
        var handler = new Handler(); using var f = await Fixture(handler); var ids = await Materials(f); using var student = await f.StudentAsync();
        handler.Reply = (_, _, _) => Task.FromResult(Json(new { course_id = f.CourseAId, results = new object[] {
            Hit(f.CourseBId, ids.Foreign, "foreign", "FOREIGN_COURSE_SECRET", .99),
            Hit(f.CourseAId, ids.Foreign, "forged", "FORGED_MATERIAL_SECRET", .98),
            Hit(f.CourseAId, ids.Failed, "failed", "FAILED_MATERIAL_SECRET", .97),
            Hit(f.CourseAId, ids.Ready, "low", "LOW_SCORE_SECRET", .1),
            Hit(f.CourseAId, ids.Ready, "too-high", "INVALID_SCORE_SECRET", 2),
            Hit(f.CourseAId, ids.Ready, "zero-page", "INVALID_PAGE_SECRET", .8, 0),
            Hit(f.CourseAId, ids.Ready, "", "MISSING_CHUNK_SECRET"),
            Hit(f.CourseAId, ids.Ready, "valid", "HIGHEST_VALID_PASSAGE", .96),
            Hit(f.CourseAId, ids.Ready, "valid", "DUPLICATE_SECRET", .95),
            Hit(f.CourseAId, ids.Ready, "valid-2", "SECOND_VALID_PASSAGE", .8),
            Hit(f.CourseAId, ids.Ready, "valid-3", "THIRD_VALID_PASSAGE", .7),
            Hit(f.CourseAId, ids.Ready, "valid-4", "FOURTH_PASSAGE_NOT_SHOWN", .65)
        } }));
        var html = await (await Ask(student, f.CourseAId)).Content.ReadAsStringAsync();
        foreach (var denied in new[] { "FOREIGN_COURSE_SECRET", "FORGED_MATERIAL_SECRET", "FAILED_MATERIAL_SECRET", "LOW_SCORE_SECRET", "INVALID_SCORE_SECRET", "INVALID_PAGE_SECRET", "MISSING_CHUNK_SECRET", "DUPLICATE_SECRET", "FOURTH_PASSAGE_NOT_SHOWN" }) Assert.DoesNotContain(denied, html);
        Assert.Contains("HIGHEST_VALID_PASSAGE", html); Assert.Contains("SECOND_VALID_PASSAGE", html); Assert.Contains("THIRD_VALID_PASSAGE", html);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("oversized")]
    public async Task InvalidQuestionDoesNotReachPython(string question)
    {
        var handler = new Handler(); using var f = await Fixture(handler); await Materials(f); using var student = await f.StudentAsync();
        if (question == "oversized") question = new string('x', 4001);
        Assert.Equal(HttpStatusCode.BadRequest, (await Ask(student, f.CourseAId, question)).StatusCode); Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task UnprocessedCourseExplainsMissingMaterialsWithoutCallingAI()
    {
        var handler = new Handler(); using var f = await Fixture(handler); using var student = await f.StudentAsync();
        var response = await Ask(student, f.CourseAId); Assert.Equal(HttpStatusCode.OK, response.StatusCode); Assert.Contains("No ready course materials yet", await response.Content.ReadAsStringAsync()); Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task EmptyRetrievalIsAHelpfulResultAndSourceTextIsHtmlEncoded()
    {
        var handler = new Handler(); using var f = await Fixture(handler); var ids = await Materials(f); using var student = await f.StudentAsync();
        handler.Reply = (_, _, _) => Task.FromResult(Json(new { course_id = f.CourseAId, results = Array.Empty<object>() }));
        Assert.Contains("No matching course sources found", await (await Ask(student, f.CourseAId)).Content.ReadAsStringAsync());
        handler.Reply = (_, _, _) => Task.FromResult(Json(new { course_id = f.CourseAId, results = new[] { Hit(f.CourseAId, ids.Ready, "safe", "<script>privateMarker()</script>") } }));
        var html = await (await Ask(student, f.CourseAId)).Content.ReadAsStringAsync(); Assert.DoesNotContain("<script>privateMarker()", html); Assert.Contains("&lt;script&gt;", html);
    }

    [Theory]
    [InlineData("http-error")]
    [InlineData("malformed-json")]
    [InlineData("wrong-course")]
    [InlineData("null-results")]
    [InlineData("connection")]
    [InlineData("timeout")]
    public async Task InvalidOrUnavailableAIProducesAControlledError(string kind)
    {
        var handler = new Handler(); using var f = await Fixture(handler, kind == "timeout"); await Materials(f); using var student = await f.StudentAsync();
        handler.Reply = async (_, _, token) => {
            if (kind == "connection") throw new HttpRequestException("private upstream details");
            if (kind == "timeout") await Task.Delay(Timeout.Infinite, token);
            return kind switch {
                "http-error" => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable) { Content = new StringContent("private upstream details") },
                "malformed-json" => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("private upstream details") },
                "wrong-course" => Json(new { course_id = f.CourseBId, results = Array.Empty<object>() }),
                _ => Json(new { course_id = f.CourseAId, results = (object?)null })
            };
        };
        var response = await Ask(student, f.CourseAId); Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode); var html = await response.Content.ReadAsStringAsync(); Assert.Contains("student-sidebar", html); Assert.Contains("Course sources could not be searched", html); Assert.DoesNotContain("private upstream details", html);
    }

    [Fact]
    public async Task EnrollmentIsRecheckedAfterRetrieval()
    {
        var handler = new Handler(); using var f = await Fixture(handler); var ids = await Materials(f); using var student = await f.StudentAsync();
        handler.Reply = async (_, _, _) => { await f.ReadAsync(db => db.StudentCourses.Where(e => e.CourseId == f.CourseAId && e.StudentId == f.StudentAId).ExecuteDeleteAsync()); return Json(new { course_id = f.CourseAId, results = new[] { Hit(f.CourseAId, ids.Ready, "private") } }); };
        var response = await Ask(student, f.CourseAId); Assert.Equal(HttpStatusCode.NotFound, response.StatusCode); Assert.DoesNotContain("Allowed source passage", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task TutorReceivesOnlyValidatedSourcesAndItsAnswerAndCitationsAppearTogether()
    {
        var handler = new Handler(); using var f = await Fixture(handler); var ids = await Materials(f); using var student = await f.StudentAsync();
        handler.Reply = (path, _, _) => Task.FromResult(path.EndsWith("rag/retrieve")
            ? Json(new { course_id = f.CourseAId, results = new[] { Hit(f.CourseAId, ids.Ready, "valid", "A primary key identifies one row."), Hit(f.CourseBId, ids.Foreign, "foreign", "FOREIGN_SECRET") } })
            : Json(new { status = "answered", explanation = "A primary key uniquely identifies each record.", next_step = "Identify the key in the Books table.", citations = new[] { "s1" } }));
        var response = await Ask(student, f.CourseAId, tutor: true); Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync(); Assert.Contains("A primary key uniquely identifies each record.", html); Assert.Contains("allowed-notes.txt", html); Assert.Contains("Page 2", html); Assert.Contains("A primary key identifies one row.", html);
        var tutor = handler.Requests.Single(r => r.Path == "/tutor/answer"); Assert.DoesNotContain("FOREIGN_SECRET", tutor.Body); using var payload = JsonDocument.Parse(tutor.Body); Assert.Equal(f.CourseAId, payload.RootElement.GetProperty("context").GetProperty("course_id").GetInt32()); Assert.Equal(1, payload.RootElement.GetProperty("sources").GetArrayLength());
    }

    [Fact]
    public async Task MissingAntiforgeryAndWrongRoleDoNotReachAI()
    {
        var handler = new Handler(); using var f = await Fixture(handler); await Materials(f); using var student = await f.StudentAsync(); using var teacher = await f.TeacherAsync(); using var anonymous = f.App.NewClient();
        var route = $"/StudentCourses/SearchSources/{f.CourseAId}";
        Assert.Equal(HttpStatusCode.BadRequest, (await student.PostAsync(route, new FormUrlEncodedContent(new Dictionary<string,string>{{"question","Test"}}))).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await teacher.PostAsync(route, new FormUrlEncodedContent(new Dictionary<string,string>()))).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await anonymous.PostAsync(route, new FormUrlEncodedContent(new Dictionary<string,string>()))).StatusCode); Assert.Empty(handler.Requests);
    }
}
