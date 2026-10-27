using System.Net;
using System.Text.Json;
using HomeworkPlatform.Web.Services.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace HomeworkPlatform.Web.Tests;

public sealed class LocalOllamaFactAttribute : FactAttribute
{
    public LocalOllamaFactAttribute()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("VELI_LIVE_PYTHON_URL")))
            Skip = "Opt-in real Python/Ollama check; run scripts/verify_live_ollama.py.";
    }
}

public class LiveOllamaIntegrationTests
{
    public record Request(string Prompt, string Task);
    public record Response(string Model,
        [property: System.Text.Json.Serialization.JsonPropertyName("response")] string Text, bool Done);

    [LocalOllamaFact]
    public async Task ActualAspNetClientPostsThroughRealPythonToConfiguredOllama()
    {
        var address = new Uri(Environment.GetEnvironmentVariable("VELI_LIVE_PYTHON_URL")!);
        Assert.True(address.IsLoopback && address.Scheme == "http");
        using var http = new HttpClient { BaseAddress = address };
        var client = new AIServiceClient(http, Options.Create(new AIServiceOptions { TimeoutSeconds = 120 }),
            NullLogger<AIServiceClient>.Instance);
        var result = await client.PostJsonAsync<Request, Response>("llm/test",
            new("Return only the word INTEGRATION_OK. Do not explain.", "general"));
        var expected = Environment.GetEnvironmentVariable("VELI_LIVE_EXPECTED") ?? "success";
        if (expected == "success")
        {
            Assert.True(result.Success);
            Assert.Equal("qwen3:4b", result.Data!.Model);
            Assert.True(result.Data.Done);
            Assert.Equal("INTEGRATION_OK", result.Data.Text.Trim());
        }
        else
        {
            Assert.Equal("http_error", expected);
            Assert.False(result.Success);
            Assert.Equal("http_error", result.Error!.Code);
            Assert.Equal(HttpStatusCode.BadGateway, result.Error.StatusCode);
            Assert.DoesNotContain("Traceback", result.Error.Message);
            Assert.DoesNotContain("127.0.0.1", result.Error.Message);
        }
        var destination = Environment.GetEnvironmentVariable("VELI_LIVE_OUTPUT_JSON");
        if (!string.IsNullOrEmpty(destination))
            await File.WriteAllTextAsync(destination, JsonSerializer.Serialize(new
            {
                success = result.Success,
                model = result.Data?.Model,
                response = result.Data?.Text,
                done = result.Data?.Done,
                error_code = result.Error?.Code,
                http_status = (int?)result.Error?.StatusCode,
                error_message = result.Error?.Message
            }, new JsonSerializerOptions { WriteIndented = true }));
    }
}
