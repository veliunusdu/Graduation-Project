# Authentication and prepared data

The app uses ASP.NET Core Identity with SQLite. Accounts are prepared in advance. There is no public registration or profile-completion endpoint. `/Account/Login` signs in using the stored account's email and password; the role is read from Identity. Existing incomplete-profile accounts can also sign in directly.

Passwords are hashed by Identity. Email addresses are unique, password requirements are unchanged, and five failed attempts lock an account for five minutes. Login accepts only safe local return URLs. All unsafe MVC actions require antiforgery tokens. Cookies are HttpOnly and SameSite=Lax, and require HTTPS outside Development.

## Local startup

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:DOTNET_ENVIRONMENT = 'Development'
dotnet run --project web/HomeworkPlatform.Web.csproj --no-launch-profile --urls http://localhost:5080
```

Open http://localhost:5080/Account/Login. Full setup and example credentials are in `KURULUM_VE_DEGISIKLIKLER.md` at the project root.

## Prepared accounts, courses and enrollments

In Development, `InitialData:Enabled` enables `InitialDataSeeder`, which reads `web/Data/initial-data.json`. It creates named accounts using `UserManager.CreateAsync`, assigns Student/Teacher roles, creates the teacher's initial courses, and records each student's course enrollments. The bundled accounts are local examples and are never automatically loaded in Production.

The seed runs in one transaction and records its version key in `DataInitializations` after success. Repeating the same key does not duplicate records, reset passwords, overwrite existing profiles, or resurrect deleted courses. Failed initialization rolls back its partial changes. Existing accounts are matched by email; a different role is rejected rather than promoted. A new key can add further prepared data; it is not an account-editing mechanism.

The `InitialDataHistory` migration adds this completion ledger. Startup applies pending migrations. Existing users, passwords, profiles and course data are preserved. The old `ProfileCompleted` database field remains for schema compatibility and no longer controls sign-in.

Teachers see their assigned courses immediately and can create additional owned courses. The create form accepts selected existing Student IDs; the controller checks role membership and saves the course plus enrollments together. Teachers can also enroll an existing student from a dropdown on their own course's details page. Students only see their active enrolled courses. There is no administrator role or page.

The legacy Development-only `--provision-teacher` command remains compatible with existing scripts; normal local startup does not require it.

## Verification

```powershell
dotnet test tests/HomeworkPlatform.Web.Tests/HomeworkPlatform.Web.Tests.csproj
node --test scripts/test_edupilot_auth.cjs
```

Tests cover prepared accounts, hashing, automatic enrollments, idempotent initialization, rollback, production seed exclusion, removed public routes, existing-account compatibility, teacher-selected enrollment, invalid selected accounts, ownership, login failures, lockout, cookies and antiforgery. Live Python/Ollama/BGE checks retain their external-service opt-in requirements.

## Education levels

`ApplicationUser.EducationLevel` stores each teacher's and student's education level. All bundled accounts now use University and all bundled students are University2. All three example courses are University / Year 2, including Mathematics.

The create/edit course form reads the current teacher record from the database on every request. EducationLevel is not bound from user input. The form displays a read-only level and offers years 1–4 for University or grades 9–12 for HighSchool. The server validates the year and rejects attempts to override the teacher's level. Student selectors and enrollment validation also require the student's level to match the course.

The `AccountEducationLevels` migration backfills existing student levels from their stored class and teacher levels from their existing courses. It updates the known example accounts and the example teacher's old high school courses to University while preserving account passwords, course IDs and enrollments. High school support and its tests remain available for a later deployment.
