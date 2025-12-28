# Icon Update Script - All Platforms
# Updates icons for WinUI, MAUI, and Blazor WebApp from source PNG

param(
    [string]$SourcePng = "docs\modern_train.png"
)

if (-not (Test-Path $SourcePng)) {
    Write-Error "Source PNG not found: $SourcePng"
    exit 1
}

Write-Host "🎨 Updating icons for all platforms..." -ForegroundColor Cyan
Write-Host ""

# 1. Copy to WinUI base location
Write-Host "📱 WinUI (Windows Desktop)..." -ForegroundColor Yellow
Copy-Item -Path $SourcePng -Destination "WinUI\Assets\mobaflow-icon.png" -Force
Write-Host "   ✅ Base PNG copied" -ForegroundColor Green

# Generate WinUI icons (12 sizes + ICO)
& .\scripts\resize-icons-dotnet.ps1
& .\scripts\create-ico.ps1

# 2. Update MAUI AppIcon (note: SVG is manually designed, PNG backup only)
Write-Host ""
Write-Host "📱 MAUI (Android)..." -ForegroundColor Yellow
Copy-Item -Path $SourcePng -Destination "MAUI\Resources\AppIcon\modern_train_backup.png" -Force
Write-Host "   ✅ PNG backup stored (SVG icon is manually designed)" -ForegroundColor Green
Write-Host "   ℹ️  MAUI uses appicon.svg (already updated with modern design)" -ForegroundColor Cyan

# 3. Update Blazor WebApp favicon
Write-Host ""
Write-Host "🌐 Blazor WebApp (Web Dashboard)..." -ForegroundColor Yellow
Copy-Item -Path "WinUI\Assets\mobaflow-icon.png" -Destination "WebApp\wwwroot\favicon.png" -Force
Copy-Item -Path "WinUI\Assets\mobaflow-icon.ico" -Destination "WebApp\wwwroot\favicon.ico" -Force
Write-Host "   ✅ favicon.png updated" -ForegroundColor Green
Write-Host "   ✅ favicon.ico updated" -ForegroundColor Green

# Optional: Create PWA icons (192x192, 512x512, Apple Touch Icon)
Write-Host ""
Write-Host "📋 Optional: Create PWA icons (192, 512, Apple Touch)? (Y/N)" -ForegroundColor Cyan
$response = Read-Host
if ($response -eq 'Y' -or $response -eq 'y') {
    Add-Type -AssemblyName System.Drawing
    $sourceImage = [System.Drawing.Image]::FromFile((Resolve-Path "WinUI\Assets\mobaflow-icon.png"))
    
    $webSizes = @(
        @{ Name = "icon-192.png"; Size = 192 }
        @{ Name = "icon-512.png"; Size = 512 }
        @{ Name = "apple-touch-icon.png"; Size = 180 }
    )
    
    foreach ($icon in $webSizes) {
        $resized = New-Object System.Drawing.Bitmap($icon.Size, $icon.Size)
        $graphics = [System.Drawing.Graphics]::FromImage($resized)
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.DrawImage($sourceImage, 0, 0, $icon.Size, $icon.Size)
        $targetPath = "WebApp\wwwroot\$($icon.Name)"
        $resized.Save($targetPath, [System.Drawing.Imaging.ImageFormat]::Png)
        $graphics.Dispose()
        $resized.Dispose()
        Write-Host "   ✅ Created: $($icon.Name)" -ForegroundColor Green
    }
    
    $sourceImage.Dispose()
}

Write-Host ""
Write-Host "✨ All platform icons updated!" -ForegroundColor Green
Write-Host ""
Write-Host "📋 Next steps:" -ForegroundColor Cyan
Write-Host "   1. Build all projects:" -ForegroundColor White
Write-Host "      dotnet build" -ForegroundColor Gray
Write-Host ""
Write-Host "   2. Clear Windows icon cache:" -ForegroundColor White
Write-Host "      ie4uinit.exe -show" -ForegroundColor Gray
Write-Host ""
Write-Host "   3. Test on all platforms:" -ForegroundColor White
Write-Host "      - WinUI: Run desktop app" -ForegroundColor Gray
Write-Host "      - MAUI: Deploy to Android device" -ForegroundColor Gray
Write-Host "      - Blazor: Open browser (check favicon)" -ForegroundColor Gray
Write-Host ""
