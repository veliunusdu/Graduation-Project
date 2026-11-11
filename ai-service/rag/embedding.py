import math

import requests

from config.settings import settings


class EmbeddingError(RuntimeError):
    """Raised when Ollama cannot produce valid embeddings."""


class OllamaEmbeddingClient:
    def __init__(self):
        self.base_url = str(settings.OLLAMA_BASE_URL).rstrip("/")
        self.model = settings.OLLAMA_EMBEDDING_MODEL
        self.timeout = settings.OLLAMA_TIMEOUT_SECONDS

    def model_identity(self) -> str:
        """Resolve the configured tag to the installed model digest."""
        name = self.model if ":" in self.model.rsplit("/", 1)[-1] else f"{self.model}:latest"
        try:
            response = requests.get(f"{self.base_url}/api/tags", timeout=self.timeout)
            response.raise_for_status()
            models = response.json()["models"]
            for item in models:
                digest = item.get("digest")
                if item.get("name") == name and isinstance(digest, str) and digest:
                    return f"{name}@{digest}"
        except (requests.RequestException, ValueError, KeyError, TypeError, AttributeError) as exc:
            raise EmbeddingError("Could not verify the installed embedding model identity.") from exc
        raise EmbeddingError("The configured embedding model is not installed.")

    def embed(self, texts: list[str]) -> list[list[float]]:
        if not texts:
            return []

        try:
            response = requests.post(
                f"{self.base_url}/api/embed",
                json={"model": self.model, "input": texts},
                timeout=self.timeout,
            )
            response.raise_for_status()
            data = response.json()
        except requests.Timeout as exc:
            raise EmbeddingError("Embedding request timed out.") from exc
        except requests.ConnectionError as exc:
            raise EmbeddingError(
                "Could not connect to Ollama for embedding."
            ) from exc
        except requests.RequestException as exc:
            raise EmbeddingError(f"Ollama embedding request failed: {exc}") from exc
        except ValueError as exc:
            raise EmbeddingError("Ollama returned invalid embedding JSON.") from exc

        embeddings = data.get("embeddings") if isinstance(data, dict) else None
        if not isinstance(embeddings, list) or len(embeddings) != len(texts):
            raise EmbeddingError("Ollama returned an invalid number of embeddings.")
        if any(
            not isinstance(vector, list)
            or not vector
            or not all(
                isinstance(value, (int, float))
                and not isinstance(value, bool)
                and math.isfinite(value)
                for value in vector
            )
            for vector in embeddings
        ):
            raise EmbeddingError("Ollama returned an invalid embedding vector.")
        dimensions = {len(vector) for vector in embeddings}
        if len(dimensions) != 1:
            raise EmbeddingError("Ollama returned inconsistent embedding dimensions.")
        return embeddings


embedding_client = OllamaEmbeddingClient()
