# Fix Package.appxmanifest BackgroundColor
# This script updates the background color to match the MOBAflow icon

$manifestPath = "WinUI\Package.appxmanifest"

if (-not (Test-Path $manifestPath)) {
    Write-Error "Package.appxmanifest not found"
    exit 1
}

Write-Host "🔧 Updating Package.appxmanifest..." -ForegroundColor Cyan

# Read manifest
$content = Get-Content -Path $manifestPath -Raw

# Replace BackgroundColor from "transparent" to MOBAflow purple
$content = $content -replace 'BackgroundColor="transparent"', 'BackgroundColor="#5B3A99"'

# Replace DisplayName from "WinUI" to "MOBAflow"
$content = $content -replace '<DisplayName>WinUI</DisplayName>', '<DisplayName>MOBAflow</DisplayName>'

# Save modified manifest
Set-Content -Path $manifestPath -Value $content -Encoding UTF8

Write-Host "✅ Package.appxmanifest updated!" -ForegroundColor Green
Write-Host "   - DisplayName: MOBAflow" -ForegroundColor Cyan
Write-Host "   - BackgroundColor: #5B3A99 (Purple)" -ForegroundColor Cyan
Write-Host ""
Write-Host "📋 Next steps:" -ForegroundColor Yellow
Write-Host "1. Clean and rebuild:" -ForegroundColor Cyan
Write-Host "   dotnet clean WinUI\WinUI.csproj" -ForegroundColor White
Write-Host "   dotnet build WinUI\WinUI.csproj" -ForegroundColor White
Write-Host ""
Write-Host "2. Delete app cache and reinstall:" -ForegroundColor Cyan
Write-Host "   - Close Visual Studio" -ForegroundColor White
Write-Host "   - Delete: WinUI\bin and WinUI\obj folders" -ForegroundColor White
Write-Host "   - Rebuild in Visual Studio" -ForegroundColor White
