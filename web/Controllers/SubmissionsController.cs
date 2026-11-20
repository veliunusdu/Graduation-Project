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

[Authorize(Roles = AppRoles.Student)]
[RequestSizeLimit(SubmissionFileOptions.RequestSizeLimit)]
[RequestFormLimits(MultipartBodyLengthLimit = SubmissionFileOptions.RequestSizeLimit)]
public class SubmissionsController(ApplicationDbContext database, SubmissionFileStore files, ILogger<SubmissionsController> logger) : Controller
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private IQueryable<Submission> Owned => database.Submissions.Include(s => s.Assignment).ThenInclude(a => a.Course).Include(s => s.Assignment).ThenInclude(a => a.Attachments).Where(s => s.StudentId == UserId);
    private Task<bool> CanSubmit(int assignmentId) => database.Assignments.AnyAsync(a => a.Id == assignmentId && a.Course.IsActive
        && database.StudentCourses.Any(e => e.CourseId == a.CourseId && e.StudentId == UserId));

    [HttpGet] public async Task<IActionResult> Index() => View(await Owned.AsNoTracking().OrderByDescending(s => s.UpdatedAt).ToListAsync());
    [HttpGet] public async Task<IActionResult> Details([FromRoute] int id)
    {
        var submission = await Owned.AsNoTracking().SingleOrDefaultAsync(s => s.Id == id);
        return submission == null ? NotFound() : View(submission);
    }
    [HttpGet] public async Task<IActionResult> Create([FromQuery] int assignmentId)
    {
        if (!await CanSubmit(assignmentId)) return NotFound();
        var existingId = await Owned.Where(s => s.AssignmentId == assignmentId).Select(s => (int?)s.Id).SingleOrDefaultAsync();
        if (existingId.HasValue) return RedirectToAction(nameof(Details), new { id = existingId.Value });
        ViewData["AssignmentId"] = assignmentId;
        ViewData["Assignment"] = await database.Assignments.AsNoTracking().Include(a => a.Course).Include(a => a.Attachments).SingleAsync(a => a.Id == assignmentId);
        return View(new SubmissionForm());
    }
    [HttpPost] public async Task<IActionResult> Create([FromQuery] int assignmentId, SubmissionForm model)
    {
        if (!await CanSubmit(assignmentId)) return NotFound();
        ViewData["AssignmentId"] = assignmentId;
        ViewData["Assignment"] = await database.Assignments.AsNoTracking().Include(a => a.Course).Include(a => a.Attachments).SingleAsync(a => a.Id == assignmentId);
        ValidateContents(model, false);
        if (!ModelState.IsValid) return View(model);
        if (await database.Submissions.AnyAsync(s => s.AssignmentId == assignmentId && s.StudentId == UserId))
            return Conflict("You already submitted this assignment. Edit your existing submission instead.");
        var upload = await SaveUpload(model);
        if (!ModelState.IsValid) return View(model);
        var submission = new Submission { AssignmentId = assignmentId, StudentId = UserId, Content = model.Content ?? "" };
        SetFile(submission, upload);
        database.Submissions.Add(submission);
        try { await database.SaveChangesAsync(); }
        catch (DbUpdateException)
        {
            files.DeleteIfPresent(upload?.StoredName);
            ModelState.AddModelError("", "Your submission could not be saved. Check your submissions and try again.");
            return View(model);
        }
        TempData["SubmissionNotice"] = "Your assignment was submitted successfully.";
        return RedirectToAction(nameof(Details), new { id = submission.Id });
    }
    [HttpGet] public async Task<IActionResult> Edit([FromRoute] int id)
    {
        var submission = await Owned.AsNoTracking().SingleOrDefaultAsync(s => s.Id == id);
        if (submission == null) return NotFound();
        ViewData["Assignment"] = submission.Assignment;
        return View(new SubmissionForm { Content = submission.Content, ExistingFileName = submission.OriginalFileName });
    }
    [HttpPost] public async Task<IActionResult> Edit([FromRoute] int id, SubmissionForm model)
    {
        var submission = await Owned.SingleOrDefaultAsync(s => s.Id == id);
        if (submission == null) return NotFound();
        model.ExistingFileName = submission.OriginalFileName;
        ViewData["Assignment"] = submission.Assignment;
        ValidateContents(model, submission.StoredFileName != null);
        if (!ModelState.IsValid) return View(model);
        var upload = await SaveUpload(model);
        if (!ModelState.IsValid) return View(model);
        var oldFile = submission.StoredFileName;
        if (upload != null) SetFile(submission, upload);
        else if (model.RemoveFile) SetFile(submission, null);
        submission.Content = model.Content ?? ""; submission.UpdatedAt = DateTime.UtcNow;
        submission.TeacherFeedback = null; submission.TeacherGrade = null; submission.ReviewedAt = null;
        try { await database.SaveChangesAsync(); }
        catch (DbUpdateException)
        {
            files.DeleteIfPresent(upload?.StoredName);
            ModelState.AddModelError("", "Your changes could not be saved. Please try again.");
            return View(model);
        }
        if (oldFile != submission.StoredFileName) files.DeleteIfPresent(oldFile);
        TempData["SubmissionNotice"] = "Your submission was updated and is awaiting review.";
        return RedirectToAction(nameof(Details), new { id });
    }
    [HttpGet] public async Task<IActionResult> Delete([FromRoute] int id)
    {
        var submission = await Owned.AsNoTracking().SingleOrDefaultAsync(s => s.Id == id);
        return submission == null ? NotFound() : View(submission);
    }
    [HttpPost, ActionName("Delete")] public async Task<IActionResult> DeleteConfirmed([FromRoute] int id)
    {
        var submission = await Owned.SingleOrDefaultAsync(s => s.Id == id);
        if (submission == null) return NotFound();
        database.Submissions.Remove(submission);
        try { await database.SaveChangesAsync(); }
        catch (DbUpdateException)
        {
            ModelState.AddModelError("", "Your submission could not be deleted. Please try again.");
            return View("Delete", submission);
        }
        files.DeleteIfPresent(submission.StoredFileName);
        TempData["SubmissionNotice"] = "Your submission was withdrawn. You can submit your work again.";
        if (await CanSubmit(submission.AssignmentId)) return RedirectToAction("Details", "StudentWork", new { id = submission.AssignmentId });
        return RedirectToAction(nameof(Index));
    }

    private void ValidateContents(SubmissionForm model, bool hasExistingFile)
    {
        if (string.IsNullOrWhiteSpace(model.Content) && model.Upload == null && !(hasExistingFile && !model.RemoveFile))
            ModelState.AddModelError("", "Write your answer or choose a file to submit.");
        if (model.RemoveFile && model.Upload != null)
            ModelState.AddModelError(nameof(model.Upload), "Do not select Remove current file when uploading a replacement.");
    }

    private async Task<StoredSubmissionFile?> SaveUpload(SubmissionForm model)
    {
        if (model.Upload == null) return null;
        try { return await files.SaveAsync(model.Upload, HttpContext.RequestAborted); }
        catch (SubmissionFileException) { ModelState.AddModelError(nameof(model.Upload), "Choose a valid PDF, DOC, DOCX, ZIP or supported UTF-8 code/text file within the size limit."); }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogError(exception, "Submission file could not be stored.");
            ModelState.AddModelError(nameof(model.Upload), "The file could not be saved. Please try again.");
        }
        return null;
    }

    private static void SetFile(Submission submission, StoredSubmissionFile? file)
    {
        submission.StoredFileName = file?.StoredName;
        submission.OriginalFileName = file?.OriginalName;
        submission.FileSize = file?.Size;
        submission.FileUploadedAt = file == null ? null : DateTime.UtcNow;
    }
}
