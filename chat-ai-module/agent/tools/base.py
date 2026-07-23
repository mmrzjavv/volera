"""Tool protocol and registry: name, description, parameters, callable."""
from collections.abc import Awaitable, Callable
from typing import Any

from pydantic import BaseModel, ConfigDict


class Tool(BaseModel):
    """A single tool: schema for the LLM and a callable to execute."""

    model_config = ConfigDict(arbitrary_types_allowed=True)

    name: str
    description: str
    parameters: dict[str, Any]  # JSON Schema for arguments
    callable: Any = None  # Callable[..., str] or Callable[..., Awaitable[str]]

    def to_openai_tool(self) -> dict[str, Any]:
        """Format for OpenAI / Ollama tools API."""
        return {
            "type": "function",
            "function": {
                "name": self.name,
                "description": self.description,
                "parameters": {
                    "type": "object",
                    "properties": self.parameters.get("properties", {}),
                    "required": self.parameters.get("required", []),
                },
            },
        }


class ToolRegistry:
    """Registry of tools: register, get by name, list for LLM."""

    def __init__(self) -> None:
        self._tools: dict[str, Tool] = {}

    def register(self, tool: Tool) -> None:
        """Register a tool by name."""
        self._tools[tool.name] = tool

    def get(self, name: str) -> Tool | None:
        """Get tool by name."""
        return self._tools.get(name)

    def list_tools(self) -> list[Tool]:
        """Return all registered tools."""
        return list(self._tools.values())

    def to_openai_tools(self) -> list[dict[str, Any]]:
        """Export tools in OpenAI format for chat completion."""
        return [t.to_openai_tool() for t in self._tools.values()]
