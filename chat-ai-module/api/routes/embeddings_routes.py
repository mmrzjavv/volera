"""Embeddings lifecycle: create, query, reembed, delete."""
from fastapi import APIRouter, Depends, HTTPException

from embeddings import BaseEmbedder, InMemoryEmbeddingStore
from embeddings import get_embedder
from api.deps import get_embedding_store
from api.schemas import (
    EmbeddingCreateRequest,
    EmbeddingReembedRequest,
    EmbeddingQueryRequest,
    EmbeddingQueryResponse,
)

router = APIRouter(prefix="/embeddings", tags=["embeddings"])


def _embedder() -> BaseEmbedder:
    return get_embedder()


@router.post("/create")
async def create_embedding(
    body: EmbeddingCreateRequest,
    store: InMemoryEmbeddingStore = Depends(get_embedding_store),
) -> dict:
    """Create and store an embedding for the given text."""
    embedder = _embedder()
    vectors = await embedder.embed([body.text])
    if not vectors:
        raise HTTPException(status_code=500, detail="Embedding failed")
    await store.create(body.id, vectors[0], text=body.text, metadata=body.metadata)
    return {"id": body.id, "status": "created"}


@router.post("/reembed")
async def reembed(
    body: EmbeddingReembedRequest,
    store: InMemoryEmbeddingStore = Depends(get_embedding_store),
) -> dict:
    """Re-embed and update stored document. Uses existing text if text not provided."""
    embedder = _embedder()
    text = body.text
    metadata = body.metadata
    if text is None or metadata is None:
        existing = await store.get(body.id)
        if not existing:
            raise HTTPException(status_code=404, detail="Embedding not found")
        _, existing_text, existing_meta = existing
        if text is None:
            text = existing_text or ""
        if metadata is None:
            metadata = existing_meta
    vectors = await embedder.embed([text])
    if not vectors:
        raise HTTPException(status_code=500, detail="Embedding failed")
    try:
        await store.reembed(body.id, vectors[0], text=text, metadata=metadata)
    except KeyError:
        raise HTTPException(status_code=404, detail="Embedding not found")
    return {"id": body.id, "status": "updated"}


@router.post("/query", response_model=EmbeddingQueryResponse)
async def query_embeddings(
    body: EmbeddingQueryRequest,
    store: InMemoryEmbeddingStore = Depends(get_embedding_store),
) -> EmbeddingQueryResponse:
    """Query embeddings by text; returns top_k nearest results."""
    embedder = _embedder()
    vectors = await embedder.embed([body.text])
    if not vectors:
        return EmbeddingQueryResponse(results=[])
    results = await store.query(vectors[0], top_k=body.top_k)
    return EmbeddingQueryResponse(
        results=[r.model_dump() for r in results]
    )


@router.delete("/{id}")
async def delete_embedding(
    id: str,
    store: InMemoryEmbeddingStore = Depends(get_embedding_store),
) -> dict:
    """Delete embedding by id."""
    found = await store.delete(id)
    if not found:
        raise HTTPException(status_code=404, detail="Embedding not found")
    return {"id": id, "status": "deleted"}
