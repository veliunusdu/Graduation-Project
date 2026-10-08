# Authentication and Roles Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans for inline execution, or superpowers:subagent-driven-development if Veli chooses delegation. Steps use checkbox syntax for tracking.

**Goal:** Implement Student registration, Identity cookie login/logout, controlled Teacher provisioning, and role-protected MVC pages.

**Architecture:** Extend the existing web application with EF Core SQLite Identity storage and a checked-in migration. MVC account flows use UserManager/SignInManager with antiforgery protection. A local command provisions Teachers; integration tests use real account flows and isolated SQLite files.

**Tech Stack:** ASP.NET Core 8, Identity/EF Core/MVC Testing 8.0.31, SQLite, xUnit, Microsoft.NET.Test.Sdk.

**Spec:** Docs/superpowers/specs/2026-10-09-authentication-design.md
**Root:** C:\Codes\Projects\Graduation Project

## Global Constraints

- Public registration accepts email, password, and confirmation only; posted role fields have no effect.
- String Identity user IDs; integer course/assignment/submission IDs remain possible later.
- Use migrations rather than EnsureCreated; no destructive database reset.
- Default SQLite storage is web/App_Data/homework.db, resolved against content root.
- Unique email; password minimum 8 with uppercase, lowercase, digit and non-alphanumeric character.
- Lockout after 5 failed attempts for 5 minutes; login counts failures.
- Cookies HttpOnly, SameSite=Lax, Secure outside Development; authentication before authorization.
- Unsafe MVC requests require antiforgery; logout is POST-only; only local return URLs.
- Teacher provisioning is Development-only and reads PROVISION_TEACHER_PASSWORD from the environment.
- No plaintext production/demo credentials in source, arguments, logs or appsettings. Synthetic passwords in isolated tests are explicitly test-only data.
- Preserve existing web/Python/docs/samples; no model inference, ownership features, push or PR in this milestone.
- Complete verification includes the full web suite, existing Python suite, and real local HTTP checks.

## Review Focus

- Forged Teacher fields during registration never grant Teacher access (Task 2).
- Existing Student emails cannot be silently promoted by provisioning (Task 3).
- External and scheme-relative return URLs cannot redirect outside the app (Task 2).
- Missing antiforgery tokens and GET logout cannot change authentication state (Task 4).
- Test hosts and provisioning tests cannot migrate/write the real homework.db (Tasks 1, 3 and 5).

## File Map

Create:
- HomeworkPlatform.sln: web and integration-test projects, also providing content-root discovery for WebApplicationFactory.
- .config/dotnet-tools.json: local dotnet-ef 8.0.31.
- web/Data/ApplicationUser.cs, ApplicationDbContext.cs, DatabaseConfiguration.cs.
- web/Data/Migrations/: InitialIdentity migration, designer, snapshot.
- web/Security/AppRoles.cs, IdentityInitializer.cs, TeacherProvisioner.cs, ProvisionTeacherCommand.cs.
- web/Models/Account/RegisterViewModel.cs, LoginViewModel.cs.
- web/Controllers/AccountController.cs, TeacherController.cs, StudentController.cs.
- web/Views/Account/Register.cshtml, Login.cshtml, AccessDenied.cshtml.
- web/Views/Teacher/Index.cshtml, web/Views/Student/Index.cshtml.
- tests/HomeworkPlatform.Web.Tests/HomeworkPlatform.Web.Tests.csproj.
- tests/HomeworkPlatform.Web.Tests/Infrastructure/AuthWebApplicationFactory.cs, FormClient.cs, ProvisioningProcess.cs.
- tests/HomeworkPlatform.Web.Tests/IdentityStoreTests.cs, AccountFlowTests.cs, ProvisioningTests.cs, RoleAccessTests.cs, AntiforgeryTests.cs.
- Docs/AUTHENTICATION_SETUP.md.

Modify Program.cs, web csproj/appsettings.json, shared layout, .gitignore, design approval status, and Weeks 1/2 tracker.

## Task 1: Isolated Identity Storage and Test Host

**Consumes:** existing MVC project.
**Produces:** ApplicationUser, ApplicationDbContext; DatabaseConfiguration.GetConnectionString(IConfiguration, IHostEnvironment) -> string; IdentityInitializer.InitializeAsync(IServiceProvider) -> Task; AuthWebApplicationFactory with a unique temporary database per instance.

