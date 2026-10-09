using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using HomeworkPlatform.Web.Data;
using HomeworkPlatform.Web.Security;
using HomeworkPlatform.Web.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace HomeworkPlatform.Web.Tests;

public class OwnershipSchemaTests
{
    [Fact]
    public async Task StartupAddsOwnershipTablesWithoutRemovingIdentity()
    {
        using var app = new AuthWebApplicationFactory();
        using var client = app.NewClient();
        await FormClient.RegisterAsync(client, "preserved@example.test");
        await IdentityInitializer.InitializeAsync(app.Services);
        await using var connection = new SqliteConnection($"Data Source={app.DatabasePath}");
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name IN ('Courses','Assignments','StudentCourses','Submissions')";
        Assert.Equal(4L, (long)(await command.ExecuteScalarAsync())!);
        using var scope = app.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = Assert.IsType<ApplicationUser>(await users.FindByEmailAsync("preserved@example.test"));
        Assert.True(await users.CheckPasswordAsync(user, FormClient.TestPassword));
        Assert.Equal(new[] { "Student" }, await users.GetRolesAsync(user));
    }
    [Fact]
    public async Task UpgradeFromIdentityOnlyPreservesAccountAndRole()
    {
        using var app = new AuthWebApplicationFactory();
        Directory.CreateDirectory(app.DirectoryPath);
        var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite($"Data Source={app.DatabasePath}").Options;
        await using (var oldDatabase = new ApplicationDbContext(options))
        {
            var migrator = oldDatabase.GetService<Microsoft.EntityFrameworkCore.Migrations.IMigrator>();
            await migrator.MigrateAsync(oldDatabase.Database.GetMigrations().First());
            var user = new ApplicationUser { Id = "existing-user", UserName = "existing@example.test", NormalizedUserName = "EXISTING@EXAMPLE.TEST", Email = "existing@example.test", NormalizedEmail = "EXISTING@EXAMPLE.TEST", SecurityStamp = Guid.NewGuid().ToString(), LockoutEnabled = true };
            user.PasswordHash = new PasswordHasher<ApplicationUser>().HashPassword(user, FormClient.TestPassword);
            oldDatabase.Users.Add(user);
            oldDatabase.Roles.Add(new IdentityRole { Id = "existing-student-role", Name = "Student", NormalizedName = "STUDENT" });
            oldDatabase.UserRoles.Add(new IdentityUserRole<string> { UserId = user.Id, RoleId = "existing-student-role" });
            await oldDatabase.SaveChangesAsync();
        }
        using var client = app.NewClient();
        Assert.Equal(System.Net.HttpStatusCode.Redirect, (await FormClient.LoginAsync(client, "existing@example.test")).StatusCode);
        Assert.Equal(System.Net.HttpStatusCode.OK, (await client.GetAsync("/Student")).StatusCode);
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(2, (await db.Database.GetAppliedMigrationsAsync()).Count());
        Assert.Equal(2, await db.Roles.CountAsync());
        Assert.Equal(0, await db.Courses.CountAsync());
    }
}
