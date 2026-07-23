"""Embeddings: store interface, embedder, and in-memory implementation."""
from embeddings.store import EmbeddingStore
from embeddings.embedder import BaseEmbedder, get_embedder
from embeddings.in_memory_store import InMemoryEmbeddingStore

__all__ = [
    "EmbeddingStore",
    "BaseEmbedder",
    "get_embedder",
    "InMemoryEmbeddingStore",
]
