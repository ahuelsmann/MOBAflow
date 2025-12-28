# MOBAflow Icon Update Guide

**Letzte Aktualisierung:** 25.12.2025  
**Version:** 2.0

Dieses Dokument beschreibt, wie die App-Icons für MOBAflow (WinUI 3) aktualisiert werden können.

---

## 🎨 Aktuelles Design

MOBAflow verwendet ein **Eisenbahn-Icon** (Frontalansicht) mit folgenden Elementen:
- 🟣 **Hintergrundfarbe:** `#5B3A99` (Lila/Violett)
- 🚂 **Lokomotive:** `#2B7CD3` (Blau) - Frontalansicht mit Fenstern
- 💡 **Scheinwerfer:** `#FFD700` (Gold) und `#FFFFFF` (Weiß)
- 🛤️ **Gleise:** `#C0C0C0` (Silber) - Perspektivische Darstellung
- 🔴 **Feedback-Punkt:** `#FF6B6B` (Rot) mit Glow-Effekt
- 📝 **Text:** `#FFFFFF` (Weiß) - "MOBA"

---

## 📂 Icon-Dateien

### **Basis-Icons (Quelle):**
```
scripts/
├── mobaflow-icon.svg          ← SVG-Vorlage (editierbar)
└── svg-to-png.ps1             ← Konvertierungs-Script
└── update-icon.ps1            ← Schnell-Update Script

WinUI/Assets/
└── mobaflow-icon.png          ← PNG-Basis (512x512, aus SVG generiert)
```

### **Generierte Icons (automatisch erstellt):**
```
WinUI/Assets/
├── Square44x44Logo.png              (44x44)   - Taskleiste
├── Square44x44Logo.scale-200.png    (88x88)   - High-DPI
├── Square150x150Logo.png            (150x150) - Start-Kachel
├── Square150x150Logo.scale-200.png  (300x300) - High-DPI
├── Wide310x150Logo.png              (310x150) - Breite Kachel
├── Wide310x150Logo.scale-200.png    (620x300) - High-DPI
├── StoreLogo.png                    (50x50)   - Store
├── StoreLogo.scale-200.png          (100x100) - High-DPI
├── SplashScreen.png                 (620x300) - Ladebildschirm
├── SplashScreen.scale-200.png       (1240x600)- High-DPI
├── LargeTile.png                    (310x310) - Große Kachel
└── LargeTile.scale-200.png          (620x620) - High-DPI
```

---

## 🚀 Icon aktualisieren (Einfache Methode)

### **Automatisches Update (Empfohlen)**

```powershell
# Im Projekt-Root-Verzeichnis
.\scripts\update-icon.ps1
```

**Was passiert:**
1. ✅ Öffnet SVG in Browser (Microsoft Edge)
2. ✅ Du speicherst es als PNG (512x512)
3. ✅ Script generiert automatisch alle 12 Icon-Größen
4. ✅ Zeigt nächste Schritte (Build, Icon-Cache löschen)

---

## 🎨 Icon Design anpassen

### **1. SVG bearbeiten**

Öffne `scripts/mobaflow-icon.svg` in einem Editor:
- **Inkscape** (empfohlen, kostenlos): https://inkscape.org
- **Figma** (online): https://figma.com
- **VS Code** mit SVG-Extension

**Farben ändern:**
```svg
<!-- Hintergrund -->
<rect fill="#5B3A99"/>  <!-- Lila -->

<!-- Lok -->
<rect fill="#2B7CD3"/>  <!-- Blau -->

<!-- Scheinwerfer -->
<circle fill="#FFD700"/> <!-- Gold -->

<!-- Feedback-Punkt -->
<circle fill="#FF6B6B"/> <!-- Rot -->
```

### **2. PNG exportieren**

**Option A: Automatisch (Inkscape)**
```powershell
.\scripts\svg-to-png.ps1
```

**Option B: Manuell (Browser)**
1. Öffne SVG in Edge: `Start-Process msedge scripts\mobaflow-icon.svg`
2. Rechtsklick → "Bild speichern unter..."
3. Speichere als `WinUI\Assets\mobaflow-icon.png` (512x512)

