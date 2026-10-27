"""Opt-in real .NET client -> FastAPI -> local Ollama tests; no production route added."""
import argparse
import importlib.util
import json
import os
from pathlib import Path
import subprocess
import sys
import time

import httpx

ROOT = Path(__file__).resolve().parents[1]
_spec = importlib.util.spec_from_file_location("live_helpers",ROOT/"scripts/verify_week5.py")
helpers = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(helpers)


def run(output_dir):
    evidence=Path(output_dir).resolve()
    evidence.mkdir(parents=True,exist_ok=False)
    cases=[("real_model","qwen3:4b","http://127.0.0.1:11434","120",None),
           ("missing_model","veli-intentionally-missing-model:no-such-tag","http://127.0.0.1:11434","30","404"),
           ("unreachable_ollama","qwen3:4b",f"http://127.0.0.1:{helpers.free_port()}","10","Could not connect"),
           ("upstream_timeout","qwen3:4b","http://127.0.0.1:11434","0.001","timed out")]
    report={"status":"failed","model":"qwen3:4b","scenarios":[],
            "boundary":"real ASP.NET AIServiceClient class, real FastAPI and local Ollama; no web analysis screen",
            "ollama_daemon_stopped":False,"model_downloads":0,"owned_python_processes_stopped":True}
    try:
        report["ollama_version"]=httpx.get("http://127.0.0.1:11434/api/version",trust_env=False,timeout=5).json()
        models=httpx.get("http://127.0.0.1:11434/api/tags",trust_env=False,timeout=5).json()["models"]
        report["model_digest"]=next(m["digest"] for m in models if m["name"]=="qwen3:4b")
        for name,model,ollama_url,timeout,expected_detail in cases:
            row={"scenario":name,"status":"FAIL"};report["scenarios"].append(row)
            environment=os.environ.copy()
            port=helpers.free_port()
            environment.update({"OLLAMA_BASE_URL":ollama_url,"OLLAMA_GENERAL_MODEL":model,
                                "OLLAMA_CODE_REVIEW_MODEL":model,"OLLAMA_TIMEOUT_SECONDS":timeout,
                                "VELI_LIVE_PYTHON_URL":f"http://127.0.0.1:{port}/",
                                "VELI_LIVE_EXPECTED":"success" if expected_detail is None else "http_error",
                                "VELI_LIVE_OUTPUT_JSON":str(evidence/f"{name}-client.json")})
            process=None
            started=time.perf_counter()
            with (evidence/f"{name}-python.log").open("w",encoding="utf-8") as log:
                try:
                    process=subprocess.Popen([sys.executable,"-m","uvicorn","api.main:app","--host","127.0.0.1","--port",str(port)],
                        cwd=ROOT/"ai-service",env=environment,stdout=log,stderr=subprocess.STDOUT,creationflags=helpers.FLAGS)
                    helpers.wait_for(f"http://127.0.0.1:{port}/health",process)
                    response=httpx.post(f"http://127.0.0.1:{port}/llm/test",
                        json={"prompt":"Return only the word INTEGRATION_OK. Do not explain.","task":"general"},trust_env=False,timeout=130)
                    if expected_detail is None:
                        data=response.json()
                        assert response.status_code==200 and data["model"]=="qwen3:4b" and data["done"] is True
                        assert data["response"].strip()=="INTEGRATION_OK"
                    else:
                        assert response.status_code==502 and expected_detail in response.json()["detail"]
                    row["python_http_status"]=response.status_code
                    row["python_error_kind_verified"]=expected_detail
                    command=["dotnet","test","tests/HomeworkPlatform.Web.Tests/HomeworkPlatform.Web.Tests.csproj",
                             "--no-restore","--no-build","--configuration","Debug",
                             "--filter","FullyQualifiedName~LiveOllamaIntegrationTests",
                             "--logger",f"trx;LogFileName={name}.trx","--results-directory",str(evidence)]
                    tested=subprocess.run(command,cwd=ROOT,env=environment,capture_output=True,text=True,timeout=150,creationflags=helpers.FLAGS)
                    (evidence/f"{name}-dotnet.txt").write_text(tested.stdout+tested.stderr,encoding="utf-8")
                    assert tested.returncode==0 and (evidence/f"{name}-client.json").exists(),"Live .NET assertion failed or did not execute."
                    row.update(status="PASS",duration_seconds=round(time.perf_counter()-started,2),
                               client=json.loads((evidence/f"{name}-client.json").read_text(encoding="utf-8-sig")))
                finally:
                    helpers.stop(process)
                    report["owned_python_processes_stopped"] &= process is None or process.poll() is not None
        report["status"]="passed"
    except Exception as exc:
        report["error"]={"type":type(exc).__name__,"message":"Live assertion failed; inspect the per-case evidence."}
    (evidence/"results.json").write_text(json.dumps(report,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
    print(json.dumps(report,ensure_ascii=False,indent=2))
    return 0 if report["status"]=="passed" else 1


if __name__=="__main__":
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output-dir",required=True)
    raise SystemExit(run(parser.parse_args().output_dir))
