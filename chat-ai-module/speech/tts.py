"""Text-to-speech: interface and placeholder implementation."""
from abc import ABC, abstractmethod

from config import get_settings


class BaseTTS(ABC):
    """TTS provider interface."""

    @abstractmethod
    async def synthesize(self, text: str) -> tuple[bytes, str]:
        """Return (audio_bytes, content_type)."""
        ...


class PlaceholderTTS(BaseTTS):
    """Placeholder: returns minimal WAV silence. Replace with real provider."""

    async def synthesize(self, text: str) -> tuple[bytes, str]:
        # Minimal WAV: 44-byte header + no samples (1 channel, 16-bit, 8kHz)
        header = (
            b"RIFF\x24\x00\x00\x00WAVEfmt \x10\x00\x00\x00"
            b"\x01\x00\x01\x00\x80\x1f\x00\x00\x00\x3e\x00\x00"
            b"\x02\x00\x10\x00data\x00\x00\x00\x00"
        )
        return (header, "audio/wav")


def get_tts() -> BaseTTS:
    """Build TTS from config (placeholder by default)."""
    settings = get_settings()
    if settings.tts_provider == "placeholder":
        return PlaceholderTTS()
    return PlaceholderTTS()
