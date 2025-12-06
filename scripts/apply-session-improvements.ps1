#!/usr/bin/env pwsh
# PowerShell 7 Script to apply ALL session improvements to EditorPage.xaml
# This restores all work from today's session
# Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT.

$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

try {
    Set-Location -Path $PSScriptRoot\..
    
    $xamlFile = 'WinUI\View\EditorPage.xaml'
    
    Write-Host "Re-applying all session improvements..." -ForegroundColor Cyan
    
    $content = Get-Content $xamlFile -Raw -Encoding UTF8
    
    # === IMPROVEMENT 1: Add DataContext Binding ===
    if ($content -notmatch 'DataContext=') {
        Write-Host "  [1/4] Adding DataContext binding..." -ForegroundColor Yellow
        $content = $content -replace '(x:Name="PageRoot"\s+mc:Ignorable="d">)', "DataContext=`"{x:Bind ViewModel}`"`n    `$1"
        Write-Host "        Done!" -ForegroundColor Green
    } else {
        Write-Host "  [1/4] DataContext already exists" -ForegroundColor Gray
    }
    
    # === IMPROVEMENT 2: Change PropertyGrid bindings from x:Bind to {Binding} ===
    $bindingCount = ($content | Select-String -Pattern 'SimplePropertyGrid.*x:Bind ViewModel\.CurrentSelectedObject' -AllMatches).Matches.Count
    if ($bindingCount -gt 0) {
        Write-Host "  [2/4] Fixing $bindingCount PropertyGrid bindings..." -ForegroundColor Yellow
        $content = $content -replace 'SelectedObject="\{x:Bind ViewModel\.CurrentSelectedObject, Mode=OneWay\}"', 'SelectedObject="{Binding CurrentSelectedObject, Mode=OneWay}"'
        Write-Host "        Done!" -ForegroundColor Green
    } else {
        Write-Host "  [2/4] PropertyGrid bindings already fixed" -ForegroundColor Gray
    }
    
    # === IMPROVEMENT 3: Update Icons ===
    $iconChanges = 0
    
    # Workflow: Flowchart -> Gear (&#xE713;)
    if ($content -match 'Glyph="&#xE9D9;"') {
        $content = $content -replace 'Glyph="&#xE9D9;"', 'Glyph="&#xE713;"'
        $iconChanges++
        Write-Host "  [3/4] Updated Workflow icon (Gear)" -ForegroundColor Green
    }
    
    # Action: Code -> Lightning (&#xE945;)
    if ($content -match 'Glyph="&#xE74E;"') {
        $content = $content -replace 'Glyph="&#xE74E;"', 'Glyph="&#xE945;"'
        $iconChanges++
        Write-Host "  [3/4] Updated Action icon (Lightning)" -ForegroundColor Green
    }
    
    if ($iconChanges -eq 0) {
        Write-Host "  [3/4] Icons already updated" -ForegroundColor Gray
    }
    
    # === IMPROVEMENT 4: Verify no broken Grid.Column attributes ===
    Write-Host "  [4/4] Verifying Grid layout..." -ForegroundColor Yellow
    
    # Check for PowerShell variables that shouldn't be there
    if ($content -match '\$\d+') {
        Write-Host "        WARNING: Found PowerShell variables in XAML!" -ForegroundColor Red
        $content = $content -replace '\$\d+', ''
        Write-Host "        Removed broken variables" -ForegroundColor Green
    }
    
    # Check for broken Grid patterns
    if ($content -match '<Grid Grid\.Column=""') {
        Write-Host "        WARNING: Found broken Grid.Column attributes!" -ForegroundColor Red
        $content = $content -replace '<Grid Grid\.Column=""', '<Grid'
        Write-Host "        Fixed Grid attributes" -ForegroundColor Green
    }
    
    Write-Host "        Layout verified!" -ForegroundColor Green
    
    # === SAVE ===
    $content | Set-Content $xamlFile -Encoding utf8BOM
    
    Write-Host "`n=== All improvements applied ===" -ForegroundColor Green
    Write-Host "  1. DataContext binding" -ForegroundColor Cyan
    Write-Host "  2. PropertyGrid {Binding} (not x:Bind)" -ForegroundColor Cyan
    Write-Host "  3. Icons (Gear, Lightning)" -ForegroundColor Cyan
    Write-Host "  4. Grid layout verified" -ForegroundColor Cyan
    
    exit 0
    
} catch {
    Write-Error "Error: $_"
    exit 1
}