- [ ] Inspect status and instructions; work in the requested main folder on feature/authentication, preserving existing changes. Record the spec approval and execution ledger. This is a normal repo checkout; do not move outputs to the older OneDrive folder.
- [ ] Add test-project infrastructure, solution, and dependency references needed to run baseline persistence tests. Use Microsoft.AspNetCore.Mvc.Testing/Microsoft.Data.Sqlite 8.0.31, Microsoft.NET.Test.Sdk 17.14.1, xunit 2.9.3, xunit.runner.visualstudio 3.1.5. Confirm availability during restore; use compatible stable replacements only if a package is unavailable, recording the decision.
- [ ] Write IdentityStoreTests.StartupAppliesIdentityMigrationAndCreatesRoles. Start the real app with an isolated connection string, query sqlite_master and AspNetRoles through a real SQLite connection, assert identity tables and exactly Teacher/Student roles. On the baseline app this must fail because startup has no identity persistence, not because the test cannot compile.
- [ ] Run `dotnet test tests/HomeworkPlatform.Web.Tests --filter FullyQualifiedName~IdentityStoreTests`; observe the expected failure.
- [ ] Add ApplicationUser : IdentityUser and ApplicationDbContext : IdentityDbContext<ApplicationUser>. Register AddIdentity<ApplicationUser,IdentityRole> with EF stores/default token providers; set the approved password/email/lockout policy. Add web Identity EF, SQLite and EF Design packages at 8.0.31, with Design PrivateAssets=all.
- [ ] Resolve default database path lazily when DbContext options are built, so test-host configuration takes effect. Normalize relative SQLite filenames against content root; preserve memory-mode connection strings. Create only the needed containing directory. Tests explicitly override ConnectionStrings:DefaultConnection with their own absolute temporary database path.
- [ ] Add local EF tool and checked-in initial migration using `dotnet ef migrations add InitialIdentity --project web --startup-project web --output-dir Data/Migrations`. Avoid launching real database initialization during design-time model generation; use a design-time context factory if needed, without duplicating schema.
- [ ] Implement initializer: migrate, then create Teacher/Student roles if missing; fail on role-creation errors. Call it before serving local requests. Do not reset or delete existing data. Add ignore rules scoped to web/App_Data local database files and sidecars.
- [ ] Re-run persistence tests; verify idempotent startup on the same isolated database and that the real local database was not modified. Complete Task 1 only with green tests.

## Task 2: Student Registration, Login and Safe Redirects

**Consumes:** Identity services/storage and roles from Task 1.
**Produces:** account routes/forms; authenticated Student cookies; local role-aware redirect behavior.

- [ ] Add FormClient helpers that fetch real HTML form tokens, retain antiforgery/auth cookies, and submit form bodies. Helpers use disabled auto-redirect and HTTPS base URL for in-process requests; never replace the authentication scheme.
- [ ] Write AccountFlowTests for valid registration/sign-in and persisted password hashing, duplicate/invalid input rejection, forged Role=Teacher remaining Student-only, invalid credentials, and external/scheme-relative return URL fallback. Use UserManager<ApplicationUser> to verify the stored hash after actual HTTP registration; never derive expected results from production helpers.
- [ ] Run the account-flow filter and observe failures against missing account routes.
- [ ] Implement view models with required email/password/confirmation validation, MVC forms with tokens, and AccountController. Registration transaction covers user creation and Student role assignment; sign in only after transaction succeeds. Invalid requests return the form with validation errors and no successful cookie.
- [ ] Implement login through PasswordSignInAsync with lockoutOnFailure=true and a generic failure message. Validate ReturnUrl with Url.IsLocalUrl; otherwise redirect to Teacher or Student according to authenticated role. Do not bind an Identity entity or a role from browser input.
- [ ] Register global AutoValidateAntiforgeryTokenAttribute, authentication middleware before authorization, and cookie options/paths as in the spec. Create an AccessDenied view returned with status 403.
- [ ] Re-run all completed tests. Add and run a lockout case with five invalid attempts followed by a correct password that remains rejected during the lockout. Verify temporary-db user state through Identity.

## Task 3: Controlled Local Teacher Provisioning

**Consumes:** initialized Identity store and UserManager/RoleManager.
**Produces:** TeacherProvisioner.ProvisionAsync(string email, string password, CancellationToken cancellationToken = default) -> Task<ProvisioningResult>; ProvisioningResult includes success/existing-account/error state without secrets. ProvisionTeacherCommand.RunAsync(string[] args, IServiceProvider services, IHostEnvironment environment, string? password) -> Task<int>.

