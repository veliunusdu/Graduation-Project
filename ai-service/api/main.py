from typing import Literal

from fastapi import FastAPI, HTTPException
from pydantic import BaseModel, Field

from config.settings import settings
from llm.ollama_client import OllamaClientError, ollama_client

app = FastAPI(title=settings.APP_NAME)


class LLMTestRequest(BaseModel):
    prompt: str = Field(min_length=1)
    task: Literal["general", "code_review"] = "general"


@app.get("/")
def read_root():
    return {"status": "ok", "message": "Python AI Service is running"}


@app.get("/health")
def health_check():
    """Report service liveness without requiring Ollama to be running."""
    return {"status": "ok"}


@app.post("/llm/test")
def test_llm(request: LLMTestRequest):
    try:
        return ollama_client.generate(
            prompt=request.prompt,
            task=request.task,
        )
    except OllamaClientError as exc:
        raise HTTPException(status_code=502, detail=str(exc)) from exc
    except ValueError as exc:
        raise HTTPException(status_code=400, detail=str(exc)) from exc
