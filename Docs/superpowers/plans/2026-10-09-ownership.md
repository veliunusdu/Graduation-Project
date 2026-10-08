# Ownership and First-Two-Weeks Completion Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans inline. Steps use checkbox syntax. Veli explicitly requested uninterrupted execution and approved choices in advance; do not request repeated approvals.

**Goal:** Finish real resource ownership and every teammate's Week 1/2 verification and evidence.
**Architecture:** Extend the existing Identity DbContext with minimal Course/Assignment/StudentCourse/Submission records. MVC controllers use authenticated owner filters and narrow form models. Integration tests use real cookies, forms and isolated SQLite.
**Tech Stack:** Existing ASP.NET Core/EF Core 8.0.31, SQLite, xUnit, Python service; no new runtime dependency.
**Spec:** Docs/superpowers/specs/2026-10-09-ownership-design.md

## Global Constraints

- Main folder C:\Codes\Projects\Graduation Project; feature/ownership, preserving all prior commits.
- String Identity owner IDs, integer resource IDs; server-derived owners; edit parents immutable.
- Teacher-scoped courses/assignments; Student-scoped submissions; foreign/missing IDs return 404.
- All unsafe actions use existing antiforgery and roles. No state-changing GET.
- Preserve Identity data with a new migration; restrict deletes that would remove student work.
- Minimal enrollment gates new submissions; no full Week 3/4/10 implementation claims.
- No model installation, AI execution, upload handling, publishing, messages, push or merge.
- Full Debug/Release web, Python, live HTTP and clean-clone verification; one final independent review.

## Review Focus

- A forged body Id/TeacherId/StudentId must not redirect a route-targeted mutation (Tasks 2/3/4).
- Invalid body input for a foreign record must still deny access before rendering secret data (Tasks 2/3/4).
- A Teacher delete cannot cascade into another person's submissions (Tasks 1/2/4).
- Duplicate submissions and enrollment manipulation must not create records under a different student/course (Task 3/4).
- New migrations must preserve Identity users and tests/demo hosts must never use the default local database (Tasks 1/5).

## File Map

Create web/Models/Domain/{Course,Assignment,StudentCourse,Submission}.cs; web/Models/Work/{CourseForm,AssignmentForm,SubmissionForm,EnrollmentForm}.cs; web/Controllers/{Courses,Assignments,StudentWork,Submissions}Controller.cs; matching Views/Courses,Assignments,StudentWork,Submissions; Data/Migrations/OwnershipResources migration.
Modify web/Data/ApplicationDbContext.cs and Teacher/Student dashboard views.
Create tests/HomeworkPlatform.Web.Tests/{OwnershipSchemaTests,TeacherOwnershipTests,StudentOwnershipTests,AuthorizationMatrixTests}.cs and Infrastructure/OwnershipFixture.cs.
Create Docs/OWNERSHIP_SETUP.md, Docs/WEEKS_1_2_COMPLETION.md and Docs/issues/closed defect reports; root README.md.
Update Docs/WEEKS_1_2_PROGRESS.md, WEB_SETUP.md, AUTHENTICATION_SETUP.md and TEAM_REVIEW.md where status is stale.

## Task 1: Migrated Ownership Resources

**Interfaces:** consumes Identity context and initializer; produces Course/Assignment/StudentCourse/Submission DbSets, string owner FKs, restricted parent deletes and unique assignment/student submission index.
- [x] Write OwnershipSchemaTests.StartupAddsOwnershipTablesWithoutRemovingIdentity, asserting four new tables and preserved existing user after initializer restart; first schema query can compile without new types.
- [x] Run schema test; expected FAIL (four tables expected, zero present).
- [x] Add models/context mapping with exact lengths, required fields, foreign keys and delete behavior from spec. Use DateTime UTC timestamps compatible with SQLite ordering.
- [x] Generate OwnershipResources migration with local dotnet-ef. Run schema and existing full suite; expected PASS, no local DB writes. Verify same-database initializer preserves user and roles.
- [x] Commit feat: add ownership resource persistence and record results.

## Task 2: Teacher-Owned Course and Assignment Flows

