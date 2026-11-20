using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using HomeworkPlatform.Web.Controllers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace HomeworkPlatform.Web.Tests.Infrastructure;

public class AuthWebApplicationFactory : WebApplicationFactory<HomeController>
{
    internal static System.Runtime.CompilerServices.ConditionalWeakTable<HttpClient, AuthWebApplicationFactory> Owners { get; } = new();
    public bool EnableInitialData { get; set; }
    public Action<IServiceCollection>? ConfigureServices {get;set;}
    public string DirectoryPath { get; } = Path.Combine(Path.GetTempPath(), "HomeworkPlatform.Tests", Guid.NewGuid().ToString("N"));
    public string DatabasePath => Path.Combine(DirectoryPath, "identity.db");
    public string EnvironmentName { get; set; } = "Development";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(EnvironmentName);
        builder.ConfigureTestServices(services => { services.AddDataProtection().UseEphemeralDataProtectionProvider(); ConfigureServices?.Invoke(services); });
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = $"Data Source={DatabasePath}",
            ["AssignmentFiles:RootPath"] = Path.Combine(DirectoryPath, "AssignmentFiles"),
            ["CourseMaterials:RootPath"] = Path.Combine(DirectoryPath, "CourseMaterials"),
            ["SubmissionFiles:RootPath"] = Path.Combine(DirectoryPath, "SubmissionFiles"),
            ["Logging:LogLevel:Default"] = "Warning",
            ["InitialData:Enabled"] = EnableInitialData.ToString()
        }));
    }

    public HttpClient NewClient()
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, BaseAddress = new Uri("https://localhost") });
        Owners.Add(client, this);
        return client;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (Directory.Exists(DirectoryPath)) Directory.Delete(DirectoryPath, true);
        }
    }
}
