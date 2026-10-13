# Ayşe - 3. hafta: eğitim seviyesi, ders ve öğrenci kaydı

## Tamamlanan kapsam

- `EducationLevel`: HighSchool (0), University (1).
- `Course`: Id, Name, Description, EducationLevel, GradeLevel, TeacherId, CreatedAt, IsActive.
- Lise için 9/10/11/12; üniversite için 1/2/3/4. Form doğrulaması ve veritabanı CHECK constraint'i geçersiz eşleşmeleri reddeder.
- `StudentCourse`: mevcut StudentId, CourseId, EnrolledAt alanları ve birleşik anahtar korundu. Aynı öğrenci aynı derse ikinci kez kaydedilmez.
- Öğretmen kendi dersini oluşturur, düzenler, siler ve mevcut Student hesabını e-posta ile derse kaydeder. TeacherId oturumdan alınır.
- Öğrenci panelindeki **My enrolled courses** bağlantısı `/StudentCourses` ekranını açar. Yalnızca oturumdaki öğrencinin StudentCourse kaydı bulunan aktif dersler listelenir. Detay ekranı dersin ödevlerini gösterir.
- Kayıtlı olmayan veya pasif dersin detayına ve ödevine doğrudan URL ile erişim 404 döndürür. Öğretmen kendi pasif dersine erişebilir ve yeniden etkinleştirebilir.
- Pasif derse yeni kayıt 409, yeni ödev teslimi 404 döndürür. Pasifleştirme mevcut kayıt/ödev/teslimleri silmez. Öğrenci geçmiş teslimlerini kendi teslimler ekranında görmeye ve düzenlemeye devam eder.
- Öğrenci kaydını öğretmen yapar; kendi kendine kayıt akışı eklenmedi.
- Python/AI servisi değişmedi.

## Çalıştırma

ZIP'i çıkartıp `HomeworkPlatform.sln` bulunan klasörde terminal açın:

```powershell
dotnet restore
dotnet build --no-restore
dotnet tool restore
dotnet ef database update --project web
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:DOTNET_ENVIRONMENT = 'Development'
dotnet run --project web --no-launch-profile --urls http://127.0.0.1:5080
```

Tarayıcı: http://127.0.0.1:5080

Uygulama başlangıcında mevcut mekanizma migration'ları da uygular. Mevcut veritabanınızı yükseltmeden önce yedekleyin. Yeni migration önceki migration'ları değiştirmez ve Identity hesaplarını korur.

Gereksinim: .NET SDK 8.0.425 veya daha yeni kararlı SDK; çalıştırmak için ASP.NET Core 8 runtime. `global.json` latestMajor seçimiyle bu bilgisayardaki SDK 9.0.200 kullanılarak doğrulandı; hedef net8.0 kaldı.

Öğretmen hesabı oluşturma için `Docs/AUTHENTICATION_SETUP.md` adımlarını kullanın. Öğrenci hesabı Register ekranından açılır. Teacher/Student rolleri ve önceki sahiplik kontrolleri korundu.

## Migration ve eski dersler

Migration: `CourseEducation`. Eğitim bilgisi olmayan eski derslere **HighSchool / 9 / aktif** başlangıç değeri atanır. Bu bir veri taşıma varsayılanıdır; derslerin gerçek eğitim seviyesi olduğu anlamına gelmez. Eski dersleri öğretmen hesabında Edit ile doğru seviye/sınıfa güncelleyin. Mevcut ders kimlikleri, öğretmen sahipliği, kayıt tarihleri, öğrenci kayıtları, ödev ve teslim içerikleri korunur.

Veritabanı sıfırlama veya migration silme gerekmez. SQLite tablo yeniden oluşturma işleminin bağımlı kayıtları koruduğu gerçek migration testiyle kontrol edildi.

## Kısa deneme

1. Öğretmen ile giriş yapıp My courses > Create course açın.
2. High School / 9 ve University / 1 seçenekleriyle iki ders oluşturun.
3. Kayıtlı bir Student e-postasını yalnızca ilk derse ekleyin.
4. Öğrenci ile giriş yapıp My enrolled courses açın. Yalnızca ilk ders görünmeli; diğer dersin detay URL'si 404 olmalı.
5. Öğretmen ilk dersi pasif yapınca öğrenci ders ve ödev listelerinde görememeli. Yeni kayıt/teslim engellenmeli. Yeniden aktif yapınca önceki kayıt üzerinden erişim geri gelmeli.
6. University / 12 gibi yanlış eşleşme kaydedilmemeli.
7. Başka öğretmen ders detayına, düzenleme/silme/kayıt işlemlerine erişememeli.

## Otomatik doğrulama

```powershell
dotnet test HomeworkPlatform.sln
dotnet ef migrations has-pending-model-changes --project web
```

12 Ekim 2026 doğrulaması: ana test çalıştırmasında 101/101 başarılı; son eklenen öğrenci ders ekranı rol testi ayrıca başarılı (toplam 102 farklı test senaryosu). Model ile migration snapshot'ı arasında bekleyen değişiklik yok.

Test ortamına özel Data Protection anahtarları bellekte tutulur; testler kullanıcı profilindeki anahtar klasörüne yazmaz. Uygulamanın gerçek kimlik doğrulama yapılandırması değişmedi. Test form yardımcısı başarısız sayfanın içeriğini hata mesajına ekleyerek teşhisi kolaylaştırır.

Testler; seviye/sınıf uç değerleri, yanlış değerlerin create/edit sırasında reddi, TeacherId sahteciliği, öğrenci ders izolasyonu, pasif ders, yeniden aktivasyon, mükerrer kayıt, yanlış rol, migration ile veri korunması ve veritabanı kısıtını kapsar. Önceki haftaların kimlik ve sahiplik testleri de çalıştırıldı.


## Migration komutu düzeltmesi

Tasarım zamanı factory artık geçici bellekteki veritabanı yerine uygulamayla aynı `web/App_Data/homework.db` dosyasını kullanır. appsettings ve ortam değişkenindeki DefaultConnection ayarları okunur. Proje kökünden `dotnet ef database update --project web` komutu doğrulandı. Ayrıca yeni bir test veritabanında üç migration başarıyla uygulandı. Mevcut veritabanını silmek veya migration dosyalarını kaldırmak gerekmez.