- [ ] Write provisioning tests for new Teacher, existing Teacher idempotence without password replacement, existing Student rejection, absent/weak password, invalid email, non-Development refusal, and malformed argument usage. Include a real executable process test: it must exit without starting an HTTP server. Before implementation, missing command behavior must produce an observed failing test; bound process waits and kill only the test-created process on timeout.
- [ ] Run the provisioning filter; confirm missing behavior failures.
- [ ] Implement the provisioning helper with a transaction around new user and role assignment. Existing Teachers stay unchanged; existing non-Teachers fail. Return safe errors without passwords. Use Identity validation and hashing rather than manual SQL or hashing.
- [ ] Implement command dispatch before the normal server runs. Parse exactly `--provision-teacher <email>`; require Development and PROVISION_TEACHER_PASSWORD. Invalid/unknown command arguments fail visibly rather than silently starting the app. The command initializes its configured database/roles and exits with 0 for created/already-existing Teacher, nonzero for failures. Reject non-Development and missing input before database writes.
- [ ] Add root-process test configuration with an isolated absolute database path, temporary environment variables and hidden process window. Do not inherit secret values into assertions/output. Test that failed production invocation leaves its target database absent.
- [ ] Re-run provisioning and earlier tests. Verify no silent Student promotion and no changed existing Teacher password.

## Task 4: Role Pages, Navigation and Logout

**Consumes:** account flows and provisioning from earlier tasks.
**Produces:** Teacher/Student pages protected by role; role-aware layout; POST-only logout.

- [ ] Write RoleAccessTests: anonymous Teacher/Student requests lead to login; authenticated Student gets Student page but Teacher access ends at 403; authenticated Teacher gets Teacher page but Student access ends at 403. Obtain Teacher via the real provisioner then perform real login; no test-auth shortcut.
- [ ] Write AntiforgeryTests for missing tokens on registration/login/logout and GET logout refusing state changes. Write logout test that signs in, submits a valid token, then loses protected access.
- [ ] Run these filters and observe missing protected-page/logout behavior.
- [ ] Implement TeacherController and StudentController with Authorize(Roles=...). Add minimal views without course/submission functionality. Implement authorized POST logout and role-aware navigation with a real antiforgery form.
- [ ] Follow wrong-role cookie redirects in tests to confirm the final access-denied page is actually 403; do not mistake a redirect for access granted or automatically follow redirects in assertions checking login challenges.
- [ ] Re-run the complete web suite. Check authentication cookies are HttpOnly, SameSite=Lax, and secure outside Development through an isolated non-Development test host using HTTPS requests.

## Task 5: End-to-End Verification and Documentation

**Consumes:** completed flows and tests.
**Produces:** actual verification evidence, AUTHENTICATION_SETUP.md, accurate tracker, reviewed patch.

- [ ] Run `dotnet restore HomeworkPlatform.sln`, `dotnet build HomeworkPlatform.sln --no-restore`, and `dotnet test HomeworkPlatform.sln --no-build`. Inspect every failure/warning before claiming completion.
- [ ] Run the existing full Python suite from ai-service using its .venv interpreter. Report and resolve regressions; do not assume unchanged code means tests pass.
- [ ] Perform a live local HTTP demonstration using a disposable isolated database, explicit Development environment, and an unused loopback port. Exercise Student registration/login, role denial, Teacher login after controlled provisioning, and token-protected logout. Verify cookie flows, not just rendered forms. Stop only owned processes and clean only verified temporary paths.
- [ ] Write setup docs with quoted PowerShell root paths, automatic local migration behavior, connection override, login/register routes, tests, and controlled Teacher provisioning. Populate PROVISION_TEACHER_PASSWORD from Read-Host -AsSecureString with a narrowly scoped conversion; clear/dispose the temporary password and restore the previous environment setting in finally. No password literal or password CLI argument.
- [ ] State that email confirmation/delivery and production provisioning/migration orchestration are not implemented. Show ownership work as pending, not protected merely by roles.
- [ ] Inspect tracked files and confirm no real database or secrets were added. Update progress only after all checks pass. Commit completed local work on its feature branch.
- [ ] Obtain one independent read-only review against the spec and plan; fix actionable defects with reproducing tests. Record actual results and any unresolved limitations. No merge/push without a separate integration choice.

## Execution Recommendation

Implement inline in this chat, with one independent review at completion. The account, storage and provisioning tasks share the same identity/transaction boundaries; a single implementer can keep them consistent. Veli reviews this plan and selects execution before product code or dependencies are added.

## Package References

The aligned 8.0.31 package versions were verified against their official NuGet pages during planning:
- https://www.nuget.org/packages/Microsoft.AspNetCore.Identity.EntityFrameworkCore/8.0.31
- https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.Sqlite/8.0.31
- https://www.nuget.org/packages/Microsoft.AspNetCore.Mvc.Testing/8.0.31