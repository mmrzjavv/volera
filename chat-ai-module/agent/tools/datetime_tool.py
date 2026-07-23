"""Current date and time tool (system-aware)."""
from datetime import datetime, timezone

from agent.tools.base import Tool


def get_current_datetime() -> str:
    """Return current date and time in ISO format (UTC)."""
    return datetime.now(timezone.utc).strftime("%Y-%m-%d %H:%M:%S UTC")


def get_datetime_tool() -> Tool:
    """Build the datetime tool for the registry."""
    return Tool(
        name="get_current_datetime",
        description="Get the current date and time (system clock, UTC). Use when the user asks for today's date, current time, or similar.",
        parameters={
            "type": "object",
            "properties": {},
            "required": [],
        },
        callable=lambda: get_current_datetime(),
    )
