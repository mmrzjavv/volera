"""Map LLM tool_calls to registered tools and execute them."""
import json
import logging
from typing import Any

from agent.tools.registry import tool_registry
from models.agent import ToolCallResult

logger = logging.getLogger(__name__)


async def execute_tool_calls(
    tool_calls: list[dict[str, Any]],
) -> list[ToolCallResult]:
    """Execute each tool call via the registry and return results for the LLM."""
    results: list[ToolCallResult] = []
    for tc in tool_calls:
        tool_call_id = tc.get("id", "")
        name = tc.get("name", "")
        arguments_str = tc.get("arguments", "{}")
        tool = tool_registry.get(name)
        if not tool:
            results.append(
                ToolCallResult(tool_call_id=tool_call_id, content=f"Unknown tool: {name}")
            )
            continue
        try:
            args = json.loads(arguments_str) if arguments_str else {}
            if not isinstance(args, dict):
                args = {}
            fn = tool.callable
            if hasattr(fn, "__call__"):
                result = fn(**args) if callable(fn) else str(fn)
            else:
                result = str(fn)
            # Support async callables
            if hasattr(result, "__await__"):
                result = await result
            results.append(ToolCallResult(tool_call_id=tool_call_id, content=str(result)))
        except Exception as e:
            logger.exception("Tool execution failed: %s", name)
            results.append(
                ToolCallResult(tool_call_id=tool_call_id, content=f"Error: {e!s}")
            )
    return results
