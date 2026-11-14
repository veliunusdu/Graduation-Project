using HomeworkPlatform.Web.Data;
using HomeworkPlatform.Web.Models;
using HomeworkPlatform.Web.Models.Domain;
using Microsoft.EntityFrameworkCore;
namespace HomeworkPlatform.Web.Services;
public class StudentPanelReader(ApplicationDbContext database)
{
    public async Task<List<StudentAssignmentRow>> Assignments(string studentId)
    {
        var assignments = await database.Assignments.AsNoTracking().Include(a => a.Course)
            .Where(a => a.Course.IsActive && database.StudentCourses.Any(e => e.CourseId == a.CourseId && e.StudentId == studentId)).ToListAsync();
        var submissions = await database.Submissions.AsNoTracking().Where(s => s.StudentId == studentId).ToDictionaryAsync(s => s.AssignmentId);
        return assignments.Select(a => new StudentAssignmentRow(a, submissions.GetValueOrDefault(a.Id)))
            .OrderBy(a => a.Assignment.DueDate ?? DateTime.MaxValue).ThenBy(a => a.Assignment.Title).ToList();
    }
    public async Task<List<StudentCourseCard>> Courses(string studentId)
    {
        var courses = await (from c in database.Courses.AsNoTracking()
            join teacher in database.Users.AsNoTracking() on c.TeacherId equals teacher.Id
            where c.IsActive && database.StudentCourses.Any(e => e.CourseId == c.Id && e.StudentId == studentId)
            orderby c.Name
            select new { Course = c, Name = (teacher.FirstName + " " + teacher.LastName).Trim(), Email = teacher.Email }).ToListAsync();
        var pending = await database.Assignments.Where(a => a.Course.IsActive &&
            database.StudentCourses.Any(e => e.CourseId == a.CourseId && e.StudentId == studentId) &&
            !database.Submissions.Any(s => s.AssignmentId == a.Id && s.StudentId == studentId))
            .GroupBy(a => a.CourseId).ToDictionaryAsync(g => g.Key, g => g.Count());
        return courses.Select(c => new StudentCourseCard(c.Course, string.IsNullOrWhiteSpace(c.Name) ? c.Email ?? "Teacher" : c.Name, pending.GetValueOrDefault(c.Course.Id))).ToList();
    }
}
