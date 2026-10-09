using System.ComponentModel.DataAnnotations;
namespace HomeworkPlatform.Web.Models.Domain;
public class Course
{
    public int Id { get; set; }
    [Required, MaxLength(120)] public string Name { get; set; } = "";
    [MaxLength(4000)] public string Description { get; set; } = "";
    public string TeacherId { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
