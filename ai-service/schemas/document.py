from typing import Literal

from pydantic import BaseModel, ConfigDict, StrictInt


class ChunkMetadata(BaseModel):
    model_config = ConfigDict(extra="forbid")

    course_id: StrictInt
    education_level: Literal["HighSchool", "University"]
    grade_level: StrictInt
    material_id: StrictInt
    page_number: StrictInt
    chunk_id: str


class ProcessedChunk(BaseModel):
    text: str
    metadata: ChunkMetadata
    embedding: list[float]


class DocumentProcessingResult(BaseModel):
    material_id: StrictInt
    chunk_count: StrictInt
    embedding_model: str
    embedding_dimensions: StrictInt
    chunks: list[ChunkMetadata]
