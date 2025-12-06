#!/usr/bin/env pwsh
# PowerShell 7 Script to find and suggest better icons for trains
# Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT.

$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

try {
    Set-Location -Path $PSScriptRoot\..
    
    $xamlFile = 'WinUI\View\EditorPage.xaml'
    
    Write-Host "Scanning EditorPage.xaml for train-related icons..." -ForegroundColor Cyan
    
    $content = Get-Content $xamlFile -Raw -Encoding UTF8
    
    # Find all unique Glyph values
    $glyphs = [regex]::Matches($content, 'Glyph="(&#x[0-9A-F]+;)"')
    $uniqueGlyphs = $glyphs | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique
    
    Write-Host "`nCurrent icons in use:" -ForegroundColor Yellow
    foreach ($glyph in $uniqueGlyphs) {
        Write-Host "  $glyph" -ForegroundColor White
    }
    
    Write-Host "`nSegoe MDL2 Assets - Recommended icons:" -ForegroundColor Cyan
    Write-Host "  Locomotive:      &#xE7C8; (Car - already good)"
    Write-Host "  Goods Wagon:     &#xE7C1; (Package/Box)"
    Write-Host "  Passenger Wagon: &#xE716; (ContactSolid)"
    Write-Host "  Alternative:     &#xE7E8; (Memo - for passenger list)"
    
    Write-Host "`nNote: WinUI uses Segoe MDL2 Assets font" -ForegroundColor Gray
    Write-Host "Full reference: https://learn.microsoft.com/en-us/windows/apps/design/style/segoe-ui-symbol-font" -ForegroundColor Gray
    
    exit 0
    
} catch {
    Write-Error "Error: $_"
    exit 1
}
