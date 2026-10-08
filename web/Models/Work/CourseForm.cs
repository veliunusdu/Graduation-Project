using System.ComponentModel.DataAnnotations;
namespace HomeworkPlatform.Web.Models.Work;
public class CourseForm
{
    [Required, StringLength(120)] public string Name { get; set; } = "";
    [StringLength(4000)] public string? Description { get; set; }
}
