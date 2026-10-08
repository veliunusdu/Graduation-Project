# Web application setup

## Current milestone

The ASP.NET Core 8 MVC app now includes Identity authentication, SQLite migrations, and Teacher/Student role restrictions. See [AUTHENTICATION_SETUP.md](AUTHENTICATION_SETUP.md) for registration, login, Teacher provisioning and tests. Ownership checks and AI integration remain later steps. The verification record below preserves the original foundation milestone.

## Prerequisites

Install .NET SDK 8.0.425 or a compatible later 8.0.4xx patch. The root global.json selects this feature band with latestPatch roll-forward; installing only .NET 10 does not satisfy it.

From PowerShell:

```powershell
Set-Location 'C:\Codes\Projects\Graduation Project'
dotnet --version
dotnet restore '.\web\HomeworkPlatform.Web.csproj'
dotnet build '.\web\HomeworkPlatform.Web.csproj' --no-restore
```

If you clone to another location, change the Set-Location path. The project commands remain relative to the repository root.

## Run locally

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:DOTNET_ENVIRONMENT = 'Development'
dotnet run --project '.\web\HomeworkPlatform.Web.csproj' --no-build --no-launch-profile --urls 'http://127.0.0.1:5080'
```

Open http://127.0.0.1:5080/ in your browser. The page should show Welcome and the HomeworkPlatform.Web application title.

Leave that terminal running. In a second PowerShell terminal, check the response:

```powershell
$response = Invoke-WebRequest -Uri 'http://127.0.0.1:5080/' -UseBasicParsing
$response.StatusCode
$response.Content -match '<h1[^>]*>Welcome</h1>'
$response.Content -match '<title>Home Page - HomeworkPlatform.Web</title>'
```

Expected: 200, True, True. Press Ctrl+C in the application terminal to stop it. The environment variable applies to that terminal session; close the terminal or restore its previous value when finished.

This explicit loopback HTTP command is for local development and does not need a trusted HTTPS development certificate. With no HTTPS endpoint configured, the standard HTTPS-redirection middleware may log that it cannot determine the HTTPS port; the verified HTTP homepage still responds normally. Do not treat this development command as a production hosting configuration.

The generated launchSettings.json also contains an https profile. Use it separately if you configure and trust a development certificate yourself; the foundation verification does not depend on it.

## Troubleshooting

- SDK selection error: check global.json and installed SDKs with dotnet --list-sdks.
- Port 5080 occupied: stop your own previously launched app or choose another unused local port and update the URL in both run and check commands. Do not terminate unrelated services.
- Restore/build error: resolve the reported SDK or dependency error before using --no-build.
- After changing C# source, build again before running with --no-build.

## Verification record

Verified on October 9 2026:

- SDK selected: 8.0.425.
- dotnet restore succeeded.
- dotnet build --no-restore succeeded with 0 warnings and 0 errors.
- A real Development-mode app returned HTTP 200 with the Welcome heading and expected application title at http://127.0.0.1:5080/.
- The process started for verification was stopped afterward.

The unchanged framework scaffold originally used build and HTTP checks. Authentication now has dedicated integration tests described in AUTHENTICATION_SETUP.md; ownership tests remain pending.

## Review and branch

An independent read-only review of the completed foundation found no actionable defects. The original foundation was saved locally on feature/aspnet-foundation. Later work continues on feature/authentication, with those earlier commits included; no branch has been pushed or merged by this workflow.

Implementation stayed in the requested main project folder on a feature branch. Native Windows progress tracking was used. Verification followed the approved scaffold exception: build and real HTTP checks rather than custom unit tests. At that foundation stage, authentication, ownership and production hosting were pending. Authentication is now implemented; ownership and production hosting remain pending. Existing Python files and template libraries were preserved; one upstream vendor comment retains harmless trailing whitespace.
