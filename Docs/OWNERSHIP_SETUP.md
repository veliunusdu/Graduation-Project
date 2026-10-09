# Ownership setup

This is the smallest working resource slice needed to demonstrate Week 2 authorization. Authentication setup and the safe local Teacher password prompt are in [AUTHENTICATION_SETUP.md](AUTHENTICATION_SETUP.md).

## Start and demonstrate

```powershell
Set-Location 'C:\Codes\Projects\Graduation Project'
dotnet restore HomeworkPlatform.sln
dotnet build HomeworkPlatform.sln --no-restore
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:DOTNET_ENVIRONMENT = 'Development'
dotnet run --project web/HomeworkPlatform.Web.csproj --no-build --no-launch-profile --urls http://localhost:5080
```

Open http://localhost:5080. Startup applies the Identity and OwnershipResources migrations and preserves existing accounts. No example accounts or passwords are seeded.

1. Register two Students through `/Account/Register`.
2. Provision two Teachers using the hidden-password command in the authentication guide. Use distinct emails and keep passwords out of arguments/source.
3. Sign in as the first Teacher, open My courses, and create a course. Its owner is the signed-in Teacher.
4. Open the course, enroll the Students by their existing emails, and create an assignment.
5. In separate browser profiles, sign in as each Student, open Enrolled assignments, and create a text submission. Each Student can edit/delete their own submission.
6. Sign in as the other Teacher, create a separate course/assignment, and try the first Teacher's URLs. They return 404. Students trying another Student's submission URLs also get 404. Valid role navigation does not replace these server checks.
7. The owning Teacher can read their assignment's submissions for review. Teacher pages do not provide Student edit/delete actions.

Text content is used for this authorization milestone. ZIP uploads, AI review, final feedback/grades and full assignment requirements are later work.

## Routes and outcomes

| Route | Access |
| --- | --- |
| `/Courses` and `/Courses/Details/{id}` | Teacher's own courses only |
| `/Courses/Create`, `/Courses/Edit/{id}`, `/Courses/Delete/{id}` | Teacher-only forms; POST creates/updates/deletes owned records |
| `/Courses/Enroll/{id}` POST | Owned course; existing Student email; duplicate enrollment is safe |
| `/Assignments` or `?courseId={id}` | Teacher's own assignments/courses |
| `/Assignments/Create?courseId={id}`, `/Assignments/Edit/{id}`, `/Assignments/Delete/{id}` | Teacher-only, owned parent/record |
| `/Assignments/Submissions/{assignmentId}` and `/Assignments/Submission/{submissionId}` | Read-only Teacher review under their own course |
| `/StudentWork` and `/StudentWork/Details/{assignmentId}` | Assignments in the signed-in Student's enrolled courses |
| `/Submissions` and `/Submissions/Details/{id}` | Student's own submissions only |
| `/Submissions/Create?assignmentId={id}` | Student enrolled in the assignment's course |
| `/Submissions/Edit/{id}`, `/Submissions/Delete/{id}` | Student's own record; edits change content only |

Anonymous protected requests redirect to Login. Authenticated wrong roles redirect through AccessDenied and end at 403. Missing/foreign records return 404. Missing antiforgery tokens return 400. Duplicate submissions return 409; edit the existing submission instead. A course containing assignments or an assignment containing student work cannot be deleted (409). GET Delete shows a confirmation and never deletes.

## Data and security choices

Owners come from the authenticated Identity user ID. Dedicated form models exclude owner IDs, entity IDs and edit parent IDs. Existing-record action IDs bind from the route; create parent IDs bind from the query, then are independently authorized. Posted Id/TeacherId/StudentId/CourseId/AssignmentId fields cannot transfer ownership or substitute an edit/delete target.

All unsafe MVC actions use the existing global antiforgery filter and Identity cookies. Authorization queries run before form validation. Teacher ownership follows Assignment -> Course.TeacherId; Student ownership follows Submission.StudentId. One submission per assignment/student is enforced by a database unique index. Restrictive foreign keys protect student work from parent deletion, in addition to controller checks. Views encode user content normally, including code containing script-like text.

The models intentionally contain only fields needed for ownership. Education/grade, full enrollment administration, rubric/requirements, difficulty/due dates, file processing and AI are later roadmap milestones. Minimal enrollment provides an actual permission boundary for submission creation; it does not complete Week 3.

## Run checks

```powershell
dotnet test HomeworkPlatform.sln
dotnet test HomeworkPlatform.sln -c Release
```

Tests use two Teachers/two Students created through real Identity flows, isolated databases, valid form tokens and real cookies. Denied mutations assert stored data and owner relationships remain unchanged. [WEEKS_1_2_COMPLETION.md](WEEKS_1_2_COMPLETION.md) records results and clean-clone/live HTTP verification.
