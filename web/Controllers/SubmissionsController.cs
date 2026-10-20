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
    private IQueryable<Submission> Owned => database.Submissions.Include(s => s.Assignment).Where(s => s.StudentId == UserId);
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
        ViewData["AssignmentId"] = assignmentId;
        return View(new SubmissionForm());
    }
    [HttpPost] public async Task<IActionResult> Create([FromQuery] int assignmentId, SubmissionForm model)
    {
        if (!await CanSubmit(assignmentId)) return NotFound();
        ViewData["AssignmentId"] = assignmentId;
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
            ModelState.AddModelError("", "Teslim kaydedilemedi. Teslimlerinizi kontrol edip tekrar deneyin.");
            return View(model);
        }
        return RedirectToAction(nameof(Details), new { id = submission.Id });
    }
    [HttpGet] public async Task<IActionResult> Edit([FromRoute] int id)
    {
        var submission = await Owned.AsNoTracking().SingleOrDefaultAsync(s => s.Id == id);
        return submission == null ? NotFound() : View(new SubmissionForm { Content = submission.Content, ExistingFileName = submission.OriginalFileName });
    }
    [HttpPost] public async Task<IActionResult> Edit([FromRoute] int id, SubmissionForm model)
    {
        var submission = await Owned.SingleOrDefaultAsync(s => s.Id == id);
        if (submission == null) return NotFound();
        model.ExistingFileName = submission.OriginalFileName;
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
            ModelState.AddModelError("", "Teslim değişiklikleri kaydedilemedi. Lütfen tekrar deneyin.");
            return View(model);
        }
        if (oldFile != submission.StoredFileName) files.DeleteIfPresent(oldFile);
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
            ModelState.AddModelError("", "Teslim silinemedi. Lütfen tekrar deneyin.");
            return View("Delete", submission);
        }
        files.DeleteIfPresent(submission.StoredFileName);
        return RedirectToAction(nameof(Index));
    }

    private void ValidateContents(SubmissionForm model, bool hasExistingFile)
    {
        if (string.IsNullOrWhiteSpace(model.Content) && model.Upload == null && !(hasExistingFile && !model.RemoveFile))
            ModelState.AddModelError("", "Teslim metni yazın veya bir ödev dosyası seçin.");
        if (model.RemoveFile && model.Upload != null)
            ModelState.AddModelError(nameof(model.Upload), "Yeni dosya yüklerken mevcut dosyayı kaldır seçeneğini işaretlemeyin; dosya otomatik değiştirilir.");
    }

    private async Task<StoredSubmissionFile?> SaveUpload(SubmissionForm model)
    {
        if (model.Upload == null) return null;
        try { return await files.SaveAsync(model.Upload, HttpContext.RequestAborted); }
        catch (SubmissionFileException exception) { ModelState.AddModelError(nameof(model.Upload), exception.Message); }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogError(exception, "Submission file could not be stored.");
            ModelState.AddModelError(nameof(model.Upload), "Dosya kaydedilemedi. Lütfen tekrar deneyin.");
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
