"""Speech-to-text: interface and placeholder implementation."""
from abc import ABC, abstractmethod

from config import get_settings


class BaseSTT(ABC):
    """STT provider interface."""

    @abstractmethod
    async def transcribe(self, audio: bytes) -> str:
        """Return transcribed text."""
        ...


class PlaceholderSTT(BaseSTT):
    """Placeholder: returns fixed message. Replace with real provider."""

    async def transcribe(self, audio: bytes) -> str:
        return "[Placeholder STT: connect a real provider to transcribe audio.]"


def get_stt() -> BaseSTT:
    """Build STT from config (placeholder by default)."""
    settings = get_settings()
    if settings.stt_provider == "placeholder":
        return PlaceholderSTT()
    return PlaceholderSTT()
