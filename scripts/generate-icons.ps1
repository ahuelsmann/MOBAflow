# MOBAflow Icon Generator Script
# This script copies the base icon to all required sizes for WinUI 3

$sourceIcon = "WinUI\Assets\mobaflow-icon.png"
$assetsDir = "WinUI\Assets"

# Check if source icon exists
if (-not (Test-Path $sourceIcon)) {
    Write-Error "Source icon not found: $sourceIcon"
    Write-Host "Please ensure mobaflow-icon.png exists in WinUI\Assets\"
    exit 1
}

Write-Host "🎨 Creating WinUI 3 app icons from mobaflow-icon.png..." -ForegroundColor Cyan

# Define required icon files
# For now, we'll copy the same icon to all sizes
# You should ideally resize these to exact dimensions using a tool like ImageMagick
$iconMappings = @{
    # Small tile (44x44)
    "Square44x44Logo.png" = $sourceIcon
    "Square44x44Logo.scale-200.png" = $sourceIcon
    
    # Medium tile (150x150)
    "Square150x150Logo.png" = $sourceIcon
    "Square150x150Logo.scale-200.png" = $sourceIcon
    
    # Wide tile (310x150)
    "Wide310x150Logo.png" = $sourceIcon
    "Wide310x150Logo.scale-200.png" = $sourceIcon
    
    # Store logo (50x50)
    "StoreLogo.png" = $sourceIcon
    "StoreLogo.scale-200.png" = $sourceIcon
    
    # Splash screen (620x300)
    "SplashScreen.png" = $sourceIcon
    "SplashScreen.scale-200.png" = $sourceIcon
    
    # Large tile (optional, 310x310)
    "LargeTile.png" = $sourceIcon
    "LargeTile.scale-200.png" = $sourceIcon
}

# Copy icons
foreach ($targetName in $iconMappings.Keys) {
    $targetPath = Join-Path $assetsDir $targetName
    Copy-Item -Path $sourceIcon -Destination $targetPath -Force
    Write-Host "✅ Created: $targetName" -ForegroundColor Green
}

Write-Host ""
Write-Host "✨ Icon generation complete!" -ForegroundColor Green
Write-Host ""
Write-Host "⚠️  NOTE: All icons are currently the same size." -ForegroundColor Yellow
Write-Host "For best results, resize each icon to its target dimensions:" -ForegroundColor Yellow
Write-Host "  - Square44x44Logo.png → 44x44px" -ForegroundColor Yellow
Write-Host "  - Square150x150Logo.png → 150x150px" -ForegroundColor Yellow
Write-Host "  - Wide310x150Logo.png → 310x150px" -ForegroundColor Yellow
Write-Host "  - SplashScreen.png → 620x300px" -ForegroundColor Yellow
Write-Host ""
Write-Host "You can use online tools like https://www.iloveimg.com/resize-image" -ForegroundColor Cyan
Write-Host "or install ImageMagick for batch resizing." -ForegroundColor Cyan
