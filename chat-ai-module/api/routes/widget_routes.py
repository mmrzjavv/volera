"""Widget API: /embed (for .NET to store in Postgres), /chat (RAG from Postgres). No in-memory store."""
import logging

from fastapi import APIRouter, Depends, HTTPException

from embeddings import get_embedder
from api.deps import get_llm_dep
from agent.core.agent import run_agent
from agent.reasoning.llm_base import BaseLLM
from api.postgres_rag import get_rag_context_for_tenant

logger = logging.getLogger(__name__)

router = APIRouter(tags=["widget"])

OLLAMA_UNAVAILABLE_MSG = (
    "Ollama unavailable. Ensure Ollama is running (ollama serve) and models are pulled: "
    "ollama pull gemma3:4b  and  ollama pull znbang/bge:large-en-v1.5-f16"
)


@router.post("/embed")
async def embed(body: dict) -> dict:
    """
    Return embedding vector for text. .NET backend calls this and stores the vector in AiContentBlocks (Postgres).
    Body: { "text": str } -> { "embedding": list[float] }
    """
    text = (body.get("text") or "").strip()
    try:
        embedder = get_embedder()
        vectors = await embedder.embed([text])
        if not vectors:
            raise HTTPException(status_code=503, detail=OLLAMA_UNAVAILABLE_MSG)
        return {"embedding": vectors[0]}
    except HTTPException:
        raise
    except Exception as e:
        logger.exception("Embed failed: %s", e)
        raise HTTPException(status_code=503, detail=OLLAMA_UNAVAILABLE_MSG) from e


@router.post("/chat")
async def chat(
    body: dict,
    llm: BaseLLM = Depends(get_llm_dep),
) -> dict:
    """
    Chat with RAG context for a tenant. Reads AiContentBlocks from Postgres (same DB as .NET).
    Body: { "tenant_id": str, "message": str, "session_id": str | null }
    Returns: { "Answer": str } for .NET client.
    """
    tenant_id = body.get("tenant_id")
    message = (body.get("message") or "").strip()
    if not tenant_id or not message:
        raise HTTPException(status_code=400, detail="tenant_id and message are required")
    try:
        embedder = get_embedder()
        query_vectors = await embedder.embed([message])
        context = None
        if query_vectors:
            context = await get_rag_context_for_tenant(tenant_id, query_vectors[0], top_k=5)
        result = await run_agent(
            user_message=message,
            conversation_history=None,
            context=context,
            llm=llm,
            use_tools=False,
        )
        return {"Answer": result.response}
    except HTTPException:
        raise
    except Exception as e:
        logger.exception("Widget chat failed: %s", e)
        raise HTTPException(status_code=503, detail=OLLAMA_UNAVAILABLE_MSG) from e
