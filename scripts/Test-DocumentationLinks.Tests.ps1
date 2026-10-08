[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$scriptPath = Join-Path $PSScriptRoot 'Test-DocumentationLinks.ps1'
$fixture = Join-Path ([IO.Path]::GetTempPath()) ('mobaflow-doc-links-' + [guid]::NewGuid().ToString('N'))
$checks = 0

function Write-FixtureFile([string] $RelativePath, [string] $Content) {
    $path = Join-Path $fixture $RelativePath
    [void] [IO.Directory]::CreateDirectory((Split-Path -Parent $path))
    [IO.File]::WriteAllText($path, $Content)
}

function Initialize-Fixture([hashtable] $Files) {
    if (Test-Path -LiteralPath $fixture) {
        Remove-Item -LiteralPath $fixture -Recurse -Force
    }

    [void] [IO.Directory]::CreateDirectory($fixture)
    foreach ($entry in $Files.GetEnumerator()) {
        Write-FixtureFile $entry.Key $entry.Value
    }

    # Tests may run inside another Git process; never reuse its index or Git directory.
    foreach ($key in @('GIT_DIR', 'GIT_WORK_TREE', 'GIT_INDEX_FILE', 'GIT_COMMON_DIR')) {
        Remove-Item -LiteralPath "Env:$key" -ErrorAction SilentlyContinue
    }

    git -C $fixture init --quiet
    git -C $fixture -c core.autocrlf=false add --all
    if ($LASTEXITCODE -ne 0) { throw 'Could not prepare the fixture repository.' }
}

function Assert-Passes([hashtable] $Files, [string] $Scenario) {
    Initialize-Fixture $Files
    try { & $scriptPath -Root $fixture 6> $null | Out-Null }
    catch { throw "Expected success for '$Scenario', but failed: $($_.Exception.Message)" }
    $script:checks++
}

function Assert-Breaks([hashtable] $Files, [string] $Scenario) {
    Initialize-Fixture $Files
    $failed = $false
    try { & $scriptPath -Root $fixture 6> $null | Out-Null }
    catch { $failed = $true }
    if (-not $failed) { throw "Expected a broken link for '$Scenario'." }
    $script:checks++
}

$image = [string]::new('x', 4)
try {
    Assert-Passes @{
        'README.md' = "See [guide](docs/guide.md#setup), ![logo](docs/images/logo.png) and [site](https://example.invalid/)."
        'docs/guide.md' = "[Back](../README.md) [Top](#setup) [mail](mailto:a@b.invalid)`n`n## Setup"
        'docs/images/logo.png' = $image
    } 'existing files, fragments, external and mail links'
    Assert-Passes @{
        'docs/index.html' = '<a href="guide.html">Guide</a><img src="images/logo%20big.png"><a href="#top">Top</a>'
        'docs/guide.html' = '<p>Guide</p>'
        'docs/images/logo big.png' = $image
    } 'website pages and encoded image paths'
    Assert-Passes @{
        'README.md' = "Use ``[x](missing.md)`` inline.`n`n``````markdown`n[y](also-missing.md)`n```````n"
    } 'link syntax inside code'
    Assert-Passes @{
        '.specify/templates/plan.md' = '[placeholder](link-to-plan.md)'
        'README.md' = 'Text'
    } 'vendored templates'

    Assert-Breaks @{ 'README.md' = '[guide](docs/missing.md)' } 'missing Markdown target'
    Assert-Breaks @{ 'README.md' = '![logo](images/missing.png)' } 'missing image'
    Assert-Breaks @{ 'docs/index.html' = '<img src="images/missing.png">' } 'missing website image'
    Assert-Breaks @{ 'docs/a.md' = "[ref]: ../missing.md`n`nSee [ref]." } 'missing reference definition'

    Write-Host "Documentation link self-tests passed ($checks checks)."
}
finally {
    Remove-Item -LiteralPath $fixture -Recurse -Force -ErrorAction SilentlyContinue
}
