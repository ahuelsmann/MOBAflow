$ErrorActionPreference='Stop'
Set-Location "C:\Repos\ahuelsmann\MOBAflow"

Write-Host "Fixing EditorPage.xaml..." -ForegroundColor Cyan

# Read current content
$lines = Get-Content "WinUI\View\EditorPage.xaml"
$newLines = @()
$inSolutionContent = $false
$skipUntilNextColumn = $false
$columnCount = 0

for ($i = 0; $i -lt $lines.Count; $i++) {
    $line = $lines[$i]
    
    # Track when we enter Solution Content
    if ($line -match '<Grid x:Name="SolutionContent"') {
        $inSolutionContent = $true
        $columnCount = 0
    }
    
    # Track when we exit Solution Content
    if ($inSolutionContent -and $line -match '<Grid x:Name="\w+Content"' -and $line -notmatch 'SolutionContent') {
        $inSolutionContent = $false
    }
    
    # In Solution Content, remove City Library column
    if ($inSolutionContent) {
        # Change column definitions: remove column 2 (City Library) and its separator
        if ($line -match '<Grid\.ColumnDefinitions>') {
            $newLines += $line
            # Next 3 lines are column definitions - keep only Projects and PropertyGrid
            $newLines += $lines[$i+1]  # Projects column
            $newLines += '                    <ColumnDefinition Width="Auto" />'  # Separator
            $newLines += '                    <ColumnDefinition Width="*" MinWidth="300" />'  # PropertyGrid
            $i += 4  # Skip original 5 column definitions
            continue
        }
        
        # Skip City Library Grid (Column 2)
        if ($line -match '<!--\s*Column 2: City Library') {
            $skipUntilNextColumn = $true
        }
        
        # Skip until next column or border
        if ($skipUntilNextColumn) {
            if ($line -match '<!--\s*Column \d+:' -or $line -match '<Grid x:Name="\w+Content"') {
                $skipUntilNextColumn = $false
            } else {
                continue
            }
        }
        
        # Fix PropertyGrid column number (was 4, now 2)
        if ($line -match 'Grid\.Column="4"' -and $line -match 'PropertyGrid') {
            $line = $line -replace 'Grid\.Column="4"', 'Grid.Column="2"'
        }
        
        # Skip separator between Projects and City Library (Column 1)
        if ($line -match '<Border Grid\.Column="1".*DividerStrokeColorDefaultBrush' -and $columnCount -eq 0) {
            $columnCount++
            # Replace with separator between Projects and PropertyGrid
            $newLines += '                <Border Grid.Column="1" Width="1" Background="{ThemeResource DividerStrokeColorDefaultBrush}" />'
            continue
        }
        
        # Skip separator between City Library and PropertyGrid (Column 3)
        if ($line -match '<Border Grid\.Column="3"') {
            continue
        }
    }
    
    $newLines += $line
}

$newLines | Set-Content "WinUI\View\EditorPage.xaml" -Encoding utf8
Write-Host "Solution Tab: Removed City Library, kept Projects + PropertyGrid" -ForegroundColor Green
