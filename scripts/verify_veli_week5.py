"""Real ASP.NET/FastAPI health plus controlled HTTP faults; no model calls.

Run with ai-service/.venv/Scripts/python.exe after a Debug web build.
Each run requires a NEW --output-dir so previous evidence is preserved.
"""
import argparse
import html
import importlib.util
import json
import os
from pathlib import Path
import secrets
import subprocess
import sys
import tempfile
import threading
import time
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer

import httpx

ROOT = Path(__file__).resolve().parents[1]
_spec = importlib.util.spec_from_file_location("week5_process_helpers", ROOT / "scripts/verify_week5.py")
helpers = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(helpers)


class VerificationFailure(Exception):
    pass


def require(condition, message):
    if not condition:
        raise VerificationFailure(message)


def check_page(response, *, success, expected_message, log_text="", expected_log=None):
    text = html.unescape(response.text)
    require(response.status_code == 200, "Status page did not return HTTP 200.")
    expected_class = "alert-success" if success else "alert-warning"
    wrong_class = "alert-warning" if success else "alert-success"
    require(expected_class in text and wrong_class not in text, "Status page displayed the wrong result class.")
    require(expected_message in text, "Expected controlled message was not displayed.")
    require(not any(marker in text for marker in ("Traceback", "System.Net", "SYNTHETIC_PRIVATE_ERROR", "Exception:")),
            "Status page exposed a diagnostic marker.")
    if expected_log is not None:
        require(expected_log in log_text, "Expected correlated server log was not recorded.")
    return {"http_status":response.status_code, "page_result":"success" if success else "warning",
            "message_verified":expected_message, "diagnostic_markers_absent":True,
            "log_verified":expected_log is not None, "expected_log":expected_log}


class FaultServer:
    """Actual loopback HTTP transport, with controlled dependency behavior."""
    def __init__(self, port):
        self.mode = "http_500"
        self.requests = []
        self.lock = threading.Lock()
        owner = self
        class Handler(BaseHTTPRequestHandler):
            def do_GET(self):
                with owner.lock:
                    mode = owner.mode
                    owner.requests.append({"mode":mode, "path":self.path})
                if mode == "timeout":
                    time.sleep(4)
                status = {"http_500":500, "http_503":503}.get(mode, 200)
                body = b"not JSON SYNTHETIC_PRIVATE_ERROR Traceback" if mode == "invalid_json" else b"SYNTHETIC_PRIVATE_ERROR Traceback"
                self.send_response(status)
                self.send_header("Content-Type", "application/json")
                self.end_headers()
                try:
                    self.wfile.write(body)
                except (BrokenPipeError, ConnectionResetError, OSError):
                    pass  # The client deliberately times out before a delayed write.
            def log_message(self, *args):
                pass
        self.server = ThreadingHTTPServer(("127.0.0.1", port), Handler)
        self.thread = threading.Thread(target=lambda:self.server.serve_forever(poll_interval=0.05), daemon=True)

    def start(self):
        self.thread.start()

    def set_mode(self, mode):
        with self.lock:
            self.mode = mode

    def close(self):
        self.server.shutdown()
        self.server.server_close()
        self.thread.join(timeout=2)


def log_since(path, offset, expected=None):
    # Child console writes may arrive just after the HTTP response.
    for _ in range(20):
        with path.open("rb") as stream:
            stream.seek(offset)
            text = stream.read().decode("utf-8", errors="replace")
        if expected is None or expected in text:
            return text
        time.sleep(0.05)
    return text


