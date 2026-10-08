# bug: provisioning tests launch a fixed Debug build

ID: TEST-001
Status: Closed — fixed and regression verified
Original owner: Veli (verification), web foundation maintained by Ayşe
Found: October 9, 2026, independent authentication milestone review
Affected file: tests/HomeworkPlatform.Web.Tests/Infrastructure/ProvisioningProcess.cs
Fix commit: a3df33e

## Reproduce

1. Restore the solution and use a checkout with no `web/bin/Debug` output, or temporarily move that directory to a verified local backup.
2. Run `dotnet test HomeworkPlatform.sln -c Release --filter FullyQualifiedName~CommandCreatesTeacherExitsAndIsIdempotentWithoutChangingPassword`.
3. The helper before the fix launches `web/bin/Debug/net8.0/HomeworkPlatform.Web.dll` even though the tests are Release. With no Debug executable, the command exits 1 and the success assertion fails (expected 0). With old Debug output, it can silently verify stale code.
4. Restore any directory set aside for reproduction; never delete user data for this check.

Expected: executable provisioning tests use the web assembly built for the running test configuration, exit 0 for valid Teacher creation, and use the isolated test database.
Actual before fix: missing or stale Debug executable is selected.

## Resolution and evidence

The helper now selects the referenced HomeController assembly's Location. MVC Testing copies its runtime configuration and dependencies beside that assembly. The existing process regression failed before the fix, then passed with Debug output set aside: all 27 authentication tests passed in Release, and all 27 passed in Debug. The expanded first-two-weeks suite subsequently passed in both configurations and a clean local clone.

This is a reproducible local issue record, not an online GitHub issue. No open runtime authorization leak was found by the completed matrix; missing routes during test-first implementation were expected feature failures, not misreported vulnerabilities.
