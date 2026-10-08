using System.Security.Claims;
using HomeworkPlatform.Web.Data;
using HomeworkPlatform.Web.Models.Domain;
using HomeworkPlatform.Web.Models.Work;
using HomeworkPlatform.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace HomeworkPlatform.Web.Controllers;

[Authorize(Roles = AppRoles.Teacher)]
public class AssignmentsController(ApplicationDbContext database) : Controller
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private IQueryable<Assignment> Owned => database.Assignments.Include(a => a.Course).Where(a => a.Course.TeacherId == UserId);
    private Task<bool> OwnsCourse(int courseId) => database.Courses.AnyAsync(c => c.Id == courseId && c.TeacherId == UserId);

    [HttpGet] public async Task<IActionResult> Index([FromQuery] int? courseId)
    {
        if (courseId.HasValue && !await OwnsCourse(courseId.Value)) return NotFound();
        var query = Owned.AsNoTracking();
        if (courseId.HasValue) query = query.Where(a => a.CourseId == courseId.Value);
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
        return View(new AssignmentForm());
    }
    [HttpPost] public async Task<IActionResult> Create([FromQuery] int courseId, AssignmentForm model)
    {
        if (!await OwnsCourse(courseId)) return NotFound();
        ViewData["CourseId"] = courseId;
        if (!ModelState.IsValid) return View(model);
        var assignment = new Assignment { CourseId = courseId, Title = model.Title.Trim(), Description = model.Description ?? "" };
        database.Assignments.Add(assignment);
        await database.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id = assignment.Id });
    }
    [HttpGet] public async Task<IActionResult> Edit([FromRoute] int id)
    {
        var assignment = await Owned.AsNoTracking().SingleOrDefaultAsync(a => a.Id == id);
        return assignment == null ? NotFound() : View(new AssignmentForm { Title = assignment.Title, Description = assignment.Description });
    }
    [HttpPost] public async Task<IActionResult> Edit([FromRoute] int id, AssignmentForm model)
    {
        var assignment = await Owned.SingleOrDefaultAsync(a => a.Id == id);
        if (assignment == null) return NotFound();
        if (!ModelState.IsValid) return View(model);
        assignment.Title = model.Title.Trim(); assignment.Description = model.Description ?? "";
        await database.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id });
    }
    [HttpGet] public async Task<IActionResult> Delete([FromRoute] int id)
    {
        var assignment = await Owned.AsNoTracking().SingleOrDefaultAsync(a => a.Id == id);
        return assignment == null ? NotFound() : View(assignment);
    }
    [HttpPost, ActionName("Delete")] public async Task<IActionResult> DeleteConfirmed([FromRoute] int id)
    {
        var assignment = await Owned.SingleOrDefaultAsync(a => a.Id == id);
        if (assignment == null) return NotFound();
        if (await database.Submissions.AnyAsync(s => s.AssignmentId == id)) return Conflict("This assignment has student work and cannot be deleted.");
        database.Assignments.Remove(assignment);
        try { await database.SaveChangesAsync(); }
        catch (DbUpdateException) { return Conflict("This assignment has dependent records and cannot be deleted."); }
        return RedirectToAction(nameof(Index), new { courseId = assignment.CourseId });
    }
    [HttpGet] public async Task<IActionResult> Submissions([FromRoute] int id)
    {
        if (!await Owned.AnyAsync(a => a.Id == id)) return NotFound();
        return View(await database.Submissions.AsNoTracking().Where(s => s.AssignmentId == id).OrderBy(s => s.CreatedAt).ToListAsync());
    }
    [HttpGet] public async Task<IActionResult> Submission([FromRoute] int id)
    {
        var submission = await database.Submissions.AsNoTracking().Include(s => s.Assignment)
            .SingleOrDefaultAsync(s => s.Id == id && s.Assignment.Course.TeacherId == UserId);
        return submission == null ? NotFound() : View(submission);
    }
}
