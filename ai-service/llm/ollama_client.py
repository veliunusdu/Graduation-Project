import requests

from config.settings import settings


class OllamaClientError(Exception):
    """Raised when Ollama cannot return a valid model response."""


class OllamaClient:
    def __init__(self):
        self.base_url = str(settings.OLLAMA_BASE_URL).rstrip("/")
        self.timeout = settings.OLLAMA_TIMEOUT_SECONDS

    def generate(self, prompt: str, task: str = "general") -> dict:
        if task == "general":
            model = settings.OLLAMA_GENERAL_MODEL
        elif task == "code_review":
            model = settings.OLLAMA_CODE_REVIEW_MODEL
        else:
            raise ValueError("task must be 'general' or 'code_review'")

        try:
            response = requests.post(
                f"{self.base_url}/api/generate",
                json={
                    "model": model,
                    "prompt": prompt,
                    "stream": False,
                },
                timeout=self.timeout,
            )
            response.raise_for_status()
            data = response.json()
        except requests.Timeout as exc:
            raise OllamaClientError("Ollama request timed out.") from exc
        except requests.ConnectionError as exc:
            raise OllamaClientError(
                "Could not connect to Ollama. Check whether Ollama is running."
            ) from exc
        except requests.HTTPError as exc:
            raise OllamaClientError(
                f"Ollama returned an HTTP error: {exc}"
            ) from exc
        except requests.RequestException as exc:
            raise OllamaClientError(f"Ollama request failed: {exc}") from exc
        except ValueError as exc:
            raise OllamaClientError("Ollama returned invalid JSON.") from exc

        if not isinstance(data, dict) or "response" not in data:
            raise OllamaClientError("Ollama response is missing the response field.")

        return {
            "model": data.get("model", model),
            "response": data["response"],
            "done": data.get("done", False),
        }


ollama_client = OllamaClient()
