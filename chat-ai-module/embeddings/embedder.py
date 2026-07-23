"""Abstract embedder: Ollama and OpenAI implementations."""
from abc import ABC, abstractmethod
import httpx

from config import get_settings


class BaseEmbedder(ABC):
    """Interface: embed texts to vectors."""

    @abstractmethod
    async def embed(self, texts: list[str]) -> list[list[float]]:
        """Return one vector per input text."""
        ...


class OllamaEmbedder(BaseEmbedder):
    """Ollama embeddings API (e.g. znbang/bge:large-en-v1.5-f16)."""

    def __init__(self, base_url: str, model: str) -> None:
        self._base_url = base_url.rstrip("/")
        self._model = model

    async def embed(self, texts: list[str]) -> list[list[float]]:
        """Embed via Ollama /api/embeddings (one prompt per request)."""
        if not texts:
            return []
        vectors = []
        async with httpx.AsyncClient(timeout=60.0) as client:
            for text in texts:
                r = await client.post(
                    f"{self._base_url}/api/embeddings",
                    json={"model": self._model, "prompt": text},
                )
                r.raise_for_status()
                data = r.json()
                vectors.append(data.get("embedding", []))
        return vectors


class OpenAIEmbedder(BaseEmbedder):
    """OpenAI embeddings API."""

    def __init__(self, api_key: str, model: str) -> None:
        from openai import AsyncOpenAI
        self._client = AsyncOpenAI(api_key=api_key)
        self._model = model

    async def embed(self, texts: list[str]) -> list[list[float]]:
        """Embed via OpenAI API."""
        if not texts:
            return []
        r = await self._client.embeddings.create(input=texts, model=self._model)
        order = sorted(r.data, key=lambda x: x.index)
        return [item.embedding for item in order]


def get_embedder() -> BaseEmbedder:
    """Build embedder from config (Ollama or OpenAI)."""
    settings = get_settings()
    if getattr(settings, "embedding_provider", "ollama") == "openai":
        return OpenAIEmbedder(
            api_key=settings.openai_api_key,
            model=settings.embedding_model,
        )
    return OllamaEmbedder(
        base_url=settings.ollama_base_url,
        model=getattr(settings, "ollama_embedding_model", "znbang/bge:large-en-v1.5-f16"),
    )
