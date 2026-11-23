import json

from tutor.service import TutorAssignmentContext, TutorRequest, TutorSource, answer
from schemas.context import AgentContext


def request(question, sources=None):
    return TutorRequest(question=question, context=AgentContext(course_id=1, education_level="HighSchool", grade_level=9), sources=sources or [])


def test_no_sources_never_calls_model(monkeypatch):
    monkeypatch.setattr("tutor.service.requests.post", lambda *a, **k: (_ for _ in ()).throw(AssertionError("model called")))
    result = answer(request("Bu dersin not politikası nedir?"))
    assert result.status == "no_source" and not result.citations


def test_explicit_full_assignment_never_calls_model(monkeypatch):
    monkeypatch.setattr("tutor.service.requests.post", lambda *a, **k: (_ for _ in ()).throw(AssertionError("model called")))
    result = answer(request("Ödevimin tamamını yaz", [TutorSource(source_id="s1", text="Ders notu")]))
    assert result.status == "guidance_only"


import pytest
from tutor.service import TutorError


@pytest.mark.parametrize("done_reason,content,should_fail", [
    ("stop", '{"explanation":"Açıklama","next_step":"Dene","citations":["s1"]}', False),
    ("length", '{"explanation":"Açıklama","next_step":"Dene","citations":["s1"]}', True),
    ("stop", '{"explanation":"Açıklama","next_step":"Dene","citations":["invented"]}', True),
    ("stop", 'invalid JSON', True),
])
def test_generated_answer_requires_complete_valid_source_references(monkeypatch, done_reason, content, should_fail):
    class Response:
        def raise_for_status(self):
            pass
        def json(self):
            return {"done": True, "done_reason": done_reason, "message": {"content": content}}
    monkeypatch.setattr("tutor.service.requests.post", lambda *a, **k: Response())
    data = request("Bölme nasıl çalışır?", [TutorSource(source_id="s1", text="Ders notu")])
    if should_fail:
        with pytest.raises(TutorError):
            answer(data)
    else:
        assert answer(data).citations == ["s1"]


def test_assignment_context_is_included_in_grounded_model_prompt(monkeypatch):
    captured = {}

    class Response:
        def raise_for_status(self):
            pass

        def json(self):
            return {
                "done": True,
                "done_reason": "stop",
                "message": {
                    "content": '{"explanation":"Açıklama","next_step":"Dene","citations":["s1"]}'
                },
            }

    def post(*args, **kwargs):
        captured.update(kwargs["json"])
        return Response()

    monkeypatch.setattr("tutor.service.requests.post", post)
    data = TutorRequest(
        question="Bu ödeve nasıl başlamalıyım?",
        context=AgentContext(course_id=1, education_level="University", grade_level=2),
        sources=[TutorSource(source_id="s1", text="Döngüler ve fonksiyonlar ders notu")],
        assignment=TutorAssignmentContext(
            assignment_id=7,
            title="Öğrenci notlarını işleme",
            description="Ortalama ve geçen öğrenci sayısını hesapla.",
        ),
    )

    assert answer(data).citations == ["s1"]
    user_payload = json.loads(captured["messages"][1]["content"])
    assert user_payload["question"] == "Bu ödeve nasıl başlamalıyım?"
    assert user_payload["assignment"] == {
        "assignment_id": 7,
        "title": "Öğrenci notlarını işleme",
        "description": "Ortalama ve geçen öğrenci sayısını hesapla.",
    }


def test_assignment_help_falls_back_to_safe_grounded_guidance_for_invalid_model_output(monkeypatch):
    class Response:
        def raise_for_status(self):
            pass

        def json(self):
            return {
                "done": True,
                "done_reason": "stop",
                "message": {
                    "content": '{"explanation":"Açıklama","next_step":"Dene","citations":["invented"]}'
                },
            }

    monkeypatch.setattr("tutor.service.requests.post", lambda *args, **kwargs: Response())
    data = TutorRequest(
        question="Bu ödeve yardım et",
        context=AgentContext(course_id=1, education_level="University", grade_level=2),
        sources=[TutorSource(source_id="s1", text="Döngüler ve fonksiyonlar ders notu")],
        assignment=TutorAssignmentContext(
            assignment_id=7,
            title="Öğrenci notlarını işleme",
            description="Ortalama hesapla.",
        ),
    )

    result = answer(data)
    assert result.status == "answered"
    assert result.citations == ["s1"]
    assert "Öğrenci notlarını işleme" in result.explanation
    assert "Tam çözümü" in result.explanation
