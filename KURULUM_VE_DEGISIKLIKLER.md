# Hazır hesaplar ve derslerle EduPilot

## Çalıştırma

ZIP'i çıkartın, `Graduation-Project-main/HomeworkPlatform.sln` dosyasını Visual Studio ile açın. Başlangıç projesi `HomeworkPlatform.Web` seçiliyken F5 ile çalıştırın. İlk açılışta veritabanı güncellenir; örnek hesaplar, dersler ve öğrenci ders kayıtları hazırlanır.

PowerShell ile çalıştırmak için proje ana klasöründe:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:DOTNET_ENVIRONMENT = 'Development'
dotnet run --project web/HomeworkPlatform.Web.csproj --no-launch-profile --urls http://localhost:5080
```

Tarayıcıda http://localhost:5080 adresini açın. Karşılama ekranındaki Sign in bağlantısıyla giriş yapın. Doğrudan giriş adresi: http://localhost:5080/Account/Login

## Örnek giriş bilgileri

| Rol | Ad soyad | E-posta | Şifre |
|---|---|---|---|
| Öğretmen | Deniz Kaya | teacher@edupilot.local | Teacher-Start8! |
| Öğrenci | Ayşe Yılmaz | student1@edupilot.local | Student-Start8! |
| Öğrenci | Mert Demir | student2@edupilot.local | Student-Start8! |
| Öğrenci | Elif Aydın | student3@edupilot.local | Student-Start8! |

Bunlar yerel geliştirme için örnek hesaplardır. Örnek veri hazırlama yalnızca Development ortamında etkindir; Production ortamında otomatik örnek hesap açılmaz. Veritabanında şifrelerin kendisi değil, Identity'nin oluşturduğu şifre özetleri saklanır.

## Yeni işleyiş

- Kayıt ve profil tamamlama ekranları kaldırıldı. Öğrenci ve öğretmen hazır e-posta/şifreyle doğrudan giriş yapar.
- Ad, soyad, e-posta, öğrencinin sınıfı ve ders bağlantıları veritabanında bulunur. Panelde bu bilgiler gösterilir.
- Öğretmen başlangıçta Introduction to Programming, Database Management ve Mathematics derslerini görür.
- Öğretmen ve üç öğrenci de University düzeyindedir. Üç öğrenci de University2 sınıfındadır. Ayşe ve Mert ilk iki derse, Elif ise üniversite düzeyindeki Mathematics dersine kayıtlıdır.
- Öğrencinin ayrıca ders kaydı yapması gerekmez; kendi dersleri panelinde otomatik görünür.
- Öğretmen yeni ders oluşturabilir. Oluşturma ekranında kayıtlı öğrencileri işaretlerse ders ve öğrenci kayıtları birlikte kaydedilir. Daha sonra ders ayrıntısından da öğrenci seçip ekleyebilir.
- Öğretmen yalnızca kendi derslerini yönetir; öğrenciler yalnızca kayıtlı oldukları dersleri görür.

## Başlangıç verilerini değiştirme

Gerçek hesap ve ders listesi, ilk çalıştırmadan önce `web/Data/initial-data.json` dosyasından düzenlenebilir. `Accounts` bölümünde kullanıcıların adı, soyadı, e-postası, başlangıç şifresi ve rolü ile `EducationLevel` değeri; öğrenciler için ayrıca `StudentClass` bulunur. `Courses` bölümünde ders, öğretmenin e-postası ve kayıtlı öğrencilerin e-postaları yer alır.

Desteklenen sınıf değerleri: `University1`–`University4`, `HighSchool9`–`HighSchool12`. Rol değerleri `Student` ve `Teacher` olmalıdır. Şifreler en az 8 karakter, büyük harf, küçük harf, rakam ve özel karakter içermelidir.

`Key` değeri aynı kaldığı sürece başlangıç verisi yalnızca bir kez uygulanır. Uygulamanın yeniden açılması hesapları veya ders kayıtlarını çoğaltmaz; şifreleri sıfırlamaz; silinen başlangıç derslerini yeniden oluşturmaz.

Sonradan yeni başlangıç kayıtları eklemek isterseniz dosyayı düzenleyip `Key` değerini örneğin `edupilot-starter-v2` yapabilirsiniz. Mevcut e-postaların şifreleri ve rolleri değiştirilmez. Bu dosya mevcut kullanıcıların adını veya şifresini güncellemek için kullanılmaz. Aynı e-postanın rolü farklı verilirse işlem geri alınır. Hatalı veri aktarımı kısmi hesaplar bırakmaz.

Veri hazırlamayı tamamen kapatmak için `web/appsettings.Development.json` içindeki `InitialData:Enabled` değerini `false` yapın.

## Eski verileri koruma

Önceki sürümdeki hesapları ve yüklenen dosyaları korumak için uygulamayı kapatın; eski projenin `web/App_Data` klasörünü yeni projenin `web` klasörüne kopyalayın. Veritabanını silmeyin. Yeni `InitialDataHistory` güncellemesi otomatik uygulanır. Eski hesaplar profil tamamlamaya yönlendirilmeden giriş yapabilir.

## Onboarding hızlandırması

- 600 ms'lik saydamlık geçişi ve sürekli çalışan simge animasyonu kaldırıldı.
- Next ve sayfa noktaları bekleme olmadan anında geçiş yapar.
- Onboarding artık Bootstrap Carousel'e bağlı değildir.
- Bu sayfada gereksiz jQuery, Bootstrap JavaScript ve genel site betiği yüklenmez.
- Son adım Sign in ekranına gider.

## Doğrulama

- Web testleri: 183 başarılı; harici Python/Ollama/BGE servisi gerektiren 2 test atlandı.
- Onboarding ve şifre görünürlüğü JavaScript testleri: 2 başarılı.
- Son onboarding görünümü için ayrıca 3 ekran testi başarılı.
- Gerçek tarayıcıda 30 art arda onboarding geçişi, hazır öğrenci ve öğretmen girişi, isim/sınıf/ders gösterimi, hatalı şifre mesajı, yeni ders açma ve seçilen öğrencinin dersi otomatik görmesi kontrol edildi. Tarayıcı betik hatası görülmedi.

```powershell
dotnet test tests/HomeworkPlatform.Web.Tests/HomeworkPlatform.Web.Tests.csproj
node --test scripts/test_edupilot_auth.cjs
```

## Üniversite örneği ve eğitim seviyesi

Örnek öğretmenin ve üç öğrencinin veritabanında `EducationLevel = University` kaydı bulunur. Öğretmen ders açarken bu değer otomatik okunur; değiştirilebilir bir eğitim seviyesi listesi gösterilmez. University öğretmenine yalnızca 1–4. yıllar sunulur. Ders düzenlemede de aynı kural uygulanır. Başka bir eğitim seviyesi veya geçersiz yıl gönderilirse kayıt yapılmaz.

Derse öğrenci seçimi aynı eğitim seviyesindeki öğrencilerle sınırlandırılmıştır. Farklı seviyedeki bir öğrenciyi elle istek göndererek eklemek de engellenir.

Lise desteği korunmuştur: ileride veritabanında `EducationLevel = HighSchool` olarak tanımlanan öğretmen yalnızca 9–12. sınıflar için ders açabilir. HighSchool öğrencilerin `StudentClass` değeri de HighSchool9–HighSchool12 aralığından seçilmelidir. Başlangıç dosyasında ders, öğretmen ve öğrenci seviyeleri uyumlu olmalıdır.

Mevcut veritabanını kullanıyorsanız uygulama açılırken `AccountEducationLevels` güncellemesi uygulanır. Örnek Elif hesabı University2 olur ve örnek Mathematics dersi University / Year 2 olarak güncellenir. Ders kimlikleri, öğrenci bağlantıları ve şifreler korunur. Giriş bilgileri değişmez.

Son eğitim seviyesi kontrolünde ayrıca 36 ilgili test ve gerçek tarayıcı kontrolleri geçti. Üniversite ve lise senaryoları, değiştirilmiş form istekleri, seviyeye göre öğrenci filtreleme ve eski veritabanının yükseltilmesi test edildi.
