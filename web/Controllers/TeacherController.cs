using HomeworkPlatform.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace HomeworkPlatform.Web.Controllers;

[Authorize(Roles = AppRoles.Teacher)]
public class TeacherController : Controller
{
    [HttpGet] public IActionResult Index() => View();
}
