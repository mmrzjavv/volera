"""Weather information tool (mock implementation; clean interface for future real API)."""
from agent.tools.base import Tool


def get_weather(location: str) -> str:
    """Return mock weather for a location. Replace with real API when needed."""
    return (
        f"Weather for {location}: 72°F (22°C), partly cloudy. "
        "Mock data — connect a real weather API to replace this."
    )


def get_weather_tool() -> Tool:
    """Build the weather tool for the registry."""
    return Tool(
        name="get_weather",
        description="Get current weather for a location. Use when the user asks about weather, temperature, or conditions somewhere.",
        parameters={
            "type": "object",
            "properties": {
                "location": {
                    "type": "string",
                    "description": "City name or location, e.g. 'London', 'New York'",
                }
            },
            "required": ["location"],
        },
        callable=lambda **kwargs: get_weather(kwargs.get("location", "unknown")),
    )