def run(output_dir):
    evidence = Path(output_dir).resolve()
    evidence.mkdir(parents=True, exist_ok=False)
    modes = ["real_python_health", "python_offline", "timeout", "http_500", "http_503", "invalid_json"]
    rows = [{"scenario":mode, "status":"NOT_RUN"} for mode in modes]
    report = {"status":"failed", "scope":"Veli Week 5 first three steps",
              "model_calls":0, "live_model_validation":"NOT_RUN_USER_DEFERRED",
              "scenarios":rows, "faults_are_controlled_http_dependencies":True,
              "isolated_database":True, "owned_processes_stopped":False}
    python_process = web_process = fault = None
    current = None
    dll = ROOT / "web/bin/Debug/net8.0/HomeworkPlatform.Web.dll"
    python_log_path, web_log_path = evidence / "python.log", evidence / "web.log"
    try:
        require(dll.exists(), "Build the Debug web application before running live checks.")
        with tempfile.TemporaryDirectory(prefix="HomeworkPlatform-veli-week5-") as temp:
            temporary = Path(temp).resolve()
            require(temporary.parent == Path(tempfile.gettempdir()).resolve()
                    and temporary.name.startswith("HomeworkPlatform-veli-week5-"), "Unexpected temporary directory.")
            python_port, web_port = helpers.free_port(), helpers.free_port()
            while web_port == python_port:
                web_port = helpers.free_port()
            password = "Smoke8!" + secrets.token_hex(12)
            environment = os.environ.copy()
            environment.update({
                "ASPNETCORE_ENVIRONMENT":"Development", "DOTNET_ENVIRONMENT":"Development",
                "ConnectionStrings__DefaultConnection":f"Data Source={temporary / 'smoke.db'}",
                "SubmissionFiles__RootPath":str(temporary / "files"),
                "AIService__BaseUrl":f"http://127.0.0.1:{python_port}/",
                "AIService__TimeoutSeconds":"2", "Logging__LogLevel__Default":"Warning",
                "PROVISION_TEACHER_PASSWORD":password,
            })
            with python_log_path.open("w",encoding="utf-8") as python_log, web_log_path.open("w",encoding="utf-8") as web_log:
                try:
                    subprocess.run(["dotnet",str(dll),"--provision-teacher","veli-smoke@example.test"],
                        cwd=ROOT / "web",env=environment,stdout=web_log,stderr=subprocess.STDOUT,
                        creationflags=helpers.FLAGS,timeout=30,check=True)
                    environment.pop("PROVISION_TEACHER_PASSWORD")
                    python_process = subprocess.Popen([sys.executable,"-m","uvicorn","api.main:app","--host","127.0.0.1","--port",str(python_port)],
                        cwd=ROOT / "ai-service",env=environment,stdout=python_log,stderr=subprocess.STDOUT,creationflags=helpers.FLAGS)
                    helpers.wait_for(f"http://127.0.0.1:{python_port}/health",python_process)
                    web_process = subprocess.Popen(["dotnet",str(dll),"--urls",f"http://127.0.0.1:{web_port}"],
                        cwd=ROOT / "web",env=environment,stdout=web_log,stderr=subprocess.STDOUT,creationflags=helpers.FLAGS)
                    report["owned_process_ids"] = {"python":python_process.pid,"web":web_process.pid}
                    base_url = f"http://127.0.0.1:{web_port}"
                    helpers.wait_for(base_url,web_process)
                    with httpx.Client(base_url=base_url,trust_env=False,timeout=15) as teacher:
                        helpers.post(teacher,"/Account/Login",{"Email":"veli-smoke@example.test","Password":password})
                        current = rows[0]
                        direct = httpx.get(f"http://127.0.0.1:{python_port}/health",timeout=3,trust_env=False)
                        require(direct.status_code == 200 and direct.json() == {"status":"ok"},"Real Python health JSON was unexpected.")
                        current.update(check_page(teacher.get("/AIStatus"),success=True,expected_message="Python AI servisine bağlantı başarılı."),
                            status="PASS", dependency="real FastAPI process", direct_python_json=direct.json())

                        current = rows[1]
                        helpers.stop(python_process)
                        require(python_process.poll() is not None,"Owned Python process did not stop.")
                        offset = web_log_path.stat().st_size
                        response = teacher.get("/AIStatus")
                        decoded = html.unescape(response.text)
                        offline_timeout = "AI servisi zamanında yanıt vermedi." in decoded
                        expected_message = "AI servisi zamanında yanıt vermedi." if offline_timeout else "AI servisine bağlanılamadı."
                        expected_log = "AI service request timed out." if offline_timeout else "AI service connection failed."
                        current.update(check_page(response,success=False,expected_message=expected_message,
                            log_text=log_since(web_log_path,offset,expected_log),expected_log=expected_log),
                            status="PASS",dependency="real Python process stopped", error_kind="timeout" if offline_timeout else "unavailable")

                        fault = FaultServer(python_port)
                        fault.start()
                        settings = {
                            "timeout":("AI servisi zamanında yanıt vermedi.","AI service request timed out."),
                            "http_500":("AI servisi isteği tamamlayamadı.","AI service returned HTTP 500."),
                            "http_503":("AI servisi isteği tamamlayamadı.","AI service returned HTTP 503."),
                            "invalid_json":("AI servisinden geçerli bir JSON yanıtı alınamadı.","AI service returned invalid JSON."),
                        }
                        for current in rows[2:]:
                            mode = current["scenario"]
                            fault.set_mode(mode)
                            offset = web_log_path.stat().st_size
                            before = len(fault.requests)
                            started = time.perf_counter()
                            response = teacher.get("/AIStatus")
                            elapsed = time.perf_counter()-started
                            message, expected_log = settings[mode]
                            current.update(check_page(response,success=False,expected_message=message,
                                log_text=log_since(web_log_path,offset,expected_log),expected_log=expected_log),
                                status="PASS",dependency="controlled loopback HTTP fault server",
                                duration_ms=round(elapsed*1000), configured_timeout_seconds=2)
                            requests = fault.requests[before:]
                            require(any(r["mode"] == mode and r["path"] == "/health" for r in requests),"Fault server did not receive the expected health request.")
                            current["fault_requests"] = requests
                        report["status"] = "passed"
                finally:
                    if fault is not None:
                        fault.close()
                    helpers.stop(web_process)
                    helpers.stop(python_process)
                    report["owned_processes_stopped"] = all(p is None or p.poll() is not None for p in (web_process,python_process))
        report["temporary_directory_removed"] = not temporary.exists()
    except Exception as exc:
        if current is not None:
            current["status"] = "FAIL"
        report["status"] = "failed"
        report["error"] = {"type":type(exc).__name__,"message":str(exc) if isinstance(exc,VerificationFailure) else "Live check failed; inspect the owned-service logs."}
    finally:
        (evidence / "results.json").write_text(json.dumps(report,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
    print(json.dumps(report,ensure_ascii=False,indent=2))
    return 0 if report["status"] == "passed" else 1


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output-dir",required=True,help="New evidence directory; an existing directory is refused")
    args = parser.parse_args(argv)
    return run(args.output_dir)


if __name__ == "__main__":
    raise SystemExit(main())
