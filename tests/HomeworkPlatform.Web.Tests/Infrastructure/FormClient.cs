using System.Net;
using System.Text.RegularExpressions;

namespace HomeworkPlatform.Web.Tests.Infrastructure;

public static class FormClient
{
    public const string TestPassword = "Test-Only8!Password";

    public static async Task<HttpResponseMessage> PostAsync(HttpClient client, string path, Dictionary<string, string> fields, string? tokenPage = null)
    {
        using var page = await client.GetAsync(tokenPage ?? path);
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        var html = await page.Content.ReadAsStringAsync();
        var token = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(token.Success, "The rendered page must provide an antiforgery token.");
        fields["__RequestVerificationToken"] = WebUtility.HtmlDecode(token.Groups[1].Value);
        return await client.PostAsync(path, new FormUrlEncodedContent(fields));
    }

    public static Task<HttpResponseMessage> RegisterAsync(HttpClient client, string email, Dictionary<string, string>? extra = null)
    {
        var fields = new Dictionary<string, string> { ["Email"] = email, ["Password"] = TestPassword, ["ConfirmPassword"] = TestPassword };
        if (extra != null) foreach (var field in extra) fields[field.Key] = field.Value;
        return PostAsync(client, "/Account/Register", fields);
    }

    public static Task<HttpResponseMessage> LoginAsync(HttpClient client, string email, string password = TestPassword, string? returnUrl = null)
        => PostAsync(client, "/Account/Login", new Dictionary<string, string>
        { ["Email"] = email, ["Password"] = password, ["ReturnUrl"] = returnUrl ?? "" });
}
