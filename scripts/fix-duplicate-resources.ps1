#!/usr/bin/env pwsh
# PowerShell 7 Script to fix duplicate Page.Resources in EditorPage.xaml
# Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT.

$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

try {
    Set-Location -Path $PSScriptRoot\..
    
    $xamlFile = 'WinUI\View\EditorPage.xaml'
    
    Write-Host "Fixing duplicate Page.Resources in EditorPage.xaml..." -ForegroundColor Yellow
    
    $content = Get-Content $xamlFile -Raw -Encoding UTF8
    
    # Remove the duplicate ResourceDictionary we added
    $content = $content -replace '<ResourceDictionary>\s*<ResourceDictionary\.ThemeDictionaries>[\s\S]*?</ResourceDictionary\.ThemeDictionaries>\s*</ResourceDictionary>\s*', ''
    
    # Now add CompactMode styles properly to existing Page.Resources
    $compactStyles = @'

        <!-- Compact Mode Styles -->
        <x:Double x:Key="ListViewItemMinHeight">32</x:Double>
        <x:Double x:Key="ListViewItemContentMinHeight">28</x:Double>
        <Thickness x:Key="ListViewItemPadding">8,4,8,4</Thickness>
        <Thickness x:Key="GridCompactPadding">2</Thickness>
'@
    
    # Insert before closing Page.Resources tag
    if ($content -match '</Page\.Resources>') {
        $content = $content -replace '(\s*)</Page\.Resources>', "$compactStyles`n`$1</Page.Resources>"
        Write-Host "  - Added compact mode styles to existing Page.Resources" -ForegroundColor Green
    }
    
    $content | Set-Content $xamlFile -Encoding utf8BOM
    
    Write-Host "`nDuplicate resources fixed!" -ForegroundColor Green
    
    exit 0
    
} catch {
    Write-Error "Error: $_"
    exit 1
}
