# Platform Icons - Übersicht

Dieses Dokument beschreibt, wo und wie Icons für die verschiedenen MOBAflow-Plattformen verwendet werden.

---

## 📱 Plattform-Übersicht

| Plattform | Icon-Format | Speicherort | Automatisch generiert? |
|-----------|-------------|-------------|------------------------|
| **WinUI (Desktop)** | PNG + ICO | `WinUI\Assets\` | ✅ Ja (12 Größen) |
| **MAUI (Android)** | SVG | `MAUI\Resources\AppIcon\appicon.svg` | ❌ Nein (manuell designed) |
| **Blazor (Web)** | PNG + ICO | `WebApp\wwwroot\favicon.*` | ✅ Ja (kopiert von WinUI) |

---

## 🎨 Design-Konsistenz

Alle Plattformen verwenden das **gleiche moderne Eisenbahn-Design**:

- 🟣 **Hintergrund:** `#5B3A99` (Lila/Violett)
- 🚂 **Lokomotive:** `#2B7CD3` (Blau) - Frontalansicht
- 🪟 **Windschutzscheibe:** `#87CEEB` (Hellblau, transparent)
- 💡 **Scheinwerfer:** `#FFD700` (Gold)
- 🛤️ **Gleise:** `#C0C0C0` (Silber)

---

## 🖥️ WinUI (Windows Desktop)

### Icon-Dateien

**Basis:**
- `WinUI\Assets\mobaflow-icon.png` (512x512) - Quelle
- `WinUI\Assets\mobaflow-icon.ico` (16/32/48/256) - In .exe eingebettet

**Generiert (12 Dateien):**
```
WinUI\Assets\
├── Square44x44Logo.png              (Taskleiste)
├── Square44x44Logo.scale-200.png    (High-DPI)
├── Square150x150Logo.png            (Start-Kachel)
├── Square150x150Logo.scale-200.png
├── Wide310x150Logo.png              (Breite Kachel)
├── Wide310x150Logo.scale-200.png
├── StoreLogo.png
├── StoreLogo.scale-200.png
├── SplashScreen.png                 (Ladebildschirm)
├── SplashScreen.scale-200.png
├── LargeTile.png
└── LargeTile.scale-200.png
```

### Update-Befehl

```powershell
.\scripts\resize-icons-dotnet.ps1  # PNG-Größen
.\scripts\create-ico.ps1           # ICO-Datei
```

### Verwendung

- **Package.appxmanifest:** Referenziert PNG-Dateien
- **WinUI.csproj:** `<ApplicationIcon>Assets\mobaflow-icon.ico</ApplicationIcon>`

---

## 📱 MAUI (Android)

### Icon-Dateien

**Basis:**
- `MAUI\Resources\AppIcon\appicon.svg` - **Hauptquelle** (manuell designed)
- `MAUI\Resources\AppIcon\appiconfg.svg` - Foreground (optional)
- `MAUI\Resources\AppIcon\modern_train_backup.png` - PNG-Backup

### SVG-Design

Das SVG ist **manuell designed** und optimiert für Android Adaptive Icons:

```svg
<svg width="456" height="456" viewBox="0 0 456 456">
  <!-- Lila Hintergrund -->
  <rect fill="#5B3A99"/>
  
  <!-- Blaue Lok (frontal) -->
  <rect fill="#2B7CD3"/>
  
  <!-- Windschutzscheibe -->
  <rect fill="#87CEEB" opacity="0.9"/>
  
  <!-- Scheinwerfer -->
  <circle fill="#FFD700"/>
  
  <!-- Gleise -->
  <rect fill="#C0C0C0"/>
</svg>
```

### Automatische Generierung

MAUI generiert automatisch beim Build:
- `mipmap-mdpi/appicon.png` (48x48)
- `mipmap-hdpi/appicon.png` (72x72)
- `mipmap-xhdpi/appicon.png` (96x96)
- `mipmap-xxhdpi/appicon.png` (144x144)
- `mipmap-xxxhdpi/appicon.png` (192x192)
- Adaptive Icons (Android 8+)

### Update-Befehl

```powershell
# SVG manuell bearbeiten (kein Script)
code MAUI\Resources\AppIcon\appicon.svg

# PNG-Backup aktualisieren
Copy-Item docs\modern_train.png MAUI\Resources\AppIcon\modern_train_backup.png
```

### Verwendung

- **MAUI.csproj:** Automatische Erkennung via `<MauiIcon Include="Resources\AppIcon\appicon.svg" />`
- **AndroidManifest.xml:** Referenziert `@mipmap/appicon`

---

## 🌐 Blazor (WebApp)

### Icon-Dateien

