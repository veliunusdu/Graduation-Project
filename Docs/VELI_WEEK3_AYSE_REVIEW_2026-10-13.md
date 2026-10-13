# Veli — Ayşe Week 3 pull ve kontrol raporu

Tarih: 13 Ekim 2026. Proje: ödev platformu (`Graduation-Project`).

## Sonuç

Ayşe'nin [PR #3](https://github.com/veliunusdu/Graduation-Project/pull/3) değişiklikleri `main` dalından fast-forward pull edildi. İncelenen HEAD: `ec7ae36c5644da373e7de9d68d5a1f6fd39e1fc2`; Ayşe'nin geliştirme commit'i: `bf12c25`.

Yerel `feature/ownership` çalışma dalı korunarak güncellendi. README'deki yerel AI/RAG notları ve önceki test hazırlığı dosyaları korundu. Yerel main/uzak feature dallarına push yapılmadı.

**Ayşe'nin mevcut suite'i: 102/102 Debug başarılı.** Veli'nin fixture verisini kullanan altı ek kontrol sonrası **108/108 Debug ve 108/108 Release başarılı; 0 hata, 0 skipped**. EF modeli ile migration snapshot'ı tutarlı. Kritik veya işlevi engelleyen bir sorun bu kapsamda bulunmadı; iki görünüm dosyası için bir Minor kodlama kaydı açık.

## Önceki altı ön koşul çözüldü

- Course artık EducationLevel, GradeLevel ve IsActive içeriyor; HighSchool 9-12 ve University 1-4 hem formda hem DB kısıtıyla doğrulanıyor.
- Öğrenci `/StudentCourses` listesi/detayı yalnızca kayıtlı ve aktif course'ları getiriyor. Kayıtsız veya pasif detay erişimi 404.
- Öğrenci ödev listesi/detayı da aktif course şartına bağlı.
- Öğretmenin kendi pasif course'una erişimi ve yeniden aktivasyonu mevcut. Pasif course'a yeni enrollment 409; yeni submission 404.
- Pasifleştirme mevcut enrollment, assignment ve submission kayıtlarını silmiyor. Öğrenci eski kendi teslimlerini okuyup düzenleyebiliyor; yeniden aktivasyon mevcut kayıtla ders erişimini açıyor.

## Veli'nin ek testleri

