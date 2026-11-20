using HomeworkPlatform.Web.Data;
using HomeworkPlatform.Web.Models;
using HomeworkPlatform.Web.Models.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
namespace HomeworkPlatform.Web.ViewComponents;
public class TeacherProfileViewComponent(UserManager<ApplicationUser> users) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var user = await users.GetUserAsync(HttpContext.User);
        var name = $"{user?.FirstName} {user?.LastName}".Trim();
        return View(new TeacherProfileViewModel(string.IsNullOrWhiteSpace(name) ? user?.Email ?? "Teacher" : name,
            user?.Email ?? "", user == null ? "" : EducationRules.Label(user.EducationLevel)));
    }
}