**Basis:**
- `WebApp\wwwroot\favicon.png` (512x512) - Modernes Browser-Icon
- `WebApp\wwwroot\favicon.ico` (16/32/48/256) - Legacy-Browser

**Optional (PWA):**
- `WebApp\wwwroot\icon-192.png` - Android Chrome
- `WebApp\wwwroot\icon-512.png` - Android maskierbar
- `WebApp\wwwroot\apple-touch-icon.png` - iOS Safari

### Update-Befehl

```powershell
# Kopiere von WinUI
Copy-Item WinUI\Assets\mobaflow-icon.png WebApp\wwwroot\favicon.png
Copy-Item WinUI\Assets\mobaflow-icon.ico WebApp\wwwroot\favicon.ico

# Optional: PWA-Icons generieren
.\scripts\update-all-icons.ps1  # Wähle 'Y' für PWA-Icons
```

### Verwendung

**`WebApp\Components\App.razor`:**
```html
<link rel="icon" type="image/png" href="favicon.png" />
<link rel="icon" type="image/x-icon" href="favicon.ico" />

<!-- Optional: PWA -->
<link rel="apple-touch-icon" href="apple-touch-icon.png" />
<link rel="manifest" href="manifest.json" />
```

**PWA Manifest (`manifest.json`):**
```json
{
  "icons": [
    { "src": "icon-192.png", "sizes": "192x192", "type": "image/png" },
    { "src": "icon-512.png", "sizes": "512x512", "type": "image/png" }
  ]
}
```

---

## 🚀 Schnell-Update (Alle Plattformen)

### Automatisches Script

```powershell
# Alle Plattformen auf einmal aktualisieren
.\scripts\update-all-icons.ps1
```

**Was passiert:**
1. ✅ WinUI: Generiert 12 PNG + ICO
2. ✅ MAUI: Kopiert PNG-Backup (SVG bleibt manuell)
3. ✅ Blazor: Kopiert favicon.png + .ico
4. ⚠️ Optional: PWA-Icons (192, 512, Apple Touch)

### Manuell (Schritt-für-Schritt)

```powershell
# 1. Quelle vorbereiten
Copy-Item docs\modern_train.png WinUI\Assets\mobaflow-icon.png

# 2. WinUI
.\scripts\resize-icons-dotnet.ps1
.\scripts\create-ico.ps1

# 3. MAUI (nur Backup, SVG manuell)
Copy-Item docs\modern_train.png MAUI\Resources\AppIcon\modern_train_backup.png

# 4. Blazor
Copy-Item WinUI\Assets\mobaflow-icon.png WebApp\wwwroot\favicon.png
Copy-Item WinUI\Assets\mobaflow-icon.ico WebApp\wwwroot\favicon.ico

# 5. Build & Deploy
dotnet build
```

---

## 📋 Checkliste nach Icon-Update

- [ ] **WinUI:** Taskleiste zeigt neues Icon
- [ ] **WinUI:** Start-Menü-Kachel aktualisiert
- [ ] **WinUI:** Icon-Cache gelöscht (`ie4uinit.exe -show`)
- [ ] **MAUI:** APK auf Android-Gerät installiert
- [ ] **MAUI:** App-Drawer zeigt neues Icon
- [ ] **Blazor:** Browser-Tab zeigt Favicon
- [ ] **Blazor:** Lesezeichen-Icon korrekt
- [ ] **Blazor:** PWA-Installation (falls aktiviert)

---

## 🛠️ Troubleshooting

### WinUI: Altes Icon in Taskleiste

```powershell
# Icon-Cache löschen
Remove-Item "$env:LOCALAPPDATA\IconCache.db" -Force
Remove-Item "$env:LOCALAPPDATA\Microsoft\Windows\Explorer\iconcache*" -Force

# Explorer neustarten
taskkill /F /IM explorer.exe
Start-Process explorer.exe

# Projekt neu bauen
dotnet clean WinUI\WinUI.csproj
dotnet build WinUI\WinUI.csproj
```

### MAUI: Icon wird nicht aktualisiert

```powershell
# APK komplett neu installieren
dotnet build MAUI\MAUI.csproj -f net9.0-android -c Release
adb uninstall com.mobaflow.mobasmart
adb install MAUI\bin\Release\net9.0-android\com.mobaflow.mobasmart-Signed.apk
```

### Blazor: Favicon cached

```
Browser-Cache leeren:
- Chrome: Ctrl+Shift+Delete
- Edge: Ctrl+Shift+Delete
- Firefox: Ctrl+Shift+Delete

Oder Hard Reload:
- Ctrl+F5 (Windows)
- Cmd+Shift+R (Mac)
```

---

*Letzte Aktualisierung: 25.12.2025*
