# ASP.NET Foundation Step 1 Design

Date: October 8 2026
Status: Written design awaiting review
Project: C:\Codes\Projects\Graduation Project

## Goal and Agreed Scope

Create the web application foundation for the first two weeks of the team roadmap. Veli is now implementing all team members' Week 1 and Week 2 tasks, step by step. This design covers only the first step: a clean ASP.NET Core MVC application that builds and runs after a clean checkout.

Veli approved this approach in chat. The existing Python service, documentation, reference samples, and repository history must be preserved.

## Approach

Use the installed .NET 8 SDK and the standard MVC template in web/. This matches the project's ASP.NET Core 8 roadmap. The alternative is to scaffold Identity, persistence, and roles immediately; defer those to the authentication step so the foundation can be verified independently.

Authentication, Teacher/Student roles, ownership enforcement, SQLite/EF Core setup, migrations, and authorization tests belong to later steps. No old ZIP code, database, uploads, or generated build output will be imported into this foundation.

## Application Structure

- web/HomeworkPlatform.Web.csproj: ASP.NET Core MVC project targeting net8.0.
- web/Program.cs: MVC registration, normal middleware, and conventional route configuration.
- web/Controllers/, Models/, Views/, wwwroot/: standard MVC template structure.
- web/appsettings.json and appsettings.Development.json: standard application configuration without secrets.
- web/Properties/launchSettings.json: documented local development launch profile.
- global.json: .NET 8 SDK selection based on installed 8.0.425, with latestPatch roll-forward and allowPrerelease false. Document the .NET 8 SDK prerequisite for other developers.
- .gitignore: ignore web bin/obj and local environment/generated files; preserve existing rules if present and do not ignore templates, source, docs, or sample fixtures.
- docs/WEB_SETUP.md: restore, build, run, and smoke-check commands.

No solution file is necessary for a single web project. Keep the standard template homepage as the first working page; styling and product features are later work.

## Runtime and Local Use

Document PowerShell commands that work from the project root, including paths with spaces. Restore and build the explicit project. For the reproducible local smoke check, run the app on http://127.0.0.1:5080 with the Development environment and no launch profile; the environment must actually be set, not assumed.

The homepage should return HTTP 200 and template HTML. Keep normal HTTPS support in the generated launch profiles, but the smoke check must not require a trusted local development certificate. Document that the explicit HTTP command is for local development.

## Error Handling and Boundaries

Keep the template's development exception behavior and production exception route. Do not expose model/service/database settings that are not implemented. If restore fails because of network or local tooling, report the concrete failure; do not claim the application works based only on generated files.

Do not change or reset user work. The folder now contains a Git repository with origin and main, so the earlier no-repository statement in CONTRIBUTING.md is outdated. Update that statement as a small documentation correction after verifying current state. Do not push or open a PR as part of this step.

## Verification and Completion

1. Confirm the installed .NET 8 SDK and inspect the current repository status before modifications.
2. Generate the MVC foundation without overwriting an existing web project.
3. Restore and build the explicit project successfully.
4. Start the app with the documented local command, request the homepage, and verify HTTP 200 plus expected HTML.
5. Stop only the app process started for this verification.
6. Verify documentation and ignored build outputs; inspect the final diff for unintended changes.

No custom unit tests are needed for an unchanged framework scaffold. Build and a real HTTP smoke check demonstrate this step's behavior. Later role/ownership behavior will require meaningful authorization tests.

Step 1 is complete when the web source is saved, restore/build succeed, the homepage responds successfully, and setup instructions reproduce the result. This does not mean Week 2 authentication is complete.

## Remaining Sequence

Step 2: verify and finish Python service setup, GET /health returning the agreed status response, dependency file, and environment configuration.

Step 3: implement Identity, hashed passwords, Teacher/Student roles, and protected actions.

Step 4: implement ownership checks using the minimum records needed for authorization demonstrations; avoid prematurely building the entire later course/assignment feature set.

Step 5: execute and document positive and negative authorization tests, and record any failures as issues.