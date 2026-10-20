import requests
import pytest

from fastapi.testclient import TestClient

from api.main import app
from llm.ollama_client import OllamaClient, OllamaClientError


client = TestClient(app)


class MockResponse:
    def __init__(self, data, status_code=200):
        self.data = data
        self.status_code = status_code

    def raise_for_status(self):
        if self.status_code >= 400:
            raise requests.HTTPError(f"HTTP {self.status_code}")

    def json(self):
        return self.data


def test_general_task_uses_general_model(monkeypatch):
    captured = {}

    def mock_post(url, json, timeout):
        captured.update(url=url, payload=json, timeout=timeout)
        return MockResponse({
            "model": "qwen3:8b",
            "response": "Hello from the model",
            "done": True,
        })

    monkeypatch.setattr("llm.ollama_client.requests.post", mock_post)

    result = OllamaClient().generate("Hello", task="general")

    assert captured["payload"]["model"] == "qwen3:8b"
    assert captured["payload"]["stream"] is False
    assert result == {
        "model": "qwen3:8b",
        "response": "Hello from the model",
        "done": True,
    }


def test_code_review_task_uses_code_model(monkeypatch):
    captured = {}

    def mock_post(url, json, timeout):
        captured.update(payload=json)
        return MockResponse({
            "model": "qwen2.5-coder:7b",
            "response": "Review completed",
            "done": True,
        })

    monkeypatch.setattr("llm.ollama_client.requests.post", mock_post)

    result = OllamaClient().generate("Review this code", task="code_review")

    assert captured["payload"]["model"] == "qwen2.5-coder:7b"
    assert result["response"] == "Review completed"


def test_invalid_task_is_rejected():
    with pytest.raises(ValueError, match="task must be"):
        OllamaClient().generate("Hello", task="unknown")


def test_connection_error_is_handled(monkeypatch):
    def mock_post(*args, **kwargs):
        raise requests.ConnectionError("connection failed")

    monkeypatch.setattr("llm.ollama_client.requests.post", mock_post)

    with pytest.raises(OllamaClientError, match="Could not connect"):
        OllamaClient().generate("Hello")


def test_timeout_is_handled(monkeypatch):
    def mock_post(*args, **kwargs):
        raise requests.Timeout("request timed out")

    monkeypatch.setattr("llm.ollama_client.requests.post", mock_post)

    with pytest.raises(OllamaClientError, match="timed out"):
        OllamaClient().generate("Hello")


def test_llm_endpoint_returns_json(monkeypatch):
    def mock_generate(prompt, task="general"):
        return {
            "model": "qwen3:8b",
            "response": f"Answer to: {prompt}",
            "done": True,
        }

    monkeypatch.setattr(
        "api.main.ollama_client.generate",
        mock_generate,
    )

    response = client.post(
        "/llm/test",
        json={"prompt": "Explain Python", "task": "general"},
    )

    assert response.status_code == 200
    assert response.json() == {
        "model": "qwen3:8b",
        "response": "Answer to: Explain Python",
        "done": True,
    }


def test_llm_endpoint_rejects_invalid_task():
    response = client.post(
        "/llm/test",
        json={"prompt": "Review code", "task": "unknown"},
    )

    assert response.status_code == 422


def test_llm_endpoint_handles_ollama_error(monkeypatch):
    def mock_generate(prompt, task="general"):
        raise OllamaClientError("Ollama is unavailable")

    monkeypatch.setattr(
        "api.main.ollama_client.generate",
        mock_generate,
    )

    response = client.post(
        "/llm/test",
        json={"prompt": "Hello"},
    )

    assert response.status_code == 502
    assert response.json()["detail"] == "Ollama is unavailable"
