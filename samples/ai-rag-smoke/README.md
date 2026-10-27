# AI ve RAG smoke test girdileri

Bu paket Veli'nin test hazırlığıdır. İçerikler sentetiktir; gerçek öğrenci verisi içermez. Ürün AI/RAG smoke testleri henüz çalıştırılmadı. 6. hafta ürün kodu hazır olduğunda [doküman işleme kontrol listesi](../../Docs/VELI_WEEK6_DOCUMENT_PROCESSING.md) izlenecek. RAG dosyaları sonraki haftaya ait hazırlıktır; bu paketin bütünlüğünü korumak için tutulur.

- valid: aynı dört beklenen metni içeren PDF, DOCX, tek slaytlı PPTX ve UTF-8 TXT.
- zero-byte: dört formatta gerçekten sıfır bayt dosyalar.
- empty-content: açılabilen fakat görünür gövde metni olmayan PDF/DOCX/PPTX ve yalnızca boşluk içeren TXT. Sıfır bayt dosyalardan farklı bir hata yoludur.
- corrupt: kesilmiş PDF, bozuk DOCX/PPTX ZIP paketleri ve UTF-8 olarak çözülemeyen TXT. TXT encoding politikası takımca netleştirilecek.
- rag: aynı konuyu içeren iki lise course'u ve bir üniversite course'u. Benzersiz sentinel'ler course filtresinin gerçekten çalışıp çalışmadığını gösterir.
- manifest.json: her girdi için SHA-256, byte boyutu, tür ve beklenen metinler.
- rag/corpus.json ve queries.json: mantıksal fixture ID'leri ve sorgu beklentileri. Gerçek kayıt ve chunk şeması değiller. Test başında yetkili gerçek course/document ID'lerine eşleştir.

## Fixture kontrolü

Doğrulayıcı pypdf 6.10.0 ve Python 3.14.3 ile doğrulandı. PDF için pypdf gerekir; DOCX/PPTX metni standart kütüphaneyle XML üzerinden kontrol edilir. Bu referans okuyucular ürün extraction kodunu çağırmaz.

İsteğe bağlı, proje bağımlılıklarından ayrı ortam:

```powershell
Set-Location 'C:\Codes\Projects\Graduation Project'
python -m venv "$env:TEMP\graduation-fixture-check"
& "$env:TEMP\graduation-fixture-check\Scripts\python.exe" -m pip install pypdf==6.10.0
& "$env:TEMP\graduation-fixture-check\Scripts\python.exe" samples/ai-rag-smoke/verify_fixtures.py
```

Beklenen: 19 fixture, integrity/reference_parser_checks/rag_fixture_consistency PASS; product_smoke_tests NOT_RUN. Bu çıktı pipeline, ürün extraction veya RAG testlerinin geçtiği anlamına gelmez.

## RAG veri yükleme

101, 102, 201 ve 999 fixture ID'leridir. Aynı test Teacher'ının erişebildiği iki lise course'u, bir üniversite course'u ve boş course oluştur. Gerçek ID eşlemesini sonuç kaydına yaz. Boş course 999 için var olmayan veya yetkisiz kayıt kullanma; böyle bir 404, boş retrieval sonucu yerine geçmez.

Yalnızca rag içindeki üç TXT'yi kendi eşlenen course'larına yükle. Aynı konuyu bütün course'larda kullan; farklı konular course filtresi olmayan bir sistemi yanlışlıkla başarılı gösterebilir. Extraction valid dosyalarını veya negatif dosyaları bu RAG corpus'una karıştırma.

Chunk ID'lerini indeksleme cevabından kaydet. Chunk sınırları bilinmiyor; sentinel içeren chunk'ları doğrulamada kullan. Sorgu yanıtındaki kaynak/chunk metinlerini denetle; yalnızca LLM cevabında sentinel aramak retrieval izolasyonunu kanıtlamaz.
