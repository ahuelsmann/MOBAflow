# MOBAflow Build & Maintenance Scripts

Dieses Verzeichnis enthält PowerShell-Scripts für Wartung und Build-Prozesse.

---

## 📂 Icon-Management Scripts

### **Basis-Dateien:**
- `mobaflow-icon.svg` - SVG-Vorlage (editierbar in Inkscape/Figma)
- Basis-PNG wird in `WinUI/Assets/mobaflow-icon.png` gespeichert

---

### **resize-icons-dotnet.ps1**

Skaliert das Basis-Icon in alle benötigten Größen für WinUI 3.

**Verwendung:**
```powershell
.\scripts\resize-icons-dotnet.ps1
```

**Parameter:**
- `-SourceIcon` (optional): Pfad zum Basis-PNG (Standard: `WinUI\Assets\mobaflow-icon.png`)
- `-AssetsDir` (optional): Ziel-Verzeichnis (Standard: `WinUI\Assets`)

**Ausgabe:** 12 PNG-Dateien in verschiedenen Größen (44x44 bis 1240x600)

---

### **create-ico.ps1**
Erstellt eine Multi-Resolution `.ico` Datei für die Windows .exe.

**Verwendung:**
```powershell
.\scripts\create-ico.ps1
```

**Parameter:**
- `-SourcePng` (optional): Pfad zum Basis-PNG
- `-OutputIco` (optional): Pfad zur Ziel-ICO-Datei

**Ausgabe:** `mobaflow-icon.ico` mit 4 Auflösungen (16, 32, 48, 256px)

---

### **fix-manifest.ps1**
Aktualisiert `Package.appxmanifest` mit korrekten Werten.

**Verwendung:**
```powershell
.\scripts\fix-manifest.ps1
```

**Änderungen:**
- `DisplayName` → "MOBAflow"
- `BackgroundColor` → "#5B3A99"

---

### **generate-icons.ps1**
Legacy-Script (verwendet `Copy-Item` ohne Skalierung).

**⚠️ Veraltet:** Verwende stattdessen `resize-icons-dotnet.ps1`

---

## 🔄 Workflow: Icons aktualisieren

```powershell
# 1. Icons skalieren
.\scripts\resize-icons-dotnet.ps1

# 2. ICO-Datei erstellen
.\scripts\create-ico.ps1

# 3. Manifest aktualisieren
.\scripts\fix-manifest.ps1

# 4. Projekt neu bauen
dotnet clean WinUI\WinUI.csproj
dotnet build WinUI\WinUI.csproj

# 5. Icon-Cache leeren
ie4uinit.exe -show
```

---

## 📖 Weitere Dokumentation

Siehe: [`docs/copilot/ICON-UPDATE-GUIDE.md`](../docs/copilot/ICON-UPDATE-GUIDE.md)

---

**Letzte Aktualisierung:** 27.12.2025
