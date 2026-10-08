using System.Security.Claims;
using HomeworkPlatform.Web.Data;
using HomeworkPlatform.Web.Models.Domain;
using HomeworkPlatform.Web.Models.Work;
using HomeworkPlatform.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace HomeworkPlatform.Web.Controllers;

[Authorize(Roles = AppRoles.Teacher)]
public class CoursesController(ApplicationDbContext database, UserManager<ApplicationUser> users) : Controller
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private IQueryable<Course> Owned => database.Courses.Where(c => c.TeacherId == UserId);

    [HttpGet] public async Task<IActionResult> Index() => View(await Owned.AsNoTracking().OrderBy(c => c.Name).ToListAsync());
    [HttpGet] public async Task<IActionResult> Details([FromRoute] int id)
    {
        var course = await Owned.AsNoTracking().SingleOrDefaultAsync(c => c.Id == id);
        return course == null ? NotFound() : await DetailsView(course);
    }
    [HttpGet] public IActionResult Create() => View(new CourseForm());
    [HttpPost] public async Task<IActionResult> Create(CourseForm model)
    {
        if (!ModelState.IsValid) return View(model);
        var course = new Course { Name = model.Name.Trim(), Description = model.Description ?? "", TeacherId = UserId };
        database.Courses.Add(course);
        await database.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id = course.Id });
    }
    [HttpGet] public async Task<IActionResult> Edit([FromRoute] int id)
    {
        var course = await Owned.AsNoTracking().SingleOrDefaultAsync(c => c.Id == id);
        return course == null ? NotFound() : View(new CourseForm { Name = course.Name, Description = course.Description });
    }
    [HttpPost] public async Task<IActionResult> Edit([FromRoute] int id, CourseForm model)
    {
        var course = await Owned.SingleOrDefaultAsync(c => c.Id == id);
        if (course == null) return NotFound();
        if (!ModelState.IsValid) return View(model);
        course.Name = model.Name.Trim(); course.Description = model.Description ?? "";
        await database.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id });
    }
    [HttpGet] public async Task<IActionResult> Delete([FromRoute] int id)
    {
        var course = await Owned.AsNoTracking().SingleOrDefaultAsync(c => c.Id == id);
        return course == null ? NotFound() : View(course);
    }
    [HttpPost, ActionName("Delete")] public async Task<IActionResult> DeleteConfirmed([FromRoute] int id)
    {
        var course = await Owned.SingleOrDefaultAsync(c => c.Id == id);
        if (course == null) return NotFound();
        if (await database.Assignments.AnyAsync(a => a.CourseId == id)) return Conflict("Remove empty assignments before deleting this course. Student work will not be deleted.");
        database.Courses.Remove(course);
        try { await database.SaveChangesAsync(); }
        catch (DbUpdateException) { return Conflict("This course has dependent records and cannot be deleted."); }
        return RedirectToAction(nameof(Index));
    }
    private async Task<IActionResult> DetailsView(Course course)
    {
        ViewData["Students"] = await (from enrollment in database.StudentCourses
            join student in database.Users on enrollment.StudentId equals student.Id
            where enrollment.CourseId == course.Id orderby student.Email select student.Email!).ToListAsync();
        return View("Details", course);
    }
}
