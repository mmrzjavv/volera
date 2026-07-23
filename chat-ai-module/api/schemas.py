"""API request/response schemas (Pydantic models for routes)."""
from typing import Any

from pydantic import BaseModel, Field


# ---- Agent ----
class ChatRequest(BaseModel):
    """POST /agent/chat body."""

    message: str = Field(..., min_length=1)
    conversation_id: str | None = None
    include_context: bool = False


class ChatResponse(BaseModel):
    """POST /agent/chat response."""

    response: str
    tool_calls: list[dict[str, Any]] | None = None


# ---- Embeddings ----
class EmbeddingCreateRequest(BaseModel):
    """POST /embeddings/create body."""

    id: str
    text: str
    metadata: dict[str, Any] = Field(default_factory=dict)


class EmbeddingReembedRequest(BaseModel):
    """POST /embeddings/reembed body."""

    id: str
    text: str | None = None
    metadata: dict[str, Any] | None = None


class EmbeddingQueryRequest(BaseModel):
    """POST /embeddings/query body."""

    text: str
    top_k: int = Field(default=5, ge=1, le=100)


class EmbeddingQueryResponse(BaseModel):
    """POST /embeddings/query response."""

    results: list[dict[str, Any]]


# ---- Speech ----
class TTSRequestBody(BaseModel):
    """POST /text/to-speech body."""

    text: str = Field(..., min_length=1)


class STTResponseBody(BaseModel):
    """POST /speech/to-text response body."""

    text: str
