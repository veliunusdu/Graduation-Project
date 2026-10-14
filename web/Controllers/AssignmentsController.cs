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
    private IQueryable<Assignment> Owned => database.Assignments.Include(a => a.Course).Include(a => a.Requirements).Where(a => a.Course.TeacherId == UserId);
    private Task<bool> OwnsCourse(int courseId) => database.Courses.AnyAsync(c => c.Id == courseId && c.TeacherId == UserId);

    private static void ApplyForm(Assignment assignment, AssignmentForm model)
    {
        assignment.Title = model.Title.Trim(); assignment.Description = model.Description ?? "";
        assignment.Difficulty = model.Difficulty; assignment.DueDate = model.DueDate;
        assignment.Requirements = model.Requirements.Select(r => new AssignmentRequirement {
            Title = r.Title.Trim(), Description = r.Description ?? "", Weight = r.Weight, Order = r.Order
        }).ToList();
    }
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
        return View(new AssignmentForm { Requirements = [new RequirementForm { Weight = 100, Order = 1 }] });
    }
    [HttpPost] public async Task<IActionResult> Create([FromQuery] int courseId, AssignmentForm model)
    {
        if (!await OwnsCourse(courseId)) return NotFound();
        ViewData["CourseId"] = courseId;
        if (!ModelState.IsValid) return View(model);
        var assignment = new Assignment { CourseId = courseId, Title = model.Title.Trim(), Description = model.Description ?? "" };
        ApplyForm(assignment, model);
        database.Assignments.Add(assignment);
        await database.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id = assignment.Id });
    }
    [HttpGet] public async Task<IActionResult> Edit([FromRoute] int id)
    {
        var assignment = await Owned.AsNoTracking().SingleOrDefaultAsync(a => a.Id == id);
        return assignment == null ? NotFound() : View(new AssignmentForm { Title = assignment.Title, Description = assignment.Description, Difficulty = assignment.Difficulty, DueDate = assignment.DueDate, Requirements = assignment.Requirements.OrderBy(r => r.Order).Select(r => new RequirementForm { Title = r.Title, Description = r.Description, Weight = r.Weight, Order = r.Order }).ToList() });
    }
    [HttpPost] public async Task<IActionResult> Edit([FromRoute] int id, AssignmentForm model)
    {
        var assignment = await Owned.SingleOrDefaultAsync(a => a.Id == id);
        if (assignment == null) return NotFound();
        if (!ModelState.IsValid) return View(model);
        database.AssignmentRequirements.RemoveRange(assignment.Requirements);
        ApplyForm(assignment, model);
        await database.SaveChangesAsync();
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
