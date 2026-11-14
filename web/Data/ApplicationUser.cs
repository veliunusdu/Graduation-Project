using Microsoft.AspNetCore.Identity;
using HomeworkPlatform.Web.Models.Domain;
using System.ComponentModel.DataAnnotations;
namespace HomeworkPlatform.Web.Data;
public class ApplicationUser : IdentityUser
{
    [MaxLength(100)] public string? FirstName { get; set; }
    [MaxLength(100)] public string? LastName { get; set; }
    [MaxLength(32)] public string? StudentClass { get; set; }
    public EducationLevel EducationLevel { get; set; } = EducationLevel.HighSchool;
    public bool ProfileCompleted { get; set; }
}
