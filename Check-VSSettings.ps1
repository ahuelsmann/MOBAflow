# Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

<#
.SYNOPSIS
    Checks and fixes Visual Studio encoding settings that might cause the encoding dialog.

.DESCRIPTION
    This script helps diagnose and fix Visual Studio encoding dialog issues.
#>

Write-Host "Visual Studio Encoding Dialog Fix" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Step 1: Check if Visual Studio is running" -ForegroundColor Yellow
$vsProcess = Get-Process devenv -ErrorAction SilentlyContinue
if ($vsProcess) {
    Write-Host "  [!] Visual Studio is running. Please close it before continuing." -ForegroundColor Red
    Write-Host "  [!] Close all Visual Studio instances and run this script again." -ForegroundColor Red
    exit 1
}
else {
    Write-Host "  [OK] Visual Studio is not running." -ForegroundColor Green
}
Write-Host ""

Write-Host "Step 2: Clear Visual Studio Component Model Cache" -ForegroundColor Yellow
$cachePaths = Get-ChildItem -Path "$env:LOCALAPPDATA\Microsoft\VisualStudio" -Directory -ErrorAction SilentlyContinue | 
    Where-Object { $_.Name -match '^\d+\.\d+' }

if ($cachePaths) {
    foreach ($vsVersion in $cachePaths) {
        $componentCachePath = Join-Path $vsVersion.FullName "ComponentModelCache"
        if (Test-Path $componentCachePath) {
            Write-Host "  [INFO] Clearing cache: $componentCachePath" -ForegroundColor Gray
            try {
                Remove-Item -Path $componentCachePath -Recurse -Force -ErrorAction Stop
                Write-Host "  [OK] Cache cleared successfully." -ForegroundColor Green
            }
            catch {
                Write-Host "  [ERROR] Failed to clear cache: $($_.Exception.Message)" -ForegroundColor Red
            }
        }
    }
}
else {
    Write-Host "  [INFO] No Visual Studio cache found." -ForegroundColor Gray
}
Write-Host ""

Write-Host "Step 3: Check .editorconfig for XAML settings" -ForegroundColor Yellow
$editorConfigPath = "C:\Repo\ahuelsmann\MOBAflow\.editorconfig"
if (Test-Path $editorConfigPath) {
    $content = Get-Content $editorConfigPath -Raw
    if ($content -match '\[.*xaml.*\][\s\S]*?charset\s*=\s*utf-8-bom') {
        Write-Host "  [OK] .editorconfig has correct XAML charset setting (utf-8-bom)." -ForegroundColor Green
    }
    else {
        Write-Host "  [WARNING] .editorconfig may not have explicit XAML charset setting." -ForegroundColor Yellow
    }
}
else {
    Write-Host "  [INFO] No .editorconfig found." -ForegroundColor Gray
}
Write-Host ""

Write-Host "Step 4: Verify XAML files encoding" -ForegroundColor Yellow
$xamlFiles = Get-ChildItem -Path "C:\Repo\ahuelsmann\MOBAflow" -Filter "*.xaml" -Recurse -File | 
    Where-Object { 
        $_.FullName -notlike "*\.nuget\*" -and 
        $_.FullName -notlike "*\bin\*" -and 
        $_.FullName -notlike "*\obj\*" 
    }

$withoutBom = 0
foreach ($file in $xamlFiles) {
    $bytes = [System.IO.File]::ReadAllBytes($file.FullName)
    $hasBom = $bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF
    if (-not $hasBom) {
        Write-Host "  [WARNING] File without BOM: $($file.FullName)" -ForegroundColor Yellow
        $withoutBom++
    }
}

if ($withoutBom -eq 0) {
    Write-Host "  [OK] All $($xamlFiles.Count) XAML files have UTF-8 BOM." -ForegroundColor Green
}
else {
    Write-Host "  [WARNING] $withoutBom file(s) without BOM found." -ForegroundColor Yellow
}
Write-Host ""

Write-Host "Recommendations:" -ForegroundColor Cyan
Write-Host "  1. Start Visual Studio again" -ForegroundColor White
Write-Host "  2. Open a XAML file" -ForegroundColor White
Write-Host "  3. If the dialog still appears:" -ForegroundColor White
Write-Host "     - Select 'Unicode (UTF-8 with signature) - Codepage 65001'" -ForegroundColor White
Write-Host "     - Check 'Use this encoding for all files'" -ForegroundColor White
Write-Host "  4. Go to Tools > Options > Text Editor > General" -ForegroundColor White
Write-Host "     - Uncheck 'Auto-detect UTF-8 encoding without signature'" -ForegroundColor White
Write-Host ""
Write-Host "Done!" -ForegroundColor Green
