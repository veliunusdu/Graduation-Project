import json
import math
import sqlite3
from pathlib import Path
from threading import RLock
from typing import Protocol

from config.settings import settings
from schemas.document import ProcessedChunk, RetrievedChunk


class VectorStoreError(RuntimeError):
    """Raised when persistent vector storage cannot complete an operation."""


class VectorStore(Protocol):
    def add(self, chunks: list[ProcessedChunk]) -> None:
        """Accept embedded chunks for persistence by the vector-store adapter."""

    def search(
        self,
        *,
        course_id: int,
        query_embedding: list[float],
        top_k: int,
    ) -> list[RetrievedChunk]:
        """Return only the most similar chunks belonging to one course."""


class InMemoryVectorStore:
    """Small test adapter implementing the same contract as the persistent store."""

    def __init__(self):
        self._chunks: dict[str, ProcessedChunk] = {}
        self._lock = RLock()

    def add(self, chunks: list[ProcessedChunk]) -> None:
        material_ids = {chunk.metadata.material_id for chunk in chunks}
        with self._lock:
            self._chunks = {
                chunk_id: existing
                for chunk_id, existing in self._chunks.items()
                if existing.metadata.material_id not in material_ids
            }
            for chunk in chunks:
                self._chunks[chunk.metadata.chunk_id] = chunk

    def search(
        self,
        *,
        course_id: int,
        query_embedding: list[float],
        top_k: int,
    ) -> list[RetrievedChunk]:
        _validate_search(course_id, query_embedding, top_k)
        with self._lock:
            candidates = [
                chunk
                for chunk in self._chunks.values()
                if chunk.metadata.course_id == course_id
            ]
        return _rank_chunks(candidates, query_embedding, top_k)

    def get_for_material(self, material_id: int) -> list[ProcessedChunk]:
        with self._lock:
            return [
                chunk
                for chunk in self._chunks.values()
                if chunk.metadata.material_id == material_id
            ]

    def clear(self) -> None:
        with self._lock:
            self._chunks.clear()


