using System.Net;
using System.Text;
using HomeworkPlatform.Web.Services.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace HomeworkPlatform.Web.Tests;

public class AIServiceClientTests
{
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) => send(request, token);
    }
    private static HttpResponseMessage Json(string body, HttpStatusCode code = HttpStatusCode.OK)
        => new(code) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private static AIServiceClient Client(HttpClient http) => new(http, Options.Create(new AIServiceOptions { TimeoutSeconds = 1 }), NullLogger<AIServiceClient>.Instance);

    [Fact]
    public async Task HealthUsesConfiguredBaseAddressAndParsesPythonJson()
    {
        using var http = new HttpClient(new Handler((request, _) =>
        {
            Assert.Equal("http://python.test/api/health", request.RequestUri!.ToString());
            Assert.Equal(HttpMethod.Get, request.Method);
            return Task.FromResult(Json("{\"status\":\"ok\"}"));
        })) { BaseAddress = new Uri("http://python.test/api/") };
        var result = await Client(http).CheckHealthAsync();
        Assert.True(result.Success); Assert.Equal("ok", result.Data!.Status);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("{\"status\":\"failed\"}")]
    [InlineData("{\"status\":123}")]
    public async Task InvalidHealthResponseIsControlled(string body)
    {
        using var http = new HttpClient(new Handler((_, _) => Task.FromResult(Json(body)))) { BaseAddress = new Uri("http://python.test/") };
        var result = await Client(http).CheckHealthAsync();
        Assert.False(result.Success); Assert.Equal("invalid_response", result.Error!.Code);
    }

    [Theory]
    [InlineData(500)] [InlineData(503)] [InlineData(404)]
    public async Task HttpErrorsDoNotExposeServerBody(int status)
    {
        using var http = new HttpClient(new Handler((_, _) => Task.FromResult(Json("secret Python traceback", (HttpStatusCode)status)))) { BaseAddress = new Uri("http://python.test/") };
        var result = await Client(http).CheckHealthAsync();
        Assert.Equal("http_error", result.Error!.Code); Assert.Equal((HttpStatusCode)status, result.Error.StatusCode);
        Assert.DoesNotContain("secret", result.Error.Message);
    }

    [Fact]
    public async Task ConnectionFailureHasSafeMessage()
    {
        using var http = new HttpClient(new Handler((_, _) => throw new HttpRequestException("private details"))) { BaseAddress = new Uri("http://python.test/") };
        var result = await Client(http).CheckHealthAsync();
        Assert.Equal("unavailable", result.Error!.Code); Assert.DoesNotContain("private", result.Error.Message);
    }

    [Fact]
    public async Task TimeoutIsControlledAndCallerCancellationPropagates()
    {
        using var http = new HttpClient(new Handler(async (_, token) => { await Task.Delay(10000, token); return Json("{}"); })) { BaseAddress = new Uri("http://python.test/") };
        Assert.Equal("timeout", (await Client(http).CheckHealthAsync()).Error!.Code);
        using var caller = new CancellationTokenSource(); caller.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Client(http).CheckHealthAsync(caller.Token));
    }

    public record ContextRequest(int CourseId, string EducationLevel);
    public record ContextResponse(int CourseId);
    [Fact]
    public async Task PostUsesStructuredSnakeCaseJsonAndParsesResponse()
    {
        using var http = new HttpClient(new Handler(async (request, _) =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("application/json", request.Content!.Headers.ContentType!.MediaType);
            var body = await request.Content.ReadAsStringAsync();
            Assert.Contains("\"course_id\":17", body); Assert.Contains("\"education_level\":\"University\"", body);
            return Json("{\"course_id\":17}");
        })) { BaseAddress = new Uri("http://python.test/") };
        var result = await Client(http).PostJsonAsync<ContextRequest, ContextResponse>("future-agent", new(17, "University"));
        Assert.True(result.Success); Assert.Equal(17, result.Data!.CourseId);
        await Assert.ThrowsAsync<ArgumentException>(() => Client(http).GetJsonAsync<ContextResponse>("https://other.test/"));
    }

    [Fact]
    public async Task ExcessiveJsonResponseIsRejected()
    {
        using var http = new HttpClient(new Handler((_, _) => Task.FromResult(Json(new string('x', 1024 * 1024 + 1))))) { BaseAddress = new Uri("http://python.test/") };
        Assert.Equal("invalid_response", (await Client(http).CheckHealthAsync()).Error!.Code);
    }
}
