using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using HomeworkPlatform.Web.Models.Domain;
namespace HomeworkPlatform.Web.Models.Work;
public class CourseForm
{
    [Required, StringLength(120)] public string Name { get; set; } = "";
    [StringLength(4000)] public string? Description { get; set; }
    [BindNever]
    public EducationLevel EducationLevel { get; set; } = EducationLevel.HighSchool;
    [Range(1, 12)] public int GradeLevel { get; set; } = 9;
    public List<string> StudentIds { get; set; } = new();
    public bool IsActive { get; set; } = true;
}
