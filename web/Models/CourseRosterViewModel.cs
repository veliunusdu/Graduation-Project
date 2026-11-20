using HomeworkPlatform.Web.Models.Domain;
namespace HomeworkPlatform.Web.Models;

public sealed record CourseStudentViewModel(string Id, string FirstName, string LastName, string Email, string? StudentClass)
{
    public string FullName => string.IsNullOrWhiteSpace(FirstName + LastName) ? "Unnamed student" : $"{FirstName} {LastName}".Trim();
    public string Initials => string.Concat(new[] { FirstName, LastName }.Where(n => !string.IsNullOrWhiteSpace(n)).Select(n => n.Trim()[..1])).ToUpperInvariant();
    public string ClassLabel => StudentClass switch
    {
        "University1" => "Year 1", "University2" => "Year 2", "University3" => "Year 3", "University4" => "Year 4",
        "HighSchool9" => "Grade 9", "HighSchool10" => "Grade 10", "HighSchool11" => "Grade 11", "HighSchool12" => "Grade 12",
        _ => "Not specified"
    };
}
public sealed record CourseRosterViewModel(Course Course, IReadOnlyList<CourseStudentViewModel> EnrolledStudents,
    IReadOnlyList<CourseStudentViewModel> AvailableStudents, int AssignmentCount, int MaterialCount);
