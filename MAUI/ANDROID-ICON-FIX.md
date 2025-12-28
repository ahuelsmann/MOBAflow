# MAUI Android Icon Fix - Anleitung

**Problem:** Fragment-Container `id/legacy` nicht gefunden nach automatischer Icon-Generierung.

**Ursache:** MAUI's automatische SVG-Icon-Generierung (`<MauiIcon>`) verschiebt Android Resource IDs.

---

## ✅ Sofortlösung (Bereits umgesetzt)

1. **MauiIcon deaktiviert** in `MAUI.csproj`
2. **Build-Cache bereinigen:**
   ```powershell
   cd MAUI
   .\fix-android-resources.ps1
   ```

3. **Rebuild:**
   - Visual Studio schließen
   - Visual Studio öffnen
   - **Rebuild Solution** (Strg+Umschalt+B)

---

## 🎨 Permanente Lösung: Manuelle Icon-Platzierung

### Option 1: Standard MAUI-Icon verwenden (schnellste Lösung)

**Nichts tun** - MAUI verwendet automatisch ein generisches Icon.

### Option 2: Eigene PNG-Icons platzieren

**Struktur erstellen:**
```
MAUI/Platforms/Android/Resources/
├── mipmap-mdpi/appicon.png      (48x48)
├── mipmap-hdpi/appicon.png      (72x72)
├── mipmap-xhdpi/appicon.png     (96x96)
├── mipmap-xxhdpi/appicon.png    (144x144)
└── mipmap-xxxhdpi/appicon.png   (192x192)
```

**Icons generieren:**

1. **Basis-Icon vorbereiten** (512x512 PNG)
2. **Online-Tool verwenden:**
   - https://icon.kitchen (empfohlen)
   - https://romannurik.github.io/AndroidAssetStudio/
   
3. **Generierte Dateien** in `Platforms/Android/Resources/` entpacken

**ODER: Mit PowerShell/ImageMagick:**

```powershell
# Beispiel (benötigt ImageMagick)
$sizes = @{
    "mipmap-mdpi" = 48
    "mipmap-hdpi" = 72
    "mipmap-xhdpi" = 96
    "mipmap-xxhdpi" = 144
    "mipmap-xxxhdpi" = 192
}

foreach ($folder in $sizes.Keys) {
    $size = $sizes[$folder]
    New-Item -Path "Platforms/Android/Resources/$folder" -ItemType Directory -Force
    magick convert "appicon-512.png" -resize "${size}x${size}" "Platforms/Android/Resources/$folder/appicon.png"
}
```

### Option 3: SVG-Icons mit festen Ressourcen-IDs

**Fortgeschritten:** Erfordert manuelle `Resource.designer.cs` Anpassungen (nicht empfohlen).

---

## 🔍 Diagnose

**Prüfen ob Problem behoben:**

```powershell
# Nach Rebuild
adb logcat | Select-String "legacy\|Fragment\|IllegalArgumentException"
```

**Erwartetes Ergebnis:** Keine Fehler mehr über `id/legacy`.

---

## 📚 Hintergrund

**Warum passiert das?**

1. MAUI generiert Android-Ressourcen (`R.id.*`) automatisch aus Build-Artefakten
2. Die Reihenfolge der Ressourcen-Generierung beeinflusst die ID-Zuweisung
3. SVG → PNG Konvertierung während des Builds ändert diese Reihenfolge
4. Fragment-Navigation erwartet fixe IDs, die sich verschoben haben

**Lösung:** Statische PNG-Icons anstelle dynamischer SVG-Konvertierung.

---

## ⚠️ Troubleshooting

**Problem weiterhin vorhanden?**

1. **Komplett Clean:**
   ```powershell
   dotnet clean
   Remove-Item -Recurse -Force obj, bin
   ```

2. **Visual Studio Cache:**
   - Extras → Optionen → Projekte und Projektmappen
   - "Binärausgabe beim Öffnen der Projekte löschen"

3. **Android Emulator neu starten**

4. **App deinstallieren:**
   ```powershell
   adb uninstall com.mobaflow.mobasmart
   ```

---

**Letzte Aktualisierung:** 28.12.2025  
**Issue:** Fragment NavigationRootManager_ElementBasedFragment / id/legacy
