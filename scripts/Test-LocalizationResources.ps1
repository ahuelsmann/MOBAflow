[CmdletBinding()]
param(
    [string] $RepositoryRoot = (Split-Path -Parent $PSScriptRoot),
    [string[]] $SourceFiles = @()
)

$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/LocalizationResourceTools.ps1"
$root = (Resolve-Path -LiteralPath $RepositoryRoot).Path
$source = Read-UiCatalogue (Join-Path $root 'SharedUI/Localization/UiStrings.resx')
$german = Read-UiCatalogue (Join-Path $root 'SharedUI/Localization/UiStrings.de-DE.resx')
foreach ($key in $source.Keys) {
    if (-not $german.ContainsKey($key)) { throw "Missing German key: $key" }
    if ((Get-UiPlaceholderSignature $source[$key] $key) -cne (Get-UiPlaceholderSignature $german[$key] $key)) {
        throw "Placeholder mismatch: $key"
    }
}
foreach ($key in $german.Keys) { if (-not $source.ContainsKey($key)) { throw "Missing English key: $key" } }
$mapping = @(Get-Content -LiteralPath (Join-Path $root 'SharedUI/Localization/WinUiResourceMap.json') -Raw | ConvertFrom-Json)
foreach ($language in @('en-US', 'de-DE')) {
    $catalogue = if ($language -eq 'en-US') { $source } else { $german }
    $actual = [IO.File]::ReadAllText((Join-Path $root "MOBAflow/Strings/$language/Resources.resw"))
    if ($actual -cne (Get-UiProjection $catalogue $mapping)) { throw "Stale WinUI projection: $language" }
}
if ($SourceFiles.Count -eq 0) {
    $SourceFiles = @('MOBAflow/View/InfoPage.xaml', 'MOBAflow/View/HelpPage.xaml', 'MOBAflow/View/SettingsPage.xaml',
        'MOBAflow/View/InfoPage.xaml.cs', 'MOBAflow/View/HelpPage.xaml.cs', 'MOBAflow/Service/NavigationItemFactory.cs',
        'MOBAsmart/Controls/LanguageSettingsView.xaml', 'MOBAsmart/Controls/AppBottomTabBar.xaml',
        'SharedUI/ViewModel/LanguageSettingsViewModel.cs')
}
foreach ($relative in $SourceFiles) {
    $content = [IO.File]::ReadAllText((Join-Path $root $relative))
    if ($relative -in @('MOBAflow/View/InfoPage.xaml', 'MOBAflow/View/HelpPage.xaml')) {
        Test-UiLiteralCoverage $content $mapping $source $relative
    }
    foreach ($match in [regex]::Matches($content, '(?:\.GetText|\.Format)\("(?<key>[A-Za-z][A-Za-z0-9_]*)"\s*(?=[,)])|\{l:Localize (?<key>[A-Za-z][A-Za-z0-9_]*)\}')) {
        $key = $match.Groups['key'].Value
        if (-not $source.ContainsKey($key)) { throw "Unknown used key '$key' in $relative" }
    }
    foreach ($match in [regex]::Matches($content, 'x:Uid="(?<uid>[A-Za-z][A-Za-z0-9_]*)"')) {
        $prefix = $match.Groups['uid'].Value + '.'
        if (-not @($mapping | Where-Object { $_.Target.StartsWith($prefix, [StringComparison]::Ordinal) }).Count) {
            throw "Unmapped x:Uid '$($match.Groups['uid'].Value)' in $relative"
        }
    }
}
if (Test-Path -LiteralPath (Join-Path $root 'MOBAflow/Service/NavigationRegistration.cs')) {
    $registration = [IO.File]::ReadAllText((Join-Path $root 'MOBAflow/Service/NavigationRegistration.cs'))
    foreach ($match in [regex]::Matches($registration, 'new PageMetadata\(\s*(?:Tag:\s*)?"(?<tag>[^"]+)"')) {
        $key = 'Nav_' + $match.Groups['tag'].Value
        if (-not $source.ContainsKey($key)) { throw "Missing navigation key: $key" }
    }
}
Write-Output "Localization passed: $($source.Count) keys, 2 languages, $($mapping.Count) WinUI targets."
