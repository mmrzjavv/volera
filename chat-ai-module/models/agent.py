"""Agent and chat DTOs: messages, tool calls, agent response."""
from enum import Enum
from typing import Any

from pydantic import BaseModel, Field


class ChatRole(str, Enum):
    """Chat message role."""

    SYSTEM = "system"
    USER = "user"
    ASSISTANT = "assistant"


class ChatMessage(BaseModel):
    """A single chat message."""

    role: ChatRole
    content: str = ""
    tool_calls: list["ToolCall"] | None = None
    tool_call_id: str | None = None
    name: str | None = None  # for tool result message

    def to_openai_dict(self) -> dict[str, Any]:
        """Convert to OpenAI-style message dict for API calls."""
        d: dict[str, Any] = {"role": self.role.value, "content": self.content or None}
        if self.tool_calls:
            d["tool_calls"] = [tc.model_dump() for tc in self.tool_calls]
        if self.tool_call_id is not None:
            d["tool_call_id"] = self.tool_call_id
        if self.name is not None:
            d["name"] = self.name
        return d

    @classmethod
    def from_openai_dict(cls, d: dict[str, Any]) -> "ChatMessage":
        """Build from OpenAI-style response message."""
        role = ChatRole(d.get("role", "user"))
        content = d.get("content") or ""
        tool_calls_raw = d.get("tool_calls")
        tool_calls = (
            [ToolCall(**tc) for tc in tool_calls_raw] if tool_calls_raw else None
        )
        return cls(
            role=role,
            content=content,
            tool_calls=tool_calls,
            tool_call_id=d.get("tool_call_id"),
            name=d.get("name"),
        )


class ToolCall(BaseModel):
    """Tool call from LLM."""

    id: str
    name: str
    arguments: str = "{}"


class ToolCallResult(BaseModel):
    """Result of executing one tool call."""

    tool_call_id: str
    content: str


class AgentResponse(BaseModel):
    """Final agent response for the chat API."""

    response: str = Field(..., description="Assistant reply text")
    tool_calls: list[dict[str, Any]] | None = Field(
        default=None,
        description="Tool calls made during this turn (for debugging)",
    )
