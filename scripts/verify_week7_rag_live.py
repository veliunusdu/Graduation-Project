"""Verify ASP.NET -> real Python -> BGE-M3 -> course-isolated RAG using isolated data."""
import argparse
import json
import os
from pathlib import Path
import socket
import subprocess
import sys
import sysconfig
import tempfile
import time
import urllib.error
import urllib.request


def get_json(url, payload=None, timeout=5):
    data = None if payload is None else json.dumps(payload).encode()
    request = urllib.request.Request(url, data=data, headers={"Content-Type": "application/json"})
    with urllib.request.urlopen(request, timeout=timeout) as response:
        return json.load(response)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output-dir", required=True)
    parser.add_argument("--ollama-url", default=os.environ.get("OLLAMA_BASE_URL", "http://127.0.0.1:11434"))
    parser.add_argument("--embedding-model", default=os.environ.get("OLLAMA_EMBEDDING_MODEL", "bge-m3"))
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[1]
    output = Path(args.output_dir).resolve()
    output.mkdir(parents=True, exist_ok=False)
    evidence = output / "acceptance.json"
    try:
        model = args.embedding_model if ":" in args.embedding_model else args.embedding_model + ":latest"
        tags = get_json(args.ollama_url.rstrip("/") + "/api/tags")
        if not any(item.get("name") == model for item in tags.get("models", [])):
            raise RuntimeError("The configured embedding model is not installed: " + model)
        # Warm up the local model before measuring the web app's unchanged 10-second timeout.
        get_json(args.ollama_url.rstrip("/") + "/api/embed", {"model": args.embedding_model, "input": "Week 7 warmup"}, timeout=120)
    except (OSError, ValueError, RuntimeError) as error:
        evidence.write_text(json.dumps({"status": "not_run", "reason": str(error), "real_bge_m3_verified": False}, indent=2), encoding="utf-8")
        print("Live verification was not run. Start Ollama and install the configured embedding model. See " + str(evidence))
        return 2
    with tempfile.TemporaryDirectory(prefix="week7-rag-") as temporary:
        env = {**os.environ, "VECTOR_DB_PATH": str(Path(temporary) / "vectors.db"), "OLLAMA_BASE_URL": args.ollama_url, "OLLAMA_EMBEDDING_MODEL": args.embedding_model}
        env["PYTHONPATH"] = os.pathsep.join(filter(None, [sysconfig.get_path("purelib"), env.get("PYTHONPATH")]))
        with socket.socket() as listener:
            listener.bind(("127.0.0.1", 0)); port = listener.getsockname()[1]
        url = f"http://127.0.0.1:{port}/"
        # The base executable avoids leaving a Windows venv launcher child behind.
        interpreter = sys._base_executable if os.name == "nt" else sys.executable
        with (output / "python.log").open("w", encoding="utf-8") as log:
            process = subprocess.Popen([interpreter, "-m", "uvicorn", "api.main:app", "--host", "127.0.0.1", "--port", str(port)], cwd=root / "ai-service", env=env, stdout=log, stderr=subprocess.STDOUT, creationflags=getattr(subprocess, "CREATE_NO_WINDOW", 0))
            try:
                for _ in range(60):
                    if process.poll() is not None:
                        raise RuntimeError("Python service exited; inspect python.log.")
                    try:
                        if get_json(url + "health", timeout=1) == {"status": "ok"}: break
                    except OSError: pass
                    time.sleep(.5)
                else: raise RuntimeError("Python service did not become ready.")
                env.update(WEEK7_PYTHON_URL=url, WEEK7_EVIDENCE_PATH=str(evidence))
                result = subprocess.run(["dotnet", "test", "tests/HomeworkPlatform.Web.Tests", "--artifacts-path", str(output / "build"), "--filter", "FullyQualifiedName~Week7LiveRagTests", "-p:NuGetAudit=false", "--logger", "trx;LogFileName=week7-live.trx", "--results-directory", str(output)], cwd=root, env=env, capture_output=True, text=True, encoding="utf-8", errors="replace", timeout=600)
                (output / "dotnet.log").write_text(result.stdout + result.stderr, encoding="utf-8")
                print(result.stdout + result.stderr)
                if result.returncode != 0 or not evidence.exists():
                    evidence.write_text(json.dumps({"status": "failed", "real_bge_m3_verified": False, "reason": "Inspect dotnet.log and the test report."}, indent=2), encoding="utf-8")
                    return result.returncode or 1
                return 0
            except (OSError, RuntimeError, subprocess.TimeoutExpired) as error:
                evidence.write_text(json.dumps({"status": "failed", "real_bge_m3_verified": False, "reason": str(error)}, indent=2), encoding="utf-8")
                print(error)
                return 1
            finally:
                process.terminate()
                try: process.wait(timeout=10)
                except subprocess.TimeoutExpired: process.kill(); process.wait(timeout=5)


if __name__ == "__main__":
    raise SystemExit(main())
