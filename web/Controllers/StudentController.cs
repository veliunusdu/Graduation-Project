using HomeworkPlatform.Web.Data;
using HomeworkPlatform.Web.Models;
using HomeworkPlatform.Web.Security;
using HomeworkPlatform.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
namespace HomeworkPlatform.Web.Controllers;
[Authorize(Roles = AppRoles.Student)]
public class StudentController(UserManager<ApplicationUser> users, StudentPanelReader panel) : Controller
{
    [HttpGet] public async Task<IActionResult> Index()
    {
        var user = await users.GetUserAsync(User);
        if (user == null) return Challenge();
        return View(new StudentDashboardViewModel(user, await panel.Courses(user.Id), await panel.Assignments(user.Id)));
    }
    [HttpGet] public async Task<IActionResult> Search(string? q)
    {
        var id = users.GetUserId(User)!;
        q = (q ?? "").Trim(); if (q.Length > 200) q = q[..200];
        if (q.Length == 0) return View(new StudentSearchViewModel(q, [], []));
        var courses = (await panel.Courses(id)).Where(c => c.Course.Name.Contains(q, StringComparison.OrdinalIgnoreCase) || c.Course.Description.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
        var assignments = (await panel.Assignments(id)).Where(a => a.Assignment.Title.Contains(q, StringComparison.OrdinalIgnoreCase) || a.Assignment.Description.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
        return View(new StudentSearchViewModel(q, courses, assignments));
    }
}
