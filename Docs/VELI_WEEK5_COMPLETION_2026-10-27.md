# Veli — 5. hafta test teslimi / 27 Ekim 2026

**5. hafta test teslimi tamamlandı; ekip PR incelemesi ve merge bekleniyor.**
Dal: `test/veli-week5-integration`, taban: güncel `origin/main`.
PR: [#9 — Veli Week 5 integration verification](https://github.com/veliunusdu/Graduation-Project/pull/9).
GitHub'a gönderildi; otomatik merge veya reviewer ataması yapılmadı.
Requirement Agent kodu/kalite kabulü ve 4. hafta ekleri bu PR'ın dışında tutuldu.

## Görev / kanıt eşlemesi

| Veli görevi | Gerçek doğrulama | Sonuç |
| --- | --- | --- |
| ASP.NET → Python → Ollama → Python → ASP.NET | 21 Ekim: gerçek AIServiceClient, FastAPI, qwen3:4b; INTEGRATION_OK | PASS |
| Python kapalı | 27 Ekim: testin Python süreci durduruldu; web güvenli uyarı/log gösterdi | PASS |
| Ollama erişilemez | 21 Ekim: gerçek client erişilemeyen loopback adrese yönlendirildi | PASS, kontrollü bağlantı hatası |
| Model bulunamadı | 21 Ekim: gerçek Ollama404 → Python502 → güvenli .NET http_error | PASS |
| Timeout | 27 Ekim: geciken HTTP yanıtı ve log; 21 Ekim: gerçek upstream timeout | PASS |
| HTTP davranışı / log | 27 Ekim:500,503,bozuk JSON; web200/güvenli uyarı ve ilgili log | PASS |

Ollama daemon'ı fiziksel olarak kapatılmadı; erişilemez endpoint ile ilgili hata yolu
test edildi. İlk başarı senaryosu servis sınıfları arasındaki gerçek zincirdir;
web'e analiz ekranı/endpoint'i eklenmiş olduğu iddia edilmez. Öğretmen sağlık ekranı
ise gerçek ASP.NET/FastAPI süreçleriyle ayrıca kontrol edildi.

## Güncel PR regresyonu

- Debug: **145 passed, 0 failed, 1 opt-in canlı test skipped**.
- Release: **145 passed, 0 failed, 1 opt-in canlı test skipped**.
- Python: **35 passed, 0 failed**, mevcut TestClient deprecation uyarısı.
- Bugünkü gerçek sağlık/kapalı servis/kontrollü HTTP hata kontrolleri: **6/6 PASS**.
- 21 Ekim gerçek model senaryoları: **4/4 PASS**, o çalışmada canlı test atlanmadı.

21 Ekim tarihli eski regresyon kayıtları Requirement/Week4 modüllerini de içeren
çalışma dalından gelir; daha büyük sayılar bu izole PR'ın güncel sayıları değildir.
Ham tarihsel kanıtlar değiştirilmeden korunmuştur. Bugünkü kayıtlar
`Docs/TestEvidence/Week5/2026-10-27/` klasöründedir; `closure.json` ayrımı açıklar.

Bugün Ollama çalışmıyordu. Ollama'yı başlatmayı da içeren komut otomatik onay
incelemesi tarafından `blocked by policy` gerekçesiyle engellendi. Engellenen başlatma
işlemi tekrar denenmedi; bugün gerçek modelin yeniden çalıştırıldığı iddia edilmez.
21 Ekim gerçek model kanıtları ve aynı servis kodu korunmuştur.

## Kapsam / güvenlik

- Kullanıcının gerçek DB'si, hesapları, upload'ları ve yerel `.env` değiştirilmedi.
- Health harness kendi geçici hesap/DB/file klasörünü ve loopback portlarını kullanır;
  yalnızca kendi başlattığı süreçleri durdurur.
- Requirement Agent'ın üç gerçek kalite denemesindeki timeout'lar farklı bir görevdir;
  bu teslim onları kapatmaz. Agent dalı yerelde ayrıca korunuyor.
- Bu PR test ve rapor teslimidir; üretim API/model varsayılanlarına değişiklik yoktur.

## Yeniden çalıştırma

```powershell
dotnet test tests/HomeworkPlatform.Web.Tests/HomeworkPlatform.Web.Tests.csproj --no-restore --configuration Debug
dotnet test tests/HomeworkPlatform.Web.Tests/HomeworkPlatform.Web.Tests.csproj --no-restore --configuration Release
Push-Location ai-service
try { & ./.venv/Scripts/python.exe -m pytest tests -q } finally { Pop-Location }
& .\ai-service\.venv\Scripts\python.exe scripts/verify_veli_week5.py --output-dir Docs/TestEvidence/Week5/new-http-run
# Ollama açık ve qwen3:4b yüklüyken, ayrı/yeni bir klasöre:
& .\ai-service\.venv\Scripts\python.exe scripts/verify_live_ollama.py --output-dir Docs/TestEvidence/Week5/new-model-run
```
