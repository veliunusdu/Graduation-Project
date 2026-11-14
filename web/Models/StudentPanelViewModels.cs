using HomeworkPlatform.Web.Data;
using HomeworkPlatform.Web.Models.Domain;
namespace HomeworkPlatform.Web.Models;
public sealed record StudentCourseCard(Course Course, string TeacherName, int PendingCount)
{
    public string ClassLabel => $"{EducationRules.Label(Course.EducationLevel)} - {(Course.EducationLevel == EducationLevel.University ? "Year" : "Grade")} {Course.GradeLevel}";
}
public sealed record StudentAssignmentRow(Assignment Assignment, Submission? Submission)
{
    public string Status => Submission?.ReviewedAt != null ? "Graded" : Submission != null ? "Awaiting review" : Assignment.DueDate?.Date < DateTime.UtcNow.Date ? "Overdue" : "Pending";
    public string Target => Submission == null ? $"/StudentWork/Details/{Assignment.Id}" : $"/Submissions/Details/{Submission.Id}";
    public string DueLabel => Assignment.DueDate?.ToString("dd MMM yyyy", System.Globalization.CultureInfo.InvariantCulture) ?? "No due date";
}
public sealed record StudentDashboardViewModel(ApplicationUser Account, IReadOnlyList<StudentCourseCard> Courses, IReadOnlyList<StudentAssignmentRow> Assignments)
{
    public string DisplayName => $"{Account.FirstName} {Account.LastName}".Trim();
    public int SubmittedCount => Assignments.Count(a => a.Submission != null);
    public int PendingCount => Assignments.Count(a => a.Submission == null);
}
public sealed record StudentAssignmentsViewModel(string? Status, int? CourseId, IReadOnlyList<StudentCourseCard> Courses, IReadOnlyList<StudentAssignmentRow> Items);
public sealed record StudentCourseDetailViewModel(StudentCourseCard Card, IReadOnlyList<CourseMaterial> Materials, IReadOnlyList<StudentAssignmentRow> Assignments);
public sealed record StudentSearchViewModel(string Query, IReadOnlyList<StudentCourseCard> Courses, IReadOnlyList<StudentAssignmentRow> Assignments);
