# Ayşe - 5. hafta ve öğrenci dosya teslimi

## Tamamlanan işler

Bu sürüm, 20 Ekim 2026 tarihinde sağlanan ve GitHub main ile birebir eşleştiği doğrulanan ZIP üzerine hazırlanmıştır. Tamamlanan 4. hafta kodları korunmuştur.

- `AIServiceClient`: ASP.NET -> Python HTTP çağrısı, `GET /health`, structured JSON GET/POST yardımcıları.
- Ayarlanabilir servis adresi ve zaman aşımı. Bağlantı hatası, zaman aşımı, HTTP hata yanıtı, geçersiz veya aşırı büyük JSON için kontrollü sonuçlar.
- Öğretmen panelinden açılan **AI servis bağlantısını kontrol et** ekranı. Servis kapalıysa uygulama açılmaya devam eder; hata aynı sayfada uyarı olarak gösterilir.
- Öğrencinin metin veya dosya ile teslim yapması; öğretmen ve dosya sahibi öğrenci için yetkili indirme.
- Dosya değiştirme, isteğe bağlı dosya kaldırma ve teslim silindiğinde fiziksel dosyayı temizleme.
- `SubmissionFiles` migration'ı. Var olan metin teslimleri ve öğretmen değerlendirmeleri korunur.

## Visual Studio ile çalıştırma

1. Arşivi ayrı bir klasöre çıkartın ve `HomeworkPlatform.sln` çözümünü açın.
2. `HomeworkPlatform.Web` başlangıç projesi olsun. Çözümü derleyip `https` profiliyle çalıştırın.
3. İlk açılışta migration otomatik uygulanır. Yeni klasördeki yeni veritabanı önceki hesapları içermez.
4. Öğretmen hesabıyla giriş yapıp paneldeki **AI servis bağlantısını kontrol et** bağlantısını açın.

Mevcut kayıtlarla devam edecekseniz önce eski uygulamayı tamamen durdurun ve `web/App_Data` klasörünün yedeğini alın. Eski `homework.db` dosyasını yeni sürümün `web/App_Data` klasörüne kopyalayıp uygulamayı başlatabilirsiniz. Bundan sonra dosyalı teslimler de varsa veritabanıyla birlikte `SubmissionFiles` klasörünü de taşıyın/yedekleyin. Kaynak kod güncellemesi için değişen dosyalar listesi `WEEK_5_CHANGED_FILES.txt` içindedir; bin/obj veya eski migration dosyalarını silmeyin.

Öğretmen hesabı oluşturma adımları: `AUTHENTICATION_SETUP.md`. Hesaplar ve gerçek dosyalar arşive dahil değildir.

## Python servisini başlatma

Proje ana klasöründe PowerShell açın. Python 3.11 veya uyumlu bir sürüm kurulu olmalıdır:

```powershell
py -3.11 -m venv ai-service/.venv
& ./ai-service/.venv/Scripts/python.exe -m pip install -r ai-service/requirements.txt
& ./ai-service/.venv/Scripts/python.exe -m uvicorn api.main:app --app-dir ai-service --host 127.0.0.1 --port 8000
```

Python servisinin terminalini açık tutun. Tarayıcıda `http://127.0.0.1:8000/health` adresi `{"status":"ok"}` döndürmeli. Web uygulamasının öğretmen panelinde sağlık kontrolünü yeniden açın; başarı mesajı görünmeli.

Python terminalinde Ctrl+C yaparak servisi kapatın ve web ekranında **Yeniden kontrol et** seçin. Bağlantı uyarısı görünmeli; ham exception/traceback görünmemeli.

Bu sağlık kontrolü Python'a HTTP erişimini doğrular. Ollama/model çıktısını veya multi-agent analizini doğrulamaz. Python/Ollama tarafında ekip arkadaşlarının mevcut kodları değiştirilmemiştir.

## Ayarlar

`web/appsettings.json` varsayılanları:

```json
{
  "AIService": {
    "BaseUrl": "http://127.0.0.1:8000/",
    "TimeoutSeconds": 10
  },
  "SubmissionFiles": {
    "RootPath": "App_Data/SubmissionFiles",
    "MaxFileSizeMB": 20
  }
}
```

Ortam değişkenleri: `AIService__BaseUrl`, `AIService__TimeoutSeconds`, `SubmissionFiles__RootPath`, `SubmissionFiles__MaxFileSizeMB`. Zaman aşımı 1-120 saniye, dosya limiti 1-20 MB aralığında yapılandırılır. Kodda veya formda kullanıcıya serbest dış servis adresi girdirilmez. HTTP yönlendirmeleri otomatik takip edilmez. JSON yanıtı en fazla 1 MB kabul edilir. POST istekleri otomatik yeniden gönderilmez. Çağıran isteği iptal ederse iptal korunur.

JSON yardımcıları Python için snake_case alan adları kullanır. Gelecek agent çağrılarında sözleşmedeki alanlar `[JsonPropertyName]` ile açıkça sabitlenebilir. Henüz eklenmemiş agent endpoint'lerine UI çağrısı yapılmaz.

## Dosya teslimi

