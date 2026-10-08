using HomeworkPlatform.Web.Data;
using HomeworkPlatform.Web.Models.Account;
using HomeworkPlatform.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HomeworkPlatform.Web.Controllers;

public class AccountController(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn, ApplicationDbContext database) : Controller
{
    [HttpGet, AllowAnonymous]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost, AllowAnonymous]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = new ApplicationUser { UserName = model.Email.Trim(), Email = model.Email.Trim() };
        await using var transaction = await database.Database.BeginTransactionAsync();
        var result = await users.CreateAsync(user, model.Password);
        if (result.Succeeded) result = await users.AddToRoleAsync(user, AppRoles.Student);
        if (!result.Succeeded)
        {
            await transaction.RollbackAsync();
            foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
            return View(model);
        }
        await transaction.CommitAsync();
        await signIn.SignInAsync(user, isPersistent: false);
        return RedirectToAction("Index", "Student");
    }

    [HttpGet, AllowAnonymous]
    public IActionResult Login(string? returnUrl = null) => View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost, AllowAnonymous]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var result = await signIn.PasswordSignInAsync(model.Email.Trim(), model.Password, isPersistent: false, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            ModelState.AddModelError("", "Unable to sign in. Check your credentials or try again later.");
            return View(model);
        }
        if (Url.IsLocalUrl(model.ReturnUrl)) return LocalRedirect(model.ReturnUrl!);
        var user = await users.FindByEmailAsync(model.Email.Trim());
        return RedirectToAction("Index", await users.IsInRoleAsync(user!, AppRoles.Teacher) ? "Teacher" : "Student");
    }

    [HttpGet, AllowAnonymous]
    public IActionResult AccessDenied()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View();
    }
}
