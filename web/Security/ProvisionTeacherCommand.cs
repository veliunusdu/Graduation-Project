using System.ComponentModel.DataAnnotations;
using HomeworkPlatform.Web.Data;
using Microsoft.AspNetCore.Identity;
namespace HomeworkPlatform.Web.Security;

public static class ProvisionTeacherCommand
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services, IHostEnvironment environment, string? password)
    {
        if (args.Length != 2 || args[0] != "--provision-teacher" || string.IsNullOrWhiteSpace(args[1]))
            return Failure("Usage: --provision-teacher <email>");
        if (!environment.IsDevelopment()) return Failure("Teacher provisioning is allowed only in Development.");
        var email = args[1].Trim();
        if (!new EmailAddressAttribute().IsValid(email) || string.IsNullOrWhiteSpace(password))
            return Failure("A valid email and PROVISION_TEACHER_PASSWORD are required.");

        try
        {
            using var scope = services.CreateScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var candidate = new ApplicationUser { UserName = email, Email = email };
            foreach (var validator in users.PasswordValidators)
                if (!(await validator.ValidateAsync(users, candidate, password)).Succeeded)
                    return Failure("Password does not meet the configured policy.");
            await IdentityInitializer.InitializeAsync(services);
            var result = await scope.ServiceProvider.GetRequiredService<TeacherProvisioner>().ProvisionAsync(email, password);
            if (!result.Success) return Failure(result.Message);
            Console.WriteLine(result.Message);
            return 0;
        }
        catch (Exception)
        {
            // Database and Identity errors must not expose password input or connection details.
            return Failure("Teacher provisioning failed. Check local database configuration and availability.");
        }
    }

    private static int Failure(string message)
    {
        Console.Error.WriteLine(message);
        return 1;
    }
}
