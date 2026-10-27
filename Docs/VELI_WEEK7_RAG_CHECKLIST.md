# Veli — 7. hafta RAG izolasyon ve kalite kontrol listesi

27 Ekim 2026. **Hazırlık hazır; ürün retrieval testleri çalıştırılmadı.**
Yayın: [PR #11 — Week 7 RAG preparation](https://github.com/veliunusdu/Graduation-Project/pull/11). Ekip incelemesi bekleniyor.
İncelenen main: `25f823a31693e6219d0f9f66178c44c49d801976` (6. hafta hazırlık PR10 dahil).
CourseMaterial/indexleme/vector store/retrieval kodu henüz yok; API yolları ve sözleşme
alanları takım adına uydurulmaz. 6. hafta hazırlığının birleşmesi ürünün hazır olduğu anlamına gelmez.

## Hedef ve kapsam

Aynı konuya ait farklı ders dokümanlarıyla zorunlu course filtresini test et;
lise sorusunda üniversite kaynağı ve diğer lise dersinin kaynağı gelmediğini doğrula.
İlgili, ilgisiz ve materyalde yanıtı olmayan soruları kaydet; top-k kaynak metadata'sını
kontrol et. Bu küçük sentinel corpus izolasyon smoke testidir; temsilî anlamsal kalite
benchmark'ı veya Tutor/LLM cevap kalitesi ölçümü değildir.

- Ayşe: yetkili kullanıcı/course context'iyle Python'a çağrı ve kayıtlı olmayan dersin engellenmesi.
- Ayşegül: embedding/vector store, course_id filtreli retrieval, metadata ve idempotency.
- Veli: kaynak/chunk düzeyinde izolasyon, sorgu kalitesi ve metadata kanıtları.

## Ön koşullar

- [ ] Gerçek materyal ingestion/processing ve indeks hazır olma kanıtı mevcut.
- [ ] BGE-M3/gerçek embedding modeli sürümü ve izole vector collection kaydedildi.
- [ ] Retrieval endpoint'i, auth/enrollment kuralları ve input/output alanları onaylı.
- [ ] top-k alanı/minimum/maksimum ve out-of-range davranışı onaylı.
- [ ] Metadata'daki course/material/chunk/page/education/grade eşlemeleri onaylı.
- [ ] Score similarity mi distance mı, sıralama yönü ve relevance threshold politikası onaylı.
- [ ] Duplicate upload/upsert/version davranışı ve count kontrolü onaylı.
- [ ] İzole DB/index/file alanı, iki actor ve dört gerçek course oluşturuldu.

Bunlar [sonuç şablonunun](VELI_WEEK7_RESULTS_TEMPLATE.json) contract/environment
alanlarına yazılır. Boş alanlar onaylı ürün sözleşmesi değildir.

## Corpus ve gerçek ID eşlemesi

[Fixture eşleme şablonu](../samples/week7-rag/corpus-mapping-template.json) dosyaları
çoğaltmadan mevcut üç TXT'yi referanslar:

| Fixture course | Mantıksal ID | Seviye/sınıf | Kaynak / sentinel |
| --- | --- | --- | --- |
| hs-a | 101 | HighSchool 9 | doc-hs-a / HS_A_ONLY_9F31 |
| hs-b | 102 | HighSchool 12 | doc-hs-b / HS_B_ONLY_8C72 |
| uni-a | 201 | University 1 | doc-uni-a / UNI_ONLY_7D64 |
| empty | 999 | HighSchool 9 | Materyalsiz, yetkili gerçek course |

Üç dokümanda aynı konu ve aynı soru kullanılır. Farklı konular filtre eksikliğini
saklayabilir; iki lise course'u yalnız education_level filtresini de yakalar.
Metin gövdesindeki101/102/201 etiketleri gerçek DB ID'si değildir. Metadata'yı dosya
metninden üretip eşitleme; gerçek upload/course/material/chunk ID'lerini kayıttan eşle.

Sadece referans verilen üç TXT'yi kendi course'larına yükle. Beklenti JSON'larını,
manifest'i veya extraction negatif dosyalarını bu corpus'a ingest etme.
`authorized_actor` dört course için de yetkili olsun; pozitif izolasyon testini bir
yetki reddi maskelemesin. `unenrolled_actor`, var olan hs-b course'una kayıtlı olmamalı.
Empty testi için var olmayan/yetkisiz course kullanma:404, boş retrieval sonucu değildir.

## Ürün test sırası

1. [Orijinal fixture kontrolünü](../samples/ai-rag-smoke/README.md) çalıştır.
   Bu kontrol gerçek embedding/vector DB çağırmaz. Ardından ayrı bir sonuç run'ı aç;
   şablonu boş bırak ve gerçek course/material/chunk eşlemesini yeni run'a yaz.
2. Üç kaynağı indeksle; ready/log/count kanıtını ve sentinel taşıyan gerçek chunk ID'lerini
   kaydet. Pozitif sorgular kendi kaynağını gerçekten bulmadan izolasyonu geçti sayma;
   boş/başarısız indeks bütün filtre testlerini yapay biçimde başarılı gösterebilir.
3. Üç ders için aynı ilgili soruyu k=1 ve k=3 ile çalıştır. Her hit'in gerçek course ID'si,
   kaynak ve chunk ID'si eşlenmiş doğru derse ait olmalı; yabancı sentinel bulunmamalı.
4. LiseA↔LiseB ve lise↔üniversite karşılaştırmalarını hit listesi üzerinden yap.
   Yalnızca LLM'nin final cevabında doğru kodun görünmesi retrieval izolasyonu kanıtı değildir.
5. [12 sorgu beklentisindeki](../samples/week7-rag/queries.json) ilgisiz ve materyalde
   olmayan soruları çalıştır. Nearest-neighbor arama düşük alaka ile yine hit döndürebilir;
   onaylı threshold/abstention politikası olmadan sıfır hit zorunlu değildir.
   Her durumda foreign-course hit yasaktır; alaka değerlendirmesini ayrıca kaydet.
6. Yetkili empty course için k=1/3 ile başarılı sıfır-hit yanıtını doğrula. Diğer üç course
   dolu ve pozitif kontroller başarılı olmalı; tüm indeksin boş olması yeterli değildir.
7. Tüm hit'lerde source metadata'yı kontrol et. TXT page alanı null olabilir; sözleşmeyi
   izle, sayfa1 uydurma. Score yönü bilinmeden yüksek/düşük sıralaması ileri sürme.
   Alan gözlenemiyorsa o kontrolü doğrulandı diye işaretleme.
8. AUTH vakasında hs-b'nin gerçek mevcut ID'sini unenrolled_actor ile kullan. KRANGE
   vakasında base sorunun k değerini onaylı sınırların dışına değiştir; hata/clamp kuralını
   sözleşmeden al. Recovery'de reddedilen sorgudan sonra authorized_actor ile hs-a'yı sorgula.
9. Aynı materyali aynı course'a yeniden ingest ederek onaylı idempotency/upsert davranışını
   ve önce/sonra benzersiz chunk sayılarını kaydet; sessiz duplicate hit oluşmamalı.

Query dosyaları mantıksal test beklentileridir, doğrudan ürün payload'ı değildir.
Gerçek request, kimlik ve alan eşlemesi onaylandıktan sonra üretilir. Gereken top-k=3
sorgusunda üçten az kendi kaynak hit'i bulunması tek başına hata değildir; en fazla k
ve pozitif kaynağın bulunması kontrol edilir. LLM'ye otomatik tamamlama yaptırıp
retrieval eksikliğini gizleme. Sorgu sırası/sonuç ID'leri ve request/correlation kayıtları saklanır.

## 18 vaka

| ID | Senaryo | Başlangıç durumu |
| --- | --- | --- |
| W7-REL-01 | relevant: hs-a / k=1 | BLOCKED |
| W7-REL-02 | relevant: hs-a / k=3 | BLOCKED |
| W7-REL-03 | relevant: hs-b / k=1 | BLOCKED |
| W7-REL-04 | relevant: hs-b / k=3 | BLOCKED |
| W7-REL-05 | relevant: uni-a / k=1 | BLOCKED |
| W7-REL-06 | relevant: uni-a / k=3 | BLOCKED |
| W7-UNRELATED-01 | unrelated: hs-a / k=3 | BLOCKED |
| W7-ABSENT-01 | absent_in_material: hs-a / k=3 | BLOCKED |
| W7-UNRELATED-02 | unrelated: uni-a / k=3 | BLOCKED |
| W7-ABSENT-02 | absent_in_material: uni-a / k=3 | BLOCKED |
| W7-EMPTY-01 | authorized_empty_course: empty / k=1 | BLOCKED |
| W7-EMPTY-02 | authorized_empty_course: empty / k=3 | BLOCKED |
| W7-META-01 | Top-k source metadata | BLOCKED |
| W7-ORDER-01 | Ranking and score semantics | BLOCKED |
| W7-AUTH-01 | Existing but unauthorized course | BLOCKED |
| W7-DEDUP-01 | Repeated material ingestion | BLOCKED |
| W7-KRANGE-01 | Top-k limits | BLOCKED |
| W7-RECOVERY-01 | Valid query after rejected input | BLOCKED |

## Kanıt ve bitiş kriteri

Her vaka için gerçek request ve retrieval hit listesi, course/material/chunk eşlemesi,
metadata, score/alaka incelemesi, HTTP durum, süre, önce/sonra count ve ilgili loglar
kaydedilir. Şifre/token/gerçek öğrenci içeriği kaydedilmez. PASS/FAIL yalnızca ürün
çağrısından ve beklenen-gerçek karşılaştırmasından sonra verilir. BLOCKED ve NOT_RUN
başarılı test değildir. Template'deki boş ID listeleri ölçülmüş sıfır sonuç anlamına gelmez.

Kapanış: pozitif kaynak isabetleri kanıtlı; course izolasyon ihlali yok; ilgili/ilgisiz/yanıtı
olmayan sorular ve metadata sonuçları onaylı politikayla raporlu. Bu18 vakanın gerçek ürün
karşılığı ve kanıtları tamamlanmadan 7. hafta kapatılmaz. Hazırlık doğrulaması ayrı
[kayıtta](TestEvidence/Week7/2026-10-27/preparation-verification.json) tutulur.
