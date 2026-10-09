# Weeks 1 and 2 completion report

Date: October 9, 2026
Status: Complete — implemented, verified and independently reviewed
Main location: C:\Codes\Projects\Graduation Project
Working branch: feature/ownership (local commits)

Veli requested all teammates' first two weeks and explicitly authorized uninterrupted design, implementation and verification. The checklist below comes from AI_Odev_Platformu_Gorev_Dagilimi.pdf, page 2, cross-checked against the roadmap's 12-week schedule. It does not claim later-week AI/domain work.

## Roadmap task-to-evidence mapping

| Week / original owner | Required work | Completed evidence |
| --- | --- | --- |
| 1 / Ayşe | Web runs locally; build/runtime errors cleaned; coherent web layout; clean clone builds and runs | ASP.NET Core 8 MVC in web; SDK pinned; solution build 0 warnings/errors; independent review; clean local clone build/test/live HTTP passed; WEB_SETUP.md |
| 1 / Ayşe | Resolve conflicting/broken migrations while checking data loss | Scratch implementation uses checked-in InitialIdentity and OwnershipResources migrations; actual Identity-only database upgrade preserves existing user/password/role; no database reset/import of old ZIP |
| 1 / Ayşegül | ai-service and api/agents/rag/llm/graphs/schemas/prompts/config folders | Required tracked folders exist; empty future modules have .gitkeep; local virtual environment excluded from Git |
| 1 / Ayşegül | Dependency file, virtual environment, independent one-command startup | Pinned runtime/dev requirements; fresh clean-clone venv install and pip check passed; live uvicorn startup from repository root passed; PYTHON_SERVICE_SETUP.md |
| 1 / Veli | Scope sections and shared understanding | PROJECT_SCOPE.md contains goal/users/education/core/AI/exclusions/stack; team agreement reported by Veli October 6; TEAM_REVIEW.md preserves provenance |
| 1 / Veli | Branch/PR naming and issue template | CONTRIBUTING.md, reusable .github Task/Bug/PR templates; actual feature branches/local commits |
| 2 / Ayşe | Real authentication, roles, hashed passwords and protected actions | ASP.NET Core Identity with SQLite; Student-only registration; controlled local Teacher provisioning; login/logout, lockout, safe return URLs, role filters and antiforgery; AUTHENTICATION_SETUP.md |
| 2 / Ayşe | Teacher only own data; Student only own submissions | Working Teacher-owned course/assignment CRUD and Student-owned submission CRUD, scoped read-only Teacher review; server-derived ownership and immutable parents; OWNERSHIP_SETUP.md |
| 2 / Ayşegül | GET /health -> 200 exact status JSON | Test and real HTTP returned {"status":"ok"} in main checkout and clean clone |
| 2 / Ayşegül | Environment/config, including Ollama address | .env.example and validated settings; env override verified through OpenAPI title; service worked with unreachable Ollama URL and no model running; eight tests |
| 2 / Veli | Student attempts Teacher pages | Wrong-role GET and valid-token POST checks end at 403; real HTTP both directions verified |
| 2 / Veli | Student edits/deletes another Student's submission | 404; no changed/deleted row, owner or parent; positive own edits/deletes also pass |
| 2 / Veli | Teacher accesses another Teacher's course/assignment | Details/edit/delete/list/parent-create/enrollment/review scoped; foreign records return 404; database unchanged after denied mutations |
| 2 / Veli | Reproduction issues for defects and recorded outcomes | Closed TEST-001 local issue records the actual Debug-only process-test defect and RED -> GREEN fix; matrix results below; final review record appended on completion |

## Verification results

- Main solution restore/build: pass, 0 warnings and 0 errors.
- Full web suite: **88 passed, 0 failed in Debug; 88 passed, 0 failed in Release**.
- Python suite: **8 passed, 0 failed**; pip check reports no broken requirements.
- Clean local clone at f046239: build clean, web 88/88, fresh independent Python environment/install/test 8/8, pip check clean.
- Main and clean-clone live servers: actual Teacher provisioning/login, two Student registrations, owned course/assignment/submission creation, enrollment, valid own edits/deletes, scoped Teacher review, role denial and logout all passed.
- Live Python service in both checkouts: health HTTP200 with exact status JSON and configuration override, no Ollama process needed.
- Only owned processes were stopped. Disposable test/demo databases were isolated from default homework.db; the default database was unchanged. No real credentials, local databases, .env or virtual environments were committed.

## Authorization matrix

