using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
namespace HomeworkPlatform.Web.Data;

// EF commands use the same persistent database as the application without starting the host.
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var current = Directory.GetCurrentDirectory();
        var root = File.Exists(Path.Combine(current, "web", "HomeworkPlatform.Web.csproj"))
            ? Path.Combine(current, "web") : current;
        var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
        var configuration = new ConfigurationBuilder().SetBasePath(root)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables().Build();
        var connection = new SqliteConnectionStringBuilder(configuration.GetConnectionString("DefaultConnection")
            ?? "Data Source=App_Data/homework.db");
        if (connection.Mode != SqliteOpenMode.Memory && connection.DataSource != ":memory:")
        {
            connection.DataSource = Path.GetFullPath(connection.DataSource, root);
            Directory.CreateDirectory(Path.GetDirectoryName(connection.DataSource)!);
        }
        return new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection.ToString()).Options);
    }
}
