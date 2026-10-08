using System.ComponentModel.DataAnnotations;
namespace HomeworkPlatform.Web.Models.Domain;
public class Assignment
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
    [Required, MaxLength(200)] public string Title { get; set; } = "";
    [MaxLength(10000)] public string Description { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
