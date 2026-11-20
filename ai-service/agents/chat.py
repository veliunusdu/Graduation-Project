import requests
from pydantic import BaseModel

from config.settings import settings


class AgentError(RuntimeError):
    """The installed chat model did not return a valid agent result."""


def complete_json(schema: type[BaseModel], system: str, user: str) -> BaseModel:
    """Ask the one installed chat model for schema-valid JSON."""
    payload = {
        "model": settings.TUTOR_MODEL,
        "stream": False,
        "think": True,
        "keep_alive": 0,
        "options": {"num_ctx": 8192, "num_predict": 1536, "temperature": 0, "seed": 42},
        "format": schema.model_json_schema(),
        "messages": [
            {"role": "system", "content": system},
            {"role": "user", "content": user},
        ],
    }
    try:
        response = requests.post(
            str(settings.OLLAMA_BASE_URL).rstrip("/") + "/api/chat",
            json=payload,
            timeout=settings.OLLAMA_TIMEOUT_SECONDS,
        )
        response.raise_for_status()
        data = response.json()
        if data.get("done") is not True or data.get("done_reason") == "length":
            raise ValueError("Incomplete output")
        return schema.model_validate_json(data["message"]["content"])
    except (requests.RequestException, ValueError, KeyError, TypeError, AttributeError) as exc:
        raise AgentError("Agent yanıtı tamamlanamadı veya doğrulanamadı.") from exc