[Test dosyası](../tests/HomeworkPlatform.Web.Tests/VeliWeek3CourseEnrollmentTests.cs), [kaydedilmiş fixture'ı](../samples/week3-course-enrollment/test-data.json) test çıktı dizinine kopyalanan sürümünden okur. Ürün import endpoint'i eklenmedi.

1. Gerçek HighSchool/9 ve University/1 kayıtları; öğrenci yalnızca lise course'una kayıtlı. Listede diğer ders görünmez ve doğrudan detay erişimi 404. Kayıtsız ikinci öğrenci hiçbirini göremez.
2. Dört geçersiz sınır: HighSchool 8/13 ve University 0/5. Create ve Edit reddedilir; mevcut dersin seviye/sınıf/sahiplik bilgisi değişmez.
3. Pasif ders yeni enrollment/teslimi engeller; enrollment tarihi ve eski teslim içerik/tarih/parent kayıtları korunur. Geçmiş teslim düzenleme ve yeniden aktivasyon sonrası erişim doğrulanır.

Bunlar altı xUnit sonucu üretir (iki Fact ve dört Theory vakası). Önceki yetki, TeacherId sahteciliği, enrollment idempotency ve migration ile veri koruma testleri de tam suite içinde yeniden geçti.

## Senaryo matrisi

Önceki hazırlıkta 6 BLOCKED olan kayıtlar artık gerçekten test edildi. **13/13 senaryo PASS, 0 BLOCKED.**

| ID | Senaryo | Durum | Önceki kayıt |
| --- | --- | --- | --- |
| W3-01 | Lise ve üniversite course verisi | PASS | BLOCKED → PASS |
| W3-02 | Sınıf/yıl aralığı doğrulaması | PASS | BLOCKED → PASS |
| W3-03 | Öğrenci yalnızca kayıtlı dersini listeler | PASS | BLOCKED → PASS |
| W3-04 | Kayıtsız course doğrudan erişimi | PASS | BLOCKED → PASS |
| W3-05 | Kayıtsız dersin ödevine/teslimine erişim engeli | PASS | Yeniden doğrulandı |
| W3-06 | Öğretmenin kendi course CRUD akışı | PASS | Yeniden doğrulandı |
| W3-07 | Başka öğretmenin course kaydına erişim | PASS | Yeniden doğrulandı |
| W3-08 | Sahte TeacherId veya body Id | PASS | Yeniden doğrulandı |
| W3-09 | Pozitif enrollment erişimi açar | PASS | Yeniden doğrulandı |
| W3-10 | Yetkisiz, hatalı ve tekrarlı enrollment | PASS | Yeniden doğrulandı |
| W3-11 | Pasif course liste/detail davranışı | PASS | BLOCKED → PASS |
| W3-12 | Pasif course enrollment/submission davranışı | PASS | BLOCKED → PASS |
| W3-13 | Rol ve form güvenliği | PASS | Yeniden doğrulandı |

Test isimleri, commit ve kanıt hash'leri [makine okunabilir kayıtta](VELI_WEEK3_AFTER_PULL_2026-10-13.json).

## Kanıtlar

- [Ayşe kodu ilk Debug çalıştırması](TestEvidence/Week3/2026-10-13/ayse-week3-debug.trx): 102/102.
- [Veli kontrolleri dahil Debug](TestEvidence/Week3/2026-10-13/veli-week3-after-pull-debug.trx): 108/108.
- [Veli kontrolleri dahil Release](TestEvidence/Week3/2026-10-13/veli-week3-after-pull-release.trx): 108/108.
- [EF model/snapshot kontrolü](TestEvidence/Week3/2026-10-13/ayse-week3-model-check.txt): bekleyen model değişikliği yok. `dotnet-ef` 8.0.31 manifest'ten restore edildi; kontrol `Data Source=:memory:` ile yapıldı.

Testler gerçek Identity/cookie/antiforgery ve izole SQLite kullanan WebApplicationFactory üzerinden HTTP çağrılarıdır. Gerçek `homework.db` güncellenmedi veya sıfırlanmadı. İnteraktif browser/görsel QA ayrıca yapılmadı.

## Küçük inceleme bulgusu

[TEST-002](issues/TEST-002-student-course-view-encoding.md): `StudentCourses/Index.cshtml:5` ve `Details.cshtml:4` kaynakları strict UTF-8 decode kontrolünde başarısız. Windows-1252 ayraç byte'ları var. UTF-8'e çevrilmeleri öneriliyor; ürün kodu bu incelemede değiştirilmedi. Fonksiyonel PASS sonucu bu metin/kodlama bulgusunu kapatmıyor.

## Migration notu

Eski course kayıtları HighSchool / 9 / aktif varsayılanıyla yükseltiliyor. Bu, dersin gerçek seviyesi olarak kabul edilmemeli; gerekiyorsa öğretmen Edit ekranında düzeltmeli. Ayşe'nin migration testinde eski course, enrollment, assignment ve submission verileri korunuyor.

## Görev durumu

Veli'nin üçüncü hafta course/enrollment test görevi işlevsel kapsamda tamamlandı ve sonuçlar kayıtlı. Önceki [hazırlık raporu](VELI_WEEK3_COURSE_ENROLLMENT.md) ve [önceki sonuç JSON'u](VELI_WEEK3_RESULTS_2026-10-13.json) pull öncesi tarihsel kayıttır; güncel durum bu rapordur. Küçük kodlama bulgusu ayrı olarak açık tutuluyor.

İnceleme sırasında commit veya push yapılmamıştı. Veli sonraki mesajında yayınlamayı istedi; test/fixture ve rapor ekleri `test/veli-week3-course-enrollment` dalında PR ile sunuluyor. Küçük kodlama issue'su yerel rapor kaydı olarak kalır; Ayşe'ye mesaj gönderilmedi.

## Push öncesi son doğrulama

108/108 Debug ve 108/108 Release yeniden geçti. [Debug TRX](TestEvidence/Week3/2026-10-13/veli-week3-push-debug.trx) ve [Release TRX](TestEvidence/Week3/2026-10-13/veli-week3-push-release.trx). Ayrıntılar sonuç JSON'unun publication_verification alanındadır. Ham kanıtlar Git newline dönüşümünden korunur; kaydedilen SHA-256 değerleri checkout sonrasında da doğrulanabilir.
