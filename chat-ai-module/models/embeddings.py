"""Embedding lifecycle DTOs: create, query, reembed."""
from typing import Any

from pydantic import BaseModel, Field


class EmbeddingDoc(BaseModel):
    """Stored embedding with id, optional text, and metadata."""

    id: str
    text: str | None = None
    metadata: dict[str, Any] = Field(default_factory=dict)


class EmbeddingCreateInput(BaseModel):
    """Input for creating an embedding."""

    id: str
    text: str
    metadata: dict[str, Any] = Field(default_factory=dict)


class EmbeddingReembedInput(BaseModel):
    """Input for re-embedding (update) an existing document."""

    id: str
    text: str | None = None  # if omitted, may use stored text
    metadata: dict[str, Any] | None = None  # if omitted, keep existing


class EmbeddingQueryInput(BaseModel):
    """Input for querying embeddings by text."""

    text: str
    top_k: int = Field(default=5, ge=1, le=100)


class EmbeddingQueryResult(BaseModel):
    """Single hit from embedding query."""

    id: str
    score: float
    metadata: dict[str, Any] = Field(default_factory=dict)
    text: str | None = None
