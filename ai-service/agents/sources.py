import math
from collections.abc import Callable

from rag.retrieval import retrieval_service
from schemas.document import RetrievalRequest, RetrievalResult, RetrievedChunk

MIN_SCORE = 0.60
MAX_SOURCES = 3
MAX_TEXT = 1500


def select_sources(result: RetrievalResult, course_id: int) -> list[RetrievedChunk]:
    chosen: list[RetrievedChunk] = []
    seen: set[str] = set()
    for hit in sorted(result.results, key=lambda item: item.score, reverse=True):
        metadata = hit.metadata
        chunk_id = metadata.chunk_id.strip()
        if (
            metadata.course_id != course_id
            or metadata.material_id <= 0
            or metadata.page_number <= 0
            or not chunk_id
            or chunk_id in seen
            or not hit.text.strip()
            or not math.isfinite(hit.score)
            or hit.score < MIN_SCORE
            or hit.score > 1.000001
        ):
            continue
        seen.add(chunk_id)
        chosen.append(hit)
        if len(chosen) == MAX_SOURCES:
            break
    return chosen


def grounded_sources(
    question: str,
    course_id: int,
    retrieve: Callable[[RetrievalRequest], RetrievalResult] = retrieval_service.retrieve,
) -> list[dict[str, str]]:
    result = retrieve(RetrievalRequest(question=question[:4000], course_id=course_id, top_k=5))
    return [
        {"source_id": hit.metadata.chunk_id.strip(), "text": hit.text.strip()[:MAX_TEXT]}
        for hit in select_sources(result, course_id)
    ]
