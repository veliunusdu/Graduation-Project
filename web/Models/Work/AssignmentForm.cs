using System.ComponentModel.DataAnnotations;
using HomeworkPlatform.Web.Models.Domain;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
namespace HomeworkPlatform.Web.Models.Work;
public class AssignmentForm
{
    [Required, StringLength(200)] public string Title { get; set; } = "";
    [StringLength(10000), Display(Name = "Requirements")] public string? Description { get; set; }
    [Required, DataType(DataType.Date)] public DateTime? DueDate { get; set; }
    public List<IFormFile> Attachments { get; set; } = [];
    [BindNever, ValidateNever] public IReadOnlyList<AssignmentAttachment> ExistingAttachments { get; set; } = [];
}
