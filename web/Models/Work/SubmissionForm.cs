using System.ComponentModel.DataAnnotations;
namespace HomeworkPlatform.Web.Models.Work;
public class SubmissionForm
{
    [Required, StringLength(100000)] public string Content { get; set; } = "";
}
