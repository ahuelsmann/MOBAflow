#!/usr/bin/env pwsh
# PowerShell 7 Script to restore EditorPage.xaml from git
# Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT.

$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

try {
    Set-Location -Path $PSScriptRoot\..
    
    Write-Host "Restoring EditorPage.xaml from git..." -ForegroundColor Yellow
    
    # Check if we have uncommitted changes
    $status = git status --porcelain WinUI/View/EditorPage.xaml
    
    if ($status) {
        Write-Host "  Found modified EditorPage.xaml" -ForegroundColor Cyan
        
        # Restore from git HEAD (last commit)
        git checkout HEAD -- WinUI/View/EditorPage.xaml
        
        Write-Host "  Restored from last commit" -ForegroundColor Green
    } else {
        Write-Host "  No changes detected in git" -ForegroundColor Yellow
    }
    
    Write-Host "`nEditorPage.xaml restored!" -ForegroundColor Green
    Write-Host "Note: All compact mode changes have been reverted" -ForegroundColor Gray
    
    exit 0
    
} catch {
    Write-Error "Error: $_"
    exit 1
}
