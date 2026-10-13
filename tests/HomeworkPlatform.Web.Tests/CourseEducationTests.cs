using System.Net;
using HomeworkPlatform.Web.Models.Domain;
using HomeworkPlatform.Web.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
namespace HomeworkPlatform.Web.Tests;
public class CourseEducationTests
{
    [Theory]
    [InlineData("0", "9")]
    [InlineData("0", "12")]
    [InlineData("1", "1")]
    [InlineData("1", "4")]
    public async Task TeacherCreatesAndEditsCourseWithEducation(string level, string grade)
    {
        using var fixture = await OwnershipFixture.CreateAsync();
        using var teacher = await fixture.TeacherAsync();
        var response = await FormClient.PostAsync(teacher, "/Courses/Create", new() { ["Name"] = "Level course", ["EducationLevel"] = level, ["GradeLevel"] = grade, ["IsActive"] = "true", ["TeacherId"] = fixture.TeacherBId });
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var course = await fixture.ReadAsync(db => db.Courses.SingleAsync(c => c.Name == "Level course"));
        Assert.Equal(int.Parse(level), (int)course.EducationLevel);
        Assert.Equal(int.Parse(grade), course.GradeLevel);
        Assert.True(course.IsActive);
        Assert.Equal(fixture.TeacherAId, course.TeacherId);
        var edit = await FormClient.PostAsync(teacher, $"/Courses/Edit/{course.Id}", new() { ["Name"] = "Updated level course", ["EducationLevel"] = "1", ["GradeLevel"] = "2", ["IsActive"] = "false" });
        Assert.Equal(HttpStatusCode.Redirect, edit.StatusCode);
        var updated = await fixture.ReadAsync(db => db.Courses.SingleAsync(c => c.Id == course.Id));
        Assert.Equal(EducationLevel.University, updated.EducationLevel);
        Assert.Equal(2, updated.GradeLevel);
        Assert.False(updated.IsActive);
        Assert.Equal(course.CreatedAt, updated.CreatedAt);
    }
    [Theory]
    [InlineData("0", "4")]
    [InlineData("1", "9")]
    [InlineData("77", "9")]
    [InlineData("1", "0")]
    [InlineData("0", "13")]
    [InlineData("University", "wrong")]
    public async Task InvalidEducationNeverPersists(string level, string grade)
    {
        using var fixture = await OwnershipFixture.CreateAsync();
        using var teacher = await fixture.TeacherAsync();
        var fields = new Dictionary<string, string> { ["Name"] = "Invalid", ["EducationLevel"] = level, ["GradeLevel"] = grade };
        Assert.Equal(HttpStatusCode.OK, (await FormClient.PostAsync(teacher, "/Courses/Create", fields)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await FormClient.PostAsync(teacher, $"/Courses/Edit/{fixture.CourseAId}", fields)).StatusCode);
        Assert.False(await fixture.ReadAsync(db => db.Courses.AnyAsync(c => c.Name == "Invalid")));
    }
    [Fact]
    public async Task StudentSeesOnlyEnrolledActiveCoursesAndCannotBypassInactiveCourse()
    {
        using var fixture = await OwnershipFixture.CreateAsync();
        using var student = await fixture.StudentAsync();
        using var teacher = await fixture.TeacherAsync();
        var html = await student.GetStringAsync("/StudentCourses");
        Assert.Contains("Alpha-private-course", html);
        Assert.DoesNotContain("Beta-private-course", html);
        Assert.Equal(HttpStatusCode.OK, (await student.GetAsync($"/StudentCourses/Details/{fixture.CourseAId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await student.GetAsync($"/StudentCourses/Details/{fixture.CourseBId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await FormClient.PostAsync(teacher, $"/Courses/Edit/{fixture.CourseAId}", new() { ["Name"] = "Alpha-private-course", ["EducationLevel"] = "0", ["GradeLevel"] = "9", ["IsActive"] = "false" })).StatusCode);
        Assert.DoesNotContain("Alpha-private-course", await student.GetStringAsync("/StudentCourses"));
        Assert.DoesNotContain("Alpha-private-assignment", await student.GetStringAsync("/StudentWork"));
        Assert.Equal(HttpStatusCode.NotFound, (await student.GetAsync($"/StudentCourses/Details/{fixture.CourseAId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await student.GetAsync($"/StudentWork/Details/{fixture.AssignmentAId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await FormClient.PostAsync(student, $"/Submissions/Create?assignmentId={fixture.FreshAssignmentId}", new() { ["Content"] = "Bypass" }, "/Student")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await FormClient.PostAsync(teacher, $"/Courses/Enroll/{fixture.CourseAId}", new() { ["Email"] = "student-a@example.test" }, $"/Courses/Details/{fixture.CourseAId}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await teacher.GetAsync($"/Courses/Details/{fixture.CourseAId}")).StatusCode);
        Assert.Equal(2, await fixture.ReadAsync(db => db.StudentCourses.CountAsync()));
        Assert.Equal(2, await fixture.ReadAsync(db => db.Submissions.CountAsync()));
        Assert.Equal(HttpStatusCode.Redirect, (await FormClient.PostAsync(teacher, $"/Courses/Edit/{fixture.CourseAId}", new() { ["Name"] = "Alpha-private-course", ["EducationLevel"] = "0", ["GradeLevel"] = "9", ["IsActive"] = "true" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await student.GetAsync($"/StudentCourses/Details/{fixture.CourseAId}")).StatusCode);
    }
    [Fact]
    public async Task EnrollmentIsOwnedRoleCheckedAndIdempotent()
    {
        using var fixture = await OwnershipFixture.CreateAsync();
        using var teacher = await fixture.TeacherAsync();
        foreach (var email in new[] { "teacher-a@example.test", "missing@example.test", "student-a@example.test" })
        {
            var response = await FormClient.PostAsync(teacher, $"/Courses/Enroll/{fixture.CourseAId}", new() { ["Email"] = email }, $"/Courses/Details/{fixture.CourseAId}");
            Assert.Equal(email.StartsWith("student") ? HttpStatusCode.Redirect : HttpStatusCode.OK, response.StatusCode);
        }
        Assert.Equal(HttpStatusCode.NotFound, (await FormClient.PostAsync(teacher, $"/Courses/Enroll/{fixture.CourseBId}", new() { ["Email"] = "student-a@example.test" }, $"/Courses/Details/{fixture.CourseAId}")).StatusCode);
        Assert.Equal(2, await fixture.ReadAsync(db => db.StudentCourses.CountAsync()));
        Assert.True(await fixture.ReadAsync(db => db.StudentCourses.AllAsync(e => e.EnrolledAt > DateTime.MinValue)));
    }
    [Fact]
    public async Task UpgradePreservesExistingCourseEnrollmentAssignmentAndSubmission()
    {
        using var app = new AuthWebApplicationFactory();
        Directory.CreateDirectory(app.DirectoryPath);
        var options = new DbContextOptionsBuilder<HomeworkPlatform.Web.Data.ApplicationDbContext>().UseSqlite($"Data Source={app.DatabasePath}").Options;
        await using var db = new HomeworkPlatform.Web.Data.ApplicationDbContext(options);
        var migrator = db.GetService<Microsoft.EntityFrameworkCore.Migrations.IMigrator>();
        await migrator.MigrateAsync("20261008215156_OwnershipResources");
        await db.Database.ExecuteSqlRawAsync("INSERT INTO AspNetUsers (Id, AccessFailedCount, EmailConfirmed, LockoutEnabled, PhoneNumberConfirmed, TwoFactorEnabled) VALUES ('legacy-teacher',0,0,0,0,0), ('legacy-student',0,0,0,0,0)");
        await db.Database.ExecuteSqlRawAsync("INSERT INTO Courses (Id,Name,Description,TeacherId,CreatedAt) VALUES (1,'Legacy course','Preserve me','legacy-teacher','2026-10-01 00:00:00')");
        await db.Database.ExecuteSqlRawAsync("INSERT INTO StudentCourses (StudentId,CourseId,EnrolledAt) VALUES ('legacy-student',1,'2026-10-02 00:00:00')");
        await db.Database.ExecuteSqlRawAsync("INSERT INTO Assignments (Id,CourseId,Title,Description,CreatedAt) VALUES (1,1,'Legacy assignment','','2026-10-01 00:00:00')");
        await db.Database.ExecuteSqlRawAsync("INSERT INTO Submissions (Id,AssignmentId,StudentId,Content,CreatedAt,UpdatedAt) VALUES (1,1,'legacy-student','Preserved submission','2026-10-02 00:00:00','2026-10-02 00:00:00')");
        await migrator.MigrateAsync();
        var course = await db.Courses.SingleAsync();
        Assert.Equal("Legacy course", course.Name);
        Assert.Equal("legacy-teacher", course.TeacherId);
        Assert.Equal(EducationLevel.HighSchool, course.EducationLevel);
        Assert.Equal(9, course.GradeLevel);
        Assert.True(course.IsActive);
        Assert.Equal(1, await db.StudentCourses.CountAsync());
        Assert.Equal("Legacy assignment", (await db.Assignments.SingleAsync()).Title);
        Assert.Equal("Preserved submission", (await db.Submissions.SingleAsync()).Content);
        await Assert.ThrowsAsync<Microsoft.Data.Sqlite.SqliteException>(() => db.Database.ExecuteSqlRawAsync("UPDATE Courses SET EducationLevel=1, GradeLevel=12 WHERE Id=1"));
    }
    [Fact]
    public async Task StudentCourseRoutesRequireStudentRole()
    {
        using var fixture = await OwnershipFixture.CreateAsync();
        using var anonymous = fixture.App.NewClient();
        using var teacher = await fixture.TeacherAsync();
        foreach (var route in new[] { "/StudentCourses", $"/StudentCourses/Details/{fixture.CourseAId}" })
        {
            var login = await anonymous.GetAsync(route);
            Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
            Assert.StartsWith("/Account/Login", login.Headers.Location!.PathAndQuery);
            var denied = await teacher.GetAsync(route);
            Assert.Equal(HttpStatusCode.Redirect, denied.StatusCode);
            Assert.StartsWith("/Account/AccessDenied", denied.Headers.Location!.PathAndQuery);
        }
    }
}
