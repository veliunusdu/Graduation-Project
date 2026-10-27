# Veli — 6. hafta doküman işleme kontrol listesi

Tarih: 27 Ekim 2026. **Hazırlık hazır; ürün testleri çalıştırılmadı.**
Yayın: [PR #10 — Week 6 test preparation](https://github.com/veliunusdu/Graduation-Project/pull/10). Ekip incelemesi bekleniyor.
İncelenen main: `55c6b4b184cea76baede85332b1218a5ef49f707`.
CourseMaterial/extraction/chunking akışı ve ilgili API sözleşmesi bu sürümde yok.
Aşağıdaki liste ürün kodu geldiğinde uygulanır; bir endpoint veya hata kodu uydurmaz.

## Kapsam ve sahiplik

- Ayşe: öğretmenin derse CourseMaterial yüklemesi, DB/dosya kaydı, izinler ve status.
- Ayşegül: PDF/DOCX/PPTX/TXT extraction, temizleme/chunking, metadata ve embedding.
- Veli: başarılı/başarısız dosyalar, course/material metadata ve işleme durumlarını test eder.

Mevcut öğrenci submission dosyası yüklemesi farklı bir akıştır. Bu testlerde öğretmenin
derse materyal yükleme/işleme özelliği kullanılır. 7. haftanın vector DB retrieval,
top-k ve RAG izolasyon kontrolleri bu listenin kapsamı dışındadır.

## Ön koşullar — gerçek testten önce doldur

- [ ] Ürün kodunun web/Python commit'leri ve çalıştırılacak instance'lar kayıtlı.
- [ ] Gerçek materyal upload, status ve chunk/metin erişim giriş noktaları onaylı.
- [ ] İzin verilen türler, type detection ve maximum_bytes politikası onaylı.
- [ ] TXT encoding ve boş metin davranışı onaylı; sessiz bozuk karakter dönüşümü yok.
- [ ] HTTP/hata eşlemesi ve invalid upload öncesi/sonrası DB kayıt politikası onaylı.
- [ ] Pending/Processing/Ready/Failed mantıksal durumlarının gerçek alan/değerleri onaylı.
- [ ] course_id/material_id ve diğer chunk alanlarının gerçek JSON/DB eşlemesi kayıtlı.
- [ ] İzole DB, geçici upload dizini, sentetik Teacher/Student ve gerçek course ID'leri hazır.
- [ ] Async bekleme/retry bütçesi ve log/correlation kaydı yöntemi belirli.

Bu bilgiler [sonuç şablonunun](VELI_WEEK6_RESULTS_TEMPLATE.json) contract/environment
alanlarına yazılır. Null alanlar takım adına kesinleştirilmiş karar değildir.

## Dosyalar ve önce çalıştırılacak kontrol

[Week6 manifest](../samples/week6-document-processing/manifest.json), mevcut16
extraction dosyasını ve bir zararsız desteklenmeyen .bin dosyasını referanslar.
Dosyalar çoğaltılmadı. Orijinal [fixture paketi](../samples/ai-rag-smoke/README.md)
toplam19 girdi içerir; bunların3 RAG girdisi yalnızca paketin bütünlüğü için korunur.
Orijinal manifest ve kaynak dosyaların SHA-256 değerleri değişmedi.

Opsiyonel referans kontrol ortamı (üretim extraction bağımlılıklarını değiştirmez):

```powershell
$fixtureEnv = Join-Path $env:TEMP 'graduation-fixture-check'
py -m venv $fixtureEnv
& "$fixtureEnv/Scripts/python.exe" -m pip install -r samples/ai-rag-smoke/requirements-fixtures.txt
& "$fixtureEnv/Scripts/python.exe" samples/ai-rag-smoke/verify_fixtures.py
```

Beklenen: fixture_count19; integrity/reference_parser_checks/rag_fixture_consistency
PASS; **product_smoke_tests NOT_RUN**. Bu okuyucular ürün kodu değildir.
.bin dosyası yalnızca sentetik metin taşır; çalıştırılmaz. Hash/boyutlar Week6
manifest'indedir. Kurulum veya model indirme bu hazırlık sırasında yapılmadı.

## Ürün test sırası

1. Şablonu ayrı bir run dosyasına kopyala; orijinali boş şablon olarak koru. Ön koşul
   eksikse BLOCKED bırak. Hazır fakat çağrılmamış vaka NOT_RUN olur.
2. Dört geçerli dosyayı ürün üzerinden işle. Çıkan metinde bütün manifest marker'ları
   ve Türkçe karakterler bulunmalı. Gerekirse LF ve NFC normalizasyonu uygula; içerik
   keyfi değiştirilmez. Beklenen metni ürün çıktısı gibi yazma.
3. Sıfır bayt, açılabilen boş içerik ve bozuk dosyaları ayrı ayrı dene. Her denemede
   material/chunk sayısını önce/sonra kaydet. Kontrollü hata veya onaylı Failed durumunda
   PASS verilebilir; sahte Ready/başarılı boş chunk kabul edilmez.
4. Desteklenmeyen uzantıyı onaylı politika ile reddet. Aynı hatanın önceki başarılı
   materyal/chunk'ları silmediğini kontrol et. HTTP kodunu sözleşmeden al, burada varsayma.
   Geçersiz UTF-8 TXT için beklenti encoding politikasına bağlıdır: açık alternate encoding
   desteği onaylanırsa, beklenen metin/sonuç çalıştırmadan önce güncellenir.
5. Yetkili gerçek course/material kayıtlarını kullanarak tüm chunk metadata'sını denetle.
   Fixture kimlikleri gerçek DB ID'si değildir; material_id kaynak kaydına bağlanmalı.
6. Başarı ve hata durum geçişlerini status endpoint/log/event kanıtıyla kaydet. Hızlı
   Processing durumu polling'de görülmediyse olmuş gibi yazma; eksik kanıtı belirt.
7. Bozuk bir girdiden sonra aynı instance'ta geçerli TXT ile recovery yap. Servis ve
   önceki veriler korunmalı; yeni geçerli iş terminal Ready'ye ulaşmalı.

Ön doğrulamada reddedilen dosya için material kaydı hiç oluşmayabilir. İzin verilen
bu politika onaylandıysa Failed satırı beklemek yerine kontrollü reddi ve değişmeyen
chunk sayısını kanıtla. Metadata gerektiren geçerli girdide ise gerçek material kaydı
zorunludur. Embedding kalitesini ya da RAG retrieval'ı yalnız extraction ile doğrulanmış sayma.

## 21 vaka

| ID | Girdi / senaryo | Hazırlıktaki durum |
| --- | --- | --- |
| W6-EXT-01 | valid: extraction.docx | BLOCKED |
| W6-EXT-02 | valid: extraction.pdf | BLOCKED |
| W6-EXT-03 | valid: extraction.pptx | BLOCKED |
| W6-EXT-04 | valid: extraction.txt | BLOCKED |
| W6-ZERO-01 | zero_byte: empty.docx | BLOCKED |
| W6-ZERO-02 | zero_byte: empty.pdf | BLOCKED |
| W6-ZERO-03 | zero_byte: empty.pptx | BLOCKED |
| W6-ZERO-04 | zero_byte: empty.txt | BLOCKED |
| W6-EMPTY-01 | empty_content: blank.docx | BLOCKED |
| W6-EMPTY-02 | empty_content: blank.pdf | BLOCKED |
| W6-EMPTY-03 | empty_content: blank.pptx | BLOCKED |
| W6-EMPTY-04 | empty_content: whitespace.txt | BLOCKED |
| W6-BAD-01 | corrupt: invalid-utf8.txt | BLOCKED |
| W6-BAD-02 | corrupt: truncated.docx | BLOCKED |
| W6-BAD-03 | corrupt: truncated.pdf | BLOCKED |
| W6-BAD-04 | corrupt: truncated.pptx | BLOCKED |
| W6-TYPE-01 | unsupported_extension: unsupported.bin | BLOCKED |
| W6-META-01 | Stored course/material identity survives extraction and chunking | BLOCKED |
| W6-STATUS-01 | Successful processing lifecycle | BLOCKED |
| W6-STATUS-02 | Failure lifecycle or validation rejection | BLOCKED |
| W6-RECOVERY-01 | Valid upload after invalid upload | BLOCKED |

## Kanıt ve bitiş kriteri

Her ürün çağrısı için web/Python commit'i, zaman, operatör, dosya hash'i, gerçek
course/material/chunk kimlikleri, HTTP sonucu, hata/status, süre, correlation/log
ve önce/sonra veri sayıları kaydedilir. Şifre, token veya gerçek öğrenci içeriği kaydetme.
PASS/FAIL yalnızca gerçek ürüne çağrıdan sonra, beklenen-gerçek karşılaştırmasıyla verilir.

6. hafta görevi, bu21 vakanın beklenen davranışları gerçek ürün üzerinde kanıtlandığında
kapanır. BLOCKED/NOT_RUN, başarılı test değildir. Hazırlık doğrulaması ayrı
[TestEvidence kaydında](TestEvidence/Week6/2026-10-27/preparation-verification.json) bulunur.
