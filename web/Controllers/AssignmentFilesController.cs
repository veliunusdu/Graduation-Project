using System.Security.Claims;
using HomeworkPlatform.Web.Data;
using HomeworkPlatform.Web.Security;
using HomeworkPlatform.Web.Services.Files;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace HomeworkPlatform.Web.Controllers;
[Authorize(Roles = AppRoles.Student + "," + AppRoles.Teacher)]
public class AssignmentFilesController(ApplicationDbContext database, AssignmentFileStore files) : Controller
{
    [HttpGet] public async Task<IActionResult> Download(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var teacher = User.IsInRole(AppRoles.Teacher);
        var student = User.IsInRole(AppRoles.Student);
        var attachment = await database.AssignmentAttachments.AsNoTracking().SingleOrDefaultAsync(a => a.Id == id &&
            ((teacher && a.Assignment.Course.TeacherId == userId) ||
             (student && a.Assignment.Course.IsActive && database.StudentCourses.Any(e => e.StudentId == userId && e.CourseId == a.Assignment.CourseId))));
        if (attachment == null) return NotFound();
        var stream = files.OpenRead(attachment.StoredFileName);
        if (stream == null) return NotFound();
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers.CacheControl = "no-store";
        return File(stream, "application/octet-stream", attachment.OriginalFileName);
    }
}
