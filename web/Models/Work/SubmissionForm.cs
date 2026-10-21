using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
namespace HomeworkPlatform.Web.Models.Work;
public class SubmissionForm
{
    [StringLength(100000)] public string? Content { get; set; }
    public IFormFile? Upload { get; set; }
    public bool RemoveFile { get; set; }
    [BindNever] public string? ExistingFileName { get; set; }
}
