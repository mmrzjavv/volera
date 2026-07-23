"""Pytest fixtures for AI Agent tests."""
import os

import pytest
from fastapi.testclient import TestClient

# Ensure project root is on path when running tests
import sys
from pathlib import Path

root = Path(__file__).resolve().parent.parent
if str(root) not in sys.path:
    sys.path.insert(0, str(root))

# Set minimal env for tests (Ollama default, no OpenAI key required for agent)
os.environ.setdefault("LLM_PROVIDER", "ollama")
os.environ.setdefault("OLLAMA_BASE_URL", "http://localhost:11434")
os.environ.setdefault("OLLAMA_MODEL", "gemma3:4b")


@pytest.fixture
def client() -> TestClient:
    """FastAPI test client."""
    from main import app
    return TestClient(app)
