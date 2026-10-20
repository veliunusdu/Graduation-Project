using System.Net;
using System.Text.Json.Serialization;

namespace HomeworkPlatform.Web.Services.AI;

public record AIServiceError(string Code, string Message, HttpStatusCode? StatusCode = null);
public record AIServiceResult<T>(T? Data, AIServiceError? Error) where T : class
{
    public bool Success => Error == null && Data != null;
}
public record AIHealthResponse([property: JsonPropertyName("status")] string? Status);
