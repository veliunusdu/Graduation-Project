from pathlib import Path

from pydantic import HttpUrl, field_validator
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
    OLLAMA_TIMEOUT_SECONDS: float = 120.0

    @field_validator("APP_NAME")
    @classmethod
    def validate_app_name(cls, value: str) -> str:
        value = value.strip()
        if not value:
            raise ValueError("APP_NAME must not be blank")
        return value


settings = Settings()
