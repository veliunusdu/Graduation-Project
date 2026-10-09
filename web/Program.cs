using HomeworkPlatform.Web.Data;
using HomeworkPlatform.Web.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var hostOptions = new[] { "--urls", "--environment", "--contentRoot", "--webroot", "--applicationName" };
var commandMode = args.Length > 0 && !IsHostingArguments(args, hostOptions);
var builder = WebApplication.CreateBuilder(commandMode ? Array.Empty<string>() : args);
builder.Services.AddControllersWithViews(options => options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute()));
builder.Services.AddDbContext<ApplicationDbContext>((services, options) => options.UseSqlite(
    DatabaseConfiguration.GetConnectionString(services.GetRequiredService<IConfiguration>(), services.GetRequiredService<IHostEnvironment>())));
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireDigit = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
}).AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
});
builder.Services.AddScoped<TeacherProvisioner>();
var app = builder.Build();
if (commandMode)
{
    Environment.ExitCode = await ProvisionTeacherCommand.RunAsync(args, app.Services, app.Environment,
        Environment.GetEnvironmentVariable("PROVISION_TEACHER_PASSWORD"));
    await app.DisposeAsync();
    return;
}
await IdentityInitializer.InitializeAsync(app.Services);
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.Run();

static bool IsHostingArguments(string[] arguments, string[] allowedOptions)
{
    for (var index = 0; index < arguments.Length; index++)
    {
        var option = arguments[index].Split('=', 2);
        if (!allowedOptions.Contains(option[0], StringComparer.OrdinalIgnoreCase)) return false;
        if (option.Length == 2)
        {
            if (string.IsNullOrWhiteSpace(option[1])) return false;
        }
        else if (++index >= arguments.Length || arguments[index].StartsWith("--")) return false;
    }
    return true;
}