### **3. Alle Größen generieren**

```powershell
.\scripts\resize-icons-dotnet.ps1
```

---

## 🔧 Manuelle Installation (ohne Scripts)

### **1. Inkscape installieren**

```powershell
# Via winget (Windows 11)
winget install Inkscape.Inkscape

# ODER Download:
# https://inkscape.org/release/
```

### **2. SVG zu PNG konvertieren**

```powershell
# Inkscape CLI (nach Installation)
$inkscape = "C:\Program Files\Inkscape\bin\inkscape.exe"
& $inkscape --export-type="png" `
    --export-filename="WinUI\Assets\mobaflow-icon.png" `
    --export-width=512 --export-height=512 `
    "scripts\mobaflow-icon.svg"
```

### **3. Icons generieren**

```powershell
.\scripts\resize-icons-dotnet.ps1
```

---
```

---

### **6. Windows Icon-Cache löschen**

Damit Windows die neuen Icons erkennt:

```powershell
# Icon-Cache leeren
ie4uinit.exe -show

# Explorer neustarten (optional)
taskkill /F /IM explorer.exe
Start-Sleep -Seconds 2
Start-Process explorer.exe
```

---

### **7. Visual Studio neu starten**

1. **Schließe Visual Studio** komplett
2. **Lösche Build-Artefakte** (optional, für sauberen Neustart):
   ```powershell
   Remove-Item -Path "WinUI\bin" -Recurse -Force
   Remove-Item -Path "WinUI\obj" -Recurse -Force
   ```
3. **Öffne die Solution** erneut in Visual Studio
4. **Rebuild** das Projekt

---

## 🔧 Scripts im Detail

### **resize-icons-dotnet.ps1**

**Pfad:** `scripts/resize-icons-dotnet.ps1`

**Zweck:** Skaliert das Basis-Icon in alle benötigten Größen für WinUI 3.

**Parameter:**
```powershell
# Standard-Werte
-SourceIcon "scripts\mobaflow-icon.png"
-AssetsDir "WinUI\Assets"

# Beispiel mit eigenen Werten
.\resize-icons-dotnet.ps1 -SourceIcon "C:\MyIcon.png"
```

**Benötigt:** System.Drawing (Windows-only, in .NET 10 enthalten)

**Ausgabe:**
- 12 PNG-Dateien in verschiedenen Größen
- Erfolgs-/Fehler-Meldungen für jede Größe

---

### **create-ico.ps1**

**Pfad:** `scripts/create-ico.ps1`

**Zweck:** Erstellt eine Multi-Resolution `.ico` Datei für die Windows .exe.

**Parameter:**
```powershell
# Standard-Werte
-SourcePng "scripts\mobaflow-icon.png"
-OutputIco "scripts\mobaflow-icon.ico"
```

**Format:** ICO mit 4 Auflösungen (16x16, 32x32, 48x48, 256x256)

**Benötigt:** System.Drawing

---

### **fix-manifest.ps1**

**Pfad:** `scripts/fix-manifest.ps1`

**Zweck:** Aktualisiert `Package.appxmanifest` mit korrekten Werten.

**Änderungen:**
- `DisplayName` → "MOBAflow"
- `BackgroundColor` → "#5B3A99"

**Wichtig:** Führe dieses Script **nach** jedem Manifest-Update aus!

---

## ⚠️ Troubleshooting

### **Problem: Icon wird nicht angezeigt**

**Lösung 1: Cache leeren**
```powershell
ie4uinit.exe -show
taskkill /F /IM explorer.exe; Start-Process explorer.exe
```

**Lösung 2: Komplett neu bauen**
```powershell
Remove-Item -Path "WinUI\bin", "WinUI\obj" -Recurse -Force
dotnet build WinUI\WinUI.csproj
```

**Lösung 3: Manifest-Designer verwenden**
1. Öffne `WinUI/Package.appxmanifest` in Visual Studio
2. Gehe zum **Visual Assets** Tab
3. Klicke **"Generate"** oder **"Update"**
4. Wähle `mobaflow-icon.png` als Quelle

