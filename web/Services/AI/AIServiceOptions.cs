using System.ComponentModel.DataAnnotations;

namespace HomeworkPlatform.Web.Services.AI;

public class AIServiceOptions
{
    public const string SectionName = "AIService";
    [Required] public string BaseUrl { get; set; } = "http://127.0.0.1:8000/";
    [Range(1, 120)] public int TimeoutSeconds { get; set; } = 10;

    public bool HasValidBaseUrl() => Uri.TryCreate(BaseUrl, UriKind.Absolute, out var uri)
        && (uri.Scheme == "http" || uri.Scheme == "https")
        && string.IsNullOrEmpty(uri.UserInfo) && string.IsNullOrEmpty(uri.Query)
        && string.IsNullOrEmpty(uri.Fragment);
}
