namespace HomeworkPlatform.Web.Models.Domain;
public class StudentCourse
{
    public string StudentId { get; set; } = "";
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
}
