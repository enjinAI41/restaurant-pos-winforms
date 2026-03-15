# Kurulum Paketi (Setup) Oluşturma

Bu proje için Windows setup paketi Inno Setup ile üretilir.

## Gereksinimler
- .NET SDK (proje ile uyumlu)
- Inno Setup (iscc komutu PATH'te olmalı)

## Hızlı Kullanım
Repo kökünden:

```powershell
.\installer\build-installer.ps1 -Configuration Release -Runtime win-x64 -Version 1.0.0 -PublicKeyPath "C:\lisans\license-public-key.xml"
```

## Çıktılar
- Publish dosyaları: `installer\publish\app`
- Setup exe: `installer\output\RivadaRestaurantPOS-Setup.exe`

## Notlar
- Kurulum klasörü: `%LocalAppData%\Rivada Restaurant POS`
- Bu sayede uygulama SQLite/log dosyalarını yazma izni sorunu olmadan kullanır.
- `-PublicKeyPath` parametresi opsiyoneldir; verilirse `license-public-key.xml` setup içine dahil edilir.
