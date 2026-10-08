using System.Diagnostics;

namespace HomeworkPlatform.Web.Tests.Infrastructure;

public static class ProvisioningProcess
{
    public static string Root => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));

    public static async Task<(int ExitCode, bool TimedOut, string Output)> RunAsync(string databasePath, string environment, string? password, params string[] arguments)
    {
        var start = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = Path.Combine(Root, "web"), RedirectStandardOutput = true, RedirectStandardError = true,
            UseShellExecute = false, CreateNoWindow = true
        };
        start.ArgumentList.Add(Path.Combine(Root, "web/bin/Debug/net8.0/HomeworkPlatform.Web.dll"));
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        start.Environment["ASPNETCORE_ENVIRONMENT"] = environment;
        start.Environment["DOTNET_ENVIRONMENT"] = environment;
        start.Environment["ConnectionStrings__DefaultConnection"] = $"Data Source={databasePath}";
        start.Environment["ASPNETCORE_URLS"] = "http://127.0.0.1:0";
        start.Environment["Logging__LogLevel__Default"] = "Warning";
        start.Environment["PROVISION_TEACHER_PASSWORD"] = password ?? "";
        using var process = Process.Start(start)!;
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var timedOut = false;
        try { await process.WaitForExitAsync(timeout.Token); }
        catch (OperationCanceledException) { timedOut = true; process.Kill(entireProcessTree: true); await process.WaitForExitAsync(); }
        return (process.ExitCode, timedOut, await stdout + await stderr);
    }
}
