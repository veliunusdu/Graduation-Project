using System.Net;
using System.Text.Json;
using HomeworkPlatform.Web.Data;
using HomeworkPlatform.Web.Models.Domain;
using HomeworkPlatform.Web.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HomeworkPlatform.Web.Tests;

// Product checks using Veli's saved Week 3 fixture, real Identity and isolated SQLite.
public class VeliWeek3CourseEnrollmentTests
{
    private static JsonDocument ReadFixture() => JsonDocument.Parse(File.ReadAllText(
        Path.Combine(AppContext.BaseDirectory, "TestData", "week3-test-data.json")));

    public static IEnumerable<object[]> InvalidEducationCases()
    {
        using var document = ReadFixture();
        return document.RootElement.GetProperty("invalid_grade_cases").EnumerateArray()
            .Select(c => new object[] { c.GetProperty("EducationLevel").GetString()!, c.GetProperty("GradeLevel").GetInt32() }).ToArray();
    }

    private static async Task<OwnershipFixture> CreateWeek3FixtureAsync()
    {
        var fixture = await OwnershipFixture.CreateAsync();
        try
        {
            using var scope = fixture.App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            using var document = ReadFixture();
            var courseIds = new Dictionary<string, int>();
            foreach (var template in document.RootElement.GetProperty("courses").EnumerateArray())
            {
                var teacherRef = template.GetProperty("TeacherRef").GetString();
                var courseId = teacherRef == "teacher-a" ? fixture.CourseAId : fixture.CourseBId;
                var course = await db.Courses.SingleAsync(c => c.Id == courseId);
                course.Name = template.GetProperty("Name").GetString()!;
                course.Description = template.GetProperty("Description").GetString()!;
                course.EducationLevel = Enum.Parse<EducationLevel>(template.GetProperty("EducationLevel").GetString()!);
                course.GradeLevel = template.GetProperty("GradeLevel").GetInt32();
                course.IsActive = template.GetProperty("IsActive").GetBoolean();
                courseIds.Add(template.GetProperty("FixtureKey").GetString()!, courseId);
            }
            db.StudentCourses.RemoveRange(await db.StudentCourses.ToListAsync());
            await db.SaveChangesAsync();
            foreach (var enrollment in document.RootElement.GetProperty("student_courses").EnumerateArray())
            {
                var studentId = enrollment.GetProperty("StudentRef").GetString() == "student-a"
                    ? fixture.StudentAId : fixture.StudentBId;
                db.StudentCourses.Add(new StudentCourse {
                    StudentId = studentId,
                    CourseId = courseIds[enrollment.GetProperty("CourseRef").GetString()!]
                });
            }
            await db.SaveChangesAsync();
            return fixture;
        }
        catch { fixture.Dispose(); throw; }
    }

