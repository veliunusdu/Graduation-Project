using HomeworkPlatform.Web.Security;
using HomeworkPlatform.Web.Services.AI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeworkPlatform.Web.Controllers;

[Authorize(Roles = AppRoles.Teacher)]
public class AIStatusController(AIServiceClient client) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
        => View(await client.CheckHealthAsync(cancellationToken));
}
