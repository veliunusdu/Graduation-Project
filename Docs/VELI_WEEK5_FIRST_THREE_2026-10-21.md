# Veli — 5. hafta ilk üç adım / 21 Ekim 2026

> Tarihsel kayıt: test sayıları o günkü birleşik çalışma dalına aittir. İzole 5. hafta PR teslimi ve güncel test sayıları için [27 Ekim kapanış raporunu](VELI_WEEK5_COMPLETION_2026-10-27.md) kullanın.

**İstenen ilk üç adım tamamlandı.** Gerçek HTTP kontrolünde 6/6 senaryo, mevcut
ASP.NET istemci/ekran testlerinde 14/14 ve tüm Python testlerinde 128/128 geçti.
Hata ve atlanan test yok; Python'da mevcut bağımlılık kaynaklı bir TestClient uyarısı var.

| Senaryo | Bağımlılık | Gerçek sonuç |
| --- | --- | --- |
| ASP.NET → Python sağlık çağrısı | Gerçek ASP.NET ve FastAPI süreçleri | Python HTTP 200 / `{"status":"ok"}`; öğretmen ekranında başarı mesajı |
| Python kapalı | Çalıştırdığımız gerçek Python süreci durduruldu | Web HTTP 200, güvenli timeout uyarısı ve eşleşen timeout logu |
| Timeout | Yerel HTTP sunucusu 4 saniye bekletiyor, ASP.NET limiti 2 saniye | Yaklaşık 2021 ms; web uyarısı ve `AI service request timed out.` |
| HTTP 500 | Kontrollü HTTP hata sunucusu | Web HTTP 200, güvenli uyarı ve HTTP 500 logu |
| HTTP 503 | Kontrollü HTTP hata sunucusu | Web HTTP 200, güvenli uyarı ve HTTP 503 logu |
| Bozuk JSON | Kontrollü HTTP sunucusu geçersiz JSON döndürüyor | Web HTTP 200, geçersiz JSON uyarısı ve logu |

Hata sayfalarında `Traceback`, `System.Net`, exception ayrıntısı veya gönderilen
sentetik özel hata işareti görünmedi. Web'in HTTP 200 döndürmesi beklenen davranış:
uygulama çökmüyor, sağlık sayfasında uyarı gösteriliyor. Bağımlılığın 500/503 yanıtı
logdan doğrulandı. Hata logları her isteğin başlangıcından sonraki kayıtlarla eşleştirildi.

Python kapalıyken Windows bağlantı girişimi 2 saniyelik sınıra ulaştı. Bu yüzden
sonuç **timeout** olarak kaydedildi; anında bağlantı reddedildiği iddia edilmedi.
Gerçek Python logunda üç başarılı `/health` çağrısı var: hazır olma kontrolü, doğrudan
JSON doğrulaması ve ASP.NET öğretmen ekranının çağrısı.

Timeout/500/503/bozuk JSON, **kontrollü HTTP hata enjeksiyonudur**. Gerçek ASP.NET
istemcisi ve yetkili öğretmen ekranı kullanıldı; bu senaryolarda bağımlılık FastAPI
veya Ollama olarak sunulmadı. Başarı senaryosu gerçek FastAPI ile çalıştı.

## Veri ve süreç güvenliği

Test kendi sentetik öğretmenini, rastgele şifresini, geçici SQLite veritabanını ve
upload klasörünü kullandı. Şifre kanıtlara yazılmadı. Mevcut uygulama veritabanı,
hesaplar, yüklenen dosyalar ve çalışan kullanıcı servisleri seçilmedi/değiştirilmedi.
Yalnızca testin başlattığı süreçler durduruldu; geçici klasör silindi. Windows launcher
ve logdaki gerçek Uvicorn PID'sinin de sonradan çalışmadığı ayrıca kontrol edildi.
Testler yalnızca loopback üzerinde geçici portlara bağlandı.

## Tekrar çalıştırma

Proje kökünde, yeni bir sonuç klasörü adı seçerek:

```powershell
dotnet build web/HomeworkPlatform.Web.csproj --no-restore --configuration Debug
& .\ai-service\.venv\Scripts\python.exe scripts/verify_veli_week5.py --output-dir Docs/TestEvidence/Week5/new-run
```

Var olan sonuç klasörü üzerine yazılmaz. Her çalıştırma kendi geçici verilerini ve
süreçlerini oluşturup temizler. Varsayılan servis timeout'u testte 2 saniyedir.
Mevcut `scripts/verify_week5.py` ve Ayşe'nin önceki kanıtları değiştirilmedi.

Kanıtlar: `Docs/TestEvidence/Week5/2026-10-21/veli-live/results.json`,
`python.log`, `web.log`, üst klasörde `ai-client-ui.trx`, `python-tests.xml`,
`python-tests.txt` ve `verification.json`. Bağımsız incelemede önemli bulgu yok.

## Bekleyen kısım

Bu sonuç **5. haftanın tamamı veya AI pipeline kabulü değildir**. Ollama/model
kurulumu ve gerçek ASP.NET → Python → Ollama → Python → ASP.NET cevabı,
Ollama kapalı/model bulunamadı/gerçek model timeout senaryoları henüz çalıştırılmadı.
Kullanıcının önceki seçimi doğrultusunda model testi ertelenmiş durumda; bu çalışmada
model çağrısı sayısı sıfırdır. Requirement Agent'ın gerçek model kalite kabulü de bekler.
