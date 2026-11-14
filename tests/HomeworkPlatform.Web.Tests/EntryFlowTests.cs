using System.Net;
using System.Text.Json;
using HomeworkPlatform.Web.Tests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace HomeworkPlatform.Web.Tests;

public class EntryFlowTests
{
    [Fact]
    public async Task AnonymousEntryStartsWithOnboardingAndLinksToSignIn()
    {
        using var app = new AuthWebApplicationFactory();
        using var client = app.NewClient();
        var html = await client.GetStringAsync("/");
        Assert.Contains("id=\"onboardCarousel\"", html);
        Assert.Contains("<a id=\"onboardNext\"", html);
        Assert.Contains("id=\"onboardStart\"", html);
        Assert.Contains("href=\"/Account/Login\"", html);
        Assert.DoesNotContain("Go to dashboard", html);
        var login = await client.GetStringAsync("/Account/Login");
        Assert.Contains("name=\"Email\"", login);
        Assert.Contains("name=\"Password\"", login);
        Assert.Contains("Welcome back", login);
    }

    [Fact]
    public async Task ExistingSessionDoesNotReplaceOnboardingWithDashboardShortcut()
    {
        using var app = new AuthWebApplicationFactory { EnableInitialData = true };
        using var client = app.NewClient();
        await FormClient.LoginAsync(client, "teacher@edupilot.local", "Teacher-Start8!");
        var html = await client.GetStringAsync("/");
        Assert.Contains("id=\"onboardCarousel\"", html);
        Assert.Contains("id=\"onboardStart\"", html);
        Assert.DoesNotContain("Go to dashboard", html);
        var login = await client.GetAsync("/Account/Login");
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Contains("name=\"Password\"", await login.Content.ReadAsStringAsync());
    }

    [Fact]
    public void VisualStudioLaunchProfilesStartAtTheOnboardingRoot()
    {
        using var app = new AuthWebApplicationFactory();
        using var scope = app.Services.CreateScope();
        var root = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>().ContentRootPath;
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "Properties", "launchSettings.json")));
        foreach (var profile in document.RootElement.GetProperty("profiles").EnumerateObject())
            Assert.Equal("", profile.Value.GetProperty("launchUrl").GetString());
    }
}
