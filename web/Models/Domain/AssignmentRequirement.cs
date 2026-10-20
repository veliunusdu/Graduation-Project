using System.ComponentModel.DataAnnotations;
namespace HomeworkPlatform.Web.Models.Domain;
public class AssignmentRequirement
{
    public int Id { get; set; }
    public int AssignmentId { get; set; }
    public Assignment Assignment { get; set; } = null!;
    [Required, MaxLength(200)] public string Title { get; set; } = "";
    [MaxLength(10000)] public string Description { get; set; } = "";
    [Range(1, 100)] public int Weight { get; set; }
    [Range(1, 100)] public int Order { get; set; }
}
