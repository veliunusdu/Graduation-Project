# Ayşegül — Hafta 7: Vector Store ve RAG Retrieval

## Kalıcı store

Python standart kütüphanesindeki SQLite kullanılır. Varsayılan dosya
`ai-service/data/vector_store.db`, ayar ise `VECTOR_DB_PATH` değeridir.

Bu seçim:

- MacBook Air M2 / 8 GB ortamında ek bir servis ve yüksek bellek tüketimi istemez.
- Week 6 `VectorStore` adaptörünü korur.
- Transaction, primary key ve indekslerle kalıcılık/idempotency sağlar.
- Sonraki aşamada başka bir vector DB adaptörüne geçişi engellemez.

Embedding vektörleri SQLite'ta kalıcı tutulur. Aramada aday kayıtlar önce zorunlu
`WHERE course_id = ?` koşuluyla seçilir, cosine similarity yalnızca bu dersin
adayları üzerinde hesaplanır.

## İndeksleme ve idempotency

`POST /documents/process` Week 6 akışını kullanmaya devam eder. Pipeline çıktısı
artık kalıcı SQLite store'a yazılır. `chunk_id` primary key'dir. Bir materyal
yeniden işlendiğinde o materyalin eski chunk'ları aynı transaction içinde
yenileriyle değiştirilir; duplicate veya artık geçerli olmayan eski chunk kalmaz.

## Retrieval API

`POST /rag/retrieve`

```json
{
  "question": "Bu derste süreklilik doğrulama kodu nedir?",
  "courseId": 101,
  "topK": 3
}
```

ASP.NET uyumu için camelCase alanlar ve Python istemcileri için `course_id` /
`top_k` karşılıkları kabul edilir. Ders kimliği zorunlu ve pozitif, top-k 1–20
aralığındadır. Cevap, sıralanmış
chunk metnini, cosine skorunu ve şu kaynak metadata'sını içerir:

- `course_id`
- `education_level`
- `grade_level`
- `material_id`
- `page_number`
- `chunk_id`

API yalnız retrieval sonucu döndürür; Tutor/LLM cevap üretimi sonraki haftanın
kapsamındadır.
