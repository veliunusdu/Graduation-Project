using System.ComponentModel.DataAnnotations;
using HomeworkPlatform.Web.Models.Domain;
namespace HomeworkPlatform.Web.Models.Work;
public class CourseForm : IValidatableObject
{
    [Required, StringLength(120)] public string Name { get; set; } = "";
    [StringLength(4000)] public string? Description { get; set; }
    [EnumDataType(typeof(EducationLevel))]
    public EducationLevel EducationLevel { get; set; } = EducationLevel.HighSchool;
    [Range(1, 12)] public int GradeLevel { get; set; } = 9;
    public bool IsActive { get; set; } = true;
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if ((EducationLevel == EducationLevel.HighSchool && (GradeLevel < 9 || GradeLevel > 12)) ||
            (EducationLevel == EducationLevel.University && (GradeLevel < 1 || GradeLevel > 4)))
            yield return new ValidationResult("Choose grades 9-12 for High School or years 1-4 for University.", new[] { nameof(GradeLevel) });
    }
}
