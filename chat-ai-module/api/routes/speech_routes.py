"""POST /text/to-speech and POST /speech/to-text."""
from fastapi import APIRouter, Depends, Request
from fastapi.responses import Response

from speech import BaseTTS, BaseSTT
from api.deps import get_tts_dep, get_stt_dep
from api.schemas import TTSRequestBody, STTResponseBody

router = APIRouter(tags=["speech"])


@router.post("/text/to-speech")
async def text_to_speech(
    body: TTSRequestBody,
    tts: BaseTTS = Depends(get_tts_dep),
) -> Response:
    """Convert text to speech; returns audio bytes."""
    audio_bytes, content_type = await tts.synthesize(body.text)
    return Response(content=audio_bytes, media_type=content_type)


@router.post("/speech/to-text", response_model=STTResponseBody)
async def speech_to_text(
    request: Request,
    stt: BaseSTT = Depends(get_stt_dep),
) -> STTResponseBody:
    """Convert speech/audio to text. Send raw audio in request body."""
    body = await request.body()
    text = await stt.transcribe(body)
    return STTResponseBody(text=text)
