from pathlib import Path

import pytest
from fastapi.testclient import TestClient

from api.main import app
from rag.document_processing import (
    DocumentProcessingError,
    ExtractedPage,
    chunk_pages,
    extract_text,
)
from rag.embedding import EmbeddingError, OllamaEmbeddingClient
from rag.pipeline import DocumentPipeline
from rag.vector_store import InMemoryVectorStore
from schemas.context import AgentContext
from schemas.document import (
    ChunkMetadata,
    DocumentProcessingResult,
    ProcessedChunk,
)


FIXTURES = Path(__file__).resolve().parents[2] / "samples" / "ai-rag-smoke"
EXPECTED_MARKERS = [
    "SMOKE_EXTRACT_ALPHA_20261011",
    "The reference value is 42.",
    "SMOKE_EXTRACT_OMEGA_20261011",
    "Türkçe karakterler: öğrenci, üniversite, ışık, ölçüm.",
]
client = TestClient(app)


@pytest.mark.parametrize("extension", ["pdf", "docx", "pptx", "txt"])
def test_extracts_supported_documents(extension):
    path = FIXTURES / "valid" / f"extraction.{extension}"

    pages = extract_text(path.read_bytes(), path.name)
    text = "\n".join(page.text for page in pages)

    assert all(marker in text for marker in EXPECTED_MARKERS)
    assert all(page.page_number >= 1 for page in pages)


@pytest.mark.parametrize("extension", ["pdf", "docx", "pptx", "txt"])
def test_rejects_empty_documents(extension):
    path = FIXTURES / "zero-byte" / f"empty.{extension}"

    with pytest.raises(DocumentProcessingError, match="empty"):
        extract_text(path.read_bytes(), path.name)


@pytest.mark.parametrize(
    "relative_path",
    [
        "empty-content/blank.pdf",
        "empty-content/blank.docx",
        "empty-content/blank.pptx",
        "empty-content/whitespace.txt",
    ],
)
def test_rejects_documents_without_extractable_text(relative_path):
    path = FIXTURES / relative_path

    with pytest.raises(DocumentProcessingError, match="no extractable text"):
        extract_text(path.read_bytes(), path.name)


@pytest.mark.parametrize(
    "relative_path",
    [
        "corrupt/truncated.pdf",
        "corrupt/truncated.docx",
        "corrupt/truncated.pptx",
        "corrupt/invalid-utf8.txt",
    ],
)
def test_rejects_corrupt_documents(relative_path):
    path = FIXTURES / relative_path

    with pytest.raises(DocumentProcessingError):
        extract_text(path.read_bytes(), path.name)


def test_rejects_unsupported_extension():
    with pytest.raises(DocumentProcessingError, match="Unsupported"):
        extract_text(b"not a supported document", "material.bin")


def test_chunks_clean_text_with_overlap_and_page_number():
    pages = [ExtractedPage(3, "alpha    beta gamma delta epsilon")]

    chunks = chunk_pages(pages, chunk_size=18, overlap=5)

    assert len(chunks) >= 2
    assert all(chunk.page_number == 3 for chunk in chunks)
    assert all("    " not in chunk.text for chunk in chunks)
    assert chunks[1].text.split()[0] == chunks[0].text.split()[-1]


@pytest.mark.parametrize(
    ("chunk_size", "overlap"),
    [(0, 0), (10, -1), (10, 10), (10, 11)],
)
def test_rejects_invalid_chunk_configuration(chunk_size, overlap):
    with pytest.raises(ValueError):
        chunk_pages(
            [ExtractedPage(1, "alpha beta")],
            chunk_size=chunk_size,
            overlap=overlap,
        )


class FakeEmbedder:
    model = "bge-m3"

    def embed(self, texts):
        return [[float(index), 0.5] for index, _ in enumerate(texts)]


class RecordingStore:
    def __init__(self):
        self.chunks = []

    def add(self, chunks):
        self.chunks.extend(chunks)


def test_pipeline_adds_required_metadata_and_forwards_embeddings():
    path = FIXTURES / "valid" / "extraction.txt"
    store = RecordingStore()
    pipeline = DocumentPipeline(embedder=FakeEmbedder(), store=store)

    result = pipeline.process(
        content=path.read_bytes(),
        filename=path.name,
        material_id=41,
        context=AgentContext(
            course_id=7,
            education_level="HighSchool",
            grade_level=11,
        ),
    )

    assert result.chunk_count == len(store.chunks) > 0
    assert result.embedding_model == "bge-m3"
    assert result.embedding_dimensions == 2
    metadata = store.chunks[0].metadata
    assert metadata.model_dump() == {
        "course_id": 7,
        "education_level": "HighSchool",
        "grade_level": 11,
        "material_id": 41,
        "page_number": 1,
        "chunk_id": "41:1",
    }


