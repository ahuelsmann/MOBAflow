# Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT.
# Generiere Android Icons aus 512x512 PNG

param([string]$SourcePng = "appicon-512.png")

Write-Host "🎨 MAUI Android Icon Generator" -ForegroundColor Cyan

if (-not (Test-Path $SourcePng)) {
    Write-Host "❌ $SourcePng nicht gefunden!" -ForegroundColor Red
    exit 1
}

[Reflection.Assembly]::LoadWithPartialName("System.Drawing") | Out-Null
$sourceImage = [System.Drawing.Image]::FromFile((Resolve-Path $SourcePng).Path)

$sizes = @{
    "mipmap-mdpi"     = 48
    "mipmap-hdpi"     = 72
    "mipmap-xhdpi"    = 96
    "mipmap-xxhdpi"   = 144
    "mipmap-xxxhdpi"  = 192
}

$OutputDir = ".\Platforms\Android\Resources"
$successCount = 0

foreach ($folder in $sizes.Keys) {
    $size = $sizes[$folder]
    $folderPath = Join-Path $OutputDir $folder
    $outputPath = Join-Path $folderPath "appicon.png"
    
    if (-not (Test-Path $folderPath)) {
        New-Item -Path $folderPath -ItemType Directory -Force | Out-Null
    }
    
    $resizedImage = New-Object System.Drawing.Bitmap($size, $size)
    $graphics = [System.Drawing.Graphics]::FromImage($resizedImage)
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $graphics.DrawImage($sourceImage, 0, 0, $size, $size)
    $graphics.Dispose()
    
    $resizedImage.Save($outputPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $resizedImage.Dispose()
    
    Write-Host "   ✅ $folder ($size x $size)" -ForegroundColor Green
    $successCount++
}

$sourceImage.Dispose()

Write-Host ""
Write-Host "🎉 $successCount Icons generiert!" -ForegroundColor Green
Write-Host ""
Write-Host "📋 Nächste Schritte:" -ForegroundColor Cyan
Write-Host "   1. dotnet clean"
Write-Host "   2. dotnet build"
