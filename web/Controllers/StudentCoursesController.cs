using System.Security.Claims;
using HomeworkPlatform.Web.Data;
using HomeworkPlatform.Web.Models.Domain;
using HomeworkPlatform.Web.Security;
using HomeworkPlatform.Web.Services.AI;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace HomeworkPlatform.Web.Controllers;
[Authorize(Roles = AppRoles.Student)]
public class StudentCoursesController(ApplicationDbContext database, AIServiceClient ai, IOptions<AIServiceOptions> options, HomeworkPlatform.Web.Services.StudentPanelReader panel) : Controller
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private IQueryable<Course> Enrolled => database.Courses.Where(c => c.IsActive &&
        database.StudentCourses.Any(e => e.CourseId == c.Id && e.StudentId == UserId));
    [HttpGet] public async Task<IActionResult> Index() => View(await panel.Courses(UserId));
    [HttpGet] public async Task<IActionResult> Details([FromRoute] int id)
    {
        var course = await Enrolled.AsNoTracking().SingleOrDefaultAsync(c => c.Id == id);
        if (course == null) return NotFound();
        var card = (await panel.Courses(UserId)).SingleOrDefault(c => c.Course.Id == id);
        if (card == null) return NotFound();
        var materials = await database.CourseMaterials.AsNoTracking().Where(m => m.CourseId == id).OrderBy(m => m.OriginalFileName).ToListAsync();
        return View(new HomeworkPlatform.Web.Models.StudentCourseDetailViewModel(card, materials, (await panel.Assignments(UserId)).Where(a => a.Assignment.CourseId == id).ToList()));
    }

    private IActionResult AIUnavailable(int courseId, string message, int status = 502)
    {
        Response.StatusCode = status;
        ViewData["AIMessage"] = message;
        return View("AIUnavailable", courseId);
    }

    public record SourceRequest(string Question, int CourseId, int TopK);
    public record SourceMetadata(int CourseId, int MaterialId, int PageNumber, string ChunkId);
    public record SourceHit(string Text, double Score, SourceMetadata? Metadata);
    public record SourceResponse(int CourseId, List<SourceHit>? Results);
    public record CourseSource(string FileName, int PageNumber, string Text)
    {
        public int MaterialId { get; init; }
        public string ChunkId { get; init; } = "";
        public double Similarity { get; init; }
    }
    public record TutorSource(string SourceId, string Text);
    public record TutorContext(int CourseId, string EducationLevel, int GradeLevel);
    public record TutorRequest(string Question, TutorContext Context, List<TutorSource> Sources);
    public record TutorResponse(string Status, string Explanation, string NextStep, List<string>? Citations);

    [HttpPost]
    public async Task<IActionResult> SearchSources([FromRoute] int id, string? question, CancellationToken ct, bool tutor = false)
    {
        var course = await Enrolled.AsNoTracking().SingleOrDefaultAsync(c => c.Id == id, ct);
        if (course == null) return NotFound();
        ViewData["CourseId"] = id;
        ViewData["CourseName"] = course.Name;
        question = question?.Trim();
        if (string.IsNullOrEmpty(question) || question.Length > 4000)
            return AIUnavailable(id, "Enter a question between 1 and 4000 characters.", 400);

        ViewData["Question"] = question;
        var readyCount = await database.CourseMaterials.CountAsync(m => m.CourseId == id && m.Status == MaterialStatus.Ready, ct);
        ViewData["ReadyMaterialCount"] = readyCount;
        if (readyCount == 0)
        {
            if (!await Enrolled.AnyAsync(c => c.Id == id, ct)) return NotFound();
            return View("SearchSources", new List<CourseSource>());
        }

        var response = await ai.PostJsonAsync<SourceRequest, SourceResponse>("rag/retrieve", new(question, id, 20), ct);
        if (!response.Success || response.Data?.CourseId != id || response.Data.Results is null)
            return AIUnavailable(id, "Course sources could not be searched right now. Check that the AI service is running and try again.");

        var hits = response.Data.Results
            .Where(hit => hit?.Metadata is { CourseId: var courseId, MaterialId: > 0, PageNumber: > 0 } && courseId == id
                && !string.IsNullOrWhiteSpace(hit.Metadata.ChunkId)
                && double.IsFinite(hit.Score) && hit.Score >= options.Value.SourceMinimumScore && hit.Score <= 1.000001
                && !string.IsNullOrWhiteSpace(hit.Text))
            .OrderByDescending(hit => hit.Score)
            .DistinctBy(hit => (hit.Metadata!.MaterialId, hit.Metadata.ChunkId)).ToList();
        var ids = hits.Select(hit => hit.Metadata!.MaterialId).Distinct().ToArray();
        var allowed = await database.CourseMaterials.AsNoTracking()
            .Where(m => m.CourseId == id && m.Status == MaterialStatus.Ready && ids.Contains(m.Id)
                && Enrolled.Any(c => c.Id == m.CourseId))
            .ToDictionaryAsync(m => m.Id, m => m.OriginalFileName, ct);
        var sources = hits.Where(hit => allowed.ContainsKey(hit.Metadata!.MaterialId)).Take(3)
            .Select(hit => new CourseSource(allowed[hit.Metadata!.MaterialId], hit.Metadata.PageNumber, hit.Text)
            { MaterialId = hit.Metadata.MaterialId, ChunkId = hit.Metadata.ChunkId, Similarity = hit.Score }).ToList();
        if (!await Enrolled.AnyAsync(c => c.Id == id, ct)) return NotFound();
        ViewData["CourseId"] = id;
        if (!tutor) return View(sources);
        course = await Enrolled.AsNoTracking().SingleOrDefaultAsync(c => c.Id == id, ct);
        if (course == null) return NotFound();
        var tutorSources = sources.Select((source, index) => new TutorSource($"s{index+1}", source.Text)).ToList();
        var reply = await ai.PostJsonAsync<TutorRequest, TutorResponse>("tutor/answer",
            new(question, new(id, course.EducationLevel.ToString(), course.GradeLevel), tutorSources), ct);
        if (!await Enrolled.AnyAsync(c => c.Id == id, ct)) return NotFound();
        if (!reply.Success || reply.Data == null || string.IsNullOrWhiteSpace(reply.Data.Explanation)
            || reply.Data.Status is not ("answered" or "no_source" or "guidance_only")
            || reply.Data.Citations == null
            || reply.Data.Citations.Any(key => !tutorSources.Any(source => source.SourceId == key)))
            return AIUnavailable(id, "The AI Tutor could not prepare a reply. Please try again later.");
        ViewData["Sources"] = sources;
        return View("Tutor", reply.Data);
    }
}
