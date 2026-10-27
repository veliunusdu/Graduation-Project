import os
from pathlib import Path

import pytest
from fastapi.testclient import TestClient

from api import main as api_main
from rag.embedding import OllamaEmbeddingClient
from rag.pipeline import DocumentPipeline
from rag.retrieval import RetrievalService
from rag.vector_store import SQLiteVectorStore
from schemas.context import AgentContext
from schemas.document import ChunkMetadata, ProcessedChunk, RetrievalRequest


pytestmark = [
    pytest.mark.ollama,
    pytest.mark.skipif(
        os.getenv("RUN_OLLAMA_TESTS") != "1",
        reason="Set RUN_OLLAMA_TESTS=1 to run local BGE-M3 integration tests.",
    ),
]

FIXTURES = (
    Path(__file__).resolve().parents[2] / "samples" / "ai-rag-smoke" / "rag"
)
COURSES = [
    (101, 1001, "hs-a.txt", "HS_A_ONLY_9F31"),
    (102, 1002, "hs-b.txt", "HS_B_ONLY_8C72"),
    (201, 2001, "uni-a.txt", "UNI_ONLY_7D64"),
]


def test_real_bge_m3_retrieval_isolates_courses(tmp_path):
    store = SQLiteVectorStore(tmp_path / "vectors.db")
    embedder = OllamaEmbeddingClient()
    pipeline = DocumentPipeline(embedder=embedder, store=store)

    for course_id, material_id, filename, _ in COURSES:
        path = FIXTURES / filename
        pipeline.process(
            content=path.read_bytes(),
            filename=filename,
            material_id=material_id,
            context=AgentContext(
                course_id=course_id,
                education_level=(
                    "University" if course_id == 201 else "HighSchool"
                ),
                grade_level=2 if course_id == 201 else 11,
            ),
        )

    service = RetrievalService(embedder=embedder, store=store)
    all_sentinels = {sentinel for _, _, _, sentinel in COURSES}
    for course_id, _, _, expected_sentinel in COURSES:
        result = service.retrieve(
            RetrievalRequest(
                question="Bu derste süreklilik doğrulama kodu nedir?",
                course_id=course_id,
                top_k=3,
            )
        )
        combined_text = " ".join(item.text for item in result.results)
        assert expected_sentinel in combined_text
        assert all(
            item.metadata.course_id == course_id for item in result.results
        )
        assert not (all_sentinels - {expected_sentinel}) & set(
            combined_text.split()
        )


def test_http_retrieve_runs_real_bge_m3_and_sqlite_end_to_end(
    tmp_path,
    monkeypatch,
):
    store = SQLiteVectorStore(tmp_path / "http-vectors.db")
    embedder = OllamaEmbeddingClient()
    pipeline = DocumentPipeline(embedder=embedder, store=store)

    for course_id, material_id, filename, _ in COURSES[:2]:
        path = FIXTURES / filename
        pipeline.process(
            content=path.read_bytes(),
            filename=filename,
            material_id=material_id,
            context=AgentContext(
                course_id=course_id,
                education_level="HighSchool",
                grade_level=11,
            ),
        )

    monkeypatch.setattr(api_main.retrieval_service, "store", store)
    monkeypatch.setattr(api_main.retrieval_service, "embedder", embedder)

    with TestClient(api_main.app) as client:
        response = client.post(
            "/rag/retrieve",
            json={
                "question": "Bu derste süreklilik doğrulama kodu nedir?",
                "courseId": 101,
                "topK": 3,
            },
        )

    assert response.status_code == 200
    payload = response.json()
    assert payload["course_id"] == 101
    assert payload["results"]
    assert all(
        result["metadata"]["course_id"] == 101
        for result in payload["results"]
    )
    combined_text = " ".join(result["text"] for result in payload["results"])
    assert "HS_A_ONLY_9F31" in combined_text
    assert "HS_B_ONLY_8C72" not in combined_text


def test_real_bge_m3_ranks_multiple_chunks_within_same_course(tmp_path):
    store = SQLiteVectorStore(tmp_path / "top-k-vectors.db")
    embedder = OllamaEmbeddingClient()
    course_id = 301
    source_texts = [
        (
            "PHOTOSYNTHESIS_TOP_41A",
            "Fotosentez sırasında bitkiler ışık enerjisini kimyasal enerjiye "
            "dönüştürür ve glikoz üretir.",
        ),
        (
            "HISTORY_OTHER_72B",
            "Osmanlı Devleti'nin kuruluş dönemi ve ilk hükümdarları tarih "
            "dersinin konusudur.",
        ),
        (
            "MATH_OTHER_93C",
            "İkinci dereceden denklemlerin kökleri diskriminant yardımıyla "
            "hesaplanır.",
        ),
    ]
    embeddings = embedder.embed([text for _, text in source_texts])
    store.add([
        ProcessedChunk(
            text=f"{marker} {text}",
            metadata=ChunkMetadata(
                course_id=course_id,
                education_level="HighSchool",
                grade_level=11,
                material_id=3000 + index,
                page_number=1,
                chunk_id=f"{3000 + index}:1",
            ),
            embedding=embedding,
        )
        for index, ((marker, text), embedding) in enumerate(
            zip(source_texts, embeddings, strict=True),
            start=1,
        )
    ])

    result = RetrievalService(embedder=embedder, store=store).retrieve(
        RetrievalRequest(
            question=(
                "Bitkiler fotosentez sırasında ışık enerjisini nasıl kullanır?"
            ),
            course_id=course_id,
            top_k=2,
        )
    )

    assert len(result.results) == 2
    assert "PHOTOSYNTHESIS_TOP_41A" in result.results[0].text
    assert result.results[0].score >= result.results[1].score
    assert all(
        item.metadata.course_id == course_id for item in result.results
    )
