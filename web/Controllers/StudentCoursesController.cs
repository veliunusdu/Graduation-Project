using System.Security.Claims;
using HomeworkPlatform.Web.Data;
using HomeworkPlatform.Web.Models.Domain;
using HomeworkPlatform.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace HomeworkPlatform.Web.Controllers;
[Authorize(Roles = AppRoles.Student)]
public class StudentCoursesController(ApplicationDbContext database) : Controller
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
}
