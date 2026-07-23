"""Tests for agent tools (datetime, weather) and registry."""
import sys
from pathlib import Path

root = Path(__file__).resolve().parent.parent
if str(root) not in sys.path:
    sys.path.insert(0, str(root))

from agent.tools.base import ToolRegistry
from agent.tools.datetime_tool import get_current_datetime, get_datetime_tool
from agent.tools.weather_tool import get_weather, get_weather_tool
from agent.tools.registry import tool_registry, register_all_tools


def test_get_current_datetime_returns_string() -> None:
    out = get_current_datetime()
    assert isinstance(out, str)
    assert "UTC" in out


def test_datetime_tool_has_no_required_params() -> None:
    tool = get_datetime_tool()
    assert tool.name == "get_current_datetime"
    assert tool.parameters.get("required") == []


def test_weather_tool_returns_mock() -> None:
    out = get_weather("London")
    assert "London" in out
    assert "Mock" in out or "72" in out


def test_weather_tool_callable() -> None:
    tool = get_weather_tool()
    out = tool.callable(location="Paris")
    assert "Paris" in out


def test_registry_register_and_get() -> None:
    reg = ToolRegistry()
    tool = get_datetime_tool()
    reg.register(tool)
    assert reg.get("get_current_datetime") is tool
    assert reg.get("nonexistent") is None


def test_register_all_tools() -> None:
    register_all_tools()
    assert tool_registry.get("get_current_datetime") is not None
    assert tool_registry.get("get_weather") is not None
    assert len(tool_registry.to_openai_tools()) >= 2
