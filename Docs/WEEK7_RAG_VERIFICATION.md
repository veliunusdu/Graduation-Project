# Week 7 - ASP.NET RAG integration

## Implemented scope (Ayse)

- `StudentCourses/SearchSources/{id}` accepts a question for an active course in which the signed-in student is enrolled.
- The web backend sends `question`, the authorized route's `course_id`, and `top_k` to Python `POST /rag/retrieve`.
- Retrieval runs only when the course has a Ready material. Pending, Processing and Failed files remain downloadable as course materials but are not accepted as RAG sources.
- The response course, chunk identifiers, page numbers, similarity values and material ownership are checked. Only Ready materials belonging to the authorized course can appear.
- Duplicate chunks are removed, the relevance threshold is applied, and up to three sources are displayed in descending similarity order.
- Source cards show the original filename, page and text, plus an authorized material download link. The student's question stays visible and the form supports another question.
- The AI Tutor path receives the validated sources. Its explanation and validated source citations are shown together. Conversation persistence is not part of Week 7.
- Enrollment is checked again after the external request. Missing materials, no matches, service failures and invalid responses have separate controlled outcomes.
- Production `AIService.TimeoutSeconds` remains **10**. No migration or account/data reset is required.

## Verification completed in this environment

- Web application suite: **237 passing tests**. Existing real-model tests were skipped because the local services were unavailable.
- Dedicated RAG/security cases cover request serialization, route/body course mismatch, foreign and inactive courses, enrollment removal, invalid questions, filtering of foreign/failed/low-score/duplicate sources, encoded source text, malformed responses, connection errors, timeout, Tutor citations and antiforgery.
- Python `tests/test_rag_retrieval.py`: **11 passing tests**, covering persistence, idempotency, course filtering, metadata, API input validation and index identity.
- Browser checks used the real ASP.NET app and real FastAPI extraction/chunking/SQLite retrieval code, with **synthetic test embeddings**. Course isolation, download links, empty results, missing Ready materials, back navigation, and desktop/mobile layouts passed.
- **Real BGE-M3/Ollama inference has not been verified on this computer.** The live check below is provided for the machine where Ollama is installed. It does not report success when the service/model is unavailable.

## Quick manual demo

1. Start Ollama and the project's Python service on the configured addresses.
2. Sign in as a teacher, open a course and upload a short material about primary keys. Wait for **Ready**. Use **Retry processing** for previously uploaded Pending/Failed materials.
3. Sign in as a student enrolled in that course. Open **Courses -> course -> Need help?**.
4. Ask `What is a primary key?` and select **Search sources**.
5. Verify that the filename, page and relevant passage are shown and the material downloads successfully.
6. Try an unrelated question. The application may show no matching sources if scores are below the relevance threshold.
7. Use **Ask AI Tutor** to verify the separate answer/citation flow when the configured Tutor model is also installed.

## Real Python/BGE verification on the model host

Prerequisites: .NET SDK, the project's Python dependencies and test dependencies, running Ollama, and the configured embedding model (`bge-m3` by default). Run from the repository root using the project's Python virtual environment:

```powershell
& '.\ai-service\.venv\Scripts\python.exe' -m pip install -r '.\ai-service\requirements-dev.txt'
& '.\ai-service\.venv\Scripts\python.exe' '.\scripts\verify_week7_rag_live.py' --output-dir '.\artifacts\week7-rag-live-1'
```

Choose a new output directory on each run. The script checks the installed model, warms it up, starts an isolated Python service on a free loopback port, and runs `Week7LiveRagTests`. It uses temporary web/vector databases and does not change normal course/account records or the application's 10-second timeout. It does not install or download Ollama models.

Outputs include `acceptance.json`, `week7-live.trx`, `dotnet.log` and `python.log`:

- Exit **0** and `acceptance.json` status `passed`: real upload/embedding/retrieval and course isolation were verified.
- Exit **2**, status `not_run`: Ollama/model prerequisites were unavailable.
- Exit **1**, status `failed`: inspect the logs and test report.

Optional arguments: `--ollama-url http://127.0.0.1:11434` and `--embedding-model bge-m3`.

## Repeat the local checks

```powershell
dotnet test '.\tests\HomeworkPlatform.Web.Tests' --filter 'FullyQualifiedName~Week7RagTests'
Push-Location '.\ai-service'
try { & '.\.venv\Scripts\python.exe' -m pytest tests/test_rag_retrieval.py -q } finally { Pop-Location }
```

The live test is opt-in and is skipped during an ordinary test run. A healthy `/health` response alone does not prove that BGE-M3 inference or retrieval works.
