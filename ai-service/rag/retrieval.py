from rag.embedding import OllamaEmbeddingClient, embedding_client
from rag.vector_store import VectorStore, vector_store
from schemas.document import RetrievalRequest, RetrievalResult


class RetrievalService:
    def __init__(
        self,
        embedder: OllamaEmbeddingClient = embedding_client,
        store: VectorStore = vector_store,
    ):
        self.embedder = embedder
        self.store = store

    def retrieve(self, request: RetrievalRequest) -> RetrievalResult:
        query_embedding = self.embedder.embed([request.question])[0]
        results = self.store.search(
            course_id=request.course_id,
            query_embedding=query_embedding,
            top_k=request.top_k,
        )
        return RetrievalResult(
            question=request.question,
            course_id=request.course_id,
            top_k=request.top_k,
            results=results,
        )


retrieval_service = RetrievalService()
