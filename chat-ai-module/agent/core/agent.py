"""Main agent loop: reason -> optional tool calls -> incorporate -> repeat until done."""
import logging
from typing import Any

from config import get_settings
from agent.reasoning.llm_base import BaseLLM
from agent.reasoning.llm import get_llm
from agent.reasoning.tool_selector import execute_tool_calls
from agent.tools.registry import tool_registry
from models.agent import AgentResponse, ToolCallResult

logger = logging.getLogger(__name__)


async def run_agent(
    user_message: str,
    conversation_history: list[dict[str, Any]] | None = None,
    context: str | None = None,
    llm: BaseLLM | None = None,
    use_tools: bool = True,
) -> AgentResponse:
    """
    Run the agent: optional context, then loop (LLM -> tool calls if any -> repeat).
    Returns final assistant reply and optional tool call history.
    Set use_tools=False for models that don't support tool calling (e.g. widget RAG chat).
    """
    settings = get_settings()
    if llm is None:
        llm = get_llm()

    # Build initial messages (no system message by default; optional context can be in first user message)
    messages: list[dict[str, Any]] = []
    if conversation_history:
        messages.extend(conversation_history)
    content = user_message
    if context:
        content = f"Context:\n{context}\n\nUser: {user_message}"
    messages.append({"role": "user", "content": content})

    tools_spec = tool_registry.to_openai_tools() if use_tools else None
    tool_calls_made: list[dict[str, Any]] = []
    steps = 0

    while steps < settings.max_agent_steps:
        steps += 1
        response = await llm.chat(
            messages,
            tools=tools_spec if tools_spec else None,
        )

        if response.tool_calls:
            tool_calls_made.extend(response.tool_calls)
            # Append assistant message with tool_calls so the model has context
            assistant_msg: dict[str, Any] = {
                "role": "assistant",
                "content": response.content or None,
                "tool_calls": [
                    {
                        "id": tc["id"],
                        "type": "function",
                        "function": {"name": tc["name"], "arguments": tc["arguments"]},
                    }
                    for tc in response.tool_calls
                ],
            }
            messages.append(assistant_msg)
            results = await execute_tool_calls(response.tool_calls)
            for r in results:
                messages.append(
                    {
                        "role": "tool",
                        "tool_call_id": r.tool_call_id,
                        "content": r.content,
                    }
                )
            continue

        # No tool calls: we have the final answer
        return AgentResponse(
            response=response.content or "",
            tool_calls=tool_calls_made if tool_calls_made else None,
        )

    # Max steps reached; return whatever we have
    last_content = messages[-1].get("content", "") if messages else ""
    return AgentResponse(
        response=last_content or "(Max steps reached.)",
        tool_calls=tool_calls_made if tool_calls_made else None,
    )
