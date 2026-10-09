using HomeworkPlatform.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace HomeworkPlatform.Web.Controllers;

[Authorize(Roles = AppRoles.Student)]
public class StudentController : Controller
{
    [HttpGet] public IActionResult Index() => View();
}
