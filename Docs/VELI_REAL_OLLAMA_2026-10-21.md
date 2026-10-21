# Veli — gerçek Qwen3/Ollama entegrasyonu / 21 Ekim 2026

**Gerçek ASP.NET AIServiceClient → FastAPI → Ollama → FastAPI → ASP.NET istemcisi zinciri doğrulandı.**
Ollama: 0.40.7. Model: qwen3:4b. Digest:
`359d7dd4bcdab3d86b87d73ac27966f4dbb9f5efdfcc75d34a8764a09474fae7`.

| Senaryo | Python sonucu | ASP.NET istemcisi |
| --- | --- | --- |
| Gerçek qwen3:4b yanıtı | HTTP 200; INTEGRATION_OK; done=true | Aynı model/cevap, başarılı |
| Bilerek bulunmayan model etiketi | Gerçek Ollama 404 → Python HTTP 502 | http_error, 502, güvenli genel mesaj |
| Ollama erişilemez | Boş loopback porta bağlantı → Python HTTP 502 | http_error, 502, güvenli genel mesaj |
| Upstream timeout | Gerçek Ollama çağrısı, requests timeout=0.001 s → HTTP 502 | http_error, 502, güvenli genel mesaj |

4/4 canlı .NET testi geçti; her vaka doğrudan Python HTTP çağrısıyla da doğrulandı.
Bu kanıt gerçek servis sınıfını ve HTTP/model çağrılarını kullanır; web'e yeni analiz
butonu veya endpoint'i eklenmedi. Önceki öğretmen sağlık ekranı testleri
VELI_WEEK5_FIRST_THREE_2026-10-21.md raporunda kayıtlıdır.

Ollama erişilemez testi kullanıcının daemon'ını fiziksel olarak kapatmadı; Python
adresi kullanılmayan yerel porta ayarlandı. Timeout hem connect hem read sınırıdır;
bunun özellikle read timeout olduğu iddia edilmez. Kullanıcının Ollama servisi açık
bırakıldı, yeni model indirilmedi. Test yalnızca kendi Python süreçlerini kapatır;
uygulama veritabanı başlatılmaz veya seçilmez.

Regresyon: **154 web testi geçti; 1 opt-in canlı test normal çalışmada atlandı.**
Canlı senaryolarda bu test dört ayrı TRX içinde gerçekten çalıştı: her biri 1 passed,
1 executed, 0 failed. Python **128/128** geçti; mevcut TestClient deprecation uyarısı
sürüyor. Bağımsız incelemede önemli bulgu yok. Ürün kodu/ortak model varsayılanları değişmedi.

## Yeniden çalıştırma

Proje kökünde, Ollama açıkken ve yeni çıktı klasörü seçilerek:

```powershell
dotnet build tests/HomeworkPlatform.Web.Tests/HomeworkPlatform.Web.Tests.csproj --no-restore --configuration Debug
& .\ai-service\.venv\Scripts\python.exe scripts/verify_live_ollama.py --output-dir Docs/TestEvidence/Week5/new-real-run
```

Test `.env` veya ekip varsayılanlarını değiştirmez. Normal Python servisini kendi
kurulu modelinle başlatmak için aynı terminalde şu environment ayarlarını kullan:

```powershell
$env:OLLAMA_GENERAL_MODEL = 'qwen3:4b'
$env:OLLAMA_CODE_REVIEW_MODEL = 'qwen3:4b'
& .\ai-service\.venv\Scripts\python.exe -m uvicorn api.main:app --app-dir ai-service --host 127.0.0.1 --port 8000
```

Bu yerel seçim, Qwen3'ün kod inceleme kalitesinin doğrulandığı anlamına gelmez.
Requirement Agent değerlendirmesi ayrı kayıttır; entegrasyon başarısı kalite kabulü değildir.

Kanıt: Docs/TestEvidence/Week5/2026-10-21/real-ollama/results.json ve aynı klasördeki
TRX/client JSON/Python logları. Normal Python invocation kullanılır, `-O` kullanılmaz
(harness assert'lere dayanır). .NET süreci 150 saniyeyi aşarsa hata/Python logu korunur,
yarım .NET stdout'u ayrıca kaydedilmez. Bunlar aracın belgelenmiş sınırlarıdır.
