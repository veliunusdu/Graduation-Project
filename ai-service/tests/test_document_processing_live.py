import os
from pathlib import Path

import pytest

from rag.embedding import OllamaEmbeddingClient
from rag.pipeline import DocumentPipeline
from rag.vector_store import InMemoryVectorStore
from schemas.context import AgentContext


pytestmark = [
    pytest.mark.ollama,
    pytest.mark.skipif(
        os.getenv("RUN_OLLAMA_TESTS") != "1",
        reason="Set RUN_OLLAMA_TESTS=1 to run the local BGE-M3 integration test.",
    ),
]

FIXTURE = (
    Path(__file__).resolve().parents[2]
    / "samples"
    / "ai-rag-smoke"
    / "valid"
    / "extraction.pdf"
)


def test_real_bge_m3_document_pipeline():
    store = InMemoryVectorStore()
    pipeline = DocumentPipeline(
        embedder=OllamaEmbeddingClient(),
        store=store,
    )

    result = pipeline.process(
        content=FIXTURE.read_bytes(),
        filename=FIXTURE.name,
        material_id=41,
        context=AgentContext(
            course_id=7,
            education_level="University",
            grade_level=2,
        ),
    )

    stored = store.get_for_material(41)
    assert result.embedding_model == "bge-m3"
    assert result.embedding_dimensions == 1024
    assert result.chunk_count == len(stored) > 0
