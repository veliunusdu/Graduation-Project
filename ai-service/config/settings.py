from pathlib import Path

from pydantic import Field, HttpUrl, field_validator
from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    """Service configuration; environment variables override the service .env."""

    model_config = SettingsConfigDict(
        env_file=Path(__file__).resolve().parents[1] / ".env",
        env_file_encoding="utf-8-sig",
        extra="ignore",
    )

    APP_NAME: str = "AI-Supported Homework Platform - AI Service"
    OLLAMA_BASE_URL: HttpUrl = "http://localhost:11434"

    OLLAMA_GENERAL_MODEL: str = "qwen3:8b"
    OLLAMA_CODE_REVIEW_MODEL: str = "qwen2.5-coder:7b"
    OLLAMA_EMBEDDING_MODEL: str = "bge-m3"
    OLLAMA_TIMEOUT_SECONDS: float = 120.0
    DOCUMENT_MAX_FILE_SIZE_BYTES: int = Field(default=20 * 1024 * 1024, ge=1)
    DOCUMENT_CHUNK_SIZE: int = 1000
    DOCUMENT_CHUNK_OVERLAP: int = 150
    VECTOR_DB_PATH: Path = (
        Path(__file__).resolve().parents[1] / "data" / "vector_store.db"
    )

    @field_validator("APP_NAME")
    @classmethod
    def validate_app_name(cls, value: str) -> str:
        value = value.strip()
        if not value:
            raise ValueError("APP_NAME must not be blank")
        return value

    @field_validator("VECTOR_DB_PATH")
    @classmethod
    def resolve_vector_db_path(cls, value: Path) -> Path:
        if value.is_absolute():
            return value
        return Path(__file__).resolve().parents[1] / value


settings = Settings()
