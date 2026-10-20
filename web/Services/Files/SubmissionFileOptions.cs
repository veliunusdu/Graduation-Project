using System.ComponentModel.DataAnnotations;

namespace HomeworkPlatform.Web.Services.Files;

public class SubmissionFileOptions
{
    public const string SectionName = "SubmissionFiles";
    [Required] public string RootPath { get; set; } = "App_Data/SubmissionFiles";
    [Range(1, 20)] public int MaxFileSizeMB { get; set; } = 20;
    // Allow enough multipart overhead while the file stream is separately limited to MaxFileSizeMB.
    public const long RequestSizeLimit = 25 * 1024 * 1024;
}

public record StoredSubmissionFile(string StoredName, string OriginalName, long Size);
public class SubmissionFileException(string message) : Exception(message);
