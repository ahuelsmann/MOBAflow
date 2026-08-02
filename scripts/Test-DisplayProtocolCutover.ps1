[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$violations = [System.Collections.Generic.List[string]]::new()

$forbiddenPaths = @(
    'MOBAdisplay/FrameSender.cs',
    'MOBAdisplay/Transport/UdpLineFrameSender.cs',
    'MOBAflow/Controls/Display/WaveshareLcd147InchConfigurationView.xaml',
    'MOBAflow/Controls/Display/WaveshareLcd169InchConfigurationView.xaml',
    'MOBAflow/Controls/Display/WaveshareLcdTouch700InchConfigurationView.xaml'
)

foreach ($relativePath in $forbiddenPaths) {
    $absolutePath = Join-Path $repositoryRoot $relativePath
    if (Test-Path -LiteralPath $absolutePath) {
        $violations.Add("Forbidden legacy path exists: $relativePath")
    }
}

$productionRoots = @(
    'MOBAdisplay/Transport',
    'MOBAdisplay/esp32/src',
    'MOBAdisplay/esp32/lib/MobaDisplayCore',
    'MOBAdisplay/esp32/lib/MobaDisplayProtocol',
    'MOBAdisplay/esp32/lib/MobaUdpPacketParser'
)
$legacyTokens = @(
    'HOST_VER',
    'FRAME_START',
    'FRAME_DONE',
    'UdpLineFrameSender',
    'SendDisplayMetadata'
)

foreach ($relativeRoot in $productionRoots) {
    $absoluteRoot = Join-Path $repositoryRoot $relativeRoot
    $files = Get-ChildItem -LiteralPath $absoluteRoot -Recurse -File |
        Where-Object { $_.Extension -in '.cs', '.cpp', '.h' }
    foreach ($file in $files) {
        foreach ($match in Select-String -LiteralPath $file.FullName -SimpleMatch -Pattern $legacyTokens) {
            $relativeFile = [System.IO.Path]::GetRelativePath($repositoryRoot, $file.FullName)
            $violations.Add("Legacy display token in ${relativeFile}:$($match.LineNumber)")
        }
    }
}

$transportFiles = Get-ChildItem -LiteralPath (Join-Path $repositoryRoot 'MOBAdisplay/Transport') -Filter '*.cs' -File
foreach ($file in $transportFiles) {
    foreach ($match in Select-String -LiteralPath $file.FullName -Pattern '\bSendFrame\s*\(') {
        $relativeFile = [System.IO.Path]::GetRelativePath($repositoryRoot, $match.Path)
        $violations.Add("Synchronous display sender method in ${relativeFile}:$($match.LineNumber)")
    }
}

if ($violations.Count -gt 0) {
    throw "Display protocol cutover invariants failed:`n$($violations -join "`n")"
}

Write-Host 'Display protocol cutover invariants passed.'
