"""Read company content from Postgres (AiContentBlocks) for RAG. No in-memory store."""
import json
import logging
import math
from typing import Any

import asyncpg

from config import get_settings

logger = logging.getLogger(__name__)


def _cosine_sim(a: list[float], b: list[float]) -> float:
    if not a or not b or len(a) != len(b):
        return 0.0
    dot = sum(x * y for x, y in zip(a, b))
    na = math.sqrt(sum(x * x for x in a))
    nb = math.sqrt(sum(x * x for x in b))
    if na == 0 or nb == 0:
        return 0.0
    return dot / (na * nb)


async def get_rag_context_for_tenant(tenant_id: str, query_embedding: list[float], top_k: int = 5) -> str | None:
    """
    Read AiContentBlocks from Postgres for this tenant (join CompanyAiWidgets by TenantId).
    Status must be Completed (2). Compute cosine similarity with query_embedding, return top_k contents joined.
    """
    settings = get_settings()
    if not settings.database_url:
        logger.warning("DATABASE_URL not set; widget RAG will have no context from Postgres")
        return None
    try:
        conn = await asyncpg.connect(settings.database_url)
        try:
            rows = await conn.fetch(
                """
                SELECT b."Content", b."EmbeddingJson"
                FROM "AiContentBlocks" b
                JOIN "CompanyAiWidgets" w ON b."CompanyAiWidgetId" = w."Id"
                WHERE w."TenantId" = $1 AND b."Status" = 2
                  AND b."Content" IS NOT NULL AND b."Content" != ''
                  AND b."EmbeddingJson" IS NOT NULL
                """,
                tenant_id,
            )
        finally:
            await conn.close()
    except Exception as e:
        logger.exception("Postgres RAG query failed: %s", e)
        return None
    if not rows:
        return None
    scored: list[tuple[str, float]] = []
    for row in rows:
        content = row["Content"] or ""
        emb_json = row["EmbeddingJson"]
        if not content or not emb_json:
            continue
        try:
            doc_emb = json.loads(emb_json)
        except (json.JSONDecodeError, TypeError):
            continue
        if not isinstance(doc_emb, list):
            continue
        score = _cosine_sim(query_embedding, doc_emb)
        scored.append((content, score))
    scored.sort(key=lambda x: -x[1])
    top = scored[:top_k]
    if not top:
        return None
    return "\n".join(t[0] for t in top).strip()