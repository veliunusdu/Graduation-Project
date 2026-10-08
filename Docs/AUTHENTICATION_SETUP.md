# Authentication setup

Run these commands in PowerShell from the main project folder:

```powershell
Set-Location 'C:\Codes\Projects\Graduation Project'
dotnet restore HomeworkPlatform.sln
dotnet build HomeworkPlatform.sln --no-restore
```

The web app uses ASP.NET Core Identity, EF Core SQLite, and the checked-in InitialIdentity migration. Startup applies pending migrations and ensures Teacher and Student roles exist. It preserves existing accounts. The default database is `web/App_Data/homework.db`, resolved against the web content root. Database files and sidecars are ignored by Git; migration source stays tracked.

Start the local web app:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:DOTNET_ENVIRONMENT = 'Development'
dotnet run --project web/HomeworkPlatform.Web.csproj --no-launch-profile --urls http://localhost:5080
```

Open http://localhost:5080. Stop the server with Ctrl+C. The explicit HTTP Development workflow can report that no HTTPS redirect port is configured. Outside Development, authentication cookies require HTTPS.

## Account flows

- `/Account/Register`: public Student registration. It accepts email, password and password confirmation; callers cannot choose Teacher through form fields.
- `/Account/Login`: cookie login with a local return URL or the account's role dashboard.
- `/Student` and `/Teacher`: minimal pages requiring their respective role. Anonymous visitors are sent to login. Authenticated wrong-role requests end at an access-denied page with HTTP 403.
- Logout uses the navigation's POST form with an antiforgery token. GET logout does not sign users out.

Passwords require at least 8 characters, including uppercase, lowercase, a digit, and a symbol. Identity stores password hashes. Email addresses must be unique. Five failed login attempts lock an account for five minutes. Login uses a generic failure message. Cookies are HttpOnly and SameSite=Lax, and unsafe account forms require antiforgery validation.

## Provision a local Teacher

Public registration creates Students. Provision Teachers through the Development-only command below. The password comes from a hidden prompt and a temporary environment setting; it is never a command argument or a literal in this guide.

Run this in a separate terminal while the server is stopped for the simplest setup:

```powershell
Set-Location 'C:\Codes\Projects\Graduation Project'
$authPreviousAspnetEnvironment = $env:ASPNETCORE_ENVIRONMENT
$authPreviousDotnetEnvironment = $env:DOTNET_ENVIRONMENT
$authPreviousTeacherPassword = $env:PROVISION_TEACHER_PASSWORD
$authSecurePassword = Read-Host 'New Teacher password' -AsSecureString
$authPasswordPointer = [IntPtr]::Zero
try {
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    $env:DOTNET_ENVIRONMENT = 'Development'
    $authPasswordPointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($authSecurePassword)
    $env:PROVISION_TEACHER_PASSWORD = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($authPasswordPointer)
    dotnet run --project web/HomeworkPlatform.Web.csproj --no-launch-profile -- --provision-teacher teacher@example.test
    if ($LASTEXITCODE -ne 0) { throw 'Teacher provisioning failed; see the safe command message above.' }
}
finally {
    $env:PROVISION_TEACHER_PASSWORD = $authPreviousTeacherPassword
    $env:ASPNETCORE_ENVIRONMENT = $authPreviousAspnetEnvironment
    $env:DOTNET_ENVIRONMENT = $authPreviousDotnetEnvironment
    if ($authPasswordPointer -ne [IntPtr]::Zero) {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($authPasswordPointer)
    }
    $authSecurePassword.Dispose()
    $authPreviousTeacherPassword = $null
}
```

The command exits without starting an HTTP server. A new valid account gets Teacher. An existing Teacher keeps its current password. An existing Student or other non-Teacher is refused rather than promoted. Invalid usage, invalid email/password, a missing password, or a non-Development environment returns nonzero. Invalid-input checks run before migration/database creation. The command gives safe messages and does not print the password.

After provisioning, start the web app and sign in with that Teacher's email and the password you entered. If it already existed, use its original password.

## Database overrides and migrations

To use a separate database, set this before either startup or provisioning:

```powershell
$env:ConnectionStrings__DefaultConnection = 'Data Source=C:\Codes\Projects\Graduation Project\web\App_Data\my-local.db'
```

Relative SQLite paths are resolved against the web content root. Memory-mode strings are preserved. Remove a temporary override when finished:

```powershell
Remove-Item Env:ConnectionStrings__DefaultConnection -ErrorAction SilentlyContinue
```

Startup applies existing migrations automatically for this local milestone. For future schema changes, restore the repository's local EF tool and generate a new migration, preserving the existing migration history:

```powershell
dotnet tool restore
dotnet ef migrations add YourMigrationName --project web --startup-project web --output-dir Data/Migrations
```

The design-time context factory generates the model without starting the server or modifying the local database. Do not delete the database to apply ordinary schema changes.

## Verification

```powershell
dotnet test HomeworkPlatform.sln
dotnet test HomeworkPlatform.sln -c Release
Push-Location ai-service
try { & '.\.venv\Scripts\python.exe' -m pytest -q }
finally { Pop-Location }
```

Web tests use real Identity accounts, SQLite migrations, form tokens and cookies. Each host owns an isolated temporary database. Provisioning process tests override their database and environment and bound their process lifetime. Test passwords are synthetic test-only fixtures, not account defaults.

Verified October 9, 2026:

- Solution restore/build passed with zero build warnings or errors.
- All 27 web tests passed in Debug and also in Release with the web Debug output temporarily set aside and restored: migration/roles, hashing, invalid/duplicate registration, forged roles, login/lockout, return URLs, provisioning success/refusals/idempotence, role access, cookie flags, antiforgery and logout.
- All 8 existing Python tests passed.
- A live HTTP server using a disposable database passed Student registration/login/logout, Teacher login after provisioning, and both wrong-role checks ending at HTTP 403. Its owned process was stopped. The real local database was unchanged.

## Milestone boundaries

Email confirmation and email delivery are not implemented; an account's email is not verified. Production Teacher provisioning and production migration orchestration remain later deployment work. Course, assignment and submission ownership enforcement is Step 4 and is still pending. Role-protected dashboard pages do not establish ownership protection for those future resources.

## Independent review and completion

One independent read-only review found no Critical or Minor defects and one Important test defect: provisioning process tests selected a fixed Debug executable. A Release-only regression reproduced the failure, then the helper was changed to run the referenced web assembly from the current test output, alongside its copied runtime configuration and dependencies. All 27 tests then passed in both build configurations. No findings remain unresolved; no second review was used to substitute for regression evidence.

Implementation stayed in Veli's chosen main folder on `feature/authentication`, with local commits only. Native Windows bookkeeping replaced shell-only skill scripts. The EF design-time factory prevents database initialization during migration generation. Supported host options remain usable, including the `--key=value` form passed by MVC Testing; malformed application commands still fail without serving HTTP. Default storage is resolved by DatabaseConfiguration rather than duplicated in appsettings.

The reviewer set aside resource ownership, production account/migration management, and email confirmation/delivery. Those exclusions match the approved milestone and remain explicitly pending. This step completes authentication and role restrictions; it does not complete all Week 2 work.
