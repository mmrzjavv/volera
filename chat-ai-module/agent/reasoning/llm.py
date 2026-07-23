"""LLM factory: select Ollama or OpenAI from config."""
from typing import Any

from config import get_settings
from agent.reasoning.llm_base import BaseLLM, LLMResponse
from agent.reasoning.providers.ollama_llm import OllamaLLM
from agent.reasoning.providers.openai_llm import OpenAILLM


def get_llm() -> BaseLLM:
    """Build the configured LLM (Ollama or OpenAI)."""
    settings = get_settings()
    if settings.llm_provider == "openai":
        return OpenAILLM(
            api_key=settings.openai_api_key,
            model=settings.openai_chat_model,
        )
    return OllamaLLM(
        base_url=settings.ollama_base_url,
        model=settings.ollama_model,
    )
