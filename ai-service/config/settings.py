from pydantic_settings import BaseSettings

class Settings(BaseSettings):
    APP_NAME: str = "AI-Supported Homework Platform - AI Service"
    OLLAMA_BASE_URL: str = "http://localhost:11434"
    
    class Config:
        env_file = ".env"

settings = Settings()
