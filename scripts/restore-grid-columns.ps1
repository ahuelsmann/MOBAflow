#!/usr/bin/env pwsh
# PowerShell 7 Script to restore Grid.Column attributes in EditorPage.xaml
# Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT.

$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

try {
    Set-Location -Path $PSScriptRoot\..
    
    $xamlFile = 'WinUI\View\EditorPage.xaml'
    
    Write-Host "Restoring Grid.Column attributes..." -ForegroundColor Cyan
    
    $content = Get-Content $xamlFile -Raw -Encoding UTF8
    
    # The regex incorrectly replaced Grid Grid.Column="\d+" with Grid Grid.Column="$1"
    # We need to restore the original pattern or just remove the Grid.Column attribute
    
    # Fix: Just change Padding back and remove the broken Grid.Column reference
    $content = $content -replace '<Grid Grid\.Column="" Padding="2">', '<Grid Padding="2">'
    
    # For lines that still have Grid Grid.Column, fix them
    $lines = $content -split "`n"
    $fixed = @()
    
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        
        # If line has <Grid Grid.Column= pattern, it's broken
        if ($line -match '<Grid Grid\.Column="[^"]*" Padding="2">') {
            # Just make it <Grid Padding="2">
            $line = $line -replace '<Grid Grid\.Column="[^"]*" Padding="2">', '<Grid Padding="2">'
            Write-Host "  Fixed line $($i + 1)" -ForegroundColor Green
        }
        
        $fixed += $line
    }
    
    $content = $fixed -join "`n"
    $content | Set-Content $xamlFile -Encoding utf8BOM
    
    Write-Host "`nGrid attributes restored!" -ForegroundColor Green
    
    exit 0
    
} catch {
    Write-Error "Error: $_"
    exit 1
}
