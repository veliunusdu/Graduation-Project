import json
from collections.abc import Callable
from typing import Literal

from pydantic import BaseModel, ConfigDict, Field, model_validator

from agents.chat import AgentError, complete_json
from agents.sources import grounded_sources
from schemas.context import AgentContext
from schemas.document import RetrievalRequest, RetrievalResult


class ReviewRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")
    title: str = Field(min_length=1, max_length=200)
    description: str = Field(min_length=1, max_length=10000)
    context: AgentContext

    @model_validator(mode="after")
    def text_must_be_present(self):
        self.title = self.title.strip()
        self.description = self.description.strip()
        if not self.title or not self.description:
            raise ValueError("Assignment title and description must contain text.")
        return self


class _GeneratedReview(BaseModel):
    model_config = ConfigDict(extra="forbid")
    status: Literal["ready", "revise"]
    summary: str = Field(min_length=1, max_length=2000)
    findings: list[str] = Field(min_length=1, max_length=5)
    source_ids: list[str] = Field(min_length=1, max_length=3)


class ReviewResponse(BaseModel):
    status: Literal["ready", "revise", "no_source"]
    summary: str
    findings: list[str] = Field(default_factory=list)
    source_ids: list[str] = Field(default_factory=list)


SYSTEM = (
    "Eklenmiş bir ödevi ders kaynaklarına göre denetleyen öğretmensin. Ödevi yeniden yazma ve çözüm üretme. "
    "Kaynakla çelişen, kaynaksız veya belirsiz noktaları belirt. Uygunsa status ready, düzeltilmesi gerekiyorsa revise olsun. "
    "source_ids yalnızca verilen kaynak kimliklerinden seçilsin. JSON şemasına uy."
)


def review(
    request: ReviewRequest,
    retrieve: Callable[[RetrievalRequest], RetrievalResult] | None = None,
) -> ReviewResponse:
    question = f"{request.title}\n{request.description}"
    retrieve_args = {} if retrieve is None else {"retrieve": retrieve}
    sources = grounded_sources(question, request.context.course_id, **retrieve_args)
    if not sources:
        return ReviewResponse(status="no_source", summary="Bu ödev için uygun ders kaynağı bulunamadı.")
    allowed = {source["source_id"] for source in sources}
    generated = complete_json(
        _GeneratedReview,
        SYSTEM,
        json.dumps(
            {
                "title": request.title,
                "description": request.description,
                "education_level": request.context.education_level,
                "grade_level": request.context.grade_level,
                "sources": sources,
            },
            ensure_ascii=False,
        ),
    )
    if not isinstance(generated, _GeneratedReview) or not set(generated.source_ids).issubset(allowed):
        raise AgentError("Agent yanıtı tamamlanamadı veya doğrulanamadı.")
    return ReviewResponse(
        status=generated.status,
        summary=generated.summary.strip(),
        findings=[item.strip() for item in generated.findings if item.strip()],
        source_ids=generated.source_ids,
    )
