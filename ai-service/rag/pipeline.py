from config.settings import settings
from rag.document_processing import chunk_pages, extract_text
from rag.embedding import OllamaEmbeddingClient, embedding_client
from rag.vector_store import VectorStore, vector_store
from schemas.context import AgentContext
from schemas.document import (
    ChunkMetadata,
    DocumentProcessingResult,
    ProcessedChunk,
)


class DocumentPipeline:
    def __init__(
        self,
        embedder: OllamaEmbeddingClient = embedding_client,
        store: VectorStore = vector_store,
    ):
        self.embedder = embedder
        self.store = store

    def process(
        self,
        *,
        content: bytes,
        filename: str,
        material_id: int,
        context: AgentContext,
    ) -> DocumentProcessingResult:
        if material_id <= 0:
            raise ValueError("material_id must be positive.")

        pages = extract_text(content, filename)
        text_chunks = chunk_pages(
            pages,
            chunk_size=settings.DOCUMENT_CHUNK_SIZE,
            overlap=settings.DOCUMENT_CHUNK_OVERLAP,
        )
        embeddings = self.embedder.embed([chunk.text for chunk in text_chunks])

        processed = []
        for index, (chunk, embedding) in enumerate(
            zip(text_chunks, embeddings, strict=True),
            start=1,
        ):
            metadata = ChunkMetadata(
                course_id=context.course_id,
                education_level=context.education_level,
                grade_level=context.grade_level,
                material_id=material_id,
                page_number=chunk.page_number,
                chunk_id=f"{material_id}:{index}",
            )
            processed.append(
                ProcessedChunk(
                    text=chunk.text,
                    metadata=metadata,
                    embedding=embedding,
                )
            )

        self.store.add(processed)
        dimensions = len(processed[0].embedding) if processed else 0
        return DocumentProcessingResult(
            material_id=material_id,
            chunk_count=len(processed),
            embedding_model=self.embedder.model,
            embedding_dimensions=dimensions,
            chunks=[chunk.metadata for chunk in processed],
        )


document_pipeline = DocumentPipeline()
