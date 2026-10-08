using System.ComponentModel.DataAnnotations;
namespace HomeworkPlatform.Web.Models.Work;
public class EnrollmentForm
{
    [Required, EmailAddress] public string Email { get; set; } = "";
}
