# Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
# Fix Android Resource ID Konflikt nach Icon-Änderungen
# Verwendung: .\fix-android-resources.ps1

Write-Host "🔧 Bereinige Android Build-Artefakte..." -ForegroundColor Yellow

# Build-Verzeichnisse löschen
$directories = @(
    "obj",
    "bin"
)

foreach ($dir in $directories) {
    if (Test-Path $dir) {
        Write-Host "   Lösche: $dir" -ForegroundColor Gray
        Remove-Item -Path $dir -Recurse -Force -ErrorAction SilentlyContinue
    }
}

# Android Resource Cache löschen
$resourceCache = "$env:LOCALAPPDATA\Xamarin\Mono for Android\Resource.designer.cache"
if (Test-Path $resourceCache) {
    Write-Host "   Lösche: Android Resource Cache" -ForegroundColor Gray
    Remove-Item -Path $resourceCache -Force -ErrorAction SilentlyContinue
}

# Temporäre Android-Build-Dateien
$tempAndroid = "$env:TEMP\Xamarin"
if (Test-Path $tempAndroid) {
    Write-Host "   Lösche: Xamarin Temp-Dateien" -ForegroundColor Gray
    Remove-Item -Path $tempAndroid -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host ""
Write-Host "✅ Bereinigung abgeschlossen!" -ForegroundColor Green
Write-Host ""
Write-Host "📋 Nächste Schritte:" -ForegroundColor Cyan
Write-Host "   1. Visual Studio schließen (falls geöffnet)"
Write-Host "   2. dotnet clean (optional)"
Write-Host "   3. Visual Studio öffnen"
Write-Host "   4. Projekt neu bauen (Rebuild)"
Write-Host ""
Write-Host "⚠️  Falls Problem weiterhin besteht:" -ForegroundColor Yellow
Write-Host "   - Prüfe ob Resources\AppIcon\appicon.svg existiert"
Write-Host "   - Erstelle manuelle PNG-Icons in Platforms\Android\Resources\mipmap-*\"
Write-Host ""
