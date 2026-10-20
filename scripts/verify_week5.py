"""Live HTTP smoke check using disposable accounts, database and file storage.

Run with ai-service/.venv/Scripts/python.exe after building Debug.
Requires the project's requirements-dev.txt. Existing app data is never used.
"""
import html
import json
import os
from pathlib import Path
import re
import secrets
import socket
import subprocess
import sys
import tempfile
import time

import httpx


ROOT = Path(__file__).resolve().parents[1]
FLAGS = getattr(subprocess, "CREATE_NO_WINDOW", 0)


def free_port():
    with socket.socket() as sock:
        sock.bind(("127.0.0.1", 0))
        return sock.getsockname()[1]


def wait_for(url, process):
    for _ in range(120):
        if process.poll() is not None:
            raise RuntimeError("Service exited; inspect its smoke log.")
        try:
            if httpx.get(url, timeout=1, trust_env=False).status_code == 200:
                return
        except httpx.HTTPError:
            pass
        time.sleep(0.25)
    raise RuntimeError("Service startup timed out.")


def post(client, path, data, token_page=None, files=None):
    response = client.get(token_page or path)
    response.raise_for_status()
    token = re.search(r'name="__RequestVerificationToken"[^>]*value="([^"]+)"', response.text)
    assert token, "Antiforgery token missing"
    fields = dict(data, __RequestVerificationToken=html.unescape(token[1]))
    response = client.post(path, data=fields, files=files)
    assert response.status_code == 302, (path, response.status_code, response.text[:200])
    return response.headers["location"]


def stop(process):
    if process is not None and process.poll() is None:
        process.terminate()
        try:
            process.wait(timeout=10)
        except subprocess.TimeoutExpired:
            process.kill()
            process.wait(timeout=5)


def main():
    evidence = ROOT / "Docs" / "TestEvidence" / "Week5"
    evidence.mkdir(parents=True, exist_ok=True)
    dll = ROOT / "web/bin/Debug/net8.0/HomeworkPlatform.Web.dll"
    assert dll.exists(), "Build the web project first."
    python_process = web_process = None
    results = {}
    with tempfile.TemporaryDirectory(prefix="HomeworkPlatform-week5-smoke-") as temp:
        temporary = Path(temp)
        python_port, web_port = free_port(), free_port()
        environment = os.environ.copy()
        password = "Smoke8!" + secrets.token_hex(12)
        environment.update({
            "ASPNETCORE_ENVIRONMENT": "Development", "DOTNET_ENVIRONMENT": "Development",
            "ConnectionStrings__DefaultConnection": f"Data Source={temporary / 'smoke.db'}",
            "SubmissionFiles__RootPath": str(temporary / "files"),
            "AIService__BaseUrl": f"http://127.0.0.1:{python_port}/",
            "AIService__TimeoutSeconds": "2", "Logging__LogLevel__Default": "Warning",
            "PROVISION_TEACHER_PASSWORD": password,
        })
        with (evidence / "live-python.log").open("w", encoding="utf-8") as python_log, (evidence / "live-web.log").open("w", encoding="utf-8") as web_log:
            try:
                subprocess.run(["dotnet", str(dll), "--provision-teacher", "smoke-teacher@example.test"],
                    cwd=ROOT / "web", env=environment, stdout=web_log, stderr=subprocess.STDOUT,
                    creationflags=FLAGS, timeout=30, check=True)
                environment.pop("PROVISION_TEACHER_PASSWORD")
                python_process = subprocess.Popen([sys.executable, "-m", "uvicorn", "api.main:app", "--host", "127.0.0.1", "--port", str(python_port)],
                    cwd=ROOT / "ai-service", stdout=python_log, stderr=subprocess.STDOUT, creationflags=FLAGS)
                wait_for(f"http://127.0.0.1:{python_port}/health", python_process)
                web_process = subprocess.Popen(["dotnet", str(dll), "--urls", f"http://127.0.0.1:{web_port}"],
                    cwd=ROOT / "web", env=environment, stdout=web_log, stderr=subprocess.STDOUT, creationflags=FLAGS)
                base_url = f"http://127.0.0.1:{web_port}"
                wait_for(base_url, web_process)
                with httpx.Client(base_url=base_url, trust_env=False, timeout=15) as teacher, httpx.Client(base_url=base_url, trust_env=False, timeout=15) as student:
                    post(teacher, "/Account/Login", {"Email": "smoke-teacher@example.test", "Password": password})
                    assert "alert-success" in teacher.get("/AIStatus").text
                    results["aspnet_to_real_python_health"] = "passed"
                    post(student, "/Account/Register", {"Email": "smoke-student@example.test", "Password": password, "ConfirmPassword": password})
                    course = post(teacher, "/Courses/Create", {"Name": "Smoke course", "EducationLevel": "1", "GradeLevel": "1", "IsActive": "true"})
                    course_id = course.rstrip("/").split("/")[-1]
                    post(teacher, f"/Courses/Enroll/{course_id}", {"Email": "smoke-student@example.test"}, course)
                    assignment = post(teacher, f"/Assignments/Create?courseId={course_id}", {
                        "Title": "Smoke assignment", "Description": "Upload a file", "Difficulty": "0", "DueDate": "2026-12-01",
                        "Requirements[0].Title": "Working code", "Requirements[0].Weight": "100", "Requirements[0].Order": "1",
                    })
                    assignment_id = assignment.rstrip("/").split("/")[-1]
                    form_url = f"/Submissions/Create?assignmentId={assignment_id}"
                    page = student.get(form_url)
                    assert 'enctype="multipart/form-data"' in page.text and 'name="Upload"' in page.text
                    data = b"print('live upload')\n"
                    submission = post(student, form_url, {}, files={"Upload": ("solution.py", data, "text/plain")})
                    submission_id = submission.rstrip("/").split("/")[-1]
                    assert 'solution.py' in student.get(submission).text
                    download = f"/SubmissionFiles/Download/{submission_id}"
                    assert student.get(download).content == data
                    assert teacher.get(download).content == data
                    assert len(list((temporary / "files").glob("*.upload"))) == 1
                    post(teacher, f"/Assignments/Review/{submission_id}", {"Feedback": "Live review", "Grade": "85"}, f"/Assignments/Submission/{submission_id}")
                    assert "Live review" in student.get(submission).text
                    results["live_file_upload_download_and_teacher_review"] = "passed"
                    stop(python_process)
                    response = teacher.get("/AIStatus")
                    assert response.status_code == 200 and "alert-warning" in response.text
                    assert "Traceback" not in response.text and "System.Net" not in response.text
                    results["python_offline_controlled_message"] = "passed"
            finally:
                stop(web_process)
                stop(python_process)
    (evidence / "live-http-results.json").write_text(json.dumps(results, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(results, indent=2))


if __name__ == "__main__":
    main()
