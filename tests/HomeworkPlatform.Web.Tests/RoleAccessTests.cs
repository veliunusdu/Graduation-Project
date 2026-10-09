using System.Net;
using HomeworkPlatform.Web.Security;
using HomeworkPlatform.Web.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace HomeworkPlatform.Web.Tests;

public class RoleAccessTests
{
    [Theory]
    [InlineData("/Teacher")]
    [InlineData("/Student")]
    public async Task AnonymousCannotOpenRolePage(string path)
    {
        using var app = new AuthWebApplicationFactory();
        using var client = app.NewClient();
        using var result = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.Redirect, result.StatusCode);
        Assert.StartsWith("https://localhost/Account/Login?", result.Headers.Location?.AbsoluteUri);
    }

    [Fact]
    public async Task ForgedTeacherRegistrationCannotAccessTeacherPage()
    {
        using var app = new AuthWebApplicationFactory();
        using var client = app.NewClient();
        await FormClient.RegisterAsync(client, "forged@example.test", new() { ["Role"] = "Teacher" });
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/Student")).StatusCode);
        using var denied = await client.GetAsync("/Teacher");
        Assert.Equal(HttpStatusCode.Redirect, denied.StatusCode);
        Assert.StartsWith("/Account/AccessDenied", denied.Headers.Location?.AbsolutePath);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(denied.Headers.Location)).StatusCode);
    }

    [Fact]
    public async Task TeacherCanAccessTeacherPageButNotStudentPage()
    {
        using var app = new AuthWebApplicationFactory();
        using var scope = app.Services.CreateScope();
        var provisioned = await scope.ServiceProvider.GetRequiredService<TeacherProvisioner>().ProvisionAsync("teacher@example.test", FormClient.TestPassword);
        Assert.True(provisioned.Success);
        using var client = app.NewClient();
        using var login = await FormClient.LoginAsync(client, "teacher@example.test");
        Assert.Equal("/Teacher", login.Headers.Location?.OriginalString);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/Teacher")).StatusCode);
        using var denied = await client.GetAsync("/Student");
        Assert.Equal(HttpStatusCode.Redirect, denied.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(denied.Headers.Location)).StatusCode);
    }

    [Fact]
    public async Task ProductionAuthenticationCookieRequiresHttps()
    {
        using var app = new AuthWebApplicationFactory { EnvironmentName = "Production" };
        using var client = app.NewClient();
        using var registration = await FormClient.RegisterAsync(client, "secure@example.test");
        var cookie = Assert.Single(registration.Headers.GetValues("Set-Cookie"), value => value.StartsWith(".AspNetCore.Identity.Application="));
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/Student")).StatusCode);
    }
}
