from tutor.service import TutorRequest, TutorSource, answer
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
