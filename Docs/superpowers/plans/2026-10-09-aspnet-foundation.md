# ASP.NET Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans for inline execution, or superpowers:subagent-driven-development if Veli chooses delegation. Steps use checkbox syntax for tracking.

**Goal:** Create a clean ASP.NET Core 8 MVC application in web/ that builds and serves its homepage.

**Architecture:** Use the standard MVC template with conventional routing and its default views and middleware. Pin the .NET 8 SDK at the project root and document explicit-project build/run commands. Keep the existing Python service and other project files intact.

**Tech Stack:** .NET SDK 8.0.425; ASP.NET Core MVC targeting net8.0; PowerShell; Git.

**Spec:** docs/superpowers/specs/2026-10-08-aspnet-foundation-design.md
**Project root:** C:\Codes\Projects\Graduation Project

## Global Constraints

- Use the installed .NET 8 SDK and the standard MVC template in web/.
- The existing Python service, documentation, reference samples, and repository history must be preserved.
- No solution file is necessary for a single web project.
- No old ZIP code, database, uploads, or generated build output will be imported.
- Authentication, roles, persistence, ownership checks, and authorization tests are later steps.
- The local smoke check uses http://127.0.0.1:5080, Development, and no launch profile.
- Do not push or open a PR as part of this step.
- No custom unit tests are needed for an unchanged framework scaffold; restore/build and a real HTTP check are required.

## Review Focus

- An existing web/ directory must not be overwritten (Task 1).
- Another installed SDK must not silently change the scaffold framework (Tasks 1 and 2).
- Paths containing spaces must work in documented commands (Task 4).
- Port 5080 may be occupied; detect it and do not stop the unrelated process (Task 3).
- Generated artifacts must be ignored without hiding fixtures or shared source (Task 4).

## File Map

Create global.json and .gitignore if missing, preserving any existing content when it appears before execution. Generate web/HomeworkPlatform.Web.csproj plus the standard MVC template files. Create docs/WEB_SETUP.md. Correct the stale repository-status statement in CONTRIBUTING.md. Update the design status to approved, based on Veli's October 9 confirmation.

No product files are written until Veli reviews this plan and chooses execution. The working folder currently has no web/, global.json, or .gitignore. Git status must be inspected with working directory and permissions set correctly; a Git error is not evidence of a clean tree.

## Task 1: Establish Safe Working State and SDK Selection

**Files:** global.json; design status update.
**Consumes:** approved spec, current repository/workspace.
**Produces:** verified .NET 8 SDK selection and a workspace safe for scaffolding.

- [x] Read applicable instructions and inspect `git status --short` and current branch using `git -C <project root>`. Preserve any user changes. Apply using-git-worktrees at execution time when isolation is needed; any worktree must belong to this actual repository, not the older OneDrive checkout. Honor Veli's requested main output location when presenting the result.
- [x] Confirm web/ is absent or empty before generation. If it contains user code, stop scaffolding and inspect it rather than using --force.
- [x] Write global.json with sdk.version 8.0.425, rollForward latestPatch, allowPrerelease false.
- [x] Run `dotnet --version` from the implementation root. Expected: 8.0.425 or a later patch in its permitted feature band, never 10.x.
- [x] Change the written design status to Approved by Veli on October 9 2026. Do not expand its scope.

## Task 2: Generate and Build the MVC Application

**Files:** web/ standard MVC files; .gitignore initial rules.
**Consumes:** pinned SDK from Task 1.
**Produces:** buildable web/HomeworkPlatform.Web.csproj targeting net8.0.

- [x] Run `dotnet new mvc --name HomeworkPlatform.Web --output web --framework net8.0 --no-restore`. Expected: template generation succeeds without overwriting files.
- [x] Add ignore patterns for **/bin/, **/obj/, .vs/, **/.venv/, **/__pycache__/, *.pyc, and .env. Keep .env.example trackable. Do not ignore docs, samples, source, or .github templates.
- [x] Inspect csproj TargetFramework and normal MVC registration/routes. Keep the template page and middleware; do not add speculative database or AI integration.
- [x] Run `dotnet restore ".\web\HomeworkPlatform.Web.csproj"`. Expected: successful restore; report actual network/tool failures.
- [x] Run `dotnet build ".\web\HomeworkPlatform.Web.csproj" --no-restore`. Expected: successful build. Inspect warnings; do not invent a zero-warning result.

## Task 3: Verify a Live Homepage

**Files:** no permanent source changes unless a real scaffold problem requires repair.
**Consumes:** built project.
**Produces:** observed HTTP 200 and correct homepage HTML.

- [x] Check whether 127.0.0.1:5080 is already listening. If occupied, report the conflict or choose an unused temporary verification port and disclose it; do not stop the listener.
- [x] Start the generated app with Development and no launch profile. Use either a tracked terminal process or a background Start-Process with -WindowStyle Hidden, retaining its process ID and task-specific log paths.
- [x] Equivalent interactive command, from the project root:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run --project '.\web\HomeworkPlatform.Web.csproj' --no-build --no-launch-profile --urls 'http://127.0.0.1:5080'
```

- [x] Wait for readiness with a bounded retry loop. Request the homepage using Invoke-WebRequest. Expected: HTTP 200, HTML containing the template Welcome heading and the app name in the document title. A listening log alone does not pass the check.
- [x] Stop only the app process started for this check and confirm that it exits. Restore any environment variable changed in the agent's persistent session if applicable.

## Task 4: Document and Check the Final Result

**Files:** docs/WEB_SETUP.md, CONTRIBUTING.md; final .gitignore review.
**Consumes:** actual successful commands and verification output.
**Produces:** reproducible setup documentation and a reviewed patch.

- [x] Write WEB_SETUP.md with .NET 8 SDK prerequisite, a quoted Set-Location command for the root path, explicit restore/build/run commands, local URL, Development/no-launch-profile explanation, and Ctrl+C shutdown instructions. Describe the framework version and the current foundation-only scope.
- [x] Document HTTPS launch-profile support without requiring a trusted certificate for the explicit local HTTP smoke command. Do not install or trust certificates automatically.
- [x] Correct CONTRIBUTING.md's outdated assertion that no Git repository exists, using observed repository state. Do not change team ownership or agreed conventions.
- [x] Run `git check-ignore` on actual web bin/obj paths and ensure source/fixture files remain visible to Git. Inspect git diff and status for unexpected modifications.
- [x] Confirm documented paths resolve and rerun a targeted command only if the final edit changed behavior or a relevant unresolved concern remains. No need to repeat a successful build for prose-only edits.
- [x] Report the application path, build and HTTP evidence, and any remaining limitations. Step 2 is Python-service verification; do not claim all Week 1/2 tasks are finished.

## Execution Recommendation

Implement inline in this chat: this is a small framework scaffold with one SDK/configuration boundary and one live smoke check. Veli can choose subagents instead, but delegation adds coordination for little independent work here. Before implementation, Veli reviews this plan and selects the execution method.
## Execution record

Implemented inline on feature/aspnet-foundation on October 9 2026. Restore/build passed and a real HTTP smoke check returned 200. See WEB_SETUP.md for reproducible commands. Work remained in Veli's requested main folder, with branch isolation rather than a separate checkout.
