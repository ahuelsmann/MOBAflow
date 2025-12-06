#!/usr/bin/env pwsh
# PowerShell 7 Script to restore ALL missing bindings in EditorPage.xaml
# Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT.

$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

try {
    Set-Location -Path $PSScriptRoot\..
    
    $xamlFile = 'WinUI\View\EditorPage.xaml'
    
    Write-Host "Restoring ALL missing bindings..." -ForegroundColor Cyan
    
    $content = Get-Content $xamlFile -Raw -Encoding UTF8
    $changes = 0
    
    # === FIX 1: Workflow SelectedItem Binding ===
    if ($content -notmatch 'SelectedItem="\{x:Bind ViewModel\.SelectedWorkflow') {
        Write-Host "  [1/3] Adding SelectedWorkflow binding..." -ForegroundColor Yellow
        
        # Find Workflow ListView and add SelectedItem
        $content = $content -replace '(ItemsSource="\{x:Bind ViewModel\.CurrentProjectViewModel\.Workflows, Mode=OneWay\}")(\s+SelectedItem="\{x:Bind ViewModel\.SelectedWorkflow, Mode=TwoWay\}")?', '$1 SelectedItem="{x:Bind ViewModel.SelectedWorkflow, Mode=TwoWay}"'
        
        # Remove duplicate if we just created one
        $content = $content -replace '(SelectedItem="\{x:Bind ViewModel\.SelectedWorkflow, Mode=TwoWay\}"\s+){2,}', '$1'
        
        Write-Host "        Done!" -ForegroundColor Green
        $changes++
    } else {
        Write-Host "  [1/3] SelectedWorkflow binding OK" -ForegroundColor Gray
    }
    
    # === FIX 2: Action SelectedItem Binding ===
    if ($content -notmatch 'SelectedItem="\{x:Bind ViewModel\.SelectedAction') {
        Write-Host "  [2/3] Adding SelectedAction binding..." -ForegroundColor Yellow
        
        # Find Actions ListView and add SelectedItem
        $content = $content -replace '(<ListView\s+Grid\.Row="1"\s+ItemsSource="\{x:Bind ViewModel\.SelectedWorkflow\.Actions, Mode=OneWay\}")', '$1 SelectedItem="{x:Bind ViewModel.SelectedAction, Mode=TwoWay}"'
        
        Write-Host "        Done!" -ForegroundColor Green
        $changes++
    } else {
        Write-Host "  [2/3] SelectedAction binding OK" -ForegroundColor Gray
    }
    
    # === FIX 3: Action DataTemplate (show Name instead of namespace) ===
    if ($content -match '<DataTemplate>\s*<Grid[^>]*>\s*<Grid\.ColumnDefinitions>[\s\S]*?<TextBlock[^>]*Text="\{Binding\}"') {
        Write-Host "  [3/3] Fixing Action DataTemplate (namespace issue)..." -ForegroundColor Yellow
        
        # Replace generic DataTemplate with typed one
        $oldActionTemplate = @'
                        <ListView.ItemTemplate>
                            <DataTemplate>
                                <Grid Padding="6">
                                    <Grid.ColumnDefinitions>
                                        <ColumnDefinition Width="Auto" />
                                        <ColumnDefinition Width="*" />
                                    </Grid.ColumnDefinitions>
                                    <FontIcon
                                        Grid.Column="0"
                                        Margin="0,0,8,0"
                                        FontSize="16"
                                        Glyph="&#xE945;" />
                                    <TextBlock
                                        Grid.Column="1"
                                        VerticalAlignment="Center"
                                        Text="{Binding}" />
                                </Grid>
                            </DataTemplate>
                        </ListView.ItemTemplate>
'@
        
        $newActionTemplate = @'
                        <ListView.ItemTemplate>
                            <DataTemplate x:DataType="action:BaseActionViewModel">
                                <Grid Padding="6">
                                    <Grid.ColumnDefinitions>
                                        <ColumnDefinition Width="Auto" />
                                        <ColumnDefinition Width="*" />
                                    </Grid.ColumnDefinitions>
                                    <FontIcon
                                        Grid.Column="0"
                                        Margin="0,0,8,0"
                                        FontSize="16"
                                        Glyph="&#xE945;" />
                                    <TextBlock
                                        Grid.Column="1"
                                        VerticalAlignment="Center"
                                        Text="{x:Bind ActionType, Mode=OneWay}" />
                                </Grid>
                            </DataTemplate>
                        </ListView.ItemTemplate>
'@
        
        $content = $content -replace [regex]::Escape($oldActionTemplate), $newActionTemplate
        
        Write-Host "        Done! (Now shows ActionType instead of namespace)" -ForegroundColor Green
        $changes++
    } else {
        Write-Host "  [3/3] Action DataTemplate OK" -ForegroundColor Gray
    }
    
    if ($changes -gt 0) {
        $content | Set-Content $xamlFile -Encoding utf8BOM
        Write-Host "`n=== $changes binding(s) restored ===" -ForegroundColor Green
    } else {
        Write-Host "`n=== All bindings already correct ===" -ForegroundColor Green
    }
    
    exit 0
    
} catch {
    Write-Error "Error: $_"
    exit 1
}
