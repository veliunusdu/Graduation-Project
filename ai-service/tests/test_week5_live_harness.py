"""Tests for the live-check validator; application transport is tested live separately."""
import importlib.util
from pathlib import Path

import httpx
import pytest

SCRIPT = Path(__file__).resolve().parents[2] / "scripts/verify_veli_week5.py"
spec = importlib.util.spec_from_file_location("verify_veli_week5", SCRIPT)
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


def page(body, status=200):
    return httpx.Response(status, text=body)


def test_validator_accepts_expected_safe_warning_and_matching_log():
    result = module.check_page(page('<div class="alert alert-warning">Controlled warning</div>'),
        success=False, expected_message="Controlled warning", log_text="AI service returned HTTP 503.", expected_log="AI service returned HTTP 503.")
    assert result["http_status"] == 200 and result["log_verified"] is True


@pytest.mark.parametrize("body,status,log", [
    ('<div class="alert alert-success">Controlled warning</div>',200,"expected log"),
    ('<div class="alert alert-warning">Wrong warning</div>',200,"expected log"),
    ('<div class="alert alert-warning">Controlled warning</div>',500,"expected log"),
    ('<div class="alert alert-warning">Controlled warning Traceback</div>',200,"expected log"),
    ('<div class="alert alert-warning">Controlled warning SYNTHETIC_PRIVATE_ERROR</div>',200,"expected log"),
    ('<div class="alert alert-warning">Controlled warning</div>',200,"different log"),
])
def test_validator_cannot_report_false_success(body,status,log):
    with pytest.raises(module.VerificationFailure):
        module.check_page(page(body,status),success=False,expected_message="Controlled warning",log_text=log,expected_log="expected log")
