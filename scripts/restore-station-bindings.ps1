#!/usr/bin/env pwsh
# PowerShell 7 Script to restore Station bindings in EditorPage.xaml
# Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT.

$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

try {
    Set-Location -Path $PSScriptRoot\..
    
    $xamlFile = 'WinUI\View\EditorPage.xaml'
    
    Write-Host "Restoring Station bindings..." -ForegroundColor Cyan
    
    $content = Get-Content $xamlFile -Raw -Encoding UTF8
    
    # Find Station ListView and add SelectedItem binding
    $stationListViewPattern = '(<ListView\s+Grid\.Row="1"[^>]*ItemsSource="\{x:Bind ViewModel\.SelectedJourney\.Stations[^>]*)'
    
    if ($content -match $stationListViewPattern) {
        # Check if SelectedItem already exists
        if ($content -match 'ViewModel\.SelectedStation') {
            Write-Host "  SelectedStation binding already exists" -ForegroundColor Green
        } else {
            Write-Host "  Adding SelectedStation binding..." -ForegroundColor Yellow
            
            # Add SelectedItem binding to Station ListView
            $content = $content -replace '(ItemsSource="\{x:Bind ViewModel\.SelectedJourney\.Stations, Mode=OneWay\}")', '$1 SelectedItem="{x:Bind ViewModel.SelectedStation, Mode=TwoWay}"'
            
            Write-Host "  Done!" -ForegroundColor Green
        }
    } else {
        Write-Host "  WARNING: Could not find Station ListView!" -ForegroundColor Red
    }
    
    $content | Set-Content $xamlFile -Encoding utf8BOM
    
    Write-Host "`nStation bindings restored!" -ForegroundColor Green
    
    exit 0
    
} catch {
    Write-Error "Error: $_"
    exit 1
}
