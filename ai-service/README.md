# AI service

Independent FastAPI service for the homework platform. Health returns exactly
`{"status":"ok"}` and does not require Ollama.

Current routes:

- `GET /` and `GET /health`
- `POST /llm/test` for the configured Qwen model chain
- `POST /documents/process` for PDF/DOCX/PPTX/TXT extraction, chunking and
  BGE-M3 embeddings
- `POST /rag/retrieve` for course-isolated similarity search

The document endpoint accepts a multipart file plus `material_id`, `course_id`,
`education_level` and `grade_level`. Every produced chunk carries those values,
`page_number` and `chunk_id`. Embedded chunks are stored in the persistent SQLite
vector store configured by `VECTOR_DB_PATH`.
`DOCUMENT_MAX_FILE_SIZE_BYTES` configures the upload limit and defaults to 20 MiB.

The retrieval endpoint accepts JSON and requires a course:

```json
{
  "question": "Bu derste süreklilik doğrulama kodu nedir?",
  "courseId": 101,
  "topK": 3
}
```

It embeds the question with BGE-M3 and returns ranked source text, similarity
score and complete chunk metadata. Candidate rows are selected with a mandatory
SQL `course_id` filter before similarity ranking. Snake-case `course_id` and
`top_k` request fields are accepted as well.

Install the required local embedding model before live document processing:

```bash
ollama pull bge-m3
```

Run tests that do not contact Ollama:

```bash
cd ai-service
.venv/bin/python -m pytest -q -m "not ollama"
```

Run the opt-in BGE-M3 integration test only when local Ollama is running and
`bge-m3` is installed:

```bash
cd ai-service
RUN_OLLAMA_TESTS=1 .venv/bin/python -m pytest -q -m ollama
```

See [setup instructions](../Docs/PYTHON_SERVICE_SETUP.md) for local environment installation, configuration, one-command startup from the repository root, and tests.

Directory responsibilities:

| Directory | Purpose |
| --- | --- |
| api | HTTP routes and application entry point |
| agents | Future agent implementations |
| rag | Document extraction, chunking, embedding pipeline and vector-store adapter |
| llm | Future model adapters |
| graphs | Future shared orchestration |
| schemas | Shared context, assignment and document-processing models |
| prompts | Future versioned agent prompts |
| config | Environment-based settings |

The placeholder directories do not imply their later features are implemented. Do not commit local .env, .venv, or generated Python caches.
