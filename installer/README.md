# Rivada POS 1.6 dağıtımı

Stok yönetimi her girişte ayrı şifre ister. `StockManagementForm.ShowForUser` yönetici yetkisini ve yeni bir şifre penceresini zorunlu tutar; stok formunun kurucusu dışarıya kapalıdır. Doğrulama başarılı olmadan stok sorgulanmaz. İptal, Esc ve pencereyi kapatma erişimi reddeder; oturum boyunca hatırlama yoktur. Bu sabit yerel şifre işletim sistemi veya veritabanı şifrelemesi değildir. 1.5'ten yükseltme veri değişikliği gerektirmez.

Yöneticiye özel Ciro Takibi, doğrudan mevcut `Payments` kayıtlarından çalışır. Tarih aralığı, nakit/kart toplamı, adisyon sayısı, ortalama hesap, günlük dağılım ve satış anındaki ürün/adet/tutar raporu sunar. Yerel gün sınırı 00.00'dır; UTC tarih sınırları `[başlangıç, bitiş)` olarak uygulanır. SQLite'ın milisaniye yuvarlamasına karşı kesin sınırlar .NET tarafında da kontrol edilir. Ödeme ve ürün sorguları ayrıdır; çok kalemli adisyonlar ciroyu çoğaltmaz. Açık adisyonlar seçilen dönemden bağımsız ayrı gösterilir. Rapor tek okuma transaction'ıyla tutarlı görünüm alır, verileri değiştirmez. 1.4'ten yükseltme yeni veri tablosu gerektirmez.

Ana ekranın üst menüsü altında bildirim çubuğu bulunur. Satılabilir adede göre kritik/tükenen stoklar ve en son masa işlemi gösterilir. Ayrıntıda güncel uyarılar ve son 100 masa hareketi listelenir; 15 saniyede bir yenilenir. Masa açılışı, kapanışı/sıfırlanması ve ödemeler `PosTableNotifications` içinde saklanır. İşlemle aynı transaction içinde çalışan trigger'lar başarısız işlemlerin bildirim üretmesini önler. Güncelleme geçmiş işlemleri yeniden bildirmez; 1.3 veritabanındaki stok ve fiyatlar korunur.

Lisans kontrolü, lisans ekranı, cihaz kodu ve lisans üretme komutları kaldırıldı.
Uygulama doğrudan kullanıcı giriş ekranına açılır. Eski lisans dosyaları varsa yok sayılır.
Stok ekranındaki açıklama alanı kaldırıldı. [Rivada menüsünün](https://rivadaotel.com/menu) 26.09.2026 tarihli 86 fiyatlı kalemi gömülü JSON kataloğundan bir kez uygulanır: eşleşen ürünlerin fiyatları güncellenir, eksikler eklenir, diğer ürünler korunur. Tüm ürünlerde takip açılıp kayıtlı stok 99 yapılır. `PosAppMigrations` işareti sonraki açılışlarda bu değerlerin sıfırlanmasını önler. Eski ürün kimlikleri ve adisyonlardaki fiyat/ad snapshot'ları korunur.

Windows x64 ve .NET 10 SDK ile mevcut kurulumu yedekleyerek güncelleyen ZIP üretin:

```powershell
.\installer\build-package.ps1
```

ZIP'i tamamen açıp `Rivada.Guncelle.exe` çalıştırın. Uygulama kapalı olmalıdır. Kurulu klasörü seçip **Yedekle ve Güncelle** kullanın. Paket SHA-256 doğrulaması yapar; mevcut uygulamayı yanındaki `Rivada POS Backups` klasörüne yedekler. Güncelleyici veri dosyalarının üzerine yazmaz. Uygulama ilk açılışta ayrıca SQLite yedeği alır ve menü/stok dönüşümünü tek transaction içinde uygular. Kullanım adımları `KULLANIM.txt` içindedir.

Inno Setup kurulu bir geliştirme ortamında klasik setup da üretilebilir:

```powershell
.\installer\build-installer.ps1 -Version 1.6.0
```

Yeni çıktılar `artifacts/packages` içindedir. Eski derleme çıktıları Git takibinden çıkarıldı; `installer/output` ve `installer/publish` yeniden commit edilmez. Güncelleme paketine veritabanı veya anahtar konulmaz; lisans dağıtımı gerekmez.

SDK PATH içinde değilse `-DotNet 'tam\yol\dotnet.exe'` verin. Yerel NuGet deposu için ZIP betiğinde `-NuGetSource 'depo\klasörü'` kullanılabilir.

Entegrasyon doğrulaması:

```powershell
dotnet run --project tests/StockChecks/StockChecks.csproj -c Release
```

Testler benzersiz geçici veritabanları oluşturur; işletmenin verilerine yazmaz. Menü eşleştirme, 99 adet başlangıcı, bir kez uygulama, geçmiş verilerin korunması, rezervasyon, ödeme, eşzamanlı stok işlemleri ve bildirimlerin transaction bütünlüğü doğrulanır. `--preview` argümanı ayrı test veritabanında stok ekranını; `--notifications-preview` örnek uyarılarla ana ekranı açar. GitHub'a yüklenmemiş eski bir stok şemasından otomatik veri aktarımı yapılmaz. Sonradan eklenen ürünlerin takibi ve adedi yönetici tarafından belirlenir.

Ciro kontrolleri nakit/kart mutabakatını, tahsil edilmemiş hesapların ayrılmasını, tarih/saat sınırlarını, tarih formatlarını, geçmiş fiyatları, kuruşlu/bedelsiz ödemeleri, başarısız işlemleri, yönetici yetkisini ve raporun salt okunur olmasını kapsar. `--revenue-preview` ayrı test veritabanında örnek satışlarla ciro ekranını açar.

Stok şifre kontrolleri gerçek modal giriş akışını sahte ve boş bir stok servisiyle çalıştırır: hatalı/boş/fazla karakterli şifre, iptal, doğru giriş, tekrar girişte yeniden şifre ve yönetici olmayan kullanıcı engeli. İşletme verilerine veya işletim sistemi giriş ekranına erişmez.
