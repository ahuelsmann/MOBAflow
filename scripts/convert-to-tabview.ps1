#!/usr/bin/env pwsh
# PowerShell 7 Script to convert EditorPage from SelectorBar to TabView
# Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT.

$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

try {
    Set-Location -Path $PSScriptRoot\..
    
    $xamlFile = 'WinUI\View\EditorPage.xaml'
    $codeFile = 'WinUI\View\EditorPage.xaml.cs'
    
    Write-Host "Converting EditorPage from SelectorBar to TabView..." -ForegroundColor Cyan
    Write-Host ""
    
    # ============================================
    # Step 1: Read current XAML
    # ============================================
    Write-Host "[1/6] Reading XAML file..." -ForegroundColor Yellow
    $xaml = Get-Content $xamlFile -Raw -Encoding UTF8
    
    if ($xaml -notmatch 'SelectorBar') {
        Write-Host "  Already using TabView (no SelectorBar found)" -ForegroundColor Green
        exit 0
    }
    
    # ============================================
    # Step 2: Replace SelectorBar with TabView opening
    # ============================================
    Write-Host "[2/6] Converting SelectorBar to TabView..." -ForegroundColor Yellow
    
    # Replace SelectorBar with TabView
    $selectorBarPattern = '(<!--\s*SelectorBar Navigation\s*-->)\s*<SelectorBar[^>]*SelectionChanged="[^"]*">'
    $tabViewReplacement = '$1' + "`r`n" + @'
        <TabView x:Name="EditorTabView" Grid.Row="1" Margin="16,8,16,8" 
                 TabWidthMode="SizeToContent" 
                 IsAddTabButtonVisible="False"
                 CanDragTabs="False"
                 CanReorderTabs="False">
'@
    $xaml = $xaml -replace $selectorBarPattern, $tabViewReplacement
    
    # ============================================
    # Step 3: Convert SelectorBarItems to TabViewItems with content
    # ============================================
    Write-Host "[3/6] Converting tabs and embedding content..." -ForegroundColor Yellow
    
    # Solution Tab
    $xaml = $xaml -replace '(<SelectorBarItem x:Name="SolutionSelector" Text="Solution">.*?</SelectorBarItem>)', @'
<TabViewItem x:Name="SolutionTab" Header="Solution">
                <TabViewItem.IconSource>
                    <FontIconSource Glyph="&#xE8F1;" />
                </TabViewItem.IconSource>
'@
    
    # Journeys Tab
    $xaml = $xaml -replace '(<SelectorBarItem x:Name="JourneysSelector" Text="Journeys">.*?</SelectorBarItem>)', @'
</TabViewItem>

            <TabViewItem x:Name="JourneysTab" Header="Journeys">
                <TabViewItem.IconSource>
                    <FontIconSource Glyph="&#xE81D;" />
                </TabViewItem.IconSource>
'@
    
    # Workflows Tab
    $xaml = $xaml -replace '(<SelectorBarItem x:Name="WorkflowsSelector" Text="Workflows">.*?</SelectorBarItem>)', @'
</TabViewItem>

            <TabViewItem x:Name="WorkflowsTab" Header="Workflows">
                <TabViewItem.IconSource>
                    <FontIconSource Glyph="&#xE713;" />
                </TabViewItem.IconSource>
'@
    
    # Trains Tab
    $xaml = $xaml -replace '(<SelectorBarItem x:Name="TrainsSelector" Text="Trains">.*?</SelectorBarItem>)', @'
</TabViewItem>

            <TabViewItem x:Name="TrainsTab" Header="Trains">
                <TabViewItem.IconSource>
                    <FontIconSource Glyph="&#xE7C0;" />
                </TabViewItem.IconSource>
'@
    
    # Close SelectorBar -> No closing yet (will close after embedding content)
    $xaml = $xaml -replace '</SelectorBar>', ''
    
    # ============================================
    # Step 4: Remove Content Area wrapper Grid
    # ============================================
    Write-Host "[4/6] Removing content area wrapper..." -ForegroundColor Yellow
    
    $xaml = $xaml -replace '<!--\s*Content Area with Multi-Column Grids\s*-->\s*<Grid Grid\.Row="2">\s*', ''
    
    # ============================================
    # Step 5: Embed content grids into TabViewItems
    # ============================================
    Write-Host "[5/6] Embedding content into tabs..." -ForegroundColor Yellow
    
    # Remove Visibility and x:Name from content grids (they become TabViewItem content)
    $xaml = $xaml -replace '<Grid x:Name="SolutionContent" Visibility="[^"]*">', '<Grid>'
    $xaml = $xaml -replace '<Grid x:Name="JourneysContent" Visibility="[^"]*">', '<Grid>'
    $xaml = $xaml -replace '<Grid x:Name="WorkflowsContent" Visibility="[^"]*">', '<Grid>'
    $xaml = $xaml -replace '<Grid x:Name="TrainsContent" Visibility="[^"]*">', '<Grid>'
    
    # Close each TabViewItem before the next tab's comment section
    $xaml = $xaml -replace '(</Grid>)\s*(<!--\s*============================================\s*-->\s*<!--\s*============================================\s*-->\s*<!--\s*Journeys Tab)', @'
$1
            </TabViewItem>

            $2
'@
    
    $xaml = $xaml -replace '(</Grid>)\s*(<!--\s*============================================\s*-->\s*<!--\s*============================================\s*-->\s*<!--\s*Workflows Tab)', @'
$1
            </TabViewItem>

            $2
'@
    
    $xaml = $xaml -replace '(</Grid>)\s*(<!--\s*============================================\s*-->\s*<!--\s*============================================\s*-->\s*<!--\s*Trains Tab)', @'
$1
            </TabViewItem>

            $2
'@
    
    # Close last TabViewItem and TabView before </Page>
    $xaml = $xaml -replace '(</Grid>)\s*(</Grid>)\s*(</Page>)', @'
$1
            </TabViewItem>
        </TabView>
    $3
'@
    
    
    # Write XAML
    Set-Content -Path $xamlFile -Value $xaml -Encoding utf8BOM
    
    # ============================================
    # Step 6: Update Code-Behind
    # ============================================
    Write-Host "[6/6] Updating code-behind..." -ForegroundColor Yellow
    
    $code = Get-Content $codeFile -Raw -Encoding UTF8
    
    # Update XML comments
    $code = $code -replace '/// Editor page with SelectorBar and inline content switching\.', '/// Editor page with TabView navigation.'
    $code = $code -replace '/// All editor tabs are defined in XAML and switched via Visibility\.', '/// All editor tabs are defined as TabViewItems in XAML.'
    
    # Update constructor comment
    $code = $code -replace '// Set initial selection to Solution\s*EditorSelectorBar\.SelectedItem = SolutionSelector;', @'
// Set initial selection to Solution tab
        EditorTabView.SelectedIndex = 0;
'@
    
    # Remove the SelectionChanged event handler method
    $code = $code -replace '/// <summary>\s*/// Handles SelectorBar selection changes by showing/hiding content grids\.\s*/// </summary>\s*private void EditorSelectorBar_SelectionChanged\(SelectorBar sender, SelectorBarSelectionChangedEventArgs args\)\s*\{[^}]*\}', ''
    
    # Clean up multiple empty lines
    $code = $code -replace '\r?\n\r?\n\r?\n+', "`r`n`r`n"
    
    Set-Content -Path $codeFile -Value $code -Encoding utf8BOM
    
    # ============================================
    # Summary
    # ============================================
    Write-Host ""
    Write-Host "Conversion completed successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Changes:" -ForegroundColor Cyan
    Write-Host "  - SelectorBar replaced with TabView" -ForegroundColor White
    Write-Host "  - Content grids moved into TabViewItems" -ForegroundColor White
    Write-Host "  - Visibility switching logic removed" -ForegroundColor White
    Write-Host "  - Code-behind SelectionChanged handler removed" -ForegroundColor White
    Write-Host ""
    Write-Host "Backups created:" -ForegroundColor Cyan
    Write-Host "  - temp_editorpage_backup.xaml" -ForegroundColor White
    Write-Host "  - temp_editorpage_cs_backup.cs" -ForegroundColor White
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Cyan
    Write-Host "  1. Build solution to verify changes" -ForegroundColor White
    Write-Host "  2. Test tab navigation in WinUI app" -ForegroundColor White
    Write-Host "  3. Delete backup files if all works correctly" -ForegroundColor White
    
    exit 0
    
} catch {
    Write-Error "Error: $_"
    Write-Host ""
    Write-Host "Restoring from backup..." -ForegroundColor Red
    if (Test-Path "temp_editorpage_backup.xaml") {
        Copy-Item "temp_editorpage_backup.xaml" -Destination $xamlFile -Force
    }
    if (Test-Path "temp_editorpage_cs_backup.cs") {
        Copy-Item "temp_editorpage_cs_backup.cs" -Destination $codeFile -Force
    }
    exit 1
}
