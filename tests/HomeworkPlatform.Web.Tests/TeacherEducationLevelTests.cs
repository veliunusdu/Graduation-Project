using System.Net;
using HomeworkPlatform.Web.Data;
using HomeworkPlatform.Web.Models.Domain;
using HomeworkPlatform.Web.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace HomeworkPlatform.Web.Tests;

public class TeacherEducationLevelTests
{
    [Fact]
    public async Task AllExampleAccountsAndCoursesAreUniversityLevel()
    {
        using var app = new AuthWebApplicationFactory { EnableInitialData = true };
        using var client = app.NewClient();
        await client.GetAsync("/");
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(4, await db.Users.CountAsync());
        Assert.True(await db.Users.AllAsync(u => u.EducationLevel == EducationLevel.University));
        Assert.True(await db.Users.Where(u => u.StudentClass != null).AllAsync(u => u.StudentClass == "University2"));
        Assert.Equal(3, await db.Courses.CountAsync());
        Assert.True(await db.Courses.AllAsync(c => c.EducationLevel == EducationLevel.University && c.GradeLevel == 2));
        Assert.DoesNotContain("high school", (await db.Courses.SingleAsync(c => c.Name == "Mathematics")).Description);
    }

    [Theory]
    [InlineData(EducationLevel.University, 1, 4)]
    [InlineData(EducationLevel.HighSchool, 9, 12)]
    public async Task CourseFormsOnlyOfferTheTeachersGrades(EducationLevel level, int first, int last)
    {
        using var fixture = await OwnershipFixture.CreateAsync();
        await fixture.ReadAsync(async db =>
        {
            await db.Users.Where(u => u.Id == fixture.TeacherAId).ExecuteUpdateAsync(s => s.SetProperty(u => u.EducationLevel, level));
            await db.Courses.Where(c => c.Id == fixture.CourseAId).ExecuteUpdateAsync(s => s.SetProperty(c => c.EducationLevel, level).SetProperty(c => c.GradeLevel, first));
            return true;
        });
        using var teacher = await fixture.TeacherAsync();
        foreach (var route in new[] { "/Courses/Create", "/Courses/Edit/" + fixture.CourseAId })
        {
            var html = await teacher.GetStringAsync(route);
            Assert.Contains("value=\"" + EducationRules.Label(level) + "\"", html);
            Assert.DoesNotContain("name=\"EducationLevel\"", html);
            foreach (var grade in new[] { 1, 2, 3, 4, 9, 10, 11, 12 })
                Assert.Equal(grade >= first && grade <= last, html.Contains("value=\"" + grade + "\""));
        }
    }

    [Theory]
    [InlineData("9")]
    [InlineData("12")]
    [InlineData("0")]
    [InlineData("5")]
    [InlineData("wrong")]
    public async Task UniversityTeacherCannotCreateOrEditWithAnInvalidYear(string year)
    {
        using var app = new AuthWebApplicationFactory { EnableInitialData = true };
        using var teacher = app.NewClient();
        await FormClient.LoginAsync(teacher, "teacher@edupilot.local", "Teacher-Start8!");
        int id;
        using (var scope = app.Services.CreateScope()) id = (await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Courses.FirstAsync()).Id;
        foreach (var route in new[] { "/Courses/Create", "/Courses/Edit/" + id })
        {
            using var response = await FormClient.PostAsync(teacher, route, new() { ["Name"] = "Invalid year", ["GradeLevel"] = year, ["IsActive"] = "true" });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("validation-summary-errors", await response.Content.ReadAsStringAsync());
        }
        using var check = app.Services.CreateScope();
        Assert.False(await check.ServiceProvider.GetRequiredService<ApplicationDbContext>().Courses.AnyAsync(c => c.Name == "Invalid year"));
    }

    [Fact]
    public async Task TeacherLevelComesFromDatabaseAndCannotBeOverridden()
    {
        using var app = new AuthWebApplicationFactory { EnableInitialData = true };
        using var teacher = app.NewClient();
        await FormClient.LoginAsync(teacher, "teacher@edupilot.local", "Teacher-Start8!");
        using var forged = await FormClient.PostAsync(teacher, "/Courses/Create", new()
        { ["Name"] = "Forged high school", ["EducationLevel"] = "HighSchool", ["GradeLevel"] = "9", ["IsActive"] = "true" });
        Assert.Equal(HttpStatusCode.OK, forged.StatusCode);
        using var created = await FormClient.PostAsync(teacher, "/Courses/Create", new()
        { ["Name"] = "Automatic university", ["GradeLevel"] = "3", ["IsActive"] = "true" });
        Assert.Equal(HttpStatusCode.Redirect, created.StatusCode);
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.False(await db.Courses.AnyAsync(c => c.Name == "Forged high school"));
        Assert.Equal(EducationLevel.University, (await db.Courses.SingleAsync(c => c.Name == "Automatic university")).EducationLevel);
        // No reliance on a level cached in the login cookie.
        var account = await db.Users.SingleAsync(u => u.Email == "teacher@edupilot.local");
        account.EducationLevel = EducationLevel.HighSchool;
        await db.SaveChangesAsync();
        var html = await teacher.GetStringAsync("/Courses/Create");
        Assert.Contains("value=\"High School\"", html);
        Assert.DoesNotContain("<option value=\"1\"", html);
    }

