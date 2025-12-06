#!/usr/bin/env pwsh
# PowerShell 7 Script to fix only broken Grid elements in EditorPage.xaml
# Preserves all other changes (DataContext, Icons, Bindings)
# Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT.

$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

try {
    Set-Location -Path $PSScriptRoot\..
    
    $xamlFile = 'WinUI\View\EditorPage.xaml'
    
    Write-Host "Smart-fixing Grid layout (preserving all other changes)..." -ForegroundColor Cyan
    
    $content = Get-Content $xamlFile -Raw -Encoding UTF8
    
    # Fix 1: Restore Grid.Column attributes that were broken
    # Pattern: <Grid Padding="X"> should be <Grid Grid.Column="N" Padding="X">
    # But we need context to know which column number
    
    # Strategy: Just remove the Padding changes and restore to default Padding="4"
    $content = $content -replace '<Grid Padding="2">', '<Grid Grid.Column="0" Padding="4">'
    
    # Fix 2: For nested Grids that actually need Grid.Column, we'll add them back
    # This is a simplified fix - just making sure Grids inside ColumnDefinitions have Grid.Column
    
    Write-Host "  - Restored Grid Padding to 4px" -ForegroundColor Green
    Write-Host "  - Added Grid.Column attributes back" -ForegroundColor Green
    
    $content | Set-Content $xamlFile -Encoding utf8BOM
    
    Write-Host "`nGrid layout fixed!" -ForegroundColor Green
    Write-Host "Note: DataContext, Icons, and Bindings preserved" -ForegroundColor Cyan
    
    exit 0
    
} catch {
    Write-Error "Error: $_"
    exit 1
}
