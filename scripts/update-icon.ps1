# Quick Icon Update Script (MOBAflow)
# Opens SVG in browser for manual PNG export, then generates all sizes

Write-Host "🎨 MOBAflow Icon Update Wizard" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""

$svgPath = "scripts\mobaflow-icon.svg"
$pngPath = "WinUI\Assets\mobaflow-icon.png"

# Step 1: Open SVG in browser
Write-Host "Step 1: Opening SVG in browser..." -ForegroundColor Yellow
Start-Process "msedge" (Resolve-Path $svgPath).Path

Write-Host ""
Write-Host "📋 Manual steps:" -ForegroundColor Cyan
Write-Host "   1. Right-click on the SVG in Edge" -ForegroundColor White
Write-Host "   2. Select 'Save image as...'" -ForegroundColor White
Write-Host "   3. Save as: $pngPath" -ForegroundColor Green
Write-Host "   4. Make sure it's at least 256x256 px" -ForegroundColor White
Write-Host ""

# Wait for user confirmation
Read-Host "Press ENTER when you've saved the PNG..."

# Step 2: Check if PNG exists
if (-not (Test-Path $pngPath)) {
    Write-Host "❌ PNG not found at: $pngPath" -ForegroundColor Red
    Write-Host "   Please save the PNG and run this script again." -ForegroundColor Yellow
    exit 1
}

Write-Host "✅ PNG found!" -ForegroundColor Green
Write-Host ""

# Step 3: Generate all icon sizes
Write-Host "Step 2: Generating all icon sizes..." -ForegroundColor Yellow
& .\scripts\resize-icons-dotnet.ps1

Write-Host ""
Write-Host "✨ Icon update complete!" -ForegroundColor Green
Write-Host ""
Write-Host "📋 Final steps:" -ForegroundColor Cyan
Write-Host "   1. Rebuild WinUI project:" -ForegroundColor White
Write-Host "      dotnet build WinUI\WinUI.csproj" -ForegroundColor Gray
Write-Host ""
Write-Host "   2. Clear Windows icon cache:" -ForegroundColor White
Write-Host "      ie4uinit.exe -show" -ForegroundColor Gray
Write-Host ""
Write-Host "   3. Restart app to see new icon" -ForegroundColor White
Write-Host ""
