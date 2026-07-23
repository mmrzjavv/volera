"""Abstract embedding store interface."""
from abc import ABC, abstractmethod
from typing import Any

from models.embeddings import EmbeddingQueryResult


class EmbeddingStore(ABC):
    """Interface for storing and querying embeddings."""

    @abstractmethod
    async def create(self, id: str, vector: list[float], text: str | None = None, metadata: dict[str, Any] | None = None) -> None:
        """Store an embedding by id."""
        ...

    @abstractmethod
    async def query(
        self,
        vector: list[float],
        top_k: int = 5,
    ) -> list[EmbeddingQueryResult]:
        """Return top_k nearest neighbors."""
        ...

    async def query_for_tenant(
        self,
        tenant_id: str,
        vector: list[float],
        top_k: int = 5,
    ) -> list[EmbeddingQueryResult]:
        """Return top_k nearest neighbors among items for this tenant. Override in subclass."""
        return await self.query(vector, top_k)

    @abstractmethod
    async def reembed(self, id: str, vector: list[float], text: str | None = None, metadata: dict[str, Any] | None = None) -> None:
        """Update existing embedding by id."""
        ...

    @abstractmethod
    async def get(self, id: str) -> tuple[list[float], str | None, dict[str, Any]] | None:
        """Get stored vector, text, metadata by id. Returns None if not found."""
        ...

    @abstractmethod
    async def delete(self, id: str) -> bool:
        """Remove embedding by id. Returns True if existed."""
        ...
