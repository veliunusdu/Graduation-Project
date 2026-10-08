using System.Net;
using HomeworkPlatform.Web.Data;
using HomeworkPlatform.Web.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace HomeworkPlatform.Web.Tests;

public class AccountFlowTests
{
    [Fact]
    public async Task RegistrationHashesPasswordAndAssignsStudentOnly()
    {
        using var app = new AuthWebApplicationFactory();
        using var client = app.NewClient();
        using var result = await FormClient.RegisterAsync(client, "student@example.test", new() { ["Role"] = "Teacher", ["Roles"] = "Teacher" });
        Assert.Equal(HttpStatusCode.Redirect, result.StatusCode);
        Assert.Equal("/Student", result.Headers.Location?.OriginalString);
        Assert.Contains(result.Headers.GetValues("Set-Cookie"), cookie => cookie.Contains(".AspNetCore.Identity.Application="));
        using var scope = app.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = Assert.IsType<ApplicationUser>(await users.FindByEmailAsync("student@example.test"));
        Assert.NotEqual(FormClient.TestPassword, user.PasswordHash);
        Assert.True(await users.CheckPasswordAsync(user, FormClient.TestPassword));
        Assert.Equal(new[] { "Student" }, await users.GetRolesAsync(user));
    }

    [Theory]
    [InlineData("invalid-email", "Test-Only8!Password", "Test-Only8!Password")]
    [InlineData("invalid@example.test", "weak", "weak")]
    [InlineData("invalid@example.test", "Test-Only8!Password", "different")]
    public async Task InvalidRegistrationCreatesNoAccount(string email, string password, string confirmation)
    {
        using var app = new AuthWebApplicationFactory();
        using var client = app.NewClient();
        using var result = await FormClient.PostAsync(client, "/Account/Register", new() { ["Email"] = email, ["Password"] = password, ["ConfirmPassword"] = confirmation });
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        using var scope = app.Services.CreateScope();
        Assert.False(scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>().Users.Any());
        Assert.False(result.Headers.TryGetValues("Set-Cookie", out var cookies) && cookies.Any(c => c.Contains(".AspNetCore.Identity.Application=")));
    }

    [Fact]
    public async Task DuplicateEmailDoesNotCreateAnotherUser()
    {
        using var app = new AuthWebApplicationFactory();
        using var first = app.NewClient();
        Assert.Equal(HttpStatusCode.Redirect, (await FormClient.RegisterAsync(first, "same@example.test")).StatusCode);
        using var second = app.NewClient();
        Assert.Equal(HttpStatusCode.OK, (await FormClient.RegisterAsync(second, "SAME@example.test")).StatusCode);
        using var scope = app.Services.CreateScope();
        Assert.Equal(1, scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>().Users.Count());
    }

    [Theory]
    [InlineData("https://outside.example/")]
    [InlineData("//outside.example/")]
    public async Task LoginRejectsExternalReturnUrl(string returnUrl)
    {
        using var app = new AuthWebApplicationFactory();
        using var registration = app.NewClient();
        await FormClient.RegisterAsync(registration, "return@example.test");
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
        await FormClient.RegisterAsync(registration, "local@example.test");
        using var client = app.NewClient();
        using var result = await FormClient.LoginAsync(client, "local@example.test", returnUrl: "/Home/Privacy");
        Assert.Equal("/Home/Privacy", result.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task InvalidCredentialsAndLockoutNeverSignIn()
    {
        using var app = new AuthWebApplicationFactory();
        using var registration = app.NewClient();
        await FormClient.RegisterAsync(registration, "locked@example.test");
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
