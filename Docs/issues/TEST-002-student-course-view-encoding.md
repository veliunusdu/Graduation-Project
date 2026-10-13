# TEST-002 — Öğrenci course görünümleri UTF-8 değil

Durum: OPEN. Öncelik: P3 / Minor. İncelenen commit: `ec7ae36` (Ayşe: `bf12c25`).

## Kanıt

- `web/Views/StudentCourses/Index.cshtml:5`: UTF-8 decode hatası; byte offset 347, ham ayraç byte `0x97`.
- `web/Views/StudentCourses/Details.cshtml:4`: UTF-8 decode hatası; byte offset 160, ham ayraç byte `0xb7`.

Bu dosyaların diğer içerikleri ASCII; ayraçların Windows-1252 byte'ları olarak kaydedilmesi UTF-8 kaynak okumasıyla uyuşmuyor. Kanıt kaynak dosya decode kontrolüdür; interaktif browser görüntüsü alınmadı. Ayraçların bozuk görünmesi olasıdır; erişim kontrolü etkilenmedi.

## Tekrar üretim

Proje kökünden Python ile:

```python
from pathlib import Path
for name in ["Index", "Details"]:
    Path(f"web/Views/StudentCourses/{name}.cshtml").read_bytes().decode("utf-8", errors="strict")
```

Her dosya ayrı çalıştırıldığında UnicodeDecodeError üretir.

## Önerilen düzeltme

İki dosyayı amaçlanan ayraç karakterlerini koruyarak UTF-8 olarak kaydet. Sonra strict UTF-8 decode, Razor build ve öğrenci course listesi/detayı görsel kontrolünü tekrar yap.

Ürün dosyaları bu incelemede değiştirilmedi. 108/108 Debug ve 108/108 Release işlevsel test sonucu bu görüntü/metin sorununu doğrulamaz veya kapatmaz. Bu yalnızca yerel issue kaydıdır; GitHub issue veya ekip mesajı oluşturulmadı.
