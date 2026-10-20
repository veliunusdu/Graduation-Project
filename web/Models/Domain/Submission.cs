using System.ComponentModel.DataAnnotations;
namespace HomeworkPlatform.Web.Models.Domain;
public class Submission
{
    public int Id { get; set; }
    public int AssignmentId { get; set; }
    public Assignment Assignment { get; set; } = null!;
    public string StudentId { get; set; } = "";
    [MaxLength(100000)] public string Content { get; set; } = "";
    [MaxLength(10000)] public string? TeacherFeedback { get; set; }
    public int? TeacherGrade { get; set; }
    public DateTime? ReviewedAt { get; set; }
    [MaxLength(39)] public string? StoredFileName { get; set; }
    [MaxLength(200)] public string? OriginalFileName { get; set; }
    public long? FileSize { get; set; }
    public DateTime? FileUploadedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
