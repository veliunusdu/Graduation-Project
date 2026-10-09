# Graduation Project

AI-assisted homework management and learning platform. The first two roadmap weeks are implemented: ASP.NET Core MVC, Identity Teacher/Student accounts, resource ownership, and an independent Python health/configuration service.

Main working folder: `C:\Codes\Projects\Graduation Project`.

## Web quickstart

Requires .NET SDK 8.0.425 or a compatible later 8.0.4xx patch selected by global.json.

```powershell
Set-Location 'C:\Codes\Projects\Graduation Project'
dotnet restore HomeworkPlatform.sln
dotnet build HomeworkPlatform.sln --no-restore
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:DOTNET_ENVIRONMENT = 'Development'
dotnet run --project web/HomeworkPlatform.Web.csproj --no-build --no-launch-profile --urls http://localhost:5080
```

Open http://localhost:5080. Register Students in the browser. Provision Teachers using the hidden-password procedure in [authentication setup](Docs/AUTHENTICATION_SETUP.md). Then follow [ownership setup](Docs/OWNERSHIP_SETUP.md) to create courses/assignments, enroll Students and test separate submissions. Migrations apply on startup; no credentials or example accounts are seeded.

## Python quickstart

Python 3.14.3 was verified for this milestone. From the repository root:

```powershell
python -m venv ai-service/.venv
& '.\ai-service\.venv\Scripts\python.exe' -m pip install -r ai-service/requirements.txt
& '.\ai-service\.venv\Scripts\python.exe' -m uvicorn api.main:app --app-dir '.\ai-service' --host 127.0.0.1 --port 8000
```

GET http://127.0.0.1:8000/health returns `{"status":"ok"}`. See [Python setup](Docs/PYTHON_SERVICE_SETUP.md) for environment settings and test dependencies. Ollama is not required for the health endpoint.

## Verify

```powershell
dotnet test HomeworkPlatform.sln
dotnet test HomeworkPlatform.sln -c Release
& '.\ai-service\.venv\Scripts\python.exe' -m pip install -r ai-service/requirements-dev.txt
Push-Location ai-service
try { & '.\.venv\Scripts\python.exe' -m pytest -q }
finally { Pop-Location }
```

[Week 1/2 completion report](Docs/WEEKS_1_2_COMPLETION.md) maps every teammate's tasks to test/live/clean-clone evidence. [Scope](Docs/PROJECT_SCOPE.md) and [collaboration conventions](CONTRIBUTING.md) describe the larger project. AI agents, RAG, files, rubrics and grading are later roadmap milestones.
