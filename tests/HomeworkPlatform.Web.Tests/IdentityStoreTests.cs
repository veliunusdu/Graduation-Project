using HomeworkPlatform.Web.Tests.Infrastructure;
using Microsoft.Data.Sqlite;

namespace HomeworkPlatform.Web.Tests;

public class IdentityStoreTests
{
    [Fact]
    public async Task StartupAppliesIdentityMigrationAndCreatesRoles()
    {
        using var app = new AuthWebApplicationFactory();
        using var client = app.NewClient();
        Assert.True((await client.GetAsync("/")).IsSuccessStatusCode);
        Directory.CreateDirectory(app.DirectoryPath);
        await using var connection = new SqliteConnection($"Data Source={app.DatabasePath}");
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name IN ('AspNetUsers','AspNetRoles','__EFMigrationsHistory')";
        Assert.Equal(3L, (long)(await command.ExecuteScalarAsync())!);
        command.CommandText = "SELECT Name FROM AspNetRoles ORDER BY Name";
        await using var reader = await command.ExecuteReaderAsync();
        var roles = new List<string>();
        while (await reader.ReadAsync()) roles.Add(reader.GetString(0));
        Assert.Equal(new[] { "Student", "Teacher" }, roles);
    }
}
