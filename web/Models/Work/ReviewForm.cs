using System.ComponentModel.DataAnnotations;
namespace HomeworkPlatform.Web.Models.Work;
public class ReviewForm
{
    [Required, StringLength(10000)] public string Feedback { get; set; } = "";
    [Required, Range(0,100)] public int? Grade { get; set; }
}
