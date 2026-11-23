import json

import pytest

from agents.author import DraftRequest, draft
from agents.chat import AgentError
from agents.reviewer import ReviewRequest, review
from config.settings import settings
from schemas.context import AgentContext
from schemas.document import ChunkMetadata, RetrievalResult, RetrievedChunk


def context():
    return AgentContext(course_id=7, education_level="University", grade_level=2)


def hit(chunk_id="c1", course_id=7, score=0.9, text="Primary keys identify a row."):
    return RetrievedChunk(
        text=text,
        score=score,
        metadata=ChunkMetadata(
            course_id=course_id,
            education_level="University",
            grade_level=2,
            material_id=3,
            page_number=1,
            chunk_id=chunk_id,
        ),
    )


def retrieve(result):
    return lambda request: result


def test_draft_without_sources_never_calls_model(monkeypatch):
    monkeypatch.setattr("agents.chat.requests.post", lambda *a, **k: (_ for _ in ()).throw(AssertionError("model called")))
    result = draft(DraftRequest(topic="Keys", context=context()), retrieve=retrieve(RetrievalResult(question="Keys", course_id=7, top_k=5, results=[])))
    assert result.status == "no_source"


def test_draft_rejects_unknown_source_ids(monkeypatch):
    class Response:
        def raise_for_status(self):
            pass
        def json(self):
            return {"done": True, "done_reason": "stop", "message": {"content": json.dumps({
                "title": "Keys", "description": "Explain a primary key.", "difficulty": "Beginner", "source_ids": ["other"]
            })}}
    monkeypatch.setattr("agents.chat.requests.post", lambda *a, **k: Response())
    with pytest.raises(AgentError):
        draft(DraftRequest(topic="Keys", context=context()), retrieve=retrieve(RetrievalResult(question="Keys", course_id=7, top_k=5, results=[hit()])))


def test_draft_uses_installed_chat_model_and_course_sources(monkeypatch):
    captured = {}

    class Response:
        def raise_for_status(self):
            pass
        def json(self):
            return {"done": True, "done_reason": "stop", "message": {"content": json.dumps({
                "title": "Primary keys", "description": "Explain how a primary key identifies a row.", "difficulty": "Beginner", "source_ids": ["c1"]
            })}}

    def post(url, json, timeout):
        captured.update(url=url, payload=json, timeout=timeout)
        return Response()

    monkeypatch.setattr("agents.chat.requests.post", post)
    result = draft(DraftRequest(topic="Keys", context=context()), retrieve=retrieve(RetrievalResult(question="Keys", course_id=7, top_k=5, results=[hit(), hit("foreign", course_id=8)])))
    assert result.status == "drafted"
    assert result.source_ids == ["c1"]
    assert captured["payload"]["model"] == settings.TUTOR_MODEL == "qwen3:1.7b"
    assert "Primary keys identify a row." in captured["payload"]["messages"][1]["content"]
    assert "foreign" not in captured["payload"]["messages"][1]["content"]


def test_review_without_sources_never_calls_model(monkeypatch):
    monkeypatch.setattr("agents.chat.requests.post", lambda *a, **k: (_ for _ in ()).throw(AssertionError("model called")))
    result = review(ReviewRequest(title="Keys", description="Explain a key.", context=context()), retrieve=retrieve(RetrievalResult(question="Keys", course_id=7, top_k=5, results=[hit(score=0.2)])))
    assert result.status == "no_source"


def test_review_accepts_grounded_result(monkeypatch):
    class Response:
        def raise_for_status(self):
            pass
        def json(self):
            return {"done": True, "done_reason": "stop", "message": {"content": json.dumps({
                "status": "revise", "summary": "The task never asks for a key example.", "findings": ["Add one example from the notes."], "source_ids": ["c1"]
            })}}
    monkeypatch.setattr("agents.chat.requests.post", lambda *a, **k: Response())
    result = review(ReviewRequest(title="Keys", description="Explain a key.", context=context()), retrieve=retrieve(RetrievalResult(question="Keys", course_id=7, top_k=5, results=[hit()])))
    assert result.status == "revise"
    assert result.source_ids == ["c1"]
