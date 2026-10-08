# AI service

Independent FastAPI service for the homework platform. Current routes are GET / and GET /health. Health returns exactly {"status":"ok"}; it does not require Ollama.

See [setup instructions](../Docs/PYTHON_SERVICE_SETUP.md) for local environment installation, configuration, one-command startup from the repository root, and tests.

Directory responsibilities:

| Directory | Purpose |
| --- | --- |
| api | HTTP routes and application entry point |
| agents | Future agent implementations |
| rag | Future retrieval and document processing |
| llm | Future model adapters |
| graphs | Future shared orchestration |
| schemas | Future shared request/output models |
| prompts | Future versioned agent prompts |
| config | Environment-based settings |

The placeholder directories do not imply their later features are implemented. Do not commit local .env, .venv, or generated Python caches.
