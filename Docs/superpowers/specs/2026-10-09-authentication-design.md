# Authentication and Roles Step 3 Design

Date: October 9 2026
Status: Approved by Veli; implemented, verified and independently reviewed (test executable finding fixed)
Project: C:\Codes\Projects\Graduation Project

## Goal and Approved Approach

Add real account authentication to the existing ASP.NET Core 8 MVC application so the first two weeks' Teacher/Student role requirements can be demonstrated. Veli approved ASP.NET Core Identity with SQLite, Student-only public registration, controlled local Teacher provisioning, role-protected pages, and authentication tests.

This design covers authentication and role restrictions. Course, assignment, and submission ownership checks remain Step 4. The existing web scaffold, Python service, scope, and samples must remain intact.

## Alternatives and Choice

Use Identity with EF Core SQLite rather than hand-written password hashing and cookie/user storage. Identity provides the user/password/role primitives while MVC controllers and views expose the small set of account flows needed here. Avoid regenerating the web project or importing the old ZIP.

Do not expose Teacher role selection in registration. Teacher accounts are provisioned through a local Development-only command; an admin panel, invitations, and production account management are outside this milestone.

## Persistence and Identity Configuration

- Add ApplicationUser deriving from IdentityUser and ApplicationDbContext deriving from IdentityDbContext<ApplicationUser>.
- Use string Identity user IDs. Future TeacherId/StudentId domain references must use that type; assignment/course/submission IDs can remain integers as proposed in the analysis contract.
- Add aligned .NET 8 Identity/EF Core packages and a checked-in initial migration. Use migrations rather than EnsureCreated; do not modify an existing unknown database destructively.
- Default SQLite storage is web/App_Data/homework.db, resolved using the application's content root rather than the invoking shell directory. ConnectionStrings:DefaultConnection can override storage.
- Ignore local database files and sidecars, but track migrations and source. No plaintext passwords or configured demo credentials in source/appsettings.
- Apply migrations and ensure Teacher/Student roles exist during this local milestone's startup. Production migration orchestration is later deployment work, not claimed as solved here.
- Require unique email; use the email as username. Password policy: at least 8 characters with uppercase, lowercase, digit, and a non-alphanumeric character. Login failure lockout: 5 failed attempts for 5 minutes; login enables lockout counting.
- Email confirmation is not required for this local milestone because no email-delivery integration is implemented. Do not imply an address has been verified.

## Routes and Account Flows

Use MVC view models rather than binding Identity entities directly. Public registration accepts email, password, and confirmation only; posted role fields have no effect.

| Route | Method | Behavior |
| --- | --- | --- |
| /Account/Register | GET | Show Student registration form. |
| /Account/Register | POST | Validate input, create hashed-password user, assign Student, sign in only after both succeed. |
| /Account/Login | GET | Show login form with optional local return URL. |
| /Account/Login | POST | Validate input and sign in through Identity; generic failure for invalid credentials or lockout. |
| /Account/Logout | POST | Sign out the application cookie and redirect to the homepage. |
| /Account/AccessDenied | GET | Render access-denied page with HTTP 403. |
| /Teacher | GET | Require Teacher; display minimal Teacher dashboard. |
| /Student | GET | Require Student; display minimal Student dashboard. |

The homepage remains public. After login/registration, redirect to a validated local return URL or the user's role dashboard. Reject external/scheme-relative return URLs and never redirect to a user-selected external host. Teacher and Student dashboards are role demonstrations, not completed course/submission features.

Enable authentication before authorization. Configure cookie login/access-denied paths; cookies are HttpOnly and SameSite=Lax. Secure cookies are required outside Development; local Development permits the existing loopback HTTP workflow. Authenticated wrong-role requests redirect through the cookie access-denied path and end at HTTP 403. Anonymous protected requests lead to login, not protected content.

Apply antiforgery validation to unsafe MVC requests. Forms include tokens; logout is not available by GET. Include tests that submit real form tokens and cookies, not test-only authentication shortcuts.

The shared layout shows Register/Login for anonymous users and the appropriate dashboard link and POST logout form for signed-in users. Role restrictions must hold even when callers bypass the navigation.

## Controlled Teacher Provisioning

Provide a local command in the web application:

    dotnet run --project web/HomeworkPlatform.Web.csproj --no-launch-profile -- --provision-teacher teacher@example.test

The command requires ASPNETCORE_ENVIRONMENT=Development and reads the password from PROVISION_TEACHER_PASSWORD in the process environment. It initializes the database/roles, creates a Teacher user, and exits without starting the HTTP server. Documentation uses a hidden password prompt to populate the temporary environment variable; do not put the password in command arguments, examples, logs, or source.

Reject invalid/missing password/email and return a nonzero exit code with a safe message. Do not print the submitted password or environment contents. If an existing user is already a Teacher, report the existing account without changing its password. If it is a Student or another existing non-Teacher user, refuse silent promotion. Report role-assignment failure rather than claiming provisioning succeeded.

Registration and provisioning must not leave a successfully reported but roleless account if role assignment fails; roll back a newly created account or use a transaction, and report a safe failure.

## Files and Boundaries

Create identity data models/context, initial migrations, role initialization/provisioning helper, AccountController and account view models, account views, minimal TeacherController/StudentController and views, and authentication integration tests. Modify Program.cs, appsettings configuration, project dependencies, and shared navigation. Keep business ownership enforcement out of this step.

Create Docs/AUTHENTICATION_SETUP.md with setup, migration/startup behavior, local provisioning, login, tests, and configuration limitations. Update WEEKS_1_2_PROGRESS.md only after verification.

Tests use a separate temporary SQLite database and isolated environment per test host. They must never alter the real local homework.db or depend on pre-existing accounts.

## Verification and Completion Criteria

1. Restore/build and apply the initial migration to an empty test database.
2. Demonstrate Student registration, persisted password hash differing from plaintext, and successful credential verification through Identity.
3. Reject malformed registration and invalid credentials; demonstrate configured lockout behavior.
4. Confirm Student-only registration even with a forged Teacher field.
5. Verify login cookies establish authentication and POST logout removes access.
6. Verify anonymous redirects and both wrong-role directions, plus legitimate role access.
7. Reject missing antiforgery tokens on account POST actions and deny GET logout.
8. Prevent external return-URL redirection.
9. Verify Teacher provisioning creates the correct role, is idempotent for an existing Teacher, refuses existing Student promotion, and fails outside Development or without required inputs.
10. Run the full web integration suite, the existing Python suite, and an actual local HTTP demonstration using an isolated demo database. Stop only the processes started for verification.
11. Review the patch and documentation before claiming Step 3 complete.

Do not claim course, assignment, or submission ownership protection until Step 4 and its tests are implemented. The next step depends on these authenticated user IDs and roles.