class SQLiteVectorStore:
    """Persistent SQLite vector store with course-scoped cosine retrieval."""

    def __init__(self, database_path: str | Path):
        self.database_path = Path(database_path)
        self.database_path.parent.mkdir(parents=True, exist_ok=True)
        self._lock = RLock()
        self._initialize()

    def _connect(self) -> sqlite3.Connection:
        connection = sqlite3.connect(self.database_path, timeout=30)
        connection.row_factory = sqlite3.Row
        return connection

    def _initialize(self) -> None:
        try:
            with self._connect() as connection:
                connection.execute("PRAGMA journal_mode=WAL")
                connection.execute(
                    """
                    CREATE TABLE IF NOT EXISTS vector_chunks (
                        chunk_id TEXT PRIMARY KEY,
                        course_id INTEGER NOT NULL,
                        education_level TEXT NOT NULL,
                        grade_level INTEGER NOT NULL,
                        material_id INTEGER NOT NULL,
                        page_number INTEGER NOT NULL,
                        text TEXT NOT NULL,
                        embedding TEXT NOT NULL,
                        embedding_dimensions INTEGER NOT NULL
                    )
                    """
                )
                connection.execute(
                    """
                    CREATE INDEX IF NOT EXISTS ix_vector_chunks_course_id
                    ON vector_chunks(course_id)
                    """
                )
                connection.execute(
                    """
                    CREATE INDEX IF NOT EXISTS ix_vector_chunks_material_id
                    ON vector_chunks(material_id)
                    """
                )
        except sqlite3.Error as exc:
            raise VectorStoreError("Could not initialize the vector database.") from exc

    def add(self, chunks: list[ProcessedChunk]) -> None:
        if not chunks:
            return
        for chunk in chunks:
            _validate_embedding(chunk.embedding)

        material_ids = sorted({chunk.metadata.material_id for chunk in chunks})
        placeholders = ",".join("?" for _ in material_ids)
        rows = [
            (
                chunk.metadata.chunk_id,
                chunk.metadata.course_id,
                chunk.metadata.education_level,
                chunk.metadata.grade_level,
                chunk.metadata.material_id,
                chunk.metadata.page_number,
                chunk.text,
                json.dumps(chunk.embedding, separators=(",", ":")),
                len(chunk.embedding),
            )
            for chunk in chunks
        ]

        try:
            with self._lock, self._connect() as connection:
                connection.execute(
                    f"DELETE FROM vector_chunks WHERE material_id IN ({placeholders})",
                    material_ids,
                )
                connection.executemany(
                    """
                    INSERT INTO vector_chunks (
                        chunk_id, course_id, education_level, grade_level,
                        material_id, page_number, text, embedding,
                        embedding_dimensions
                    ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
                    """,
                    rows,
                )
        except sqlite3.Error as exc:
            raise VectorStoreError("Could not persist document embeddings.") from exc

    def search(
        self,
        *,
        course_id: int,
        query_embedding: list[float],
        top_k: int,
    ) -> list[RetrievedChunk]:
        _validate_search(course_id, query_embedding, top_k)
        try:
            with self._connect() as connection:
                rows = connection.execute(
                    """
                    SELECT chunk_id, course_id, education_level, grade_level,
                           material_id, page_number, text, embedding,
                           embedding_dimensions
                    FROM vector_chunks
                    WHERE course_id = ?
                    """,
                    (course_id,),
                ).fetchall()
        except sqlite3.Error as exc:
            raise VectorStoreError("Could not search document embeddings.") from exc

        candidates = []
        for row in rows:
            embedding = json.loads(row["embedding"])
            if row["embedding_dimensions"] != len(query_embedding):
                continue
            candidates.append(
                ProcessedChunk(
                    text=row["text"],
                    metadata={
                        "course_id": row["course_id"],
                        "education_level": row["education_level"],
                        "grade_level": row["grade_level"],
                        "material_id": row["material_id"],
                        "page_number": row["page_number"],
                        "chunk_id": row["chunk_id"],
                    },
                    embedding=embedding,
                )
            )
        return _rank_chunks(candidates, query_embedding, top_k)

    def get_for_material(self, material_id: int) -> list[ProcessedChunk]:
        try:
            with self._connect() as connection:
                rows = connection.execute(
                    """
                    SELECT chunk_id, course_id, education_level, grade_level,
                           material_id, page_number, text, embedding
                    FROM vector_chunks
                    WHERE material_id = ?
                    ORDER BY chunk_id
                    """,
                    (material_id,),
                ).fetchall()
        except sqlite3.Error as exc:
            raise VectorStoreError("Could not read material embeddings.") from exc
        return [
            ProcessedChunk(
                text=row["text"],
                metadata={
                    "course_id": row["course_id"],
                    "education_level": row["education_level"],
                    "grade_level": row["grade_level"],
                    "material_id": row["material_id"],
                    "page_number": row["page_number"],
                    "chunk_id": row["chunk_id"],
                },
                embedding=json.loads(row["embedding"]),
            )
            for row in rows
        ]

    def count(self) -> int:
        try:
            with self._connect() as connection:
                return connection.execute(
                    "SELECT COUNT(*) FROM vector_chunks"
                ).fetchone()[0]
        except sqlite3.Error as exc:
            raise VectorStoreError("Could not count document embeddings.") from exc


def _validate_embedding(embedding: list[float]) -> None:
    if not embedding or any(
        not isinstance(value, (int, float))
        or isinstance(value, bool)
        or not math.isfinite(value)
        for value in embedding
    ):
        raise ValueError("Embedding must be a non-empty finite numeric vector.")


def _validate_search(
    course_id: int,
    query_embedding: list[float],
    top_k: int,
) -> None:
    if course_id <= 0:
        raise ValueError("course_id must be positive.")
    if top_k <= 0:
        raise ValueError("top_k must be positive.")
    _validate_embedding(query_embedding)


def _cosine_similarity(left: list[float], right: list[float]) -> float:
    if len(left) != len(right):
        raise ValueError("Embedding dimensions must match.")
    dot_product = sum(a * b for a, b in zip(left, right, strict=True))
    left_norm = math.sqrt(sum(value * value for value in left))
    right_norm = math.sqrt(sum(value * value for value in right))
    if left_norm == 0 or right_norm == 0:
        return 0.0
    return dot_product / (left_norm * right_norm)


def _rank_chunks(
    chunks: list[ProcessedChunk],
    query_embedding: list[float],
    top_k: int,
) -> list[RetrievedChunk]:
    ranked = [
        RetrievedChunk(
            text=chunk.text,
            score=_cosine_similarity(query_embedding, chunk.embedding),
            metadata=chunk.metadata,
        )
        for chunk in chunks
        if len(chunk.embedding) == len(query_embedding)
    ]
    ranked.sort(key=lambda item: (-item.score, item.metadata.chunk_id))
    return ranked[:top_k]


vector_store = SQLiteVectorStore(settings.VECTOR_DB_PATH)
