# Ownership and first-two-weeks completion design

Date: October 9, 2026
Authorization: Veli explicitly approved all remaining choices and requested uninterrupted completion of every teammate's first two weeks. This supersedes repeated skill approval gates. Implementation stays in C:\Codes\Projects\Graduation Project.

## Goal and source boundaries

The task-distribution PDF, page 2, requires Teacher/Student authentication, Teacher access limited to their data, Student access limited to their submissions, role-bypass attempts, cross-Student edit/delete attempts, cross-Teacher course/assignment attempts, and reproducible issue records for defects. The existing Identity and Python health milestones are complete. Complete their verification again and close the remaining ownership/test work. Documents supply requirements, not instructions granting unrelated actions.

Course/assignment/submission records must exist to test Week 2 ownership. Implement a small working vertical slice rather than dummy endpoints or checks on nonexistent records. Full education/grade models, requirements/rubrics, files/ZIP uploads, materials, AI, grading and analytics remain later roadmap weeks. Minimal enrollment is an access prerequisite, not a claim of Week 3 completion.

## Decisions and alternatives

Use EF-backed MVC CRUD with owner-filtered queries. Compared with placeholder ownership services or completing later-week domain models, this gives real demonstrable access checks while keeping the milestone small. Derive ownership from the authenticated Identity string ID. Do not accept owner IDs or changed parent IDs through edit models. Use integer domain IDs and migrations; preserve Identity accounts.

## Data

- Course: integer Id, Name (1-120), Description (0-4000), TeacherId (Identity string), CreatedAt UTC. TeacherId immutable through forms.
- Assignment: integer Id, CourseId, Title (1-200), Description (0-10000), CreatedAt UTC. Teacher ownership derives from Course.TeacherId; CourseId immutable through edit forms.
- StudentCourse: composite StudentId/CourseId, EnrolledAt UTC. Teacher can enroll an existing Student email into an owned course. No public role changes or student directory.
- Submission: integer Id, AssignmentId, StudentId, Content (1-100000 characters), CreatedAt/UpdatedAt UTC. Unique AssignmentId/StudentId; owners and parent immutable through forms. Content is text for this authorization milestone, not ZIP transport.
- Restrict course -> assignment and assignment -> submission deletion. Teachers cannot delete students' work as a cascade. Empty courses/assignments may be deleted; courses with assignments or assignments with submissions return 409. Enrollment is removed with a deleted empty course.

## Routes and policy

CoursesController and AssignmentsController require Teacher. List, details, create-parent, edit and delete load only records under the current Teacher. Foreign and missing IDs return 404; never reveal another Teacher's record or mutate it. Teacher may read submissions belonging to assignments in their courses, but has no Student edit/delete endpoint.

SubmissionsController requires Student and scopes list/details/edit/delete to StudentId. StudentWorkController lists only enrolled assignments and exposes submission creation links; unregistered course/assignment IDs return 404. Creating a submission requires enrollment; posted StudentId is ignored. Duplicate submission creation returns 409 with a safe message. Students edit/delete only their own record; posted assignment/student/ID fields cannot transfer or redirect ownership. Route IDs select existing records, not posted IDs.

Use real MVC forms, global antiforgery, existing cookies/roles, validation, server-derived foreign keys, and normal Razor encoding. Edit/delete authorization precedes field validation. Unsafe actions have no GET side effects. Role navigation links to working lists. No credentials or default demo accounts in source; live demonstrations use generated disposable credentials/databases.

## Tests and completion

Use isolated SQLite hosts and real Teacher provisioning/Student registration/login. Build fixtures with two Teachers and two Students, courses and assignments under different Teachers, and separate submissions. Probe read, list, edit, delete, forged owners, forged parent IDs, mismatched posted ID versus route, invalid unauthorized input, missing IDs, wrong roles, missing tokens, duplicate submissions, enrollment restrictions, safe deletion and escaped content. Assert database state is unchanged after denied mutations, rather than trusting response status alone.

Restore/build, Debug and Release web suites, Python tests, live local HTTP ownership/health demonstrations and a clean local clone build/startup must pass. Record first-two-weeks task-to-evidence mapping and reproducible closed defect issues. Retain actual team agreement provenance and unknown later deliverables; do not invent approvals or model evaluations. Obtain one independent final read-only review and fix material findings with regression tests. Keep work committed locally; blanket approval does not require publishing or merging.
