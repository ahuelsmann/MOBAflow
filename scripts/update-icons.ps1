#!/usr/bin/env pwsh
# PowerShell 7 Script to update icons in EditorPage.xaml
# Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT.

$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

try {
    Set-Location -Path $PSScriptRoot\..
    
    $xamlFile = 'WinUI\View\EditorPage.xaml'
    
    if (-not (Test-Path $xamlFile)) {
        Write-Error "File not found: $xamlFile"
        exit 1
    }
    
    Write-Host "Updating icons in EditorPage.xaml..." -ForegroundColor Cyan
    
    $content = Get-Content $xamlFile -Raw -Encoding UTF8
    
    # Track changes
    $changes = 0
    
    # Replace Workflow icons (&#xE9D9; -> &#xE713; Gear/Zahnrad)
    if ($content -match 'Glyph="&#xE9D9;"') {
        $content = $content -replace 'Glyph="&#xE9D9;"', 'Glyph="&#xE713;"'
        Write-Host "  - Workflow icon: Flowchart -> Gear" -ForegroundColor Green
        $changes++
    }
    
    # Replace Action icons (&#xE74E; -> &#xE945; Lightning/Blitz)
    if ($content -match 'Glyph="&#xE74E;"') {
        $content = $content -replace 'Glyph="&#xE74E;"', 'Glyph="&#xE945;"'
        Write-Host "  - Action icon: Code -> Lightning" -ForegroundColor Green
        $changes++
    }
    
    # Save changes
    if ($changes -gt 0) {
        $content | Set-Content $xamlFile -Encoding utf8BOM
        Write-Host "`nSuccessfully updated $changes icon(s)!" -ForegroundColor Green
        exit 0
    } else {
        Write-Host "No icons to update (already current)" -ForegroundColor Yellow
        exit 0
    }
    
} catch {
    Write-Error "Error: $_"
    exit 1
}
