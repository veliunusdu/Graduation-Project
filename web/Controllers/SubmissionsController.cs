using System.Security.Claims;
using HomeworkPlatform.Web.Data;
using HomeworkPlatform.Web.Models.Domain;
using HomeworkPlatform.Web.Models.Work;
using HomeworkPlatform.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace HomeworkPlatform.Web.Controllers;

[Authorize(Roles = AppRoles.Student)]
public class SubmissionsController(ApplicationDbContext database) : Controller
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private IQueryable<Submission> Owned => database.Submissions.Include(s => s.Assignment).Where(s => s.StudentId == UserId);
    private Task<bool> CanSubmit(int assignmentId) => database.Assignments.AnyAsync(a => a.Id == assignmentId
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
        if (!ModelState.IsValid) return View(model);
        if (await database.Submissions.AnyAsync(s => s.AssignmentId == assignmentId && s.StudentId == UserId))
            return Conflict("You already submitted this assignment. Edit your existing submission instead.");
        var submission = new Submission { AssignmentId = assignmentId, StudentId = UserId, Content = model.Content };
        database.Submissions.Add(submission);
        try { await database.SaveChangesAsync(); }
        catch (DbUpdateException) { return Conflict("Submission could not be saved. Reload your submissions and try again."); }
        return RedirectToAction(nameof(Details), new { id = submission.Id });
    }
    [HttpGet] public async Task<IActionResult> Edit([FromRoute] int id)
    {
        var submission = await Owned.AsNoTracking().SingleOrDefaultAsync(s => s.Id == id);
        return submission == null ? NotFound() : View(new SubmissionForm { Content = submission.Content });
    }
    [HttpPost] public async Task<IActionResult> Edit([FromRoute] int id, SubmissionForm model)
    {
        var submission = await Owned.SingleOrDefaultAsync(s => s.Id == id);
        if (submission == null) return NotFound();
        if (!ModelState.IsValid) return View(model);
        submission.Content = model.Content; submission.UpdatedAt = DateTime.UtcNow;
        await database.SaveChangesAsync();
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
        await database.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
