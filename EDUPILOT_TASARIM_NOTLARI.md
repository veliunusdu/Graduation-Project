# EduPilot tasarımı ve hazır hesap akışı

EduPilot'un mor renk paleti, kartları, yerel SVG simgeleri ve mobil uyumlu giriş ekranı korunmuştur. Bu sürümde kayıt ve profil tamamlama ekranları kaldırılmıştır.

Kullanıcı hazır e-posta ve şifresiyle giriş yapar; ad, soyad ve öğrencinin sınıfı veritabanından alınır. Öğretmenin hazır dersleri ve öğrencinin ders kayıtları otomatik gösterilir. Öğretmen ayrıca yeni ders oluşturup sistemdeki öğrencilerden seçim yapabilir.

Onboarding geçişleri anlıktır. Bootstrap Carousel, bekleme süreleri ve sürekli simge animasyonu kaldırıldı. Karşılama sayfası artık jQuery ve Bootstrap JavaScript dosyalarını yüklemiyor. Skip ve Sign in bağlantıları doğrudan girişe gider.

Çalıştırma, örnek giriş bilgileri, başlangıç hesaplarını düzenleme ve mevcut veritabanını koruma adımları için `KURULUM_VE_DEGISIKLIKLER.md` dosyasına bakın.

Örnek verilerin tamamı üniversite düzeyindedir. Eğitim seviyesi ders formunda öğretmenin veritabanı kaydından gelen sabit bilgi olarak görünür; üniversite öğretmenine yalnızca 1–4. yıllar gösterilir.
