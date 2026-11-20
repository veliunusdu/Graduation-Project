using HomeworkPlatform.Web.Data;
using HomeworkPlatform.Web.Models;
using HomeworkPlatform.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace HomeworkPlatform.Web.Controllers;

[Authorize(Roles = AppRoles.Teacher)]
public class TeacherController(ApplicationDbContext database, UserManager<ApplicationUser> users) : Controller
{
    private string UserId => users.GetUserId(User)!;

    [HttpGet] public async Task<IActionResult> Search(string? q)
    {
        q = (q ?? "").Trim();
        if (q.Length > 200) q = q[..200];
        var courses = database.Courses.AsNoTracking().Where(c => c.TeacherId == UserId);
        var assignments = database.Assignments.AsNoTracking().Include(a => a.Course).Where(a => a.Course.TeacherId == UserId);
        if (q.Length == 0) return View(new TeacherSearchViewModel(q, [], []));
        var term = q.ToLowerInvariant();
        return View(new TeacherSearchViewModel(q,
            await courses.Where(c => c.Name.ToLower().Contains(term) || c.Description.ToLower().Contains(term)).OrderBy(c => c.Name).Take(100).ToListAsync(),
            await assignments.Where(a => a.Title.ToLower().Contains(term) || a.Description.ToLower().Contains(term)).OrderBy(a => a.Title).Take(100).ToListAsync()));
    }

    [HttpGet] public async Task<IActionResult> Submissions()
    {
        var rows = await (from s in database.Submissions.AsNoTracking().Include(s => s.Assignment).ThenInclude(a => a.Course)
            join student in database.Users.AsNoTracking() on s.StudentId equals student.Id
            where s.Assignment.Course.TeacherId == UserId
            orderby s.UpdatedAt descending
            select new TeacherSubmissionRow(s, (student.FirstName + " " + student.LastName).Trim(), student.Email ?? "")).ToListAsync();
        return View(rows);
    }

    [HttpGet] public async Task<IActionResult> Index()
    {
        var user = await users.GetUserAsync(User);
        if (user == null) return Challenge();
        var courses = await database.Courses.AsNoTracking().Where(c => c.TeacherId == user.Id).OrderBy(c => c.Name).ToListAsync();
        var counts = await database.StudentCourses.Where(e => e.Course.TeacherId == user.Id)
            .GroupBy(e => e.CourseId).ToDictionaryAsync(g => g.Key, g => g.Count());
        return View(new DashboardViewModel(user, courses) { EnrollmentCounts = counts, PendingReviews = await database.Submissions.CountAsync(s => s.Assignment.Course.TeacherId == user.Id && s.ReviewedAt == null) });
    }
}
