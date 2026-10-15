[CmdletBinding()]
param([string] $RepositoryRoot = (Split-Path -Parent $PSScriptRoot))

$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/LocalizationResourceTools.ps1"
$root = (Resolve-Path -LiteralPath $RepositoryRoot).Path
$mapping = @(Get-Content -LiteralPath (Join-Path $root 'SharedUI/Localization/WinUiResourceMap.json') -Raw | ConvertFrom-Json)
foreach ($language in @('en-US', 'de-DE')) {
    $source = if ($language -eq 'en-US') { 'UiStrings.resx' } else { 'UiStrings.de-DE.resx' }
    $catalogue = Read-UiCatalogue (Join-Path $root "SharedUI/Localization/$source")
    $directory = Join-Path $root "MOBAflow/Strings/$language"
    $null = New-Item -ItemType Directory -Path $directory -Force
    [IO.File]::WriteAllText((Join-Path $directory 'Resources.resw'), (Get-UiProjection $catalogue $mapping), [Text.UTF8Encoding]::new($false))
}
Write-Output 'Generated WinUI resources for en-US and de-DE.'
