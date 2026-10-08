from fastapi import FastAPI

from config.settings import settings

app = FastAPI(title=settings.APP_NAME)


@app.get("/")
def read_root():
    return {"status": "ok", "message": "Python AI Service is running"}


@app.get("/health")
def health_check():
    """Report service liveness without requiring Ollama to be running."""
    return {"status": "ok"}
