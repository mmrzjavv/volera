"""Speech DTOs: TTS and STT request/response."""
from pydantic import BaseModel, Field


class TTSRequest(BaseModel):
    """Input for text-to-speech."""

    text: str = Field(..., min_length=1)


class TTSResponse(BaseModel):
    """TTS returns raw audio bytes; this is for API metadata if needed."""

    content_type: str = "audio/wav"
    # Actual audio returned as Response(body=bytes)


class STTRequest(BaseModel):
    """STT accepts audio; body is raw bytes, this is for validation/options."""


class STTResponse(BaseModel):
    """Speech-to-text result."""

    text: str = Field(..., description="Transcribed text")
