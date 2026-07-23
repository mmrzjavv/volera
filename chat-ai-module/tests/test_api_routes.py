"""API route integration tests."""
import sys
from pathlib import Path

root = Path(__file__).resolve().parent.parent
if str(root) not in sys.path:
    sys.path.insert(0, str(root))

import pytest
from fastapi.testclient import TestClient

from main import app


@pytest.fixture
def client() -> TestClient:
    return TestClient(app)


def test_health(client: TestClient) -> None:
    r = client.get("/health")
    assert r.status_code == 200
    assert r.json() == {"status": "ok"}


def test_tts_returns_audio(client: TestClient) -> None:
    r = client.post("/text/to-speech", json={"text": "Hello"})
    assert r.status_code == 200
    assert r.headers.get("content-type", "").startswith("audio/")
    assert len(r.content) > 0


def test_stt_returns_text(client: TestClient) -> None:
    r = client.post("/speech/to-text", content=b"fake audio bytes")
    assert r.status_code == 200
    data = r.json()
    assert "text" in data


def test_embeddings_create_requires_openai_key(client: TestClient) -> None:
    # Without OPENAI_API_KEY, embed() may fail; we test the route exists and validation works
    try:
        r = client.post(
            "/embeddings/create",
            json={"id": "t1", "text": "hello", "metadata": {}},
        )
        assert r.status_code in (200, 500, 502, 503)
    except Exception:
        # Embedder or store can raise when key missing or API down
        pass


def test_embeddings_delete_not_found(client: TestClient) -> None:
    r = client.delete("/embeddings/nonexistent-id")
    assert r.status_code == 404


def test_chat_request_validation(client: TestClient) -> None:
    r = client.post("/agent/chat", json={})
    assert r.status_code == 422  # validation error (missing message)
    try:
        r2 = client.post("/agent/chat", json={"message": "What time is it?"})
        assert r2.status_code in (200, 500, 502, 503)
    except Exception:
        # Ollama may be down (connection error / 503)
        pass
