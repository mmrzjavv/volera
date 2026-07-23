"""POST /agent/chat."""
from fastapi import APIRouter, Depends

from agent.core.agent import run_agent
from agent.reasoning.llm_base import BaseLLM
from api.deps import get_llm_dep
from api.schemas import ChatRequest, ChatResponse

router = APIRouter(prefix="/agent", tags=["agent"])


@router.post("/chat", response_model=ChatResponse)
async def chat(
    body: ChatRequest,
    llm: BaseLLM = Depends(get_llm_dep),
) -> ChatResponse:
    """Run the agent on the user message. Optionally include context from embeddings (future)."""
    result = await run_agent(
        user_message=body.message,
        conversation_history=None,
        context=None,
        llm=llm,
    )
    return ChatResponse(
        response=result.response,
        tool_calls=result.tool_calls,
    )
