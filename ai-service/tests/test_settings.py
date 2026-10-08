from pathlib import Path
import runpy
from urllib.parse import urlparse

import pytest
from pydantic import ValidationError

from config.settings import Settings


@pytest.fixture(autouse=True)
def clean_settings_environment(monkeypatch):
    monkeypatch.delenv("APP_NAME", raising=False)
    monkeypatch.delenv("OLLAMA_BASE_URL", raising=False)


def test_environment_overrides_dotenv(tmp_path, monkeypatch):
    dotenv = tmp_path / ".env"
    dotenv.write_text("OLLAMA_BASE_URL=http://127.0.0.1:19001\n", encoding="utf-8")
    monkeypatch.setenv("OLLAMA_BASE_URL", "http://127.0.0.1:19002")
    settings = Settings(_env_file=dotenv)
    assert urlparse(str(settings.OLLAMA_BASE_URL)).port == 19002


def test_app_name_can_be_configured(tmp_path):
    dotenv = tmp_path / ".env"
    dotenv.write_text("APP_NAME=Configured service\n", encoding="utf-8")
    assert Settings(_env_file=dotenv).APP_NAME == "Configured service"


@pytest.mark.parametrize("value", ["", "not-a-url", "ftp://localhost:11434"])
def test_invalid_ollama_url_is_rejected(monkeypatch, value):
    monkeypatch.setenv("OLLAMA_BASE_URL", value)
    with pytest.raises(ValidationError):
        Settings(_env_file=None)


def test_blank_app_name_is_rejected(monkeypatch):
    monkeypatch.setenv("APP_NAME", "   ")
    with pytest.raises(ValidationError):
        Settings(_env_file=None)


def test_service_dotenv_is_loaded_from_another_working_directory(tmp_path, monkeypatch):
    # Copy the actual configuration module into an isolated service tree.
    # This exercises its path-resolution behavior without changing the user's .env.
    service = tmp_path / "service"
    config = service / "config"
    config.mkdir(parents=True)
    actual = Path(__file__).resolve().parents[1] / "config" / "settings.py"
    (config / "settings.py").write_bytes(actual.read_bytes())
    (service / ".env").write_text(
        "OLLAMA_BASE_URL=http://127.0.0.1:19999\n", encoding="utf-8"
    )
    other = tmp_path / "other-working-directory"
    other.mkdir()
    monkeypatch.chdir(other)
    namespace = runpy.run_path(str(config / "settings.py"))
    assert urlparse(str(namespace["settings"].OLLAMA_BASE_URL)).port == 19999
