using HomeworkPlatform.Web.Data;
using HomeworkPlatform.Web.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace HomeworkPlatform.Web.Tests;

public class ProvisioningTests
{
    [Fact]
    public async Task CommandCreatesTeacherExitsAndIsIdempotentWithoutChangingPassword()
    {
        using var app = new AuthWebApplicationFactory();
        var created = await ProvisioningProcess.RunAsync(app.DatabasePath, "Development", FormClient.TestPassword, "--provision-teacher", "teacher@example.test");
        Assert.False(created.TimedOut, "Provisioning must exit without running a server.");
        Assert.Equal(0, created.ExitCode);
        Assert.DoesNotContain(FormClient.TestPassword, created.Output);
        var again = await ProvisioningProcess.RunAsync(app.DatabasePath, "Development", "Another-Only9!Password", "--provision-teacher", "teacher@example.test");
        Assert.False(again.TimedOut);
        Assert.Equal(0, again.ExitCode);
        using var scope = app.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = Assert.IsType<ApplicationUser>(await users.FindByEmailAsync("teacher@example.test"));
        Assert.Equal(new[] { "Teacher" }, await users.GetRolesAsync(user));
        Assert.True(await users.CheckPasswordAsync(user, FormClient.TestPassword));
        Assert.False(await users.CheckPasswordAsync(user, "Another-Only9!Password"));
    }

    [Fact]
    public async Task CommandRefusesExistingStudentPromotion()
    {
        using var app = new AuthWebApplicationFactory();
        using var client = app.NewClient();
        await FormClient.RegisterAsync(client, "student@example.test");
        var result = await ProvisioningProcess.RunAsync(app.DatabasePath, "Development", FormClient.TestPassword, "--provision-teacher", "student@example.test");
        Assert.False(result.TimedOut);
        Assert.NotEqual(0, result.ExitCode);
        using var scope = app.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        Assert.Equal(new[] { "Student" }, await users.GetRolesAsync((await users.FindByEmailAsync("student@example.test"))!));
    }

    [Theory]
    [InlineData("Production", "Test-Only8!Password", "teacher@example.test")]
    [InlineData("Development", "", "teacher@example.test")]
    [InlineData("Development", "weak", "teacher@example.test")]
    [InlineData("Development", "Test-Only8!Password", "not-email")]
    public async Task InvalidProvisioningFailsWithoutCreatingDatabase(string environment, string password, string email)
    {
        using var app = new AuthWebApplicationFactory();
        var result = await ProvisioningProcess.RunAsync(app.DatabasePath, environment, password, "--provision-teacher", email);
        Assert.False(result.TimedOut);
        Assert.NotEqual(0, result.ExitCode);
        Assert.False(File.Exists(app.DatabasePath));
        if (password.Length > 0) Assert.DoesNotContain(password, result.Output);
    }

    [Theory]
    [InlineData("--unknown", "value")]
    [InlineData("--provision-teacher", "")]
    public async Task MalformedCommandExitsWithoutServingOrWriting(string argument, string value)
    {
        using var app = new AuthWebApplicationFactory();
        var args = value.Length == 0 ? new[] { argument } : new[] { argument, value };
        var result = await ProvisioningProcess.RunAsync(app.DatabasePath, "Development", FormClient.TestPassword, args);
        Assert.False(result.TimedOut);
        Assert.NotEqual(0, result.ExitCode);
        Assert.False(File.Exists(app.DatabasePath));
    }
}
