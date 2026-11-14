using System.Net;
using HomeworkPlatform.Web.Data;
using HomeworkPlatform.Web.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace HomeworkPlatform.Web.Tests;

public class AccountFlowTests
{
    [Theory]
    [InlineData("Student")]
    [InlineData("Teacher")]
    public async Task PreloadedAccountHashesPasswordAndSignsInWithStoredRole(string role)
    {
        using var app = new AuthWebApplicationFactory();
        using var client = app.NewClient();
        using var result = await FormClient.CreateAccountAndLoginAsync(client, "student@example.test", new() { ["Role"] = role, ["Roles"] = "Administrator" });
        Assert.Equal(HttpStatusCode.Redirect, result.StatusCode);
        Assert.Equal("/" + role, result.Headers.Location?.OriginalString);
        Assert.Contains(result.Headers.GetValues("Set-Cookie"), cookie => cookie.Contains(".AspNetCore.Identity.Application="));
        using var scope = app.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = Assert.IsType<ApplicationUser>(await users.FindByEmailAsync("student@example.test"));
        Assert.NotEqual(FormClient.TestPassword, user.PasswordHash);
        Assert.True(await users.CheckPasswordAsync(user, FormClient.TestPassword));
        Assert.Equal(new[] { role }, await users.GetRolesAsync(user));
        Assert.True(user.ProfileCompleted);
        Assert.Equal("Test", user.FirstName);
        Assert.Equal("User", user.LastName);
        Assert.Equal(role == "Student" ? "HighSchool10" : null, user.StudentClass);
    }

    [Theory]
    [InlineData("https://outside.example/")]
    [InlineData("//outside.example/")]
    public async Task LoginRejectsExternalReturnUrl(string returnUrl)
    {
        using var app = new AuthWebApplicationFactory();
        using var registration = app.NewClient();
        await FormClient.CreateAccountAndLoginAsync(registration, "return@example.test");
        using var client = app.NewClient();
        using var result = await FormClient.LoginAsync(client, "return@example.test", returnUrl: returnUrl);
        Assert.Equal(HttpStatusCode.Redirect, result.StatusCode);
        Assert.Equal("/Student", result.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task LoginAcceptsLocalReturnUrl()
    {
        using var app = new AuthWebApplicationFactory();
        using var registration = app.NewClient();
        await FormClient.CreateAccountAndLoginAsync(registration, "local@example.test");
        using var client = app.NewClient();
        using var result = await FormClient.LoginAsync(client, "local@example.test", returnUrl: "/Home/Privacy");
        Assert.Equal("/Home/Privacy", result.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task InvalidCredentialsAndLockoutNeverSignIn()
    {
        using var app = new AuthWebApplicationFactory();
        using var registration = app.NewClient();
        await FormClient.CreateAccountAndLoginAsync(registration, "locked@example.test");
        using var client = app.NewClient();
        for (var attempt = 0; attempt < 5; attempt++)
        {
            using var failed = await FormClient.LoginAsync(client, "locked@example.test", "Wrong-Only8!Password");
            Assert.Equal(HttpStatusCode.OK, failed.StatusCode);
            Assert.Contains("Unable to sign in", await failed.Content.ReadAsStringAsync());
        }
        using var correct = await FormClient.LoginAsync(client, "locked@example.test");
        Assert.Equal(HttpStatusCode.OK, correct.StatusCode);
        Assert.False(correct.Headers.TryGetValues("Set-Cookie", out var cookies) && cookies.Any(c => c.Contains(".AspNetCore.Identity.Application=")));
        using var scope = app.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        Assert.True(await users.IsLockedOutAsync((await users.FindByEmailAsync("locked@example.test"))!));
    }
}
