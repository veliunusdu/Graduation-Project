# Ayşe - 4. hafta teslimi

## Kapsam
- Assignment: Difficulty (Beginner / Intermediate / Advanced), DueDate, CreatedByAI.
- AssignmentRequirement: Id, AssignmentId, Title, Description, Weight, Order; ödevle ilişki ve silmede cascade.
- Öğretmen ödev formunda gereksinim ekleyebilir, değiştirebilir ve kaldırabilir. Ödev silme mevcut teslim korumasını korur.
- Gereksinimler ödev detayında öğretmene ve kayıtlı öğrenciye sıralı gösterilir. Ağırlıklar değerlendirme ölçütünü oluşturur; ayrı AI rubric tablosu bu haftanın Ayşe kapsamında değildir.
- Öğretmen teslimi inceler, 0-100 not ve geri bildirim kaydeder. Öğrenci kendi tesliminde sonucu görür. Teslim değiştirilirse değerlendirme sıfırlanır.
- CreatedByAI öğretmen formundan bağlanmaz; manuel oluşturulan ödevlerde false kalır.
- Yeni/değiştirilen ödevlerde teslim tarihi, 1-100 gereksinim, benzersiz sıra ve toplam %100 ağırlık zorunludur.

## Veritabanı
AssignmentRequirements ve TeacherReview migration dosyaları eklenmiştir. Eski ödevlerde DueDate nullable bırakılmıştır; bilinmeyen tarihe yapay değer atanmaz. Eski ödev düzenlenirken öğretmen tarih ve gereksinimleri tamamlamalıdır.

```powershell
dotnet tool restore
dotnet ef database update --project web --startup-project web
dotnet run --project web
```

Mevcut veritabanında geçişten önce yedek alın. Uygulama mevcut başlangıç migration davranışını korur. Gereksinimler ödev düzenlemesinde aynı işlem içinde yenilenir; ileride gereksinim Id değerlerine bağlı AI bulguları eklendiğinde güncelleme stratejisi yeniden ele alınmalıdır.

## Demo
1. Öğretmen olarak giriş yap; ders oluştur ve öğrenciyi kaydet.
2. Dersin ödevlerinden yeni ödev aç; zorluk ve tarih seç.
3. İki gereksinim ekle: sıra 1 / ağırlık 40 ve sıra 2 / ağırlık 60; kaydet.
4. Öğrenci olarak kayıtlı dersteki ödevi aç ve teslim oluştur.
5. Öğretmen olarak Student submissions -> teslim inceleme ekranına geç; not ve geri bildirim kaydet.
6. Öğrenci olarak teslim detayında değerlendirmeyi gör.

## Doğrulama
Week4AssignmentTests: geçersiz zorluk, eksik tarih, hatalı toplam, tekrar sıra, boş başlık, gereksinimsiz ödev, geçersiz not, başka öğretmenin değerlendirme girişimi ve AI olmadan uçtan uca akış.
TeacherOwnershipTests içindeki geçerli ödev girdileri yeni zorunlu alanlara uyarlanmıştır.
Test sonuçları: Docs/TestEvidence/Week4/ayse-week4.trx.

Sonuç: 115 test başarılı, 0 başarısız, 0 atlanan. Derleme başarılı; bekleyen model değişikliği yok. İlk kısıtlı ortam denemesi testhost bağlantı zaman aşımı nedeniyle çalışmadı; normal ortamda tüm testler başarıyla tamamlandı.

## Yerel denemeler sonrası düzeltmeler
Öğretmen teslim listesi ve değerlendirme ekranına öğrenci e-postası eklendi. Tarayıcı çevirisinin oluşturduğu iç öğelere tıklamada gereksinim kaldırma düzeltildi; tarayıcıda kaldırma, yeniden ekleme ve alan indeksleri kontrol edildi. Silinmesi engellenen ödevin uyarısı aynı silme ekranında düğmenin üstünde gösteriliyor. Kullanıcı yerel ortamda gereksinim kaldırmayı, teslimi olmayan ödevin silinmesini ve teslimi olan ödevin korunmasını doğruladı.
