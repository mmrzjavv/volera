"""Global tool registry; register built-in tools at startup."""
from agent.tools.base import ToolRegistry
from agent.tools.datetime_tool import get_datetime_tool
from agent.tools.weather_tool import get_weather_tool

# Singleton registry used by the agent
tool_registry = ToolRegistry()


def register_all_tools() -> None:
    """Register all built-in tools. Call from app startup."""
    tool_registry.register(get_datetime_tool())
    tool_registry.register(get_weather_tool())
