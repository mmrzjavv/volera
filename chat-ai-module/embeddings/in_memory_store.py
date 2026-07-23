"""In-memory embedding store with simple cosine similarity."""
import math
from typing import Any

from models.embeddings import EmbeddingQueryResult

from embeddings.store import EmbeddingStore


def _cosine_sim(a: list[float], b: list[float]) -> float:
    """Cosine similarity between two vectors."""
    if not a or not b or len(a) != len(b):
        return 0.0
    dot = sum(x * y for x, y in zip(a, b))
    na = math.sqrt(sum(x * x for x in a))
    nb = math.sqrt(sum(x * x for x in b))
    if na == 0 or nb == 0:
        return 0.0
    return dot / (na * nb)


class InMemoryEmbeddingStore(EmbeddingStore):
    """Store (id, vector, text, metadata) in memory; query by vector with cosine similarity."""

    def __init__(self) -> None:
        self._items: dict[str, tuple[list[float], str | None, dict[str, Any]]] = {}

    async def create(
        self,
        id: str,
        vector: list[float],
        text: str | None = None,
        metadata: dict[str, Any] | None = None,
    ) -> None:
        self._items[id] = (vector, text, metadata or {})

    async def query(
        self,
        vector: list[float],
        top_k: int = 5,
    ) -> list[EmbeddingQueryResult]:
        return await self._query_impl(vector, top_k, metadata_filter=None)

    async def query_for_tenant(
        self,
        tenant_id: str,
        vector: list[float],
        top_k: int = 5,
    ) -> list[EmbeddingQueryResult]:
        return await self._query_impl(vector, top_k, metadata_filter={"tenant_id": tenant_id})

    async def _query_impl(
        self,
        vector: list[float],
        top_k: int,
        metadata_filter: dict[str, Any] | None,
    ) -> list[EmbeddingQueryResult]:
        if not self._items:
            return []
        scored: list[tuple[str, float, str | None, dict[str, Any]]] = []
        for id, (v, text, meta) in self._items.items():
            if metadata_filter:
                if not all(meta.get(k) == v for k, v in metadata_filter.items()):
                    continue
            score = _cosine_sim(vector, v)
            scored.append((id, score, text, meta))
        scored.sort(key=lambda x: -x[1])
        return [
            EmbeddingQueryResult(id=s[0], score=s[1], metadata=s[3], text=s[2])
            for s in scored[:top_k]
        ]

    async def get(
        self, id: str
    ) -> tuple[list[float], str | None, dict[str, Any]] | None:
        if id not in self._items:
            return None
        v, text, meta = self._items[id]
        return (v, text, meta)

    async def reembed(
        self,
        id: str,
        vector: list[float],
        text: str | None = None,
        metadata: dict[str, Any] | None = None,
    ) -> None:
        if id not in self._items:
            raise KeyError(id)
        _, old_text, old_meta = self._items[id]
        self._items[id] = (
            vector,
            text if text is not None else old_text,
            metadata if metadata is not None else old_meta,
        )

    async def delete(self, id: str) -> bool:
        if id in self._items:
            del self._items[id]
            return True
        return False