---

### **Problem: Icons sind verschwommen**

**Ursache:** Falsche Skalierung oder niedrige Ausgangsauflösung.

**Lösung:**
1. Stelle sicher, dass `mobaflow-icon.png` **mindestens 256x256px** groß ist
2. Verwende das `resize-icons-dotnet.ps1` Script (verwendet High-Quality Interpolation)
3. Optional: Erstelle jede Größe manuell in einem Grafikprogramm

---

### **Problem: "System.Drawing not found" Fehler**

**Ursache:** System.Drawing ist auf diesem System nicht verfügbar (z.B. Linux/Mac).

**Lösung:**
- **Windows:** Sollte automatisch verfügbar sein
- **Linux/Mac:** Verwende Online-Tools oder ImageMagick:
  ```bash
  # Install ImageMagick
  brew install imagemagick  # macOS
  sudo apt install imagemagick  # Linux
  
  # Resize manually
  magick convert mobaflow-icon.png -resize 44x44 Square44x44Logo.png
  ```

---

## 📋 Checkliste für Icon-Update

- [ ] Neues Icon erstellt/bearbeitet (`mobaflow-icon.png`)
- [ ] Icons skaliert (`resize-icons-dotnet.ps1`)
- [ ] ICO-Datei erstellt (`create-ico.ps1`)
- [ ] Manifest aktualisiert (`fix-manifest.ps1`)
- [ ] Projekt neu gebaut (`dotnet clean && dotnet build`)
- [ ] Icon-Cache geleert (`ie4uinit.exe -show`)
- [ ] Explorer neugestartet (optional)
- [ ] Visual Studio neugestartet
- [ ] App gestartet und Icon geprüft ✅

---

## 🎨 Design-Richtlinien

### **Farben:**
| Farbe | Hex | Verwendung |
|-------|-----|------------|
| Lila (Primär) | `#5B3A99` | Hintergrund, Branding |
| Blau (Akzent) | `#2B7CD3` | Lokomotive, Technik |
| Silber | `#C0C0C0` | Gleise, Metall |
| Rot | `#FF6B6B` | Feedback-Punkt, Warnung |
| Weiß | `#FFFFFF` | Text, Kontrast |

### **Icon-Größen (WinUI 3):**
| Name | Größe | Verwendung |
|------|-------|------------|
| Square44x44Logo | 44x44 | Kleines Tile, Taskbar |
| Square150x150Logo | 150x150 | Mittleres Tile |
| Wide310x150Logo | 310x150 | Breites Tile |
| StoreLogo | 50x50 | Microsoft Store |
| SplashScreen | 620x300 | App-Startbildschirm |
| LargeTile | 310x310 | Großes Tile |

**Scale-200 Varianten:** 2x Auflösung für High-DPI Displays.

---

## 📖 Weitere Ressourcen

- **WinUI 3 App Icon Guidelines:** [Microsoft Docs](https://docs.microsoft.com/en-us/windows/apps/design/style/iconography/app-icon-design)
- **SVG zu PNG Online-Converter:** [CloudConvert](https://cloudconvert.com/svg-to-png)
- **ICO Generator:** [RealFaviconGenerator](https://realfavicongenerator.net/)
- **Inkscape (SVG Editor):** [inkscape.org](https://inkscape.org/)
- **Figma (Design Tool):** [figma.com](https://www.figma.com/)

---

## 🔄 Workflow-Zusammenfassung (Ultra-Kompakt)

```powershell
# 1. Icon vorbereiten (mobaflow-icon.png)
# 2. Scripts ausführen
.\scripts\resize-icons-dotnet.ps1
.\scripts\create-ico.ps1
.\scripts\fix-manifest.ps1

# 3. Neu bauen
dotnet clean WinUI\WinUI.csproj
dotnet build WinUI\WinUI.csproj

# 4. Cache leeren
ie4uinit.exe -show

# 5. Visual Studio neustarten → Fertig! 🎉
```

---

**Letzte Aktualisierung:** 27.12.2025  
**Autor:** MOBAflow Development Team  
**Version:** 1.0

