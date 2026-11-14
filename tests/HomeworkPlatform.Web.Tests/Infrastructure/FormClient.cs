using HomeworkPlatform.Web.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text.RegularExpressions;

namespace HomeworkPlatform.Web.Tests.Infrastructure;

public static class FormClient
{
    public const string TestPassword = "Test-Only8!Password";

    public static async Task<HttpResponseMessage> PostAsync(HttpClient client, string path, Dictionary<string, string> fields, string? tokenPage = null)
    {
        using var page = await client.GetAsync(tokenPage ?? path);
        Assert.True(page.StatusCode == HttpStatusCode.OK, $"Token page {tokenPage ?? path}: {page.StatusCode}\n{await page.Content.ReadAsStringAsync()}");
        var html = await page.Content.ReadAsStringAsync();
        var token = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(token.Success, "The rendered page must provide an antiforgery token.");
        fields["__RequestVerificationToken"] = WebUtility.HtmlDecode(token.Groups[1].Value);
        return await client.PostAsync(path, new FormUrlEncodedContent(fields));
    }

    public static async Task<HttpResponseMessage> CreateAccountAndLoginAsync(HttpClient client, string email, Dictionary<string, string>? extra = null)
    {
        Assert.True(AuthWebApplicationFactory.Owners.TryGetValue(client, out var app));
        using var scope = app!.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var role = extra?.GetValueOrDefault("Role") ?? "Student";
        var user = new ApplicationUser { UserName = email, Email = email, FirstName = "Test", LastName = "User",
            StudentClass = role == "Student" ? "HighSchool10" : null, ProfileCompleted = true };
        Assert.True((await users.CreateAsync(user, TestPassword)).Succeeded);
        Assert.True((await users.AddToRoleAsync(user, role)).Succeeded);
        return await LoginAsync(client, email);
    }

    public static Task<HttpResponseMessage> LoginAsync(HttpClient client, string email, string password = TestPassword, string? returnUrl = null)
        => PostAsync(client, "/Account/Login", new Dictionary<string, string>
        { ["Email"] = email, ["Password"] = password, ["ReturnUrl"] = returnUrl ?? "" });
}
