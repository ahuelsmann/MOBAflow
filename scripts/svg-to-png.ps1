# SVG to PNG Converter (MOBAflow)
# Converts mobaflow-icon.svg to PNG using built-in Windows tools

param(
    [string]$SvgPath = "scripts\mobaflow-icon.svg",
    [string]$OutputPath = "WinUI\Assets\mobaflow-icon.png",
    [int]$Size = 512  # Generate high-res PNG for better quality
)

Write-Host "🎨 Converting SVG to PNG..." -ForegroundColor Cyan
Write-Host ""

if (-not (Test-Path $SvgPath)) {
    Write-Error "SVG file not found: $SvgPath"
    exit 1
}

# Method 1: Try using Windows Presentation Foundation (WPF)
try {
    Add-Type -AssemblyName PresentationCore
    Add-Type -AssemblyName WindowsBase
    
    # Read SVG content
    $svgContent = Get-Content $SvgPath -Raw
    
    # Create WPF image from SVG
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($svgContent)
    $stream = New-Object System.IO.MemoryStream(, $bytes)
    
    # Load SVG as DrawingImage
    $drawingImage = New-Object System.Windows.Media.Imaging.DrawingImage
    
    Write-Host "⚠️  WPF SVG conversion requires external library." -ForegroundColor Yellow
    Write-Host "    Using alternative method..." -ForegroundColor Yellow
}
catch {
    # WPF doesn't natively support SVG, need alternative
}

# Method 2: Use Edge/Chrome to render SVG (Windows 10/11)
Write-Host "📋 Alternative: Manual conversion steps:" -ForegroundColor Cyan
Write-Host ""
Write-Host "1. Open SVG in browser:" -ForegroundColor Yellow
Write-Host "   Start-Process 'msedge' '$((Resolve-Path $SvgPath).Path)'" -ForegroundColor White
Write-Host ""
Write-Host "2. Take screenshot or use browser dev tools:" -ForegroundColor Yellow
Write-Host "   - F12 → Console → Run:" -ForegroundColor White
Write-Host "     const canvas = document.createElement('canvas');" -ForegroundColor Gray
Write-Host "     canvas.width = 512; canvas.height = 512;" -ForegroundColor Gray
Write-Host "     const ctx = canvas.getContext('2d');" -ForegroundColor Gray
Write-Host "     const img = document.querySelector('svg');" -ForegroundColor Gray
Write-Host "     // ... (manual process)" -ForegroundColor Gray
Write-Host ""
Write-Host "3. OR: Use online converter:" -ForegroundColor Yellow
Write-Host "   https://svgtopng.com" -ForegroundColor Cyan
Write-Host "   https://cloudconvert.com/svg-to-png" -ForegroundColor Cyan
Write-Host ""

# Method 3: Try Inkscape CLI (if installed)
$inkscapePaths = @(
    "C:\Program Files\Inkscape\bin\inkscape.exe",
    "C:\Program Files (x86)\Inkscape\bin\inkscape.exe",
    "$env:LOCALAPPDATA\Programs\Inkscape\bin\inkscape.exe"
)

$inkscape = $null
foreach ($path in $inkscapePaths) {
    if (Test-Path $path) {
        $inkscape = $path
        break
    }
}

if ($inkscape) {
    Write-Host "✅ Found Inkscape: $inkscape" -ForegroundColor Green
    Write-Host "   Converting SVG to PNG..." -ForegroundColor Cyan
    
    $absInputPath = (Resolve-Path $SvgPath).Path
    $absOutputPath = Join-Path $PWD $OutputPath
    
    & $inkscape --export-type="png" --export-filename="$absOutputPath" --export-width=$Size --export-height=$Size "$absInputPath"
    
    if (Test-Path $OutputPath) {
        Write-Host "✅ PNG created: $OutputPath ($Size x $Size)" -ForegroundColor Green
        Write-Host ""
        Write-Host "📋 Next step: Generate all icon sizes" -ForegroundColor Cyan
        Write-Host "   .\scripts\resize-icons-dotnet.ps1" -ForegroundColor White
    }
} else {
    Write-Host "⚠️  Inkscape not found." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "📥 Install Inkscape (recommended):" -ForegroundColor Cyan
    Write-Host "   winget install Inkscape.Inkscape" -ForegroundColor White
    Write-Host ""
    Write-Host "   OR download from: https://inkscape.org/release/" -ForegroundColor White
    Write-Host ""
    Write-Host "   Then run this script again." -ForegroundColor White
}

Write-Host ""