| Scenario | Expected / actual | Regression location |
| --- | --- | --- |
| Anonymous protected resources | Login redirect; pass | AuthorizationMatrixTests.AnonymousResourceRequestsLeadToLogin |
| Wrong-role resource GET and POST | Redirect to AccessDenied, final 403; pass; unchanged data | WrongRoleCannotReadResourceLists / WrongRoleCannotMutateResourcesEvenWithValidToken |
| Cross-Teacher read/edit/delete | 404; pass; foreign record unchanged | TeacherOwnershipTests.ForeignTeacherRecordIsNotReadable / ForeignTeacherMutationDoesNotChangeData |
| Cross-Student read/edit/delete | 404; pass; foreign content/owner/parent preserved | StudentOwnershipTests.OtherStudentSubmissionIsNotReadable / OtherStudentEditOrDeleteLeavesSubmissionUnchanged |
| Forged body owner/parent/Id | Ignored; route target and ownership preserved; pass | OwnCrudIgnoresForgedOwnerParentAndBodyId / OwnSubmissionCrudIgnoresForgedStudentParentAndBodyId |
| Invalid foreign body or missing ID | 404; pass; no secret form/data returned or mutations | ForeignInvalidBodyAndForgedRouteTargetRemainDenied |
| Missing antiforgery tokens | 400; pass; no mutation | ResourcePostWithoutTokenIsRejectedWithoutMutation |
| GET Delete | Confirmation only; pass; records still exist | GetDeleteOnlyShowsConfirmation |
| Unenrolled assignment / duplicate submission | 404 / 409; pass; original submission preserved | ListsAndCreationRequireEnrollmentAndDuplicateDoesNotOverwrite |
| Foreign enrollment / non-Student email / duplicate enrollment | 404 / validation / idempotent success; pass | EnrollmentCannotTargetForeignCourseOrNonStudentAndDuplicateIsSafe |
| Parent delete with student work | 409; pass; submissions remain | TeacherCannotDeleteParentsContainingStudentWork |
| Script-like student text | Encoded on Student and Teacher views; pass | SubmissionContentIsEncodedForBothStudentAndTeacher |
| Invalid owned inputs | Validation response; pass; no record changed/created | InvalidResourceInputDoesNotCreateOrChangeRecords |
| Identity-only DB upgrade | User can still login; password/role retained; migration adds resources | OwnershipSchemaTests.UpgradeFromIdentityOnlyPreservesAccountAndRole |

The 37 new matrix cases supplement 14 Teacher ownership cases, eight Student ownership cases, two schema cases and the 27 existing authentication cases. Tests use actual Identity accounts, tokens/cookies and isolated SQLite, not a fake authentication scheme. First schema and CRUD tests failed before implementation and passed afterward.

## Decisions and remaining roadmap work

The written design/plan live under Docs/superpowers. Veli's blanket authorization replaces repeated permission gates; implementation stays in the chosen folder on a feature branch with native Windows bookkeeping. Minimum resource/enrollment models are necessary to demonstrate Week 2 access against real records. Full Week 3 education/grade/enrollment administration, Week 4 requirements/rubrics, uploads, AI integration/Ollama, RAG, agents, grading and analytics remain later work. This report does not imply production deployment readiness or completion of the full graduation project.

Email verification/delivery and production Teacher/migration administration are still future work. Exact October 13 submission requirements, supervisor-specific requirements, shared analysis contract acceptance, and model choice/benchmarks remain recorded as unknown/proposed rather than invented. These do not block the defined first-two-weeks checklist. No publishing, push, merge, external messages or online issue creation was performed.

## Final independent review

One read-only review of a3df33e..0afad22936f2260dc889b300494a110b9f750894 found no Critical or Important defects. It confirmed all five tasks, route/query binding, ownership checks before validation, role/antiforgery boundaries, restrictive foreign keys, unique submissions, migration preservation and isolated verification data.

The reviewer noted two Minor items. The contradictory historical sentence saying ownership was still pending was corrected as part of final documentation closure; human prose does not need a test mirroring its text. One optional UI improvement is deferred: Teacher submission list/detail currently identify work by submission ID and timestamp, without showing the Student's email/name. This does not affect access isolation; add a narrow Teacher-scoped review projection when extending the later Teacher review UI.

The review set aside full education/enrollment administration, requirements/rubrics/deadlines/grading, uploads/AI/integration, production provisioning/deployment/migration orchestration, email verification, and independent teammate/supervisor acceptance. These are later roadmap or external acceptance matters, not missing first-two-weeks implementation. Reported team agreement and unknown deliverables remain accurately recorded.

## Execution rulings

- Blanket approval superseded repeated skill review/permission/finish prompts. Design and plan were still written/self-reviewed. Cost if a choice needs changing: Veli reviews a concrete implementation and can request adjustments.
- Work stayed in the explicitly requested main folder on feature/ownership, with native Windows bookkeeping. Cost: a normal checkout shares its active branch rather than a second isolated checkout; local commits preserve the work.
- Minimal course/assignment/enrollment/submission models were added to test real Week 2 ownership. Cost: later roadmap migrations will extend those models; this does not substitute for their full future behavior.
- Enrollment writes and individual Teacher submission reads were tested/implemented with the Student flow, where their permission outcome can be observed. Cost: a small internal task-boundary change; final interfaces and verification remain unchanged.
- Later roadmap features/production/external acceptance were excluded according to the defined first-two-weeks scope. Cost: this is a local milestone, not the completed/deployed graduation project.

The exact documented `dotnet run` provisioning and `--urls` startup commands were also exercised successfully with isolated storage and a generated transient password. Prior environment settings were restored and the owned process tree stopped. All implementation is saved in local commits; no permission request remains outstanding.
