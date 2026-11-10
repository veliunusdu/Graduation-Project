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
public class StudentCoursesController(ApplicationDbContext database, AIServiceClient ai, IOptions<AIServiceOptions> options) : Controller
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private IQueryable<Course> Enrolled => database.Courses.Where(c => c.IsActive &&
        database.StudentCourses.Any(e => e.CourseId == c.Id && e.StudentId == UserId));
    [HttpGet] public async Task<IActionResult> Index() => View(await Enrolled.AsNoTracking().OrderBy(c => c.Name).ToListAsync());
    [HttpGet] public async Task<IActionResult> Details([FromRoute] int id)
    {
        var course = await Enrolled.AsNoTracking().SingleOrDefaultAsync(c => c.Id == id);
        if (course == null) return NotFound();
        ViewData["Assignments"] = await database.Assignments.AsNoTracking().Where(a => a.CourseId == id).OrderBy(a => a.Title).ToListAsync();
        return View(course);
    }

    public record SourceRequest(string Question, int CourseId, int TopK);
    public record SourceMetadata(int CourseId, int MaterialId, int PageNumber, string ChunkId);
    public record SourceHit(string Text, double Score, SourceMetadata? Metadata);
    public record SourceResponse(int CourseId, List<SourceHit>? Results);
    public record CourseSource(string FileName, int PageNumber, string Text);

    [HttpPost]
    public async Task<IActionResult> SearchSources([FromRoute] int id, string? question, CancellationToken ct)
    {
        if (!await Enrolled.AnyAsync(c => c.Id == id, ct)) return NotFound();
        question = question?.Trim();
        if (string.IsNullOrEmpty(question) || question.Length > 4000)
            return BadRequest("1–4000 karakterlik bir soru girin.");

        var response = await ai.PostJsonAsync<SourceRequest, SourceResponse>("rag/retrieve", new(question, id, 20), ct);
        if (!response.Success || response.Data?.CourseId != id || response.Data.Results is null)
            return StatusCode(502, "Ders kaynakları şu anda aranamadı.");

        var hits = response.Data.Results
            .Where(hit => hit?.Metadata is { CourseId: var courseId, PageNumber: > 0 } && courseId == id
                && double.IsFinite(hit.Score) && hit.Score >= options.Value.SourceMinimumScore && hit.Score <= 1.000001
                && !string.IsNullOrWhiteSpace(hit.Text))
            .OrderByDescending(hit => hit.Score).ToList();
        var ids = hits.Select(hit => hit.Metadata!.MaterialId).Distinct().ToArray();
        var allowed = await database.CourseMaterials.AsNoTracking()
            .Where(m => m.CourseId == id && m.Status == MaterialStatus.Ready && ids.Contains(m.Id)
                && Enrolled.Any(c => c.Id == m.CourseId))
            .ToDictionaryAsync(m => m.Id, m => m.OriginalFileName, ct);
        var sources = hits.Where(hit => allowed.ContainsKey(hit.Metadata!.MaterialId)).Take(3)
            .Select(hit => new CourseSource(allowed[hit.Metadata!.MaterialId], hit.Metadata.PageNumber, hit.Text)).ToList();
        if (!await Enrolled.AnyAsync(c => c.Id == id, ct)) return NotFound();
        ViewData["CourseId"] = id;
        return View(sources);
    }
}
