# MAUI Android Icon Setup - Schritt-für-Schritt Anleitung

## ⚡ Schnellste Lösung (10 Minuten)

### Online Icon Generator verwenden:

1. **Öffne:** https://icon.kitchen/
2. **Lade Icon hoch:**
   - Verwende die alten Icon-Dateien von `WinUI/Assets/` (z.B. `mobaflow-icon.png`)
   - ODER erstelle ein neues Icon im Online-Tool
3. **Wähle Android aus**
4. **Klicke "Download"**
5. **Entpacke die Dateien:**
   - Kopiere alle `mipmap-*` Ordner nach `MAUI/Platforms/Android/Resources/`

### Oder: Manuelles Generieren (1 Minute Vorbereitung)

```powershell
# 1. Vorbereitung: Icon mit 512x512 Pixel speichern
# Dateiname: appicon-512.png (im MAUI-Projektroot)

# 2. Script ausführen
cd MAUI
.\generate-android-icons.ps1

# 3. Build
dotnet clean
dotnet build
```

---

## 📋 Manuelle Dateiplatzierung (Fallback)

Falls die Scripts nicht funktionieren, erstelle diese Struktur manuell:

```
MAUI/Platforms/Android/Resources/
├── mipmap-mdpi/
│   └── appicon.png       (48x48)
├── mipmap-hdpi/
│   └── appicon.png       (72x72)
├── mipmap-xhdpi/
│   └── appicon.png       (96x96)
├── mipmap-xxhdpi/
│   └── appicon.png       (144x144)
└── mipmap-xxxhdpi/
    └── appicon.png       (192x192)
```

**Icon-Größen schnell erstellen:**
- Online: https://romannurik.github.io/AndroidAssetStudio/
- Offline: ImageMagick CLI
- GIMP/Photoshop (Export in verschiedenen Größen)

---

## 🔍 Überprüfung

Nach dem Platzieren der Icons:

```powershell
# Zeige Dateien
Get-ChildItem -Path "MAUI/Platforms/Android/Resources" -Recurse -Filter "appicon.png"

# Build
dotnet clean
dotnet build

# Deploy
dotnet build -t install -f net9.0-android
```

---

## ❓ Icon-Größen verstehen

| DPI Level | Multiplikator | Größe | Geräte |
|-----------|--------------|-------|--------|
| mdpi | 1x | 48x48 | Basis Referenz (160 DPI) |
| hdpi | 1.5x | 72x72 | Ältere Phones (240 DPI) |
| xhdpi | 2x | 96x96 | Moderne Phones (320 DPI) |
| xxhdpi | 3x | 144x144 | HD Phones (480 DPI) |
| xxxhdpi | 4x | 192x192 | Premium Phones (640 DPI) |

Android wählt automatisch die beste Größe für das Gerät aus.

---

## 🎨 Alternative: SVG zu PNG Konvertierung

Wenn Sie die alten Icon-Dateien von WinUI verwenden möchten:

```powershell
# Konvertiere SVG zu PNG (benötigt Inkscape)
$inkscape = "C:\Program Files\Inkscape\bin\inkscape.exe"
& $inkscape --export-type=png `
    --export-filename="appicon-512.png" `
    --export-width=512 --export-height=512 `
    "WinUI/Assets/mobaflow-icon.svg"

# Dann Icons generieren
.\generate-android-icons.ps1
```

---

## 📝 Checkliste

- [ ] Icon-Quelle vorbereitet (512x512 PNG oder SVG)
- [ ] Icons für alle DPI-Level erstellt
- [ ] Dateien in `Platforms/Android/Resources/mipmap-*/` platziert
- [ ] `dotnet clean && dotnet build` ausgeführt
- [ ] App neu gestartet auf Gerät/Emulator
- [ ] Icon angezeigt (nicht mehr Android-Männchen)
- [ ] Splash Screen aktualisiert (falls vorhanden)

---

**Letzte Aktualisierung:** 28.12.2025  
**Autor:** MOBAflow Development Team
