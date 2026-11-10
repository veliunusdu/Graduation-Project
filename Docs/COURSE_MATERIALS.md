# Course materials

Entry point: a teacher's Course Details -> Ders materyalleri. Only the owner of
an active course can upload PDF/DOCX/PPTX/UTF-8 TXT (default 20 MiB). Files use
random storage names outside wwwroot. Enrolled students in active courses can
list/download Ready materials. Failed records and details remain teacher-only.
All POST actions use the global antiforgery filter. Student upload is forbidden.

Upload persists a CourseMaterial, atomically claims Processing, then calls real
Python POST /documents/process using multipart file/material_id/course_id/
education_level/grade_level. The request waits for completion. The list can be
opened in a second tab and refreshed. Validated identity/metadata marks Ready;
transport/parser/response errors mark Failed with a safe message. Explicit retry
is permitted for Failed/Uploaded or a Processing lease older than five minutes;
a conditional update prevents normal overlapping retries. A stale completion
cannot overwrite a later lease. A crash after the Python commit may leave
Processing: reprocessing the same material relies on Python idempotency.

## Configuration and migration

The existing AIService BaseUrl must point to Python. AIService TimeoutSeconds is
shared with other calls (default 10; range 1..120); use 120 for real document
processing if necessary. No models or model defaults changed. Ollama and BGE-M3
must be available to Python.

CourseMaterials:RootPath defaults to App_Data/CourseMaterials; MaxFileSizeMB
inherits the 1..20 limit. Keep it outside wwwroot and separate from submissions.
The generated CourseMaterials migration creates one new table and index without
changing existing student/assignment rows. Normal web startup runs pending EF
migrations, as in the existing application. Back up the deployment DB before
updating. Development and tests here used only disposable databases; no user's
existing database was migrated.

No delete/replacement interface is provided in this first version. Downloads
are attachments. Indexing credentials and raw model errors are not shown in UI.
Course deletion is restricted by the material FK. Education/grade edits are blocked while a course has material records, to
prevent stale index metadata; other course edits remain available. The index stores the Ollama model name and digest. A changed digest or an older
populated database without provenance returns HTTP 409 for processing/retrieval.
Keep the old database as a backup, set VECTOR_DB_PATH to a new file and index
the original materials again before enabling retrieval. Change model versions
while the service is idle so in-flight embeddings cannot span a tag update.
PPTX extraction reads table cells on the correct slide.

## Verification

Tests use real Identity, antiforgery, SQLite and private per-test file roots.
Controlled processor/HTTP handlers test rejection and response identity. Ordinary
regression skips live Ollama and live material facts. For the live material check:

    python scripts/verify_material_live.py --output-dir <new-evidence-directory>

Use the service Python environment with uvicorn/requests installed. Use `--dependencies <directory>` if parser packages are installed in a separate
temporary target directory.
The harness starts only an owned loopback Python server, uses a temporary vector
DB, opts into ActualWebUploadReachesRealPythonAndBge, and stops that server.
It does not download models or stop the user's Ollama daemon.

Verified 11 November 2026: 161 Debug and 161 Release tests passed, with two
opt-in live facts skipped per ordinary run. The real MVC -> Python -> BGE-M3
upload fact passed in its dedicated run on 10 November. The isolated test DB
applied the migration. The EF model snapshot check found no pending changes.
