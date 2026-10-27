from threading import RLock
from typing import Protocol

from schemas.document import ProcessedChunk


class VectorStore(Protocol):
    def add(self, chunks: list[ProcessedChunk]) -> None:
        """Accept embedded chunks for persistence by the vector-store adapter."""


class InMemoryVectorStore:
    """Week 6 adapter boundary; a persistent vector DB replaces this in Week 7."""

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


vector_store = InMemoryVectorStore()
