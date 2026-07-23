"""LLM response type and abstract base (no provider imports)."""
from abc import ABC, abstractmethod
from typing import Any


class LLMResponse:
    """Response from an LLM call: content and optional tool_calls."""

    def __init__(
        self,
        content: str,
        tool_calls: list[dict[str, Any]] | None = None,
    ) -> None:
        self.content = content
        self.tool_calls = tool_calls or []


class BaseLLM(ABC):
    """Abstract LLM interface: chat with optional tools."""

    @abstractmethod
    async def chat(
        self,
        messages: list[dict[str, Any]],
        tools: list[dict[str, Any]] | None = None,
    ) -> LLMResponse:
        """Send messages to the LLM; optionally pass tool definitions. Returns content and any tool_calls."""
        ...
