"""Ollama LLM client (OpenAI-compatible API). Active by default; model: gemma3:4b."""
from typing import Any

from openai import AsyncOpenAI

from agent.reasoning.llm_base import BaseLLM, LLMResponse


def _parse_tool_calls(message: Any) -> list[dict[str, Any]]:
    """Extract tool_calls from OpenAI-style message in id/name/arguments form."""
    out: list[dict[str, Any]] = []
    for tc in getattr(message, "tool_calls", []) or []:
        fn = getattr(tc, "function", None) or {}
        name = getattr(fn, "name", None) or (fn.get("name") if isinstance(fn, dict) else "")
        args = getattr(fn, "arguments", None) or (fn.get("arguments") if isinstance(fn, dict) else "{}")
        out.append({
            "id": getattr(tc, "id", "") or (tc.get("id") if isinstance(tc, dict) else ""),
            "name": name,
            "arguments": args,
        })
    return out


class OllamaLLM(BaseLLM):
    """Ollama via OpenAI-compatible client. Uses OLLAMA_BASE_URL and OLLAMA_MODEL."""

    def __init__(self, base_url: str, model: str) -> None:
        # Ollama exposes OpenAI-compatible API at /v1 (e.g. /v1/chat/completions)
        url = base_url.rstrip("/")
        if not url.endswith("/v1"):
            url = f"{url}/v1"
        self._client = AsyncOpenAI(base_url=url, api_key="ollama")
        self._model = model

    async def chat(
        self,
        messages: list[dict[str, Any]],
        tools: list[dict[str, Any]] | None = None,
    ) -> LLMResponse:
        """Call Ollama chat completion. Tools in OpenAI format."""
        kwargs: dict[str, Any] = {"model": self._model, "messages": messages}
        if tools:
            kwargs["tools"] = tools
            kwargs["tool_choice"] = "auto"
        response = await self._client.chat.completions.create(**kwargs)
        choice = response.choices[0] if response.choices else None
        if not choice:
            return LLMResponse(content="")
        msg = choice.message
        content = (msg.content or "").strip()
        tool_calls = _parse_tool_calls(msg)
        return LLMResponse(content=content, tool_calls=tool_calls)
