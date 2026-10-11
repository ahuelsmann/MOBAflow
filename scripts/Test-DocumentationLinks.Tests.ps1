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

    # External links are requested only by the scheduled workflow; check that every link syntax is found.
    Initialize-Fixture @{
        'README.md' = "[a](https://a.invalid/) <https://b.invalid/path> [c][ref]`n`n[ref]: https://c.invalid/`n<a href=`"https://d.invalid/`">d</a>"
    }
    $external = @(& $scriptPath -Root $fixture -ListExternalLinks 6> $null)
    $expected = @('https://a.invalid/', 'https://b.invalid/path', 'https://c.invalid/', 'https://d.invalid/')
    if ((Compare-Object $expected $external)) {
        throw "Expected external links $($expected -join ', '), but found $($external -join ', ')."
    }
    $checks++

    # Simulate HTTP responses without external services or real retry delays.
    function Invoke-WebRequest {
        param($Uri, $Method, $TimeoutSec, $MaximumRedirection, [switch] $SkipHttpErrorCheck, $UserAgent)
        $global:documentationLinkHttpTest.RequestCount++
        if ($global:documentationLinkHttpTest.Responses.Count -eq 0) {
            throw 'Unexpected additional HTTP request.'
        }

        $response = $global:documentationLinkHttpTest.Responses.Dequeue()
        if ($response -eq 'network-error') {
            throw [Net.Http.HttpRequestException]::new('Simulated network failure.')
        }

        return [pscustomobject] @{ StatusCode = $response }
    }

    function Start-Sleep {
        param([int] $Seconds)
        $global:documentationLinkHttpTest.RetryDelays.Add($Seconds)
    }

    function Assert-ExternalResult([object[]] $Responses, [bool] $ShouldFail, [int] $Attempts, [string] $Scenario) {
        Initialize-Fixture @{ 'README.md' = '[external](https://example.invalid/)' }
        # The invoked checker has its own script scope; mocks share this test-owned state explicitly.
        $global:documentationLinkHttpTest = [pscustomobject] @{
            Responses = [Collections.Generic.Queue[object]]::new()
            RequestCount = 0
            RetryDelays = [Collections.Generic.List[int]]::new()
        }
        foreach ($response in $Responses) { $global:documentationLinkHttpTest.Responses.Enqueue($response) }
        $failed = $false
        $failureMessage = $null
        try { & $scriptPath -Root $fixture -IncludeExternal 6> $null | Out-Null }
        catch { $failed = $true; $failureMessage = $_.Exception.Message }
        if ($failed -ne $ShouldFail -or $global:documentationLinkHttpTest.RequestCount -ne $Attempts) {
            throw "Unexpected result for '$Scenario': failed=$failed, requests=$($global:documentationLinkHttpTest.RequestCount). $failureMessage"
        }

        $expectedDelays = @(1..($Attempts - 1) | Where-Object { $Attempts -gt 1 })
        if (Compare-Object $expectedDelays $global:documentationLinkHttpTest.RetryDelays.ToArray()) {
            throw "Unexpected retry delays for '$Scenario': $($global:documentationLinkHttpTest.RetryDelays -join ', ')."
        }
        $script:checks++
    }

    Assert-ExternalResult @(200) $false 1 'reachable page'
    Assert-ExternalResult @(404) $true 1 'missing page is not retried or ignored'
    Assert-ExternalResult @(403) $true 1 'denied request is not retried or ignored'
    Assert-ExternalResult @(429) $false 1 'existing rate-limit policy'
    Assert-ExternalResult @(503, 200) $false 2 'temporary service failure recovers'
    Assert-ExternalResult @(500, 502, 200) $false 3 'temporary server failures recover'
    Assert-ExternalResult @(503, 503, 503) $true 3 'persistent service failure still fails'
    Assert-ExternalResult @('network-error', 200) $false 2 'temporary network failure recovers'
    Assert-ExternalResult @('network-error', 'network-error', 'network-error') $true 3 'persistent network failure still fails'

    Write-Host "Documentation link self-tests passed ($checks checks)."
}
finally {
    Remove-Variable -Name documentationLinkHttpTest -Scope Global -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $fixture -Recurse -Force -ErrorAction SilentlyContinue
}