- Her teslimde tek ek dosya; çok dosyalı projeler ZIP olarak yüklenir. Metin açıklaması da eklenebilir.
- PDF, ZIP, TXT/MD ve yaygın kod türleri desteklenir: CS, PY, JAVA, JS/TS/JSX/TSX, C/CPP/H/HPP, HTML/CSS, SQL, JSON/XML, IPYNB, RAZOR/CSHTML, SLN/CSPROJ, YAML/YML.
- Dosya varsayılan olarak en fazla 20 MB olabilir. HTTP isteğinin toplam sınırı 25 MB'dir; bunun üzerindeki istek web sunucusu tarafından reddedilebilir.
- Boş dosya, yasak uzantı ve geçersiz temel dosya biçimi reddedilir. Kod/metin UTF-8 olmalıdır. PDF imzası kontrol edilir; ZIP açılmadan dizin bilgileri kontrol edilir. ZIP'te en fazla 2000 kayıt, toplam 200 MB açılmış boyut ve güvenli yollar kabul edilir. Bunlar biçim/yol kontrolleridir; antivirüs veya kod analizi değildir.
- Dosyalar `wwwroot` dışında rastgele kimlikle saklanır. Özgün isim sadece kayıt ve indirme adıdır. Dosyalar çalıştırılmaz ve ZIP sunucuda açılmaz.
- Dosya indirme `SubmissionFiles/Download/{submissionId}` üzerinden yapılır. Başka öğrenciler ve başka dersin öğretmeni 404 alır. İndirme attachment olarak, octet-stream ve nosniff/no-store başlıklarıyla döner.
- Yeni dosya seçmek eskisini değiştirir. Yeni dosya seçilmezse mevcut dosya korunur. Mevcut dosyayı kaldırırken geriye en azından metin kalmalıdır.
- Değişen teslimin önceki not/geri bildirimi temizlenir; öğretmen yeniden değerlendirebilir.
- Dosya doğrulama veya veritabanına yazma başarısızsa önceki dosya korunur ve yeni dosya temizlenir. Başarılı veritabanı güncellemesinden sonra eski dosya kaldırılır. İşletim sistemi dosya temizliğini engellerse sunucu günlüğüne uyarı yazılır.
- Form hatasından sonra tarayıcı güvenliği nedeniyle dosya yeniden seçilmelidir.

## Deneme sırası

1. Öğretmen ders açsın, öğrenciyi kaydetsin ve gereksinimli bir ödev oluştursun.
2. Öğrenci teslim formunda açıklamayı boş bırakıp `.pdf`, `.py` veya `.zip` dosyası seçerek kaydetsin.
3. Öğrenci ve dersin öğretmeni dosya bağlantısından aynı dosyayı indirebilsin.
4. Öğretmen not/geri bildirim versin. Öğrenci sonucu görsün.
5. Öğrenci teslimi düzenleyip farklı dosya seçsin. Yeni dosya görünsün; eski değerlendirme temizlensin.
6. Düzenlemede dosya seçmeden metni değiştirin; dosya korunmalı.
7. Dosyayı kaldırıp metin bırakın; dosya bağlantısı kalkmalı.
8. Yasak uzantı, boş veya 20 MB üstü dosya deneyin; kayıt oluşmadan uyarı görünmeli.

## Doğrulama

- 145 web testi başarılı, 0 başarısız, 0 atlanan. Yetki, dosya içerikleri, boyut, değiştirme/temizleme, veritabanı yazma hatası ve eski veritabanından geçiş dahil.
- Mevcut Python servisinin 28 testi başarılı. Bağımlılıktan gelen bir TestClient deprecation uyarısı mevcut.
- Gerçek FastAPI ve ASP.NET süreçleriyle HTTP üzerinden `/health` başarı yanıtı, Python kapalı uyarısı, dosyalı teslim, öğrenci/öğretmen indirmesi ve değerlendirme doğrulandı.
- EF model kontrolü: bekleyen migration farkı yok.
- .NET paket güvenlik verisi sorgusunda ortam kaynaklı NU1900 uyarısı alındı; bağımlılık güvenlik denetimi bu ortamda tamamlanamadı. Derleme ve testler başarılıdır.

Kanıtlar: `Docs/TestEvidence/Week5/ayse-week5.trx`, `live-http-results.json` ve `python-tests.txt`.

Tekrar çalıştırma:

```powershell
dotnet test HomeworkPlatform.sln
& ./ai-service/.venv/Scripts/python.exe -m pip install -r ai-service/requirements-dev.txt
Push-Location ai-service
try { & ./.venv/Scripts/python.exe -m pytest -q } finally { Pop-Location }
& ./ai-service/.venv/Scripts/python.exe scripts/verify_week5.py
```

`verify_week5.py` canlı kontrol için geçici hesap, veritabanı ve dosya klasörü kullanır; iş bitince kendi süreçlerini kapatır. Mevcut kayıtları kullanmaz. Öncesinde Debug derlemesi yapılmalıdır.

Web tasarımı ve AI analizini tetikleme bu değişikliğin kapsamında değildir. Dosya teslimi kullanıcının isteğiyle 10. haftadan öne alınmıştır.
