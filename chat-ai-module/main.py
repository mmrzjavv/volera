"""FastAPI application: AI Agent service for API Gateway."""
import logging
from contextlib import asynccontextmanager

from fastapi import FastAPI
from openai import APIConnectionError, InternalServerError

from config import get_settings
from agent.tools.registry import register_all_tools
from api.routes import agent_routes, embeddings_routes, speech_routes, widget_routes

settings = get_settings()
logging.basicConfig(level=getattr(logging, settings.log_level.upper(), logging.INFO))


@asynccontextmanager
async def lifespan(app: FastAPI):
    """Register tools on startup."""
    register_all_tools()
    yield


app = FastAPI(
    title="Chat AI Agent",
    description="Production-grade AI Agent with tools, embeddings, and speech APIs.",
    version="0.1.0",
    lifespan=lifespan,
)


app.include_router(agent_routes.router)
app.include_router(embeddings_routes.router)
app.include_router(speech_routes.router)
app.include_router(widget_routes.router)


@app.exception_handler(APIConnectionError)
@app.exception_handler(InternalServerError)
async def openai_error_handler(request, exc: Exception):
    """Return 503 when Ollama/LLM is unavailable (e.g. not running or model not pulled)."""
    from fastapi.responses import JSONResponse
    return JSONResponse(
        status_code=503,
        content={
            "detail": (
                "Ollama unavailable. Ensure Ollama is running (ollama serve) and models are pulled: "
                "ollama pull gemma3:4b  and  ollama pull znbang/bge:large-en-v1.5-f16"
            )
        },
    )


@app.get("/health")
def health() -> dict:
    """Health check for API Gateway."""
    return {"status": "ok"}
