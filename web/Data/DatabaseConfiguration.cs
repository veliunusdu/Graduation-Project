using Microsoft.Data.Sqlite;
namespace HomeworkPlatform.Web.Data;

public static class DatabaseConfiguration
{
    public static string GetConnectionString(IConfiguration configuration, IHostEnvironment environment)
    {
        var connection = new SqliteConnectionStringBuilder(configuration.GetConnectionString("DefaultConnection") ?? "Data Source=App_Data/homework.db");
        if (connection.Mode != SqliteOpenMode.Memory && connection.DataSource != ":memory:")
        {
            connection.DataSource = Path.GetFullPath(connection.DataSource, environment.ContentRootPath);
            Directory.CreateDirectory(Path.GetDirectoryName(connection.DataSource)!);
        }
        return connection.ToString();
    }
}
