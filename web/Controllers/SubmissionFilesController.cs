using System.Security.Claims;
using HomeworkPlatform.Web.Data;
using HomeworkPlatform.Web.Security;
using HomeworkPlatform.Web.Services.Files;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HomeworkPlatform.Web.Controllers;

[Authorize(Roles = AppRoles.Student + "," + AppRoles.Teacher)]
public class SubmissionFilesController(ApplicationDbContext database, SubmissionFileStore files) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Download([FromRoute] int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isTeacher = User.IsInRole(AppRoles.Teacher);
        var isStudent = User.IsInRole(AppRoles.Student);
        var submission = await database.Submissions.AsNoTracking().SingleOrDefaultAsync(s => s.Id == id &&
            ((isStudent && s.StudentId == userId) || (isTeacher && s.Assignment.Course.TeacherId == userId)));
        if (submission?.StoredFileName == null) return NotFound();
        var stream = files.OpenRead(submission.StoredFileName);
        if (stream == null) return NotFound();
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers.CacheControl = "no-store";
        return File(stream, "application/octet-stream", submission.OriginalFileName ?? "submission", enableRangeProcessing: false);
    }
}
