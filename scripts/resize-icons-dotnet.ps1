# MOBAflow Icon Resizer (PowerShell + .NET)
# This script resizes icons using System.Drawing (Windows only)

param(
    [string]$SourceIcon = "WinUI\Assets\mobaflow-icon.png",
    [string]$AssetsDir = "WinUI\Assets"
)

# Check if source exists
if (-not (Test-Path $SourceIcon)) {
    Write-Error "Source icon not found: $SourceIcon"
    exit 1
}

Write-Host "🎨 Resizing WinUI 3 app icons..." -ForegroundColor Cyan
Write-Host ""

# Load System.Drawing assembly (Windows only)
Add-Type -AssemblyName System.Drawing

# Define icon sizes
$iconSizes = @(
    @{ Name = "Square44x44Logo.png"; Width = 44; Height = 44 }
    @{ Name = "Square44x44Logo.scale-200.png"; Width = 88; Height = 88 }
    @{ Name = "Square150x150Logo.png"; Width = 150; Height = 150 }
    @{ Name = "Square150x150Logo.scale-200.png"; Width = 300; Height = 300 }
    @{ Name = "Wide310x150Logo.png"; Width = 310; Height = 150 }
    @{ Name = "Wide310x150Logo.scale-200.png"; Width = 620; Height = 300 }
    @{ Name = "StoreLogo.png"; Width = 50; Height = 50 }
    @{ Name = "StoreLogo.scale-200.png"; Width = 100; Height = 100 }
    @{ Name = "SplashScreen.png"; Width = 620; Height = 300 }
    @{ Name = "SplashScreen.scale-200.png"; Width = 1240; Height = 600 }
    @{ Name = "LargeTile.png"; Width = 310; Height = 310 }
    @{ Name = "LargeTile.scale-200.png"; Width = 620; Height = 620 }
)

$successCount = 0
$totalCount = $iconSizes.Count

foreach ($icon in $iconSizes) {
    try {
        $targetPath = Join-Path $AssetsDir $icon.Name
        
        # Load source image
        $sourceImage = [System.Drawing.Image]::FromFile((Resolve-Path $SourceIcon))
        
        # Create new bitmap with target size
        $resized = New-Object System.Drawing.Bitmap($icon.Width, $icon.Height)
        $graphics = [System.Drawing.Graphics]::FromImage($resized)
        
        # Set high-quality resize settings
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
        $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        
        # Draw resized image
        $graphics.DrawImage($sourceImage, 0, 0, $icon.Width, $icon.Height)
        
        # Save
        $resized.Save($targetPath, [System.Drawing.Imaging.ImageFormat]::Png)
        
        # Cleanup
        $graphics.Dispose()
        $resized.Dispose()
        $sourceImage.Dispose()
        
        Write-Host "✅ Created: $($icon.Name) ($($icon.Width)x$($icon.Height))" -ForegroundColor Green
        $successCount++
    }
    catch {
        Write-Host "❌ Failed to create $($icon.Name): $_" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "✨ Icon resizing complete: $successCount/$totalCount successful" -ForegroundColor Green
Write-Host ""
Write-Host "📋 Next steps:" -ForegroundColor Yellow
Write-Host "1. Rebuild WinUI project:" -ForegroundColor Cyan
Write-Host "   dotnet build WinUI\WinUI.csproj" -ForegroundColor White
Write-Host ""
Write-Host "2. Clear Windows icon cache:" -ForegroundColor Cyan
Write-Host "   ie4uinit.exe -show" -ForegroundColor White
Write-Host ""
Write-Host "3. Restart Visual Studio and rebuild" -ForegroundColor Cyan
