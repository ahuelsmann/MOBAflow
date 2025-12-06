#!/usr/bin/env pwsh
# PowerShell 7 Script to fix XAML parse error in EditorPage.xaml
# Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT.

$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

try {
    Set-Location -Path $PSScriptRoot\..
    
    $xamlFile = 'WinUI\View\EditorPage.xaml'
    
    Write-Host "Fixing XAML parse error in EditorPage.xaml..." -ForegroundColor Red
    
    $content = Get-Content $xamlFile -Raw -Encoding UTF8
    
    # Remove any $1, $2, etc. that were inserted by mistake
    if ($content -match '\$\d+') {
        Write-Host "  Found PowerShell variable in XAML (line with `$1 or similar)" -ForegroundColor Yellow
        
        # Show the problematic line
        $lines = $content -split "`n"
        for ($i = 0; $i -lt $lines.Count; $i++) {
            if ($lines[$i] -match '\$\d+') {
                Write-Host "  Line $($i + 1): $($lines[$i].Trim())" -ForegroundColor Red
            }
        }
        
        # Remove all $1, $2, $3 etc.
        $content = $content -replace '\$\d+', ''
        
        Write-Host "  Removed PowerShell variables from XAML" -ForegroundColor Green
    }
    
    # Clean up any double spaces or empty lines that might have been created
    $content = $content -replace '  +', ' '
    
    $content | Set-Content $xamlFile -Encoding utf8BOM
    
    Write-Host "`nXAML fixed! Please rebuild." -ForegroundColor Green
    
    exit 0
    
} catch {
    Write-Error "Error: $_"
    exit 1
}
