from typing import Literal

from fastapi import FastAPI, File, Form, HTTPException, UploadFile
from pydantic import BaseModel, Field
from starlette.concurrency import run_in_threadpool

from config.settings import settings
from llm.ollama_client import OllamaClientError, ollama_client
from rag.document_processing import DocumentProcessingError
from rag.embedding import EmbeddingError
from rag.pipeline import document_pipeline
from rag.retrieval import retrieval_service
from rag.vector_store import VectorStoreError
from schemas.context import AgentContext
from schemas.document import (
    DocumentProcessingResult,
    RetrievalRequest,
    RetrievalResult,
)

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


@app.post("/documents/process", response_model=DocumentProcessingResult)
async def process_document(
    file: UploadFile = File(...),
    material_id: int = Form(...),
    course_id: int = Form(...),
    education_level: str = Form(...),
    grade_level: int = Form(...),
):
    """Extract, chunk and embed one CourseMaterial supplied by the web backend."""
    try:
        content = await file.read(settings.DOCUMENT_MAX_FILE_SIZE_BYTES + 1)
        if len(content) > settings.DOCUMENT_MAX_FILE_SIZE_BYTES:
            raise HTTPException(
                status_code=413,
                detail="Document exceeds the configured file size limit.",
            )
        context = AgentContext(
            course_id=course_id,
            education_level=education_level,
            grade_level=grade_level,
        )
        return await run_in_threadpool(
            document_pipeline.process,
            content=content,
            filename=file.filename or "",
            material_id=material_id,
            context=context,
        )
    except DocumentProcessingError as exc:
        raise HTTPException(status_code=422, detail=str(exc)) from exc
    except EmbeddingError as exc:
        raise HTTPException(status_code=502, detail=str(exc)) from exc
    except ValueError as exc:
        raise HTTPException(status_code=422, detail=str(exc)) from exc


@app.post("/rag/retrieve", response_model=RetrievalResult)
async def retrieve_chunks(request: RetrievalRequest):
    """Retrieve course-isolated source chunks for a student question."""
    try:
        return await run_in_threadpool(retrieval_service.retrieve, request)
    except EmbeddingError as exc:
        raise HTTPException(status_code=502, detail=str(exc)) from exc
    except VectorStoreError as exc:
        raise HTTPException(
            status_code=500,
            detail="Vector store operation failed.",
        ) from exc
