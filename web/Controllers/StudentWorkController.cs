using System.Security.Claims;
using HomeworkPlatform.Web.Data;
using HomeworkPlatform.Web.Models.Domain;
using HomeworkPlatform.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace HomeworkPlatform.Web.Controllers;

[Authorize(Roles = AppRoles.Student)]
public class StudentWorkController(ApplicationDbContext database) : Controller
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private IQueryable<Assignment> Enrolled => database.Assignments.Include(a => a.Course)
        .Where(a => database.StudentCourses.Any(e => e.CourseId == a.CourseId && e.StudentId == UserId));
    [HttpGet] public async Task<IActionResult> Index() => View(await Enrolled.AsNoTracking().OrderBy(a => a.Title).ToListAsync());
    [HttpGet] public async Task<IActionResult> Details([FromRoute] int id)
    {
        var assignment = await Enrolled.AsNoTracking().SingleOrDefaultAsync(a => a.Id == id);
        if (assignment == null) return NotFound();
        ViewData["SubmissionId"] = await database.Submissions.Where(s => s.AssignmentId == id && s.StudentId == UserId).Select(s => (int?)s.Id).SingleOrDefaultAsync();
        return View(assignment);
    }
}
