"""Tests for in-memory embedding store."""
import sys
from pathlib import Path

root = Path(__file__).resolve().parent.parent
if str(root) not in sys.path:
    sys.path.insert(0, str(root))

import pytest
from embeddings.in_memory_store import InMemoryEmbeddingStore


@pytest.mark.asyncio
async def test_create_and_query() -> None:
    store = InMemoryEmbeddingStore()
    v = [1.0, 0.0, 0.0]
    await store.create("a", v, text="hello", metadata={"x": 1})
    results = await store.query([1.0, 0.0, 0.0], top_k=1)
    assert len(results) == 1
    assert results[0].id == "a"
    assert results[0].score == 1.0
    assert results[0].text == "hello"
    assert results[0].metadata == {"x": 1}


@pytest.mark.asyncio
async def test_delete() -> None:
    store = InMemoryEmbeddingStore()
    await store.create("b", [0.0, 1.0], text="b")
    found = await store.delete("b")
    assert found is True
    results = await store.query([0.0, 1.0], top_k=5)
    assert len(results) == 0
    found2 = await store.delete("b")
    assert found2 is False


@pytest.mark.asyncio
async def test_reembed() -> None:
    store = InMemoryEmbeddingStore()
    await store.create("c", [1.0, 0.0], text="old", metadata={})
    await store.reembed("c", [0.0, 1.0], text="new", metadata={"k": "v"})
    results = await store.query([0.0, 1.0], top_k=1)
    assert results[0].id == "c"
    assert results[0].text == "new"
    assert results[0].metadata == {"k": "v"}


@pytest.mark.asyncio
async def test_get() -> None:
    store = InMemoryEmbeddingStore()
    await store.create("d", [1.0, 0.0], text="d", metadata={"m": 1})
    got = await store.get("d")
    assert got is not None
    vec, text, meta = got
    assert vec == [1.0, 0.0]
    assert text == "d"
    assert meta == {"m": 1}
    assert await store.get("missing") is None
