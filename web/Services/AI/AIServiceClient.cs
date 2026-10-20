using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace HomeworkPlatform.Web.Services.AI;

public class AIServiceClient(HttpClient http, IOptions<AIServiceOptions> options, ILogger<AIServiceClient> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };
    private const int MaxResponseBytes = 1024 * 1024;

    public async Task<AIServiceResult<AIHealthResponse>> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var result = await GetJsonAsync<AIHealthResponse>("health", cancellationToken);
        return result.Success && result.Data?.Status != "ok"
            ? Failure<AIHealthResponse>("invalid_response", "AI servisi beklenen sağlık yanıtını vermedi.") : result;
    }

    public Task<AIServiceResult<T>> GetJsonAsync<T>(string endpoint, CancellationToken cancellationToken = default) where T : class
        => SendAsync<T>(new HttpRequestMessage(HttpMethod.Get, RelativeEndpoint(endpoint)), cancellationToken);

    public Task<AIServiceResult<TResponse>> PostJsonAsync<TRequest, TResponse>(string endpoint, TRequest payload,
        CancellationToken cancellationToken = default) where TResponse : class
        => SendAsync<TResponse>(new HttpRequestMessage(HttpMethod.Post, RelativeEndpoint(endpoint))
        {
            Content = JsonContent.Create(payload, options: JsonOptions)
        }, cancellationToken);

    private static string RelativeEndpoint(string endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint) || endpoint.StartsWith('/') || endpoint.Contains('\\')
            || endpoint.Contains("..") || Uri.TryCreate(endpoint, UriKind.Absolute, out _))
            throw new ArgumentException("A relative AI service endpoint is required.", nameof(endpoint));
        return endpoint;
    }

    private async Task<AIServiceResult<T>> SendAsync<T>(HttpRequestMessage request, CancellationToken cancellationToken) where T : class
    {
        using (request)
        using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
        {
            timeout.CancelAfter(TimeSpan.FromSeconds(options.Value.TimeoutSeconds));
            try
            {
                using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
                if (!response.IsSuccessStatusCode)
                {
                    logger.LogWarning("AI service returned HTTP {StatusCode}.", (int)response.StatusCode);
                    return new(null, new("http_error", "AI servisi isteği tamamlayamadı. Lütfen daha sonra tekrar deneyin.", response.StatusCode));
                }
                await using var input = await response.Content.ReadAsStreamAsync(timeout.Token);
                using var buffer = new MemoryStream();
                var chunk = new byte[8192];
                int count;
                while ((count = await input.ReadAsync(chunk, timeout.Token)) > 0)
                {
                    if (buffer.Length + count > MaxResponseBytes)
                        return Failure<T>("invalid_response", "AI servisinin yanıtı izin verilen boyutu aşıyor.");
                    await buffer.WriteAsync(chunk.AsMemory(0, count), timeout.Token);
                }
                var data = JsonSerializer.Deserialize<T>(buffer.ToArray(), JsonOptions);
                return data == null ? Failure<T>("invalid_response", "AI servisinden geçerli bir JSON yanıtı alınamadı.") : new(data, null);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning("AI service request timed out.");
                return Failure<T>("timeout", "AI servisi zamanında yanıt vermedi. Lütfen tekrar deneyin.");
            }
            catch (HttpRequestException)
            {
                logger.LogWarning("AI service connection failed.");
                return Failure<T>("unavailable", "AI servisine bağlanılamadı. Servisin çalıştığını kontrol edin.");
            }
            catch (IOException)
            {
                logger.LogWarning("AI service response stream failed.");
                return Failure<T>("unavailable", "AI servisiyle bağlantı kesildi. Lütfen tekrar deneyin.");
            }
            catch (JsonException)
            {
                logger.LogWarning("AI service returned invalid JSON.");
                return Failure<T>("invalid_response", "AI servisinden geçerli bir JSON yanıtı alınamadı.");
            }
        }
    }

    private static AIServiceResult<T> Failure<T>(string code, string message) where T : class => new(null, new(code, message));
}