    [Fact]
    public async Task OtherLevelStudentsAreHiddenAndCannotBeEnrolledByPostingTheirIds()
    {
        using var app = new AuthWebApplicationFactory { EnableInitialData = true };
        using var teacher = app.NewClient();
        await FormClient.LoginAsync(teacher, "teacher@edupilot.local", "Teacher-Start8!");
        var highSchool = new ApplicationUser { UserName = "school@example.test", Email = "school@example.test", EducationLevel = EducationLevel.HighSchool, StudentClass = "HighSchool10" };
        int courseId;
        using (var scope = app.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            Assert.True((await users.CreateAsync(highSchool, FormClient.TestPassword)).Succeeded);
            Assert.True((await users.AddToRoleAsync(highSchool, "Student")).Succeeded);
            courseId = (await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Courses.FirstAsync()).Id;
        }
        Assert.DoesNotContain(highSchool.Email, await teacher.GetStringAsync("/Courses/Create"));
        Assert.DoesNotContain(highSchool.Email, await teacher.GetStringAsync("/Courses/Details/" + courseId));
        using var create = await FormClient.PostAsync(teacher, "/Courses/Create", new()
        { ["Name"] = "Cross-level enrollment", ["GradeLevel"] = "1", ["IsActive"] = "true", ["StudentIds"] = highSchool.Id });
        Assert.Equal(HttpStatusCode.OK, create.StatusCode);
        using var enroll = await FormClient.PostAsync(teacher, "/Courses/Enroll/" + courseId, new() { ["Email"] = highSchool.Email }, "/Courses/Details/" + courseId);
        Assert.Equal(HttpStatusCode.OK, enroll.StatusCode);
        using var check = app.Services.CreateScope();
        var db = check.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.False(await db.StudentCourses.AnyAsync(e => e.StudentId == highSchool.Id));
        Assert.False(await db.Courses.AnyAsync(c => c.Name == "Cross-level enrollment"));
    }

    [Fact]
    public async Task UpgradeConvertsTheOldExampleDataWithoutLosingPasswordsOrEnrollments()
    {
        using var app = new AuthWebApplicationFactory { EnableInitialData = true };
        Directory.CreateDirectory(app.DirectoryPath);
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite($"Data Source={app.DatabasePath}").Options;
        var ids = new[] { "teacher", "student1", "student2", "student3" };
        var passwords = new Dictionary<string, string>();
        await using (var old = new ApplicationDbContext(options))
        {
            var migrator = old.GetService<IMigrator>();
            await migrator.MigrateAsync(old.Database.GetMigrations().Single(m => m.EndsWith("_EnglishCourseData")));
            await old.Database.ExecuteSqlRawAsync("INSERT INTO AspNetRoles (Id,Name,NormalizedName) VALUES ('teacher-role','Teacher','TEACHER'),('student-role','Student','STUDENT')");
            foreach (var id in ids)
            {
                var email = id + "@edupilot.local";
                var account = new ApplicationUser { Id = id };
                var hash = new PasswordHasher<ApplicationUser>().HashPassword(account, FormClient.TestPassword);
                passwords[id] = hash;
                var studentClass = id == "teacher" ? null : id == "student3" ? "HighSchool10" : "University2";
                await old.Database.ExecuteSqlInterpolatedAsync($@"INSERT INTO AspNetUsers (Id,UserName,NormalizedUserName,Email,NormalizedEmail,PasswordHash,SecurityStamp,FirstName,LastName,StudentClass,ProfileCompleted,AccessFailedCount,EmailConfirmed,LockoutEnabled,PhoneNumberConfirmed,TwoFactorEnabled)
                    VALUES ({id},{email},{email.ToUpperInvariant()},{email},{email.ToUpperInvariant()},{hash},{Guid.NewGuid().ToString()},'Existing','Account',{studentClass},1,0,0,1,0,0)");
                await old.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO AspNetUserRoles (UserId,RoleId) VALUES ({id},{(id == "teacher" ? "teacher-role" : "student-role")})");
            }
            await old.Database.ExecuteSqlRawAsync("INSERT INTO Courses (Id,Name,Description,TeacherId,CreatedAt,EducationLevel,GradeLevel,IsActive) VALUES (100,'Mathematics','Introductory course in high school mathematics.','teacher','2026-11-13',0,10,1),(101,'Introduction to Programming','','teacher','2026-11-13',1,2,1),(102,'Database Management','','teacher','2026-11-13',1,2,1)");
            await old.Database.ExecuteSqlRawAsync("INSERT INTO StudentCourses (StudentId,CourseId,EnrolledAt) VALUES ('student3',100,'2026-11-13'),('student1',101,'2026-11-13'),('student2',102,'2026-11-13')");
            await old.Database.ExecuteSqlRawAsync("INSERT INTO DataInitializations (Key,CompletedAt) VALUES ('edupilot-starter-v1','2026-11-13')");
        }
        using var client = app.NewClient();
        await client.GetAsync("/");
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(4, await db.Users.CountAsync());
        Assert.Equal(3, await db.Courses.CountAsync());
        Assert.Equal(3, await db.StudentCourses.CountAsync());
        Assert.True(await db.Users.AllAsync(u => u.EducationLevel == EducationLevel.University));
        Assert.True(await db.Courses.AllAsync(c => c.EducationLevel == EducationLevel.University && c.GradeLevel == 2));
        Assert.Equal("University2", (await db.Users.SingleAsync(u => u.Id == "student3")).StudentClass);
        Assert.Equal("Introductory course in university mathematics.", (await db.Courses.SingleAsync(c => c.Id == 100)).Description);
        foreach (var user in await db.Users.ToListAsync()) Assert.Equal(passwords[user.Id], user.PasswordHash);
    }
}
