using System.ComponentModel.DataAnnotations;
namespace HomeworkPlatform.Web.Models.Domain;
public class Submission
{
    public int Id { get; set; }
    public int AssignmentId { get; set; }
    public Assignment Assignment { get; set; } = null!;
    public string StudentId { get; set; } = "";
    [Required, MaxLength(100000)] public string Content { get; set; } = "";
    [MaxLength(10000)] public string? TeacherFeedback { get; set; }
    public int? TeacherGrade { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
