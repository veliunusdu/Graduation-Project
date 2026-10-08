using System.Net;
using HomeworkPlatform.Web.Tests.Infrastructure;

namespace HomeworkPlatform.Web.Tests;

public class AntiforgeryTests
{
    [Theory]
    [InlineData("/Account/Register")]
    [InlineData("/Account/Login")]
    public async Task AccountPostWithoutTokenIsRejected(string path)
    {
        using var app = new AuthWebApplicationFactory();
        using var client = app.NewClient();
        using var result = await client.PostAsync(path, new FormUrlEncodedContent(new Dictionary<string, string>
        { ["Email"] = "untokened@example.test", ["Password"] = FormClient.TestPassword, ["ConfirmPassword"] = FormClient.TestPassword }));
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task LogoutWithoutTokenOrByGetCannotSignOut()
    {
        using var app = new AuthWebApplicationFactory();
        using var client = app.NewClient();
        await FormClient.RegisterAsync(client, "stay@example.test");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/Account/Logout", new FormUrlEncodedContent(new Dictionary<string, string>()))).StatusCode);
        var get = await client.GetAsync("/Account/Logout");
        Assert.True(get.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.MethodNotAllowed);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/Student")).StatusCode);
    }

    [Fact]
    public async Task ValidPostLogoutRemovesProtectedAccess()
    {
        using var app = new AuthWebApplicationFactory();
        using var client = app.NewClient();
        await FormClient.RegisterAsync(client, "logout@example.test");
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/Student")).StatusCode);
        using var logout = await FormClient.PostAsync(client, "/Account/Logout", new(), "/Student");
        Assert.Equal(HttpStatusCode.Redirect, logout.StatusCode);
        Assert.Equal("/", logout.Headers.Location?.OriginalString);
        var protectedPage = await client.GetAsync("/Student");
        Assert.Equal(HttpStatusCode.Redirect, protectedPage.StatusCode);
        Assert.StartsWith("/Account/Login", protectedPage.Headers.Location?.AbsolutePath);
    }
}
