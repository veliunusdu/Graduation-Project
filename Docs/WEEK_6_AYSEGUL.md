# Ayşegül — Hafta 6: doküman işleme ve embedding

## Sorumluluk

- PDF, DOCX, PPTX ve UTF-8 TXT dosyalarından metin çıkarma
- Metni temizleyip örtüşmeli parçalara bölme
- Her parçaya `course_id`, `education_level`, `grade_level`, `material_id`,
  `page_number` ve `chunk_id` metadata'sı ekleme
- Ollama üzerindeki BGE-M3 ile embedding üretme
- Embedded chunk'ları vector-store adaptörüne iletme

CourseMaterial upload/DB/status işleri Ayşe'nin, kalıcı vector DB ve retrieval ise
7. haftanın kapsamındadır.

## Python sözleşmesi

`POST /documents/process` multipart isteği:

- `file`
- `material_id`
- `course_id`
- `education_level`: `HighSchool` veya `University`
- `grade_level`: lise için 9–12, üniversite için 1–4

Başarılı cevap chunk sayısını, `bge-m3` modelini, embedding boyutunu ve chunk
metadata listesini döndürür. Okunamayan/boş içerik 422, embedding servisi hatası
502 döndürür. `DOCUMENT_MAX_FILE_SIZE_BYTES` ile yapılandırılan dosya boyutu
limiti aşıldığında 413 döndürülür.

## Doğrulama

- Ollama gerektirmeyen Python test paketi: 69 test başarılı, 1 Ollama testi
  ayrıştırıldı.
- Komut: `python -m pytest -q -m "not ollama"`
- Yerel Ollama ve BGE-M3 gerektiren opt-in test:
  `RUN_OLLAMA_TESTS=1 python -m pytest -q -m ollama`
- Dört geçerli fixture formatında extraction doğrulandı.
- Sıfır bayt, boş içerik, bozuk içerik ve desteklenmeyen türler kontrollü
  reddediliyor.
- Gerçek `extraction.pdf -> chunk -> bge-m3 -> vector-store adapter` smoke testi:
  1 chunk, 1024 boyutlu embedding.

Kalıcı vector DB henüz eklenmedi; Week 6 adaptörü bellekte tutar ve Week 7'de
kalıcı store ile değiştirilecektir.