**Interfaces:** produces Courses GET Index/Details/Create/Edit/Delete, POST Create/Edit/Delete/Enroll; Assignments GET Index(courseId optional)/Details/Create(courseId)/Edit/Delete/Submissions/Submission, POST Create(courseId)/Edit/Delete. Returns 404 for inaccessible IDs, 409 for restricted delete; bound forms exclude owner/parent/Id fields.
- [x] Add OwnershipFixture real Teacher/Student accounts; helpers use FormClient tokens from an accessible dashboard when probing foreign paths.
- [x] Write TeacherOwnershipTests: own create/read/edit/delete, own lists, cross-Teacher read/edit/delete, forged TeacherId/Id/CourseId, foreign parent creation, missing IDs, restricted deletes and escaped fields. Verify unchanged DB for rejected mutations.
- [x] Run TeacherOwnershipTests; expected FAIL on missing routes.
- [x] Implement narrow view models, owner-filtered controllers and usable CRUD/confirmation views. Scope before validation; resolve create parent ownership; never Update a posted entity wholesale.
- [x] Add enrollment form for existing Student email scoped to an owned course, safe duplicate behavior, and Teacher-only submission read through owned assignment. Do not expose an account directory.
- [x] Run teacher/schema and existing suites; expected PASS. Commit and ledger.

## Task 3: Student-Owned Submissions

**Interfaces:** consumes enrollment and assignment/course records; produces StudentWork Index/Details(id) scoped to enrolled assignments, Submissions Index/Details/Create(assignmentId)/Edit/Delete with GET forms and POST mutations. New submissions derive StudentId; edits change Content only.
- [x] Write StudentOwnershipTests for own creation/list/detail/edit/delete, cross-Student read/edit/delete, forged StudentId/AssignmentId/Id, duplicate creation, unenrolled assignment creation and Teacher reads scoped through course. Assert DB results.
- [x] Run filter; expected FAIL on missing routes.
- [x] Implement narrow form, enrollment-filtered reads, owner-filtered writes, unique-constraint handling and HTML-encoded views. Course/assignment/student IDs cannot be changed in edit bodies.
- [x] Link Teacher/Student dashboards to usable lists. Run full suite; expected PASS. Commit and ledger.

## Task 4: Adversarial Authorization Matrix

**Interfaces:** consumes real routes and accounts; produces named regression checks and documented response/state outcomes.
- [x] Add AuthorizationMatrixTests for wrong-role and anonymous route challenges, missing tokens on unsafe resource actions, GET delete not mutating, foreign invalid body 404, route/body-ID mismatch, safe deletion with existing child data, enrollment attempt on foreign course/non-Student email, and escaped script-like submission content.
- [x] Run matrix before any discovered fixes; expected forbidden mutations are rejected and state unchanged. Any failure receives a reproducible regression and closed local bug report; fix with RED -> GREEN, never mark failed checks passed.
- [x] Run whole Debug/Release suites; expected PASS. Commit tests and fixes; record matrix results.

## Task 5: Complete First-Two-Weeks Evidence

**Interfaces:** produces setup, startup commands, roadmap mapping, complete progress tracker and reviewable local commits.
- [x] Restore/build solution, run full Debug/Release tests and Python suite; expected no failures or build warnings/errors.
- [x] Run disposable real HTTP web demo with two Teachers/two Students, cross-owner read/edit/delete attempts, unchanged-data assertions, enrollment and valid own edits; stop only owned processes.
- [x] Run real Python GET /health and environment override checks; expected HTTP200 exact status JSON, independent startup without Ollama.
- [x] Clone the locally committed branch into owned scratch, build/test/start web and create fresh Python venv/install/start/test there; expected clean-clone setup succeeds with no preexisting DB/.env/venv required. Cleanup only verified paths.
- [x] Save OWNERSHIP_SETUP.md and WEEKS_1_2_COMPLETION.md mapping each PDF page2 task to evidence, with exact tested statuses, bugs and boundaries. Save closed issue for previously discovered Debug-only provisioning helper and any actual new defects; no invented online issues.
- [x] Update tracker and stale setup/team-record statements without inventing teammate/supervisor approvals. Save concise root quickstart README.
- [ ] Commit and obtain one independent read-only final review. Fix Critical/Important with failing regression and full green suite; record deferred Minor/rulings if any. Keep local branch and give self-contained completion answer without permission menu.
