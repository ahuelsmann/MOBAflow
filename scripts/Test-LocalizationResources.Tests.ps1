$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
. "$PSScriptRoot/LocalizationResourceTools.ps1"
$fixture = Join-Path ([IO.Path]::GetTempPath()) ('mobaflow-localization-' + [Guid]::NewGuid().ToString('N'))
$null = New-Item -ItemType Directory -Path $fixture
$cataloguePath = Join-Path $fixture 'SharedUI/Localization'
$null = New-Item -ItemType Directory -Path $cataloguePath -Force
$passed = 0

function Write-FixtureCatalogue([string] $Name, [string] $Greeting, [string] $Template) {
    $escapedGreeting = [Security.SecurityElement]::Escape($Greeting)
    $escapedTemplate = [Security.SecurityElement]::Escape($Template)
    $xml = "<root><data name='Greeting'><value>$escapedGreeting</value><comment>Greeting label</comment></data><data name='Template'><value>$escapedTemplate</value><comment>0 number, 1 date</comment></data></root>"
    [IO.File]::WriteAllText((Join-Path $cataloguePath $Name), $xml)
}

function Reset-Fixture {
    Write-FixtureCatalogue 'UiStrings.resx' 'Hello' 'Value {0:N2} on {1:d} {{units}}'
    Write-FixtureCatalogue 'UiStrings.de-DE.resx' 'Hallo' '{1:d}: {0:N2} {{Einheiten}}'
    [IO.File]::WriteAllText((Join-Path $cataloguePath 'WinUiResourceMap.json'), '[{"Key":"Greeting","Target":"Greeting.Text"}]')
    [IO.File]::WriteAllText((Join-Path $fixture 'fixture.xaml'), '<TextBlock x:Uid="Greeting" />')
    & "$PSScriptRoot/Update-LocalizationResources.ps1" -RepositoryRoot $fixture | Out-Null
}

function Assert-Rejected([string] $Expected) {
    try { & "$PSScriptRoot/Test-LocalizationResources.ps1" -RepositoryRoot $fixture -SourceFiles @('fixture.xaml') | Out-Null }
    catch {
        if ($_.Exception.Message -notlike "*$Expected*") { throw "Expected '$Expected', got '$($_.Exception.Message)'" }
        return
    }
    throw "Invalid fixture was accepted: $Expected"
}

try {
    Reset-Fixture
    & "$PSScriptRoot/Test-LocalizationResources.ps1" -RepositoryRoot $fixture -SourceFiles @('fixture.xaml') | Out-Null
    $passed++

    Reset-Fixture
    $path = Join-Path $cataloguePath 'UiStrings.de-DE.resx'
    [IO.File]::WriteAllText($path, '<root><data name="Greeting"><value>Hallo</value><comment>Greeting</comment></data></root>')
    Assert-Rejected 'Missing German key: Template'; $passed++

    Reset-Fixture
    $xml = [IO.File]::ReadAllText($path).Replace('</root>', '<data name="greeting"><value>Hallo</value><comment>Duplicate</comment></data></root>')
    [IO.File]::WriteAllText($path, $xml)
    Assert-Rejected 'Duplicate resource key'; $passed++

    Reset-Fixture
    Write-FixtureCatalogue 'UiStrings.de-DE.resx' '' '{0} {1}'
    Assert-Rejected 'Empty resource: Greeting'; $passed++

    Reset-Fixture
    Write-FixtureCatalogue 'UiStrings.de-DE.resx' 'Hallo' '{1} {2}'
    Assert-Rejected 'Placeholder mismatch: Template'; $passed++

    Reset-Fixture
    Write-FixtureCatalogue 'UiStrings.resx' 'Hello' '{0} {2}'
    Write-FixtureCatalogue 'UiStrings.de-DE.resx' 'Hallo' '{1} {2}'
    Assert-Rejected 'Placeholder mismatch: Template'; $passed++

    Reset-Fixture
    Write-FixtureCatalogue 'UiStrings.de-DE.resx' 'Hallo' 'Broken {'
    Assert-Rejected 'Malformed template: Template'; $passed++

    Reset-Fixture
    [IO.File]::AppendAllText((Join-Path $fixture 'MOBAflow/Strings/de-DE/Resources.resw'), 'stale')
    Assert-Rejected 'Stale WinUI projection: de-DE'; $passed++

    Reset-Fixture
    [IO.File]::WriteAllText((Join-Path $fixture 'fixture.xaml'), '{l:Localize Missing}')
    Assert-Rejected 'Unknown used key'; $passed++

    Reset-Fixture
    [IO.File]::WriteAllText((Join-Path $fixture 'fixture.xaml'), '<TextBlock x:Uid="Unmapped" />')
    Assert-Rejected 'Unmapped x:Uid'; $passed++

    Reset-Fixture
    [IO.File]::WriteAllText((Join-Path $cataloguePath 'WinUiResourceMap.json'), '[{"Key":"Missing","Target":"Greeting.Text"}]')
    Assert-Rejected 'Unknown mapped key'; $passed++

    Write-Output "Localization resource tooling: $passed isolated cases passed."
}
finally {
    $resolved = [IO.Path]::GetFullPath($fixture)
    $tempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    if (-not $resolved.StartsWith($tempRoot, [StringComparison]::OrdinalIgnoreCase) -or
        [IO.Path]::GetFileName($resolved) -notmatch '^mobaflow-localization-[a-f0-9]{32}$') {
        throw 'Refusing cleanup outside the owned localization fixture directory.'
    }
    Remove-Item -LiteralPath $resolved -Recurse -Force
}
