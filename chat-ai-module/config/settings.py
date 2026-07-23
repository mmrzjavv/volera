"""Pydantic settings loaded from environment and .env."""
from functools import lru_cache
from typing import Literal

from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    """Application settings. Load from env and .env file."""

    model_config = SettingsConfigDict(
        env_file=".env",
        env_file_encoding="utf-8",
        extra="ignore",
    )

    # LLM provider: "ollama" (default) or "openai"
    llm_provider: Literal["ollama", "openai"] = "ollama"

    # Ollama (active by default)
    ollama_base_url: str = "http://localhost:11434"
    ollama_model: str = "gemma3:4b"

    # OpenAI (inactive until llm_provider=openai)
    openai_api_key: str = ""
    openai_chat_model: str = "gpt-4o"

    # Embeddings: "ollama" (use Ollama embedding model) or "openai"
    embedding_provider: Literal["ollama", "openai"] = "ollama"
    ollama_embedding_model: str = "znbang/bge:large-en-v1.5-f16"
    # OpenAI embedding model (when embedding_provider=openai)
    embedding_model: str = "text-embedding-3-small"

    # Agent behavior
    max_agent_steps: int = 10

    # Log level
    log_level: str = "INFO"

    # Speech (placeholder; future provider names)
    tts_provider: str = "placeholder"
    stt_provider: str = "placeholder"

    # Postgres: same DB as .NET backend (ConnectionStrings:DefaultConnection). For widget RAG.
    database_url: str = "postgresql://root:aJQaDWqyk8KscNi1Bu4D0bJ5@monte-rosa.liara.cloud:30845/postgres"


@lru_cache
def get_settings() -> Settings:
    """Cached settings instance."""
    return Settings()
