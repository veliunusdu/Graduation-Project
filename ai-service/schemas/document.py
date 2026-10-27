from typing import Literal

from pydantic import (
    AliasChoices,
    BaseModel,
    ConfigDict,
    Field,
    StrictInt,
    field_validator,
)


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


class RetrievalRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")

    question: str = Field(min_length=1, max_length=4000)
    course_id: StrictInt = Field(
        gt=0,
        validation_alias=AliasChoices("course_id", "courseId"),
    )
    top_k: StrictInt = Field(
        default=5,
        ge=1,
        le=20,
        validation_alias=AliasChoices("top_k", "topK"),
    )

    @field_validator("question")
    @classmethod
    def question_must_contain_text(cls, value: str) -> str:
        value = value.strip()
        if not value:
            raise ValueError("question must not be blank.")
        return value


class RetrievedChunk(BaseModel):
    text: str
    score: float
    metadata: ChunkMetadata


class RetrievalResult(BaseModel):
    question: str
    course_id: StrictInt
    top_k: StrictInt
    results: list[RetrievedChunk]