def test_document_endpoint_returns_pipeline_result(monkeypatch):
    expected = DocumentProcessingResult(
        material_id=41,
        chunk_count=1,
        embedding_model="bge-m3",
        embedding_dimensions=2,
        chunks=[{
            "course_id": 7,
            "education_level": "University",
            "grade_level": 2,
            "material_id": 41,
            "page_number": 1,
            "chunk_id": "41:1",
        }],
    )

    calls = {}

    def process(**kwargs):
        calls["content"] = kwargs["content"]
        return expected

    async def run_in_threadpool(function, **kwargs):
        calls["offloaded"] = True
        return function(**kwargs)

    monkeypatch.setattr("api.main.document_pipeline.process", process)
    monkeypatch.setattr("api.main.run_in_threadpool", run_in_threadpool)
    response = client.post(
        "/documents/process",
        files={"file": ("material.txt", b"example", "text/plain")},
        data={
            "material_id": "41",
            "course_id": "7",
            "education_level": "University",
            "grade_level": "2",
        },
    )

    assert response.status_code == 200
    assert response.json()["chunks"][0]["course_id"] == 7
    assert calls == {"content": b"example", "offloaded": True}


def test_document_endpoint_rejects_file_over_configured_limit(monkeypatch):
    monkeypatch.setattr("api.main.settings.DOCUMENT_MAX_FILE_SIZE_BYTES", 4)

    response = client.post(
        "/documents/process",
        files={"file": ("material.txt", b"12345", "text/plain")},
        data={
            "material_id": "41",
            "course_id": "7",
            "education_level": "University",
            "grade_level": "2",
        },
    )

    assert response.status_code == 413
    assert response.json()["detail"] == (
        "Document exceeds the configured file size limit."
    )


def test_document_endpoint_maps_embedding_failure_to_bad_gateway(monkeypatch):
    def fail(**kwargs):
        raise EmbeddingError("model unavailable")

    monkeypatch.setattr("api.main.document_pipeline.process", fail)
    response = client.post(
        "/documents/process",
        files={"file": ("material.txt", b"example", "text/plain")},
        data={
            "material_id": "41",
            "course_id": "7",
            "education_level": "HighSchool",
            "grade_level": "10",
        },
    )

    assert response.status_code == 502
    assert response.json()["detail"] == "model unavailable"


def test_embedding_client_uses_configured_model(monkeypatch):
    captured = {}

    class Response:
        def raise_for_status(self):
            return None

        def json(self):
            return {"embeddings": [[0.1, 0.2]]}

    def post(url, json, timeout):
        captured.update(url=url, payload=json, timeout=timeout)
        return Response()

    monkeypatch.setattr("rag.embedding.requests.post", post)
    embedder = OllamaEmbeddingClient()

    assert embedder.embed(["sample"]) == [[0.1, 0.2]]
    assert captured["payload"] == {"model": "bge-m3", "input": ["sample"]}


@pytest.mark.parametrize(
    ("texts", "payload", "message"),
    [
        (["one"], {"embeddings": []}, "invalid number"),
        (["one"], {"embeddings": [[True, 0.2]]}, "invalid embedding vector"),
        (["one"], {"embeddings": [[float("nan"), 0.2]]}, "invalid embedding vector"),
        (
            ["one", "two"],
            {"embeddings": [[0.1, 0.2], [0.3]]},
            "inconsistent embedding dimensions",
        ),
    ],
)
def test_embedding_client_rejects_invalid_vectors(
    monkeypatch, texts, payload, message
):
    class Response:
        def raise_for_status(self):
            return None

        def json(self):
            return payload

    monkeypatch.setattr(
        "rag.embedding.requests.post",
        lambda *args, **kwargs: Response(),
    )

    with pytest.raises(EmbeddingError, match=message):
        OllamaEmbeddingClient().embed(texts)


def test_in_memory_store_replaces_all_chunks_when_material_is_reprocessed():
    def chunk(chunk_id):
        return ProcessedChunk(
            text=chunk_id,
            metadata=ChunkMetadata(
                course_id=7,
                education_level="University",
                grade_level=2,
                material_id=41,
                page_number=1,
                chunk_id=chunk_id,
            ),
            embedding=[0.1, 0.2],
        )

    store = InMemoryVectorStore()
    store.add([chunk("41:1"), chunk("41:2"), chunk("41:3")])
    store.add([chunk("41:1")])

    assert [item.metadata.chunk_id for item in store.get_for_material(41)] == [
        "41:1"
    ]
