using System.Net;
using HomeworkPlatform.Web.Tests.Infrastructure;

namespace HomeworkPlatform.Web.Tests;

public class EduPilotViewTests
{
    [Theory]
    [InlineData("/", "onboardCarousel")]
    [InlineData("/Account/Login", "Welcome back")]
    public async Task AuthScreensUseEduPilotThemeAndWorkingLocalAssets(string path, string content)
    {
        using var app = new AuthWebApplicationFactory();
        using var client = app.NewClient();
        var html = await client.GetStringAsync(path);
        Assert.Contains("EduPilot", html);
        Assert.Contains("class=\"ep-page\"", html);
        Assert.Contains("/css/edupilot-auth.css", html);
        Assert.Contains(content, html);
        Assert.DoesNotContain("<nav", html);
        Assert.DoesNotContain("/Account/Register", html);
        if (path == "/")
        {
            Assert.DoesNotContain("bootstrap.bundle.min.js", html);
            Assert.DoesNotContain("jquery.min.js", html);
        }
        foreach (var asset in new[] { "/css/edupilot-auth.css", "/js/edupilot-auth.js", "/images/edupilot-icons.svg" })
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(asset)).StatusCode);
    }

    [Fact]
    public async Task SignInUsesExistingCredentialsWithoutUnimplementedActions()
    {
        using var app = new AuthWebApplicationFactory();
        using var client = app.NewClient();
        var html = await client.GetStringAsync("/Account/Login");
        Assert.Contains("name=\"Email\"", html);
        Assert.Contains("name=\"Password\"", html);
        Assert.Contains("aria-label=\"Show password\"", html);
        Assert.DoesNotContain("name=\"Role\"", html);
        Assert.DoesNotContain("RememberMe", html);
        Assert.DoesNotContain("href=\"#\"", html);
    }
}
