$ErrorActionPreference='Stop'
[Console]::OutputEncoding=[Text.Encoding]::UTF8
[Console]::InputEncoding=[Text.Encoding]::UTF8
$ProgressPreference='SilentlyContinue'
if ($PSStyle) { $PSStyle.OutputRendering='Ansi' }

cd C:\Repos\ahuelsmann\MOBAflow

Write-Host "🔧 Fixing EditorPage.xaml VisualStateManager..."

$lines = Get-Content "WinUI\View\EditorPage.xaml"

# Find VisualStateManager block (currently in PropertiesPanel Grid)
$vsmStart = -1
$vsmEnd = -1
for ($i = 0; $i -lt $lines.Length; $i++) {
    if ($lines[$i] -match '<VisualStateManager.VisualStateGroups>') {
        $vsmStart = $i
    }
    if ($vsmStart -ge 0 -and $lines[$i] -match '</VisualStateManager.VisualStateGroups>') {
        $vsmEnd = $i
        break
    }
}

if ($vsmStart -lt 0) {
    Write-Host "❌ VisualStateManager block not found!"
    exit 1
}

Write-Host "✅ Found VisualStateManager at lines $vsmStart to $vsmEnd"

# Extract VisualStateManager block
$vsmBlock = $lines[$vsmStart..$vsmEnd]

# Remove from original location
$linesWithoutVsm = $lines[0..($vsmStart-1)] + $lines[($vsmEnd+1)..($lines.Length-1)]

# Find main Grid (after <Page.Resources>)
$mainGridStart = -1
for ($i = 0; $i -lt $linesWithoutVsm.Length; $i++) {
    if ($linesWithoutVsm[$i] -match '^\s*<Grid>\s*$' -and $linesWithoutVsm[$i-1] -notmatch 'Grid>') {
        $mainGridStart = $i
        break
    }
}

if ($mainGridStart -lt 0) {
    Write-Host "❌ Main Grid not found!"
    exit 1
}

Write-Host "✅ Found main Grid at line $mainGridStart"

# Insert VisualStateManager after main Grid opening tag
$finalLines = $linesWithoutVsm[0..$mainGridStart] + $vsmBlock + $linesWithoutVsm[($mainGridStart+1)..($linesWithoutVsm.Length-1)]

# Save
$finalLines | Set-Content "WinUI\View\EditorPage.xaml"

Write-Host "✅ VisualStateManager moved to Page level!"
