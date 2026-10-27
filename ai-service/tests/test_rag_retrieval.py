from fastapi.testclient import TestClient

from api.main import app
from rag.pipeline import DocumentPipeline
from rag.retrieval import RetrievalService
from rag.vector_store import SQLiteVectorStore
from schemas.context import AgentContext
from schemas.document import (
    ChunkMetadata,
    ProcessedChunk,
    RetrievalRequest,
    RetrievalResult,
)


client = TestClient(app)


def make_chunk(
    chunk_id: str,
    *,
    course_id: int,
    material_id: int,
    text: str,
    embedding: list[float],
) -> ProcessedChunk:
    return ProcessedChunk(
        text=text,
        metadata=ChunkMetadata(
            course_id=course_id,
            education_level="HighSchool",
            grade_level=11,
            material_id=material_id,
            page_number=1,
            chunk_id=chunk_id,
        ),
        embedding=embedding,
    )


def test_sqlite_store_persists_chunks_after_reopening(tmp_path):
    database = tmp_path / "vectors.db"
    first_store = SQLiteVectorStore(database)
    first_store.add([
        make_chunk(
            "41:1",
            course_id=7,
            material_id=41,
            text="persistent source",
            embedding=[1.0, 0.0],
        )
    ])

    reopened_store = SQLiteVectorStore(database)
    stored = reopened_store.get_for_material(41)

    assert len(stored) == 1
    assert stored[0].text == "persistent source"
    assert stored[0].metadata.course_id == 7


def test_sqlite_store_is_idempotent_and_removes_stale_material_chunks(tmp_path):
    store = SQLiteVectorStore(tmp_path / "vectors.db")
    original = [
        make_chunk(
            f"41:{index}",
            course_id=7,
            material_id=41,
            text=f"version one {index}",
            embedding=[1.0, float(index)],
        )
        for index in range(1, 4)
    ]

    store.add(original)
    store.add(original)
    assert store.count() == 3

    store.add([
        make_chunk(
            "41:1",
            course_id=7,
            material_id=41,
            text="version two",
            embedding=[1.0, 0.0],
        )
    ])

    stored = store.get_for_material(41)
    assert store.count() == 1
    assert [(chunk.metadata.chunk_id, chunk.text) for chunk in stored] == [
        ("41:1", "version two")
    ]


def test_similarity_search_enforces_course_filter_before_ranking(tmp_path):
    store = SQLiteVectorStore(tmp_path / "vectors.db")
    store.add([
        make_chunk(
            "101:1",
            course_id=101,
            material_id=101,
            text="HS_A_ONLY_9F31 relevant",
            embedding=[1.0, 0.0],
        ),
        make_chunk(
            "101:2",
            course_id=101,
            material_id=101,
            text="less relevant",
            embedding=[0.6, 0.8],
        ),
        make_chunk(
            "102:1",
            course_id=102,
            material_id=102,
            text="HS_B_ONLY_8C72 would score highest",
            embedding=[1.0, 0.0],
        ),
        make_chunk(
            "201:1",
            course_id=201,
            material_id=201,
            text="UNI_ONLY_7D64 forbidden",
            embedding=[1.0, 0.0],
        ),
    ])

    results = store.search(
        course_id=101,
        query_embedding=[1.0, 0.0],
        top_k=2,
    )

    assert [result.metadata.chunk_id for result in results] == ["101:1", "101:2"]
    assert all(result.metadata.course_id == 101 for result in results)
    assert results[0].score > results[1].score
    assert "HS_B_ONLY_8C72" not in " ".join(result.text for result in results)
    assert "UNI_ONLY_7D64" not in " ".join(result.text for result in results)


def test_similarity_search_respects_top_k_and_returns_source_metadata(tmp_path):
    store = SQLiteVectorStore(tmp_path / "vectors.db")
    store.add([
        make_chunk(
            f"41:{index}",
            course_id=7,
            material_id=41,
            text=f"source {index}",
            embedding=[1.0, index / 10],
        )
        for index in range(1, 4)
    ])

    results = store.search(
        course_id=7,
        query_embedding=[1.0, 0.0],
        top_k=1,
    )

    assert len(results) == 1
    assert results[0].text == "source 1"
    assert results[0].metadata.model_dump() == {
        "course_id": 7,
        "education_level": "HighSchool",
        "grade_level": 11,
        "material_id": 41,
        "page_number": 1,
        "chunk_id": "41:1",
    }


def test_empty_course_returns_no_results(tmp_path):
    store = SQLiteVectorStore(tmp_path / "vectors.db")

    assert store.search(
        course_id=999,
        query_embedding=[1.0, 0.0],
        top_k=3,
    ) == []


class QueryEmbedder:
    model = "bge-m3"

    def __init__(self):
        self.inputs = []

    def embed(self, texts):
        self.inputs.append(texts)
        return [[1.0, 0.0] for _ in texts]


def test_retrieval_service_embeds_question_and_applies_requested_course(tmp_path):
    store = SQLiteVectorStore(tmp_path / "vectors.db")
    store.add([
        make_chunk(
            "41:1",
            course_id=7,
            material_id=41,
            text="course seven",
            embedding=[1.0, 0.0],
        ),
        make_chunk(
            "42:1",
            course_id=8,
            material_id=42,
            text="course eight",
            embedding=[1.0, 0.0],
        ),
    ])
    embedder = QueryEmbedder()
    service = RetrievalService(embedder=embedder, store=store)

    result = service.retrieve(
        RetrievalRequest(question="  course question  ", course_id=7, top_k=5)
    )

    assert embedder.inputs == [["course question"]]
    assert [item.text for item in result.results] == ["course seven"]
    assert result.course_id == 7


def test_document_pipeline_writes_to_persistent_store(tmp_path):
    store = SQLiteVectorStore(tmp_path / "vectors.db")
    pipeline = DocumentPipeline(embedder=QueryEmbedder(), store=store)

    pipeline.process(
        content=b"Week 7 persistent pipeline text",
        filename="material.txt",
        material_id=41,
        context=AgentContext(
            course_id=7,
            education_level="HighSchool",
            grade_level=11,
        ),
    )

    reopened = SQLiteVectorStore(tmp_path / "vectors.db")
    assert reopened.count() == 1
    assert reopened.get_for_material(41)[0].metadata.course_id == 7


def test_retrieval_endpoint_contract(monkeypatch):
    expected = RetrievalResult(
        question="What is the source?",
        course_id=7,
        top_k=2,
        results=[{
            "text": "source text",
            "score": 0.95,
            "metadata": {
                "course_id": 7,
                "education_level": "University",
                "grade_level": 2,
                "material_id": 41,
                "page_number": 3,
                "chunk_id": "41:2",
            },
        }],
    )
    monkeypatch.setattr(
        "api.main.retrieval_service.retrieve",
        lambda request: expected,
    )

    response = client.post(
        "/rag/retrieve",
        json={
            "question": "What is the source?",
            "courseId": 7,
            "topK": 2,
        },
    )

    assert response.status_code == 200
    assert response.json() == expected.model_dump()


def test_retrieval_endpoint_requires_course_id_and_valid_top_k():
    missing_course = client.post(
        "/rag/retrieve",
        json={"question": "question"},
    )
    invalid_top_k = client.post(
        "/rag/retrieve",
        json={"question": "question", "course_id": 7, "top_k": 21},
    )

    assert missing_course.status_code == 422
    assert invalid_top_k.status_code == 422
