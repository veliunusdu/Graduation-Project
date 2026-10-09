# Python AI service setup

## Current milestone

The shared FastAPI service provides startup, configuration, a root response, and GET /health. Its required directories are api, agents, rag, llm, graphs, schemas, prompts, and config. Agent implementations and Ollama inference belong to later milestones.

## Install in a local environment

The setup was verified with Python 3.14.3 on Windows. Direct runtime and test dependencies are pinned in requirements.txt and requirements-dev.txt; transitive dependencies are resolved by pip and are not a complete lockfile.

From the repository root:

```powershell
Set-Location 'C:\Codes\Projects\Graduation Project'
python --version
python -m venv '.\ai-service\.venv'
& '.\ai-service\.venv\Scripts\python.exe' -m pip install -r '.\ai-service\requirements.txt'
```

If cloning elsewhere, adjust Set-Location. Commands use the virtual environment's executable directly, so PowerShell activation and execution-policy changes are unnecessary.

## Configure

Create a local environment file only if it does not already exist:

```powershell
if (-not (Test-Path -LiteralPath '.\ai-service\.env')) {
    Copy-Item -LiteralPath '.\ai-service\.env.example' -Destination '.\ai-service\.env'
}
```

Available settings:

| Setting | Meaning | Default |
| --- | --- | --- |
| APP_NAME | Service title; must not be blank | AI-Supported Homework Platform - AI Service |
| OLLAMA_BASE_URL | Valid HTTP/HTTPS address for later model calls | http://localhost:11434 |

Environment variables override the service's .env file. The file path is resolved relative to the service source, so starting from the repository root does not skip it. HttpUrl normalizes URL formatting; future clients should convert the setting to str. Unrelated .env keys are ignored to permit later service configuration additions.

Example override in the current terminal:

```powershell
$env:OLLAMA_BASE_URL = 'http://127.0.0.1:11434'
```

Local .env and Python caches are ignored by Git and have been removed from current tracking. Their existing local copies are preserved. This does not rewrite earlier Git history. Commit .env.example as the shared configuration example.

## Start with one command

From the repository root after installation:

```powershell
& '.\ai-service\.venv\Scripts\python.exe' -m uvicorn api.main:app --app-dir '.\ai-service' --host 127.0.0.1 --port 8000
```

The service is independent of the ASP.NET application. Leave its terminal running and use Ctrl+C to stop it. If port 8000 is occupied, choose another unused port and use that same port in your checks. Do not stop an unrelated process.

## Check health

In another terminal:

```powershell
$response = Invoke-WebRequest -Uri 'http://127.0.0.1:8000/health' -UseBasicParsing
$response.StatusCode
$response.Content
```

Expected HTTP status: 200. Expected JSON object: {"status":"ok"} with no additional fields.

This endpoint checks service liveness. It does not contact Ollama or assert that a model is installed or ready. GET / returns the startup message. Interactive API documentation is available at /docs.

## Run tests

Install development dependencies into the same local environment:

```powershell
& '.\ai-service\.venv\Scripts\python.exe' -m pip install -r '.\ai-service\requirements-dev.txt'
Push-Location '.\ai-service'
try {
    & '.\.venv\Scripts\python.exe' -m pytest
} finally {
    Pop-Location
}
& '.\ai-service\.venv\Scripts\python.exe' -m pip check
```

Tests exercise the exact health payload, environment precedence, configured title, invalid configuration, and .env loading from another working directory. They require no running Ollama instance or external model calls.

## References

- Pydantic settings: https://docs.pydantic.dev/latest/concepts/pydantic_settings/
- FastAPI testing: https://fastapi.tiangolo.com/tutorial/testing/

## Verification record

Verified on October 9 2026:

- Created a fresh project-local virtual environment and installed requirements-dev.txt, including runtime dependencies.
- The eight tests passed in that environment with no warnings.
- pip check reported no broken requirements.
- The documented root-directory startup command served /health with HTTP 200 and exactly {"status":"ok"}.
- APP_NAME set through the process environment appeared in the live OpenAPI title.
- Health remained available with an alternate Ollama URL and without starting Ollama.
- The verification server was stopped afterward.

The tests first reproduced six health/configuration failures before production changes. This verifies the service foundation and configuration, not model inference or later AI features.
An independent read-only review of the Step 2 patch found no actionable defects. The local work is saved on feature/python-service-foundation, based on the completed ASP.NET foundation. No merge or push was performed.
