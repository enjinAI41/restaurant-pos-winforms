# Rivada Restaurant POS

Windows x64 üzerinde çalışan .NET 10 / WinForms restoran otomasyonu. Masa ve adisyon, menü yönetimi, ürün/adet stok takibi, stok ve masa bildirimleri, nakit/kart ödeme ve ciro raporları içerir. Veriler yerel SQLite veritabanında tutulur.

## 1.6 sürümü

- Stok yönetimi her açılışta şifre ister; yönetici yetkisi de gereklidir.
- Ciro, ödeme tarihine göre hesaplanır; günlük sınır yerel saat 00.00'dır.
- Günlük/haftalık/aylık/özel tarih aralığı, nakit/kart ve ürün satışları raporlanır.
- Lisans süresi veya cihaz kodu kontrolü yoktur.

## Mevcut müşteriyi güncelleme

Sürüm ZIP'ini kurulu program klasörünün dışında tamamen çıkarın. POS'u kapatın ve **Rivada.Guncelle.exe** çalıştırın. Mevcut POS kısayolunun işaret ettiği klasörü seçip **Yedekle ve Güncelle** kullanın. İşlemden sonra aynı kısayoldan açın. Programı kaldırmayın ve `restaurant.db` dosyasını değiştirmeyin.

Güncelleyici mevcut kurulumu yedekler ve yalnızca doğrulanan uygulama dosyalarını kopyalar. 1.3 ve sonrası veritabanlarında stoklar korunur. Daha eski sürümlerden geçişte menü ve 99 adet başlangıç stokları bir kez uygulanır; ayrıntılar [kullanım kılavuzundadır](installer/KULLANIM.txt).

## Geliştirme ve paketleme

.NET 10 SDK ve Windows gerekir. Dağıtım paketi .NET çalışma ortamını içerir.

```powershell
dotnet build Rivada.Restaurant.WinForms.csproj -c Release
dotnet run --project tests/StockChecks/StockChecks.csproj -c Release
.\installer\build-package.ps1
```

Çıktı `artifacts/packages` klasörüne yazılır. Kaynak depoya müşteri veritabanı, lisans/özel anahtar veya derlenmiş paket eklenmez. [Paketleme ayrıntıları](installer/README.md).
