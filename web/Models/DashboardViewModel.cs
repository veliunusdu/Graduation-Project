using HomeworkPlatform.Web.Data;
using HomeworkPlatform.Web.Models.Domain;
namespace HomeworkPlatform.Web.Models;

public sealed record DashboardViewModel(ApplicationUser Account, IReadOnlyList<Course> Courses)
{
    public int PendingReviews { get; init; }
    public IReadOnlyDictionary<int, int> EnrollmentCounts { get; init; } = new Dictionary<int, int>();
    public string DisplayName => string.IsNullOrWhiteSpace(Account.FirstName) && string.IsNullOrWhiteSpace(Account.LastName)
        ? Account.Email ?? "" : $"{Account.FirstName} {Account.LastName}".Trim();
    public string ClassLabel => Account.StudentClass switch
    {
        "University1" => "University — Year 1", "University2" => "University — Year 2",
        "University3" => "University — Year 3", "University4" => "University — Year 4",
        "HighSchool9" => "High school — Grade 9", "HighSchool10" => "High school — Grade 10",
        "HighSchool11" => "High school — Grade 11", "HighSchool12" => "High school — Grade 12",
        _ => Account.StudentClass ?? ""
    };
}
