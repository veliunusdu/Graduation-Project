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
}
