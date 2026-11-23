using System.Security.Claims;
using HomeworkPlatform.Web.Data;
using HomeworkPlatform.Web.Models.Domain;
using HomeworkPlatform.Web.Models.Work;
using HomeworkPlatform.Web.Security;
using HomeworkPlatform.Web.Services.AI;
using HomeworkPlatform.Web.Services.Files;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace HomeworkPlatform.Web.Controllers;

[Authorize(Roles = AppRoles.Teacher)]
[RequestSizeLimit(SubmissionFileOptions.RequestSizeLimit)]
[RequestFormLimits(MultipartBodyLengthLimit = SubmissionFileOptions.RequestSizeLimit)]
public class AssignmentsController(ApplicationDbContext database, AssignmentFileStore files, AIServiceClient ai, ILogger<AssignmentsController> logger) : Controller
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private IQueryable<Assignment> Owned => database.Assignments.Include(a => a.Course).Include(a => a.Requirements).Include(a => a.Attachments).Where(a => a.Course.TeacherId == UserId);
    private Task<bool> OwnsCourse(int courseId) => database.Courses.AnyAsync(c => c.Id == courseId && c.TeacherId == UserId);

    private static void ApplyForm(Assignment assignment, AssignmentForm model)
    {
        assignment.Title = model.Title.Trim(); assignment.Description = model.Description ?? "";
        assignment.DueDate = model.DueDate;
    }
    private async Task<bool> AddFiles(Assignment assignment, AssignmentForm model)
    {
        if (model.Attachments.Count > 5 || assignment.Attachments.Count + model.Attachments.Count > 5)
            ModelState.AddModelError(nameof(model.Attachments), "An assignment can have up to 5 attachments.");
        if (model.Attachments.Sum(f => (decimal)f.Length) > 20 * 1024 * 1024)
            ModelState.AddModelError(nameof(model.Attachments), "New attachments must total 20 MB or less.");
        if (!ModelState.IsValid) return false;
        var saved = new List<AssignmentAttachment>();
        try
        {
            foreach (var upload in model.Attachments)
            {
                var file = await files.SaveAsync(upload, HttpContext.RequestAborted);
                saved.Add(new AssignmentAttachment { StoredFileName = file.StoredName, OriginalFileName = file.OriginalName, FileSize = file.Size });
            }
            assignment.Attachments.AddRange(saved);
            return true;
        }
        catch (Exception exception) when (exception is SubmissionFileException or IOException or UnauthorizedAccessException or OperationCanceledException)
        {
            foreach (var file in saved) files.DeleteIfPresent(file.StoredFileName);
            if (exception is OperationCanceledException) throw;
            logger.LogWarning(exception, "Assignment attachments could not be stored.");
            ModelState.AddModelError(nameof(model.Attachments), "The attachment could not be saved. Use a valid PDF, DOCX, PPTX or UTF-8 TXT file (up to 20 MB total).");
            return false;
        }
    }

    [HttpPost] public async Task<IActionResult> RemoveAttachment(int id)
    {
        var attachment = await database.AssignmentAttachments.Include(a => a.Assignment)
            .SingleOrDefaultAsync(a => a.Id == id && a.Assignment.Course.TeacherId == UserId);
        if (attachment == null) return NotFound();
        database.AssignmentAttachments.Remove(attachment);
        await database.SaveChangesAsync();
        files.DeleteIfPresent(attachment.StoredFileName);
        return RedirectToAction(nameof(Edit), new { id = attachment.AssignmentId });
    }

    [HttpGet] public async Task<IActionResult> Index([FromQuery] int? courseId)
    {
        if (courseId.HasValue && !await OwnsCourse(courseId.Value)) return NotFound();
        var query = Owned.AsNoTracking();
        if (courseId.HasValue) query = query.Where(a => a.CourseId == courseId.Value);
        ViewData["SubmissionCounts"] = await database.Submissions.Where(s => s.Assignment.Course.TeacherId == UserId)
            .GroupBy(s => s.AssignmentId).ToDictionaryAsync(g => g.Key, g => g.Count());
        return View(await query.OrderBy(a => a.Title).ToListAsync());
    }
    [HttpGet] public async Task<IActionResult> Details([FromRoute] int id)
    {
        var assignment = await Owned.AsNoTracking().SingleOrDefaultAsync(a => a.Id == id);
        return assignment == null ? NotFound() : View(assignment);
    }
    [HttpGet] public async Task<IActionResult> Create([FromQuery] int courseId)
    {
        if (!await OwnsCourse(courseId)) return NotFound();
        ViewData["CourseId"] = courseId;
        ViewData["CourseName"] = await database.Courses.Where(c => c.Id == courseId && c.TeacherId == UserId).Select(c => c.Name).SingleAsync();
        return View(new AssignmentForm());
    }

    public record AgentContext(int CourseId, string EducationLevel, int GradeLevel);
    public record DraftRequest(string Topic, AgentContext Context);
    public record DraftResponse(string Status, string? Title, string? Description, string? Difficulty, List<string>? SourceIds);
    public record ReviewRequest(string Title, string Description, AgentContext Context);
    public record ReviewResponse(string Status, string Summary, List<string>? Findings, List<string>? SourceIds);

    [HttpPost]
    public async Task<IActionResult> Draft([FromQuery] int courseId, string topic, CancellationToken cancellationToken)
    {
        var course = await database.Courses.AsNoTracking().SingleOrDefaultAsync(c => c.Id == courseId && c.TeacherId == UserId, cancellationToken);
        if (course == null) return NotFound();
        ViewData["CourseId"] = course.Id;
        ViewData["CourseName"] = course.Name;
        topic = topic?.Trim() ?? "";
        if (topic.Length is < 1 or > 500)
        {
            ModelState.AddModelError("Topic", "Enter a topic of up to 500 characters.");
            return View("Create", new AssignmentForm());
        }
        var result = await ai.PostJsonAsync<DraftRequest, DraftResponse>("assignments/draft",
            new(topic, new(course.Id, course.EducationLevel.ToString(), course.GradeLevel)), cancellationToken);
        if (!result.Success || result.Data == null || result.Data.Status is not ("drafted" or "no_source"))
        {
            ModelState.AddModelError("", "The assignment agent could not draft from the course materials. Check that the AI service is running and try again.");
            return View("Create", new AssignmentForm());
        }
        if (result.Data.Status == "no_source")
        {
            ModelState.AddModelError("", "No ready course material matched this topic. Upload and process materials, then try again.");
            return View("Create", new AssignmentForm { });
        }
        var title = result.Data.Title?.Trim() ?? "";
        var description = result.Data.Description?.Trim() ?? "";
        if (title.Length is < 1 or > 200 || description.Length is < 1 or > 10000
            || result.Data.Difficulty is not ("Beginner" or "Intermediate" or "Advanced"))
        {
            ModelState.AddModelError("", "The assignment agent returned an incomplete draft.");
            return View("Create", new AssignmentForm());
        }
        TempData["AiDraftTitle"] = title;
        TempData["AiDraftDescription"] = description;
        TempData["AiDraftDifficulty"] = result.Data.Difficulty;
        ViewData["DraftNotice"] = "Review the draft, set a due date, then save it.";
        return View("Create", new AssignmentForm { Title = title, Description = description });
    }

    [HttpPost]
    public async Task<IActionResult> Check([FromRoute] int id, CancellationToken cancellationToken)
    {
        var assignment = await Owned.AsNoTracking().SingleOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (assignment == null) return NotFound();
        if (string.IsNullOrWhiteSpace(assignment.Title) || string.IsNullOrWhiteSpace(assignment.Description))
        {
            ViewData["ReviewError"] = "Add a title and requirements before testing this assignment.";
            return View("Details", assignment);
        }
        var result = await ai.PostJsonAsync<ReviewRequest, ReviewResponse>("assignments/review",
            new(assignment.Title, assignment.Description, new(assignment.CourseId, assignment.Course.EducationLevel.ToString(), assignment.Course.GradeLevel)), cancellationToken);
        if (!result.Success || result.Data == null || result.Data.Status is not ("ready" or "revise" or "no_source")
            || string.IsNullOrWhiteSpace(result.Data.Summary))
            ViewData["ReviewError"] = "The test agent could not check this assignment. Check that the AI service is running and try again.";
        else
            ViewData["Review"] = result.Data;
        return View("Details", assignment);
    }

    [HttpPost] public async Task<IActionResult> Create([FromQuery] int courseId, AssignmentForm model)
    {
        if (!await OwnsCourse(courseId)) return NotFound();
        ViewData["CourseId"] = courseId;
        ViewData["CourseName"] = await database.Courses.Where(c => c.Id == courseId && c.TeacherId == UserId).Select(c => c.Name).SingleAsync();
        if (!ModelState.IsValid) return View(model);
        var assignment = new Assignment { CourseId = courseId, Title = model.Title.Trim(), Description = model.Description ?? "" };
        ApplyForm(assignment, model);
        if (!await AddFiles(assignment, model)) return View(model);
        var draftedTitle = TempData["AiDraftTitle"] as string;
        var draftedDescription = TempData["AiDraftDescription"] as string;
        var draftedDifficulty = TempData["AiDraftDifficulty"] as string;
        if (draftedTitle == assignment.Title && draftedDescription == assignment.Description)
        {
            assignment.CreatedByAI = true;
            if (Enum.TryParse<Difficulty>(draftedDifficulty, out var difficulty)) assignment.Difficulty = difficulty;
        }
        database.Assignments.Add(assignment);
        try { await database.SaveChangesAsync(); }
        catch (DbUpdateException exception)
        {
            foreach (var attachment in assignment.Attachments) files.DeleteIfPresent(attachment.StoredFileName);
            logger.LogError(exception, "Assignment could not be saved.");
            ModelState.AddModelError("", "The assignment could not be saved. Please try again.");
            return View(model);
        }
        return RedirectToAction(nameof(Details), new { id = assignment.Id });
    }
    [HttpGet] public async Task<IActionResult> Edit([FromRoute] int id)
    {
        var assignment = await Owned.AsNoTracking().SingleOrDefaultAsync(a => a.Id == id);
        return assignment == null ? NotFound() : View(new AssignmentForm { Title = assignment.Title, Description = assignment.Description, DueDate = assignment.DueDate, ExistingAttachments = assignment.Attachments });
    }
    [HttpPost] public async Task<IActionResult> Edit([FromRoute] int id, AssignmentForm model)
    {
        var assignment = await Owned.SingleOrDefaultAsync(a => a.Id == id);
        if (assignment == null) return NotFound();
        model.ExistingAttachments = assignment.Attachments.ToList();
        if (!ModelState.IsValid) return View(model);
        if (!await AddFiles(assignment, model)) return View(model);
        ApplyForm(assignment, model);
        try { await database.SaveChangesAsync(); }
        catch (DbUpdateException exception)
        {
            foreach (var attachment in assignment.Attachments.Except(model.ExistingAttachments)) files.DeleteIfPresent(attachment.StoredFileName);
            logger.LogError(exception, "Assignment changes could not be saved.");
            ModelState.AddModelError("", "Changes could not be saved. Please try again.");
            return View(model);
        }
        return RedirectToAction(nameof(Details), new { id });
    }
    [HttpGet] public async Task<IActionResult> Delete([FromRoute] int id)
    {
        var assignment = await Owned.AsNoTracking().SingleOrDefaultAsync(a => a.Id == id);
        return assignment == null ? NotFound() : View(assignment);
    }
    private IActionResult DeleteWarning(Assignment assignment, string message)
    {
        Response.StatusCode = StatusCodes.Status409Conflict;
        ViewData["DeleteWarning"] = message;
        return View("Delete", assignment);
    }
    [HttpPost, ActionName("Delete")] public async Task<IActionResult> DeleteConfirmed([FromRoute] int id)
    {
        var assignment = await Owned.SingleOrDefaultAsync(a => a.Id == id);
        if (assignment == null) return NotFound();
        if (await database.Submissions.AnyAsync(s => s.AssignmentId == id)) return DeleteWarning(assignment, "Bu ödev için öğrenci teslimi bulunduğundan ödev silinemez.");
        database.Assignments.Remove(assignment);
        try { await database.SaveChangesAsync(); }
        catch (DbUpdateException) { return DeleteWarning(assignment, "Bu ödeve bağlı kayıtlar bulunduğundan ödev silinemedi."); }
        foreach (var attachment in assignment.Attachments) files.DeleteIfPresent(attachment.StoredFileName);
        return RedirectToAction(nameof(Index), new { courseId = assignment.CourseId });
    }
    [HttpGet] public async Task<IActionResult> Submissions([FromRoute] int id)
    {
        if (!await Owned.AnyAsync(a => a.Id == id)) return NotFound();
        var submissions = await database.Submissions.AsNoTracking()
            .Where(s => s.AssignmentId == id).OrderBy(s => s.CreatedAt).ToListAsync();
        var studentIds = submissions.Select(s => s.StudentId).Distinct().ToList();
        ViewData["StudentEmails"] = await database.Users.AsNoTracking()
            .Where(u => studentIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Email ?? u.UserName ?? "Bilinmeyen öğrenci");
        return View(submissions);
    }
    [HttpPost] public async Task<IActionResult> Review([FromRoute] int id, ReviewForm model)
    {
        var submission = await database.Submissions.Include(s => s.Assignment)
            .SingleOrDefaultAsync(s => s.Id == id && s.Assignment.Course.TeacherId == UserId);
        if (submission == null) return NotFound();
        if (!ModelState.IsValid)
        {
            await LoadStudentEmail(submission.StudentId);
            return View("Submission", submission);
        }
        submission.TeacherFeedback = model.Feedback.Trim(); submission.TeacherGrade = model.Grade;
        submission.ReviewedAt = DateTime.UtcNow;
        await database.SaveChangesAsync();
        return RedirectToAction(nameof(Submission), new { id });
    }
    private async Task LoadStudentEmail(string studentId)
    {
        ViewData["StudentEmail"] = await database.Users.AsNoTracking()
            .Where(u => u.Id == studentId).Select(u => u.Email ?? u.UserName).SingleOrDefaultAsync();
    }
    [HttpGet] public async Task<IActionResult> Submission([FromRoute] int id)
    {
        var submission = await database.Submissions.AsNoTracking().Include(s => s.Assignment)
            .SingleOrDefaultAsync(s => s.Id == id && s.Assignment.Course.TeacherId == UserId);
        if (submission == null) return NotFound();
        await LoadStudentEmail(submission.StudentId);
        return View(submission);
    }
}
