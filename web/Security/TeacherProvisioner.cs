using System.ComponentModel.DataAnnotations;
using HomeworkPlatform.Web.Data;
using Microsoft.AspNetCore.Identity;
namespace HomeworkPlatform.Web.Security;

public record ProvisioningResult(bool Success, bool Existing, string Message);

public class TeacherProvisioner(UserManager<ApplicationUser> users, ApplicationDbContext database)
{
    public async Task<ProvisioningResult> ProvisionAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        email = email.Trim();
        if (!new EmailAddressAttribute().IsValid(email) || string.IsNullOrWhiteSpace(password))
            return new(false, false, "A valid email and password are required.");
        var candidate = new ApplicationUser { UserName = email, Email = email };
        foreach (var validator in users.PasswordValidators)
            if (!(await validator.ValidateAsync(users, candidate, password)).Succeeded)
                return new(false, false, "Password does not meet the configured policy.");

        var existing = await users.FindByEmailAsync(email);
        if (existing != null)
            return await users.IsInRoleAsync(existing, AppRoles.Teacher)
                ? new(true, true, "Teacher already exists; password unchanged.")
                : new(false, true, "Existing non-Teacher account cannot be promoted by this command.");

        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        var result = await users.CreateAsync(candidate, password);
        if (result.Succeeded) result = await users.AddToRoleAsync(candidate, AppRoles.Teacher);
        if (!result.Succeeded)
        {
            await transaction.RollbackAsync(cancellationToken);
            return new(false, false, "Teacher creation failed validation or role assignment.");
        }
        await transaction.CommitAsync(cancellationToken);
        return new(true, false, "Teacher created.");
    }
}
