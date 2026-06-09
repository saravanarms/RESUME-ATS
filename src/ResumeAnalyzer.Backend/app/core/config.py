import os
from pydantic_settings import BaseSettings, SettingsConfigDict
from typing import Optional

class Settings(BaseSettings):
    PROJECT_NAME: str = "AI Resume Analyzer Backend"
    API_V1_STR: str = "/api/v1"
    
    # Security / JWT
    # In production, this must be a secure randomly generated secret
    JWT_SECRET: str = "3aef34f19b88cd26e8524a87268d876d75c7429671d603a11b6d573d8a54d5ab"
    JWT_ALGORITHM: str = "HS256"
    ACCESS_TOKEN_EXPIRE_MINUTES: int = 60 * 24 * 7  # 7 days
    
    # Azure Cloud Storage (Optional for backend if the backend gets files uploaded directly or parses from raw payload)
    AZURE_STORAGE_CONNECTION_STRING: Optional[str] = None
    AZURE_CONTAINER_NAME: str = "resumes"
    
    # Rate Limiting
    RATE_LIMIT_REQUESTS: int = 100
    RATE_LIMIT_WINDOW_SECONDS: int = 60
    
    model_config = SettingsConfigDict(
        env_file=".env",
        env_file_encoding="utf-8",
        extra="ignore"
    )

settings = Settings()
