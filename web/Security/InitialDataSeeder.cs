using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using HomeworkPlatform.Web.Data;
using HomeworkPlatform.Web.Models.Domain;
using HomeworkPlatform.Web.Models.Work;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HomeworkPlatform.Web.Security;

public sealed class InitialDataDocument
{
    public string Key { get; set; } = "";
    public List<InitialAccount> Accounts { get; set; } = new();
    public List<InitialCourse> Courses { get; set; } = new();
}
public sealed class InitialAccount
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string Role { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public EducationLevel EducationLevel { get; set; } = EducationLevel.University;
    public string? StudentClass { get; set; }
}
public sealed class InitialCourse
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string TeacherEmail { get; set; } = "";
    public EducationLevel EducationLevel { get; set; }
    public int GradeLevel { get; set; }
    public List<string> StudentEmails { get; set; } = new();
}

public sealed class InitialDataSeeder(ApplicationDbContext database, UserManager<ApplicationUser> users)
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var environment = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
        // Bundled accounts are examples for local development, never production defaults.
        if (!environment.IsDevelopment() || !config.GetValue<bool>("InitialData:Enabled")) return;
        var path = Path.GetFullPath(config["InitialData:File"] ?? "Data/initial-data.json", environment.ContentRootPath);
        await using var input = File.OpenRead(path);
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        var data = await JsonSerializer.DeserializeAsync<InitialDataDocument>(input, options)
            ?? throw new InvalidOperationException("Initial data is empty.");
        await scope.ServiceProvider.GetRequiredService<InitialDataSeeder>().SeedAsync(data);
    }

    public async Task SeedAsync(InitialDataDocument data)
    {
        if (string.IsNullOrWhiteSpace(data.Key) || data.Key.Length > 100)
            throw new InvalidOperationException("Initial data needs a valid version key.");
        if (await database.DataInitializations.AnyAsync(x => x.Key == data.Key)) return;
        if (data.Accounts.Select(a => a.Email.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() != data.Accounts.Count)
            throw new InvalidOperationException("Initial account emails must be unique.");
        var accounts = new Dictionary<string, ApplicationUser>(StringComparer.OrdinalIgnoreCase);
        await using var transaction = await database.Database.BeginTransactionAsync();
        foreach (var account in data.Accounts)
        {
            if (account.Role is not (AppRoles.Student or AppRoles.Teacher)
                || !new EmailAddressAttribute().IsValid(account.Email.Trim())
                || string.IsNullOrWhiteSpace(account.FirstName) || account.FirstName.Length > 100
                || string.IsNullOrWhiteSpace(account.LastName) || account.LastName.Length > 100
                || !Enum.IsDefined(account.EducationLevel)
                || (account.Role == AppRoles.Student && !EducationRules.IsValidClass(account.EducationLevel, account.StudentClass)))
                throw new InvalidOperationException("An initial account has invalid name, email, role or class information.");
            var email = account.Email.Trim();
            var user = await users.FindByEmailAsync(email);
            if (user == null)
            {
                user = new ApplicationUser { UserName = email, Email = email, FirstName = account.FirstName.Trim(),
                    LastName = account.LastName.Trim(), EducationLevel = account.EducationLevel, StudentClass = account.Role == AppRoles.Student ? account.StudentClass : null, ProfileCompleted = true };
                var result = await users.CreateAsync(user, account.Password);
                if (result.Succeeded) result = await users.AddToRoleAsync(user, account.Role);
                if (!result.Succeeded) throw new InvalidOperationException("Initial account creation failed. Check the fields and password policy.");
            }
            else if (!await users.IsInRoleAsync(user, account.Role) || user.EducationLevel != account.EducationLevel)
                throw new InvalidOperationException("An existing account has a different role or education level; initial data will not change it.");
            accounts.Add(email, user);
        }
        foreach (var item in data.Courses)
        {
            var form = new CourseForm { Name = item.Name, Description = item.Description, EducationLevel = item.EducationLevel, GradeLevel = item.GradeLevel };
            if (!Validator.TryValidateObject(form, new ValidationContext(form), new List<ValidationResult>(), true)
                || !EducationRules.IsValidGrade(item.EducationLevel, item.GradeLevel)
                || !accounts.TryGetValue(item.TeacherEmail.Trim(), out var teacher) || !await users.IsInRoleAsync(teacher, AppRoles.Teacher) || teacher.EducationLevel != item.EducationLevel)
                throw new InvalidOperationException("An initial course has invalid fields or teacher assignment.");
            var course = await database.Courses.SingleOrDefaultAsync(c => c.TeacherId == teacher.Id && c.Name == item.Name.Trim());
            if (course == null)
            {
                course = new Course { Name = item.Name.Trim(), Description = item.Description, TeacherId = teacher.Id,
                    EducationLevel = item.EducationLevel, GradeLevel = item.GradeLevel };
                database.Courses.Add(course);
                await database.SaveChangesAsync();
            }
            foreach (var email in item.StudentEmails.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (!accounts.TryGetValue(email.Trim(), out var student) || !await users.IsInRoleAsync(student, AppRoles.Student) || student.EducationLevel != item.EducationLevel)
                    throw new InvalidOperationException("An initial course refers to an unknown student.");
                if (!await database.StudentCourses.AnyAsync(e => e.CourseId == course.Id && e.StudentId == student.Id))
                    database.StudentCourses.Add(new StudentCourse { CourseId = course.Id, StudentId = student.Id });
            }
            await database.SaveChangesAsync();
        }
        database.DataInitializations.Add(new DataInitialization { Key = data.Key });
        await database.SaveChangesAsync();
        await transaction.CommitAsync();
    }

}
