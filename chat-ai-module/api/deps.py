"""FastAPI dependencies: build agent, embedding store, TTS, STT from config."""
from functools import lru_cache

from embeddings import get_embedder, InMemoryEmbeddingStore
from speech import get_tts, get_stt
from agent.reasoning.llm import get_llm


@lru_cache
def get_embedding_store() -> InMemoryEmbeddingStore:
    """Singleton in-memory embedding store."""
    return InMemoryEmbeddingStore()


def get_embedder_dep():
    """Embedder for embeddings API (create, query, reembed)."""
    return get_embedder()


def get_llm_dep():
    """LLM (Ollama or OpenAI from config)."""
    return get_llm()


def get_tts_dep():
    """TTS provider."""
    return get_tts()


def get_stt_dep():
    """STT provider."""
    return get_stt()
