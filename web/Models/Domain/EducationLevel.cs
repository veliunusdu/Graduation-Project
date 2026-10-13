using System.ComponentModel.DataAnnotations;
namespace HomeworkPlatform.Web.Models.Domain;
public enum EducationLevel
{
    [Display(Name = "High School")] HighSchool = 0,
    University = 1
}
