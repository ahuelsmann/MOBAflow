# MOBAflow Icon Update Guide

**Letzte Aktualisierung:** 27.12.2025  
**Version:** 1.0

Dieses Dokument beschreibt, wie die App-Icons für MOBAflow (WinUI 3) aktualisiert werden können.

---

## 🎨 Übersicht

MOBAflow verwendet ein **lila-blaues** Icon-Design mit folgenden Elementen:
- 🟣 **Hintergrundfarbe:** `#5B3A99` (Lila/Violett)
- 🚂 **Lokomotive:** `#2B7CD3` (Blau)
- 🛤️ **Gleise:** `#C0C0C0` (Silber)
- 🔴 **Feedback-Punkt:** `#FF6B6B` (Rot)
- 📝 **Text:** `#FFFFFF` (Weiß)

---

## 📂 Icon-Dateien

### **Basis-Icons (Quelle):**
```
scripts/
├── mobaflow-icon.svg      ← SVG-Vorlage (editierbar in Inkscape/Figma)

WinUI/Assets/
└── mobaflow-icon.png      ← PNG-Basis (256x256 oder größer, manuell erstellt)
```

### **Generierte Icons (automatisch erstellt):**
```
WinUI/Assets/
├── mobaflow-icon.ico                ← Windows .exe Icon (Multi-Resolution)
├── Square44x44Logo.png              (44x44)
├── Square44x44Logo.scale-200.png    (88x88)
├── Square150x150Logo.png            (150x150)
├── Square150x150Logo.scale-200.png  (300x300)
├── Wide310x150Logo.png              (310x150)
├── Wide310x150Logo.scale-200.png    (620x300)
├── StoreLogo.png                    (50x50)
├── StoreLogo.scale-200.png          (100x100)
├── SplashScreen.png                 (620x300)
├── SplashScreen.scale-200.png       (1240x600)
├── LargeTile.png                    (310x310)
└── LargeTile.scale-200.png          (620x620)
```


---

## 🚀 Icon aktualisieren (Schritt-für-Schritt)

### **1. Neues Icon vorbereiten**

**Option A: SVG bearbeiten**
1. Öffne `scripts/mobaflow-icon.svg` in einem Editor (Inkscape, Figma, VS Code)
2. Passe das Design an (Farben, Form, Text)
3. Exportiere als PNG (mindestens 256x256px)
4. Speichere als `WinUI/Assets/mobaflow-icon.png`


**Option B: PNG direkt ersetzen**
1. Erstelle ein neues Icon (256x256px oder größer)
2. Speichere es als `WinUI/Assets/mobaflow-icon.png`
3. Stelle sicher, dass es **transparenten Hintergrund** oder **#5B3A99** hat


---

### **2. Icons in alle Größen generieren**

Führe das PowerShell-Script aus:

```powershell
# Im Projekt-Root-Verzeichnis
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass -Force
.\scripts\resize-icons-dotnet.ps1
```

**Was das Script macht:**
- ✅ Lädt `mobaflow-icon.png`
- ✅ Skaliert es in 12 verschiedene Größen (44x44 bis 1240x600)
- ✅ Verwendet High-Quality Bicubic Interpolation
- ✅ Speichert alle Icons in `scripts/`

**Ausgabe:**
```
🎨 Resizing WinUI 3 app icons...

✅ Created: Square44x44Logo.png (44x44)
✅ Created: Square44x44Logo.scale-200.png (88x88)
...
✨ Icon resizing complete: 12/12 successful
```

---

### **3. Windows .ico Datei erstellen** (optional)

Für die `.exe` wird eine `.ico` Datei benötigt:

```powershell
.\scripts\create-ico.ps1
```

**Was das Script macht:**
- ✅ Konvertiert PNG zu ICO (Multi-Resolution: 16, 32, 48, 256px)
- ✅ Speichert als `scripts/mobaflow-icon.ico`

**Ausgabe:**
```
🎨 Converting PNG to ICO format...
✅ ICO file created successfully: scripts\mobaflow-icon.ico
   Contains sizes: 16, 32, 48, 256px
```

---

### **4. Package.appxmanifest aktualisieren**

Passe die Manifest-Datei an (Hintergrundfarbe + DisplayName):

```powershell
.\scripts\fix-manifest.ps1
```

**Was das Script macht:**
- ✅ Ändert `BackgroundColor="transparent"` → `BackgroundColor="#5B3A99"`
- ✅ Ändert `<DisplayName>WinUI</DisplayName>` → `<DisplayName>MOBAflow</DisplayName>`

**Ausgabe:**
```
🔧 Updating Package.appxmanifest...
✅ Package.appxmanifest updated!
   - DisplayName: MOBAflow
   - BackgroundColor: #5B3A99 (Purple)
```

---

### **5. Projekt neu bauen**

```powershell
# Clean (alte Builds löschen)
dotnet clean WinUI\WinUI.csproj

# Build (neu kompilieren)
dotnet build WinUI\WinUI.csproj
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

