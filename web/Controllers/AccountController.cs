using HomeworkPlatform.Web.Data;
using HomeworkPlatform.Web.Models.Account;
using HomeworkPlatform.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HomeworkPlatform.Web.Controllers;

public class AccountController(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn) : Controller
{
    [HttpGet, AllowAnonymous]
    public IActionResult Login(string? returnUrl = null) => View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost, AllowAnonymous]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await users.FindByEmailAsync(model.Email.Trim());
        var result = user == null ? Microsoft.AspNetCore.Identity.SignInResult.Failed
            : await signIn.CheckPasswordSignInAsync(user!, model.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            ModelState.AddModelError("", "Unable to sign in. Check your email and password, or try again later if your account is temporarily locked.");
            return View(model);
        }
        await signIn.SignInAsync(user!, isPersistent: false);
        if (Url.IsLocalUrl(model.ReturnUrl)) return LocalRedirect(model.ReturnUrl!);
        return RedirectToAction("Index", await users.IsInRoleAsync(user!, AppRoles.Teacher) ? "Teacher" : "Student");
    }

    [HttpPost, Authorize]
    public async Task<IActionResult> Logout()
    {
        await signIn.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    [HttpGet, AllowAnonymous]
    public IActionResult AccessDenied()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View();
    }
}
