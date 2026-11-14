using System.Security.Claims;
using HomeworkPlatform.Web.Data;
using HomeworkPlatform.Web.Models.Domain;
using HomeworkPlatform.Web.Models.Work;
using HomeworkPlatform.Web.Security;
using HomeworkPlatform.Web.Services.Files;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace HomeworkPlatform.Web.Controllers;

[Authorize(Roles = AppRoles.Teacher)]
[RequestSizeLimit(SubmissionFileOptions.RequestSizeLimit)]
[RequestFormLimits(MultipartBodyLengthLimit = SubmissionFileOptions.RequestSizeLimit)]
public class AssignmentsController(ApplicationDbContext database, AssignmentFileStore files, ILogger<AssignmentsController> logger) : Controller
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
    [HttpPost] public async Task<IActionResult> Create([FromQuery] int courseId, AssignmentForm model)
    {
        if (!await OwnsCourse(courseId)) return NotFound();
        ViewData["CourseId"] = courseId;
        ViewData["CourseName"] = await database.Courses.Where(c => c.Id == courseId && c.TeacherId == UserId).Select(c => c.Name).SingleAsync();
        if (!ModelState.IsValid) return View(model);
        var assignment = new Assignment { CourseId = courseId, Title = model.Title.Trim(), Description = model.Description ?? "" };
        ApplyForm(assignment, model);
        if (!await AddFiles(assignment, model)) return View(model);
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
