#!/usr/bin/env pwsh
# PowerShell 7 Script to enable Compact Mode in WinUI 3 EditorPage
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
    
    Write-Host "Enabling Compact Mode in EditorPage.xaml..." -ForegroundColor Cyan
    
    $content = Get-Content $xamlFile -Raw -Encoding UTF8
    
    # Check if Page.Resources already exists
    if ($content -match '<Page\.Resources>') {
        Write-Host "  Page.Resources already exists, adding CompactMode ResourceDictionary..." -ForegroundColor Yellow
        
        # Add ResourceDictionary with CompactMode after opening Page.Resources tag
        $resourceDictionary = @'

        <ResourceDictionary>
            <ResourceDictionary.ThemeDictionaries>
                <ResourceDictionary x:Key="Default">
                    <x:Double x:Key="ListViewItemMinHeight">32</x:Double>
                    <x:Double x:Key="ListViewItemContentMinHeight">28</x:Double>
                    <Thickness x:Key="ListViewItemPadding">8,4,8,4</Thickness>
                </ResourceDictionary>
                <ResourceDictionary x:Key="Light">
                    <x:Double x:Key="ListViewItemMinHeight">32</x:Double>
                    <x:Double x:Key="ListViewItemContentMinHeight">28</x:Double>
                    <Thickness x:Key="ListViewItemPadding">8,4,8,4</Thickness>
                </ResourceDictionary>
                <ResourceDictionary x:Key="Dark">
                    <x:Double x:Key="ListViewItemMinHeight">32</x:Double>
                    <x:Double x:Key="ListViewItemContentMinHeight">28</x:Double>
                    <Thickness x:Key="ListViewItemPadding">8,4,8,4</Thickness>
                </ResourceDictionary>
            </ResourceDictionary.ThemeDictionaries>
        </ResourceDictionary>
'@
        
        $content = $content -replace '(<Page\.Resources>)', "`$1$resourceDictionary"
    } else {
        Write-Host "  Page.Resources does not exist, creating with CompactMode..." -ForegroundColor Yellow
        
        # Create new Page.Resources section before first Grid
        $newResources = @'

    <Page.Resources>
        <ResourceDictionary>
            <ResourceDictionary.ThemeDictionaries>
                <ResourceDictionary x:Key="Default">
                    <x:Double x:Key="ListViewItemMinHeight">32</x:Double>
                    <x:Double x:Key="ListViewItemContentMinHeight">28</x:Double>
                    <Thickness x:Key="ListViewItemPadding">8,4,8,4</Thickness>
                </ResourceDictionary>
                <ResourceDictionary x:Key="Light">
                    <x:Double x:Key="ListViewItemMinHeight">32</x:Double>
                    <x:Double x:Key="ListViewItemContentMinHeight">28</x:Double>
                    <Thickness x:Key="ListViewItemPadding">8,4,8,4</Thickness>
                </ResourceDictionary>
                <ResourceDictionary x:Key="Dark">
                    <x:Double x:Key="ListViewItemMinHeight">32</x:Double>
                    <x:Double x:Key="ListViewItemContentMinHeight">28</x:Double>
                    <Thickness x:Key="ListViewItemPadding">8,4,8,4</Thickness>
                </ResourceDictionary>
            </ResourceDictionary.ThemeDictionaries>
        </ResourceDictionary>
'@
        
        $content = $content -replace '(\s+)<Page\.Resources>', "$1<Page.Resources>$newResources"
    }
    
    # Add compact spacing to Grids (reduce Padding)
    $content = $content -replace 'Grid Grid\.Column="\d+" Padding="4">', 'Grid Grid.Column="$1" Padding="2">'
    $content = $content -replace 'Padding="16,6,24,4"', 'Padding="12,4,12,4"'
    $content = $content -replace 'Padding="6">', 'Padding="4">'
    
    # Reduce margins
    $content = $content -replace 'Margin="0,0,8,0"', 'Margin="0,0,6,0"'
    
    Write-Host "  - ListView items: Reduced height and padding" -ForegroundColor Green
    Write-Host "  - Grid padding: Reduced from 4 to 2" -ForegroundColor Green
    Write-Host "  - Item margins: Reduced spacing" -ForegroundColor Green
    
    $content | Set-Content $xamlFile -Encoding utf8BOM
    
    Write-Host "`nCompact Mode enabled successfully!" -ForegroundColor Green
    Write-Host "Note: This applies to all ListViews in EditorPage" -ForegroundColor Gray
    
    exit 0
    
} catch {
    Write-Error "Error: $_"
    exit 1
}
