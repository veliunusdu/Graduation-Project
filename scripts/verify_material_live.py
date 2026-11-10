"""Run the opt-in MVC -> real Python -> BGE check with isolated stores."""
import argparse, os, subprocess, sys, tempfile, socket, time, urllib.request
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument("--output-dir",required=True);p.add_argument("--dependencies");a=p.parse_args()
root=Path(__file__).resolve().parents[1];out=Path(a.output_dir).resolve();out.mkdir(parents=True,exist_ok=False)
with tempfile.TemporaryDirectory(prefix="material-live-") as temp:
 env=os.environ.copy();env.update(VECTOR_DB_PATH=str(Path(temp)/"vectors.db"),OLLAMA_BASE_URL="http://127.0.0.1:11434",OLLAMA_EMBEDDING_MODEL="bge-m3")
 if a.dependencies:env["PYTHONPATH"]=a.dependencies
 with socket.socket() as s:s.bind(("127.0.0.1",0));port=s.getsockname()[1]
 url=f"http://127.0.0.1:{port}/";log=(out/"python.log").open("w",encoding="utf-8")
 proc=subprocess.Popen([sys.executable,"-m","uvicorn","api.main:app","--host","127.0.0.1","--port",str(port)],cwd=root/"ai-service",env=env,stdout=log,stderr=subprocess.STDOUT,creationflags=getattr(subprocess,"CREATE_NO_WINDOW",0))
 try:
  for _ in range(60):
   if proc.poll() is not None:raise RuntimeError("Python server exited")
   try:
    with urllib.request.urlopen(url+"health",timeout=1) as response:
     if response.status==200:break
   except OSError:pass
   time.sleep(.5)
  else:raise RuntimeError("Python server not ready")
  env["VELI_MATERIAL_PYTHON_URL"]=url
  r=subprocess.run(["dotnet","test","tests/HomeworkPlatform.Web.Tests","--filter","FullyQualifiedName~ActualWebUploadReachesRealPythonAndBge","--logger","trx;LogFileName=live.trx","--results-directory",str(out)],cwd=root,env=env,capture_output=True,text=True,timeout=180)
  (out/"dotnet.log").write_text(r.stdout+r.stderr,encoding="utf-8");print(r.stdout+r.stderr);code=r.returncode
 finally:
  proc.terminate()
  try:proc.wait(timeout=10)
  except subprocess.TimeoutExpired:proc.kill();proc.wait(timeout=5)
  log.close()
raise SystemExit(code)
