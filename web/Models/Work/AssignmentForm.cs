using System.ComponentModel.DataAnnotations;
namespace HomeworkPlatform.Web.Models.Work;
public class AssignmentForm
{
    [Required, StringLength(200)] public string Title { get; set; } = "";
    [StringLength(10000)] public string? Description { get; set; }
}
