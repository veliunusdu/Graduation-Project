using HomeworkPlatform.Web.Models.Domain;
namespace HomeworkPlatform.Web.Models;
public sealed record TeacherProfileViewModel(string Name, string Email, string EducationLevel)
{
    public string Initials => string.Concat(Name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(n => n[0])).ToUpperInvariant();
}
public sealed record TeacherSearchViewModel(string Query, IReadOnlyList<Course> Courses, IReadOnlyList<Assignment> Assignments);
public sealed record TeacherSubmissionRow(Submission Submission, string StudentName, string StudentEmail);
