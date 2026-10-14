using System.ComponentModel.DataAnnotations;
using HomeworkPlatform.Web.Models.Domain;
namespace HomeworkPlatform.Web.Models.Work;
public class AssignmentForm : IValidatableObject
{
    [Required, StringLength(200)] public string Title { get; set; } = "";
    [StringLength(10000)] public string? Description { get; set; }
    [EnumDataType(typeof(Difficulty))] public Difficulty Difficulty { get; set; }
    [Required, DataType(DataType.Date)] public DateTime? DueDate { get; set; }
    public List<RequirementForm> Requirements { get; set; } = [];
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (Requirements.Count is < 1 or > 100)
            yield return new ValidationResult("Add between 1 and 100 requirements.", [nameof(Requirements)]);
        if (Requirements.Sum(r => (long)r.Weight) != 100)
            yield return new ValidationResult("Requirement weights must total 100%.", [nameof(Requirements)]);
        if (Requirements.Select(r => r.Order).Distinct().Count() != Requirements.Count)
            yield return new ValidationResult("Requirement order values must be unique.", [nameof(Requirements)]);
    }
}
public class RequirementForm
{
    [Required, StringLength(200)] public string Title { get; set; } = "";
    [StringLength(10000)] public string? Description { get; set; }
    [Range(1,100)] public int Weight { get; set; }
    [Range(1,100)] public int Order { get; set; }
}
