using HomeworkPlatform.Web.Data;
using HomeworkPlatform.Web.Models.Domain;
using HomeworkPlatform.Web.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace HomeworkPlatform.Web.Tests.Infrastructure;

public sealed class OwnershipFixture : IDisposable
{
    public AuthWebApplicationFactory App { get; } = new();
    public string TeacherAId { get; private set; } = "";
    public string TeacherBId { get; private set; } = "";
    public string StudentAId { get; private set; } = "";
    public string StudentBId { get; private set; } = "";
    public int CourseAId { get; private set; }
    public int CourseBId { get; private set; }
    public int AssignmentAId { get; private set; }
    public int AssignmentBId { get; private set; }
    public int FreshAssignmentId { get; private set; }
    public int SubmissionAId { get; private set; }
    public int SubmissionBId { get; private set; }

    public static async Task<OwnershipFixture> CreateAsync()
    {
        var fixture = new OwnershipFixture();
        try
        {
            using var scope = fixture.App.Services.CreateScope();
            var provisioner = scope.ServiceProvider.GetRequiredService<TeacherProvisioner>();
            Assert.True((await provisioner.ProvisionAsync("teacher-a@example.test", FormClient.TestPassword)).Success);
            Assert.True((await provisioner.ProvisionAsync("teacher-b@example.test", FormClient.TestPassword)).Success);
            using var first = fixture.App.NewClient();
            using var second = fixture.App.NewClient();
            Assert.Equal(System.Net.HttpStatusCode.Redirect, (await FormClient.RegisterAsync(first, "student-a@example.test")).StatusCode);
            Assert.Equal(System.Net.HttpStatusCode.Redirect, (await FormClient.RegisterAsync(second, "student-b@example.test")).StatusCode);
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            fixture.TeacherAId = (await users.FindByEmailAsync("teacher-a@example.test"))!.Id;
            fixture.TeacherBId = (await users.FindByEmailAsync("teacher-b@example.test"))!.Id;
            fixture.StudentAId = (await users.FindByEmailAsync("student-a@example.test"))!.Id;
            fixture.StudentBId = (await users.FindByEmailAsync("student-b@example.test"))!.Id;
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var courseA = new Course { Name = "Alpha-private-course", TeacherId = fixture.TeacherAId };
            var courseB = new Course { Name = "Beta-private-course", TeacherId = fixture.TeacherBId };
            db.AddRange(courseA, courseB);
            await db.SaveChangesAsync();
            var assignmentA = new Assignment { CourseId = courseA.Id, Title = "Alpha-private-assignment" };
            var assignmentB = new Assignment { CourseId = courseB.Id, Title = "Beta-private-assignment" };
            var fresh = new Assignment { CourseId = courseA.Id, Title = "Fresh assignment" };
            db.AddRange(assignmentA, assignmentB, fresh,
                new StudentCourse { StudentId = fixture.StudentAId, CourseId = courseA.Id },
                new StudentCourse { StudentId = fixture.StudentBId, CourseId = courseA.Id });
            await db.SaveChangesAsync();
            var submissionA = new Submission { AssignmentId = assignmentA.Id, StudentId = fixture.StudentAId, Content = "Alice-private-content" };
            var submissionB = new Submission { AssignmentId = assignmentA.Id, StudentId = fixture.StudentBId, Content = "Bob-private-content" };
            db.AddRange(submissionA, submissionB);
            await db.SaveChangesAsync();
            fixture.CourseAId = courseA.Id; fixture.CourseBId = courseB.Id;
            fixture.AssignmentAId = assignmentA.Id; fixture.AssignmentBId = assignmentB.Id; fixture.FreshAssignmentId = fresh.Id;
            fixture.SubmissionAId = submissionA.Id; fixture.SubmissionBId = submissionB.Id;
            return fixture;
        }
        catch { fixture.Dispose(); throw; }
    }

    public async Task<HttpClient> TeacherAsync(bool other = false)
    {
        var client = App.NewClient();
        Assert.Equal(System.Net.HttpStatusCode.Redirect, (await FormClient.LoginAsync(client, other ? "teacher-b@example.test" : "teacher-a@example.test")).StatusCode);
        return client;
    }
    public async Task<HttpClient> StudentAsync(bool other = false)
    {
        var client = App.NewClient();
        Assert.Equal(System.Net.HttpStatusCode.Redirect, (await FormClient.LoginAsync(client, other ? "student-b@example.test" : "student-a@example.test")).StatusCode);
        return client;
    }
    public async Task<T> ReadAsync<T>(Func<ApplicationDbContext, Task<T>> read)
    {
        using var scope = App.Services.CreateScope();
        return await read(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());
    }
    public void Dispose() => App.Dispose();
}