    [Fact]
    public async Task HighSchoolAndUniversityFixtureHasOnlyOneStudentEnrollmentAndNoCourseLeak()
    {
        using var fixture = await CreateWeek3FixtureAsync();
        var highSchool = await fixture.ReadAsync(db => db.Courses.SingleAsync(c => c.Id == fixture.CourseAId));
        var university = await fixture.ReadAsync(db => db.Courses.SingleAsync(c => c.Id == fixture.CourseBId));
        Assert.Equal(EducationLevel.HighSchool, highSchool.EducationLevel);
        Assert.Equal(9, highSchool.GradeLevel);
        Assert.Equal(fixture.TeacherAId, highSchool.TeacherId);
        Assert.Equal(EducationLevel.University, university.EducationLevel);
        Assert.Equal(1, university.GradeLevel);
        Assert.Equal(fixture.TeacherBId, university.TeacherId);
        Assert.Equal(1, await fixture.ReadAsync(db => db.StudentCourses.CountAsync()));
        using var student = await fixture.StudentAsync();
        var html = WebUtility.HtmlDecode(await student.GetStringAsync("/StudentCourses"));
        Assert.Contains(highSchool.Name, html);
        Assert.DoesNotContain(university.Name, html);
        Assert.Equal(HttpStatusCode.OK, (await student.GetAsync($"/StudentCourses/Details/{highSchool.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await student.GetAsync($"/StudentCourses/Details/{university.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await student.GetAsync($"/StudentWork/Details/{fixture.AssignmentBId}")).StatusCode);
        using var unenrolled = await fixture.StudentAsync(other: true);
        var empty = WebUtility.HtmlDecode(await unenrolled.GetStringAsync("/StudentCourses"));
        Assert.DoesNotContain(highSchool.Name, empty);
        Assert.DoesNotContain(university.Name, empty);
        Assert.Equal(HttpStatusCode.NotFound, (await unenrolled.GetAsync($"/StudentCourses/Details/{highSchool.Id}")).StatusCode);
    }

    [Theory]
    [MemberData(nameof(InvalidEducationCases))]
    public async Task FixtureGradeBoundariesRejectCreateAndEditWithoutChangingCourse(string level, int grade)
    {
        using var fixture = await CreateWeek3FixtureAsync();
        using var teacher = await fixture.TeacherAsync();
        var original = await fixture.ReadAsync(db => db.Courses.SingleAsync(c => c.Id == fixture.CourseAId));
        var fields = new Dictionary<string, string> {
            ["Name"] = "Invalid fixture grade", ["EducationLevel"] = level,
            ["GradeLevel"] = grade.ToString(), ["IsActive"] = "true"
        };
        Assert.Equal(HttpStatusCode.OK, (await FormClient.PostAsync(teacher, "/Courses/Create", new(fields))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await FormClient.PostAsync(teacher, $"/Courses/Edit/{original.Id}", new(fields))).StatusCode);
        Assert.False(await fixture.ReadAsync(db => db.Courses.AnyAsync(c => c.Name == fields["Name"])));
        var stored = await fixture.ReadAsync(db => db.Courses.SingleAsync(c => c.Id == original.Id));
        Assert.Equal(original.Name, stored.Name);
        Assert.Equal(original.EducationLevel, stored.EducationLevel);
        Assert.Equal(original.GradeLevel, stored.GradeLevel);
        Assert.Equal(original.TeacherId, stored.TeacherId);
        Assert.Equal(original.IsActive, stored.IsActive);
        Assert.Equal(2, await fixture.ReadAsync(db => db.Courses.CountAsync()));
    }

    [Fact]
    public async Task InactiveCourseBlocksNewWorkButKeepsOldSubmissionAndReactivationRestoresAccess()
    {
        using var fixture = await CreateWeek3FixtureAsync();
        using var teacher = await fixture.TeacherAsync();
        using var student = await fixture.StudentAsync();
        var original = await fixture.ReadAsync(db => db.Courses.SingleAsync(c => c.Id == fixture.CourseAId));
        var enrolledAt = await fixture.ReadAsync(db => db.StudentCourses.Select(e => e.EnrolledAt).SingleAsync());
        var before = await fixture.ReadAsync(db => db.Submissions.SingleAsync(s => s.Id == fixture.SubmissionAId));
        var fields = new Dictionary<string, string> {
            ["Name"] = original.Name, ["EducationLevel"] = "0", ["GradeLevel"] = "9", ["IsActive"] = "false"
        };
        Assert.Equal(HttpStatusCode.Redirect, (await FormClient.PostAsync(teacher, $"/Courses/Edit/{original.Id}", fields)).StatusCode);
        Assert.DoesNotContain(original.Name, WebUtility.HtmlDecode(await student.GetStringAsync("/StudentCourses")));
        Assert.Equal(HttpStatusCode.NotFound, (await student.GetAsync($"/StudentCourses/Details/{original.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await student.GetAsync($"/Submissions/Create?assignmentId={fixture.FreshAssignmentId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await FormClient.PostAsync(student, $"/Submissions/Create?assignmentId={fixture.FreshAssignmentId}", new() { ["Content"] = "Must not persist" }, "/Student")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await FormClient.PostAsync(teacher, $"/Courses/Enroll/{original.Id}", new() { ["Email"] = "student-b@example.test" }, $"/Courses/Details/{original.Id}")).StatusCode);
        Assert.Equal(1, await fixture.ReadAsync(db => db.StudentCourses.CountAsync()));
        Assert.Equal(enrolledAt, await fixture.ReadAsync(db => db.StudentCourses.Select(e => e.EnrolledAt).SingleAsync()));
        Assert.Equal(2, await fixture.ReadAsync(db => db.Submissions.CountAsync()));
        var after = await fixture.ReadAsync(db => db.Submissions.SingleAsync(s => s.Id == before.Id));
        Assert.Equal(before.Content, after.Content);
        Assert.Equal(before.CreatedAt, after.CreatedAt);
        Assert.Equal(before.UpdatedAt, after.UpdatedAt);
        Assert.Equal(before.AssignmentId, after.AssignmentId);
        Assert.Equal(HttpStatusCode.OK, (await student.GetAsync($"/Submissions/Details/{before.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await student.GetAsync($"/Submissions/Edit/{before.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await FormClient.PostAsync(student, $"/Submissions/Edit/{before.Id}", new() { ["Content"] = "Updated historical work" })).StatusCode);
        Assert.Equal("Updated historical work", await fixture.ReadAsync(db => db.Submissions.Where(s => s.Id == before.Id).Select(s => s.Content).SingleAsync()));
        fields["IsActive"] = "true";
        Assert.Equal(HttpStatusCode.Redirect, (await FormClient.PostAsync(teacher, $"/Courses/Edit/{original.Id}", fields)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await student.GetAsync($"/StudentCourses/Details/{original.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await student.GetAsync($"/Submissions/Create?assignmentId={fixture.FreshAssignmentId}")).StatusCode);
        Assert.Contains(original.Name, WebUtility.HtmlDecode(await student.GetStringAsync("/StudentCourses")));
        Assert.Equal(original.CreatedAt, await fixture.ReadAsync(db => db.Courses.Where(c => c.Id == original.Id).Select(c => c.CreatedAt).SingleAsync()));
        Assert.Equal(enrolledAt, await fixture.ReadAsync(db => db.StudentCourses.Select(e => e.EnrolledAt).SingleAsync()));
    }
}
