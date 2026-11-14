using System.Net;
using HomeworkPlatform.Web.Data;
using HomeworkPlatform.Web.Security;
using HomeworkPlatform.Web.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HomeworkPlatform.Web.Tests;

public class PreloadedDataTests
{
    [Theory]
    [InlineData("teacher@edupilot.local", "Teacher-Start8!", "/Teacher", "Deniz", "Introduction to Programming")]
    [InlineData("student1@edupilot.local", "Student-Start8!", "/Student", "Ayşe", "Database Management")]
    [InlineData("student3@edupilot.local", "Student-Start8!", "/Student", "Elif", "Mathematics")]
    public async Task PreparedAccountSignsInAndLoadsItsNameAndCourses(string email, string password, string dashboard, string name, string course)
    {
        using var app = new AuthWebApplicationFactory { EnableInitialData = true };
        using var client = app.NewClient();
        using var result = await FormClient.LoginAsync(client, email, password);
        Assert.Equal(dashboard, result.Headers.Location?.OriginalString);
        var html = WebUtility.HtmlDecode(await client.GetStringAsync(dashboard));
        Assert.Contains(name, html);
        Assert.Contains(email, html);
        Assert.Contains(course, html);
        if (email.StartsWith("student3")) Assert.DoesNotContain("Database Management", html);
        using var scope = app.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = (await users.FindByEmailAsync(email))!;
        Assert.NotEqual(password, user.PasswordHash);
        Assert.True(await users.CheckPasswordAsync(user, password));
    }

    [Theory]
    [InlineData("/Account/Register")]
    [InlineData("/Account/CompleteProfile")]
    public async Task RemovedPublicAccountRoutesCannotCreateOrChangeAccounts(string route)
    {
        using var app = new AuthWebApplicationFactory();
        using var client = app.NewClient();
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(route)).StatusCode);
        using var result = await FormClient.PostAsync(client, route, new() { ["Email"] = "forged@example.test", ["Password"] = FormClient.TestPassword, ["Role"] = "Teacher" }, "/Account/Login");
        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
        using var scope = app.Services.CreateScope();
        Assert.False(await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Users.AnyAsync());
    }

    [Fact]
    public async Task RestartDoesNotDuplicateDataResetPasswordsOrRestoreDeletedCourses()
    {
        using var app = new AuthWebApplicationFactory { EnableInitialData = true };
        using var client = app.NewClient();
        await client.GetAsync("/");
        using (var scope = app.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var teacher = (await users.FindByEmailAsync("teacher@edupilot.local"))!;
            Assert.True((await users.ChangePasswordAsync(teacher, "Teacher-Start8!", "Changed-Only9!Password")).Succeeded);
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Courses.Remove(await db.Courses.FirstAsync());
            await db.SaveChangesAsync();
        }
        await InitialDataSeeder.InitializeAsync(app.Services);
        await InitialDataSeeder.InitializeAsync(app.Services);
        using var check = app.Services.CreateScope();
        var database = check.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(4, await database.Users.CountAsync());
        Assert.Equal(2, await database.Courses.CountAsync());
        Assert.Single(await database.DataInitializations.ToListAsync());
        var manager = check.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var existing = (await manager.FindByEmailAsync("teacher@edupilot.local"))!;
        Assert.True(await manager.CheckPasswordAsync(existing, "Changed-Only9!Password"));
    }

    [Fact]
    public async Task InvalidInitialAccountRollsBackEarlierAccounts()
    {
        using var app = new AuthWebApplicationFactory();
        using var client = app.NewClient();
        await client.GetAsync("/");
        using (var scope = app.Services.CreateScope())
        {
            var data = new InitialDataDocument { Key = "invalid-test", Accounts = new()
            {
                new() { Email = "valid@example.test", Password = FormClient.TestPassword, FirstName = "Test", LastName = "Valid", Role = "Teacher" },
                new() { Email = "weak@example.test", Password = "weak", FirstName = "Test", LastName = "Weak", Role = "Teacher" }
            } };
            await Assert.ThrowsAsync<InvalidOperationException>(() => scope.ServiceProvider.GetRequiredService<InitialDataSeeder>().SeedAsync(data));
        }
        using var check = app.Services.CreateScope();
        var database = check.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.False(await database.Users.AnyAsync());
        Assert.False(await database.DataInitializations.AnyAsync());
    }

    [Fact]
    public async Task ProductionNeverLoadsBundledDevelopmentAccounts()
    {
        using var app = new AuthWebApplicationFactory { EnvironmentName = "Production", EnableInitialData = true };
        using var client = app.NewClient();
        await client.GetAsync("/");
        using var scope = app.Services.CreateScope();
        Assert.False(await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Users.AnyAsync());
    }

    [Fact]
    public async Task ExistingAccountDoesNotNeedTheRemovedProfileStep()
    {
        using var app = new AuthWebApplicationFactory();
        using var client = app.NewClient();
        using (var scope = app.Services.CreateScope())
        {
            var manager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = new ApplicationUser { UserName = "legacy@example.test", Email = "legacy@example.test", ProfileCompleted = false };
            Assert.True((await manager.CreateAsync(user, FormClient.TestPassword)).Succeeded);
            Assert.True((await manager.AddToRoleAsync(user, "Student")).Succeeded);
        }
        using var login = await FormClient.LoginAsync(client, "legacy@example.test");
        Assert.Equal("/Student", login.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task TeacherCanCreateAdditionalCourseAndEnrollASelectedStudent()
    {
        using var fixture = await OwnershipFixture.CreateAsync();
        using var teacher = await fixture.TeacherAsync();
        var choices = await teacher.GetStringAsync("/Courses/Create");
        Assert.Contains(fixture.StudentAId, choices);
        using var created = await FormClient.PostAsync(teacher, "/Courses/Create", new()
        { ["Name"] = "Extra course", ["EducationLevel"] = "0", ["GradeLevel"] = "9", ["IsActive"] = "true", ["StudentIds"] = fixture.StudentAId, ["TeacherId"] = fixture.TeacherBId });
        Assert.Equal(HttpStatusCode.Redirect, created.StatusCode);
        var courseId = await fixture.ReadAsync(async db =>
        {
            var course = await db.Courses.SingleAsync(c => c.Name == "Extra course");
            Assert.Equal(fixture.TeacherAId, course.TeacherId);
            Assert.True(await db.StudentCourses.AnyAsync(e => e.CourseId == course.Id && e.StudentId == fixture.StudentAId));
            Assert.False(await db.StudentCourses.AnyAsync(e => e.CourseId == course.Id && e.StudentId == fixture.StudentBId));
            return course.Id;
        });
        using var student = await fixture.StudentAsync();
        Assert.Contains("Extra course", await student.GetStringAsync("/Student"));
        Assert.Equal(HttpStatusCode.OK, (await student.GetAsync("/StudentCourses/Details/" + courseId)).StatusCode);
    }

    [Fact]
    public async Task NewCourseCannotEnrollATeacherOrUnknownAccount()
    {
        using var fixture = await OwnershipFixture.CreateAsync();
        using var teacher = await fixture.TeacherAsync();
        foreach (var invalid in new[] { fixture.TeacherAId, "unknown-id" })
        {
            using var result = await FormClient.PostAsync(teacher, "/Courses/Create", new()
            { ["Name"] = "Invalid selection", ["StudentIds"] = invalid, ["EducationLevel"] = "0", ["GradeLevel"] = "9", ["IsActive"] = "true" });
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        }
        Assert.False(await fixture.ReadAsync(db => db.Courses.AnyAsync(c => c.Name == "Invalid selection")));
    }
}
