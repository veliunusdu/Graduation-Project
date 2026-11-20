# EduPilot university update

This update replaces account registration with development-only preloaded university accounts, adds matching teacher and student workspaces, and includes the accumulated course, assignment, attachment and review fixes. Existing database migrations preserve accounts and enrollments. New migrations add account profiles, seed history, education levels and assignment attachments.

Teacher material uploads automatically start AI processing. Enrolled students can still download course materials while processing is pending or failed. Source search and Tutor only use ready materials from the enrolled course, with validated citations and protected downloads. The web AI timeout remains 10 seconds.

## Setup and data

- Open `HomeworkPlatform.sln` and run the web project in Development. See `KURULUM_VE_DEGISIKLIKLER.md` for demo accounts and initialization behavior.
- Local databases, uploaded submissions and teacher attachments are intentionally excluded from Git. Existing local data stays in the original project folder.
- Python/Ollama configuration remains unchanged; the Python production implementation is unchanged in this update.

## Validation on 2026-11-14

- `dotnet test HomeworkPlatform.sln --nologo --verbosity minimal`: 237 passed, 3 skipped, 0 failed.
- `node --test scripts/test_edupilot_auth.cjs`: 2 passed.
- `git diff --cached --check`: passed.
- No database, uploaded-file, build-output or local editor paths are included in this change.
- Real Python/BGE/Ollama tests were skipped because those services are on a teammate's computer. This is not evidence of a real-model acceptance pass. See `Docs/WEEK7_RAG_VERIFICATION.md` for the opt-in verification procedure.
- Existing xUnit2013 collection assertion warning remains in `ReviewedSubmissionActionsTests`.
