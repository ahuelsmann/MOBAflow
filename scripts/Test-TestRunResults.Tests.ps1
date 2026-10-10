[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$scriptPath = Join-Path $PSScriptRoot 'Test-TestRunResults.ps1'
$fixture = Join-Path ([IO.Path]::GetTempPath()) ('mobaflow-test-results-' + [guid]::NewGuid().ToString('N'))
[void] [IO.Directory]::CreateDirectory($fixture)
$checks = 0

function Write-Trx([string] $Name, [int] $Total, [int] $Executed, [int] $Passed) {
    $file = Join-Path $fixture "$Name.trx"
    $content = @"
<?xml version="1.0" encoding="utf-8"?>
<TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">
  <ResultSummary outcome="Completed">
    <Counters total="$Total" executed="$Executed" passed="$Passed" failed="$($Executed - $Passed)" error="0" />
  </ResultSummary>
</TestRun>
"@
    [IO.File]::WriteAllText($file, $content)
    return $file
}

function Assert-Succeeds([hashtable] $Parameters, [string] $Scenario) {
    try { & $scriptPath @Parameters | Out-Null }
    catch { throw "Expected success for '$Scenario', but failed: $($_.Exception.Message)" }
    $script:checks++
}

function Assert-Fails([hashtable] $Parameters, [string] $Scenario, [string] $ExpectedMessage) {
    $message = $null
    try { & $scriptPath @Parameters | Out-Null }
    catch { $message = $_.Exception.Message }
    if ($null -eq $message) { throw "Expected failure for '$Scenario'." }
    if ($message -notlike "*$ExpectedMessage*") { throw "Unexpected failure for '$Scenario': $message" }
    $script:checks++
}

try {
    $allPassed = Write-Trx 'all-passed' 6 6 6
    $skipped = Write-Trx 'skipped' 6 3 3
    $failed = Write-Trx 'failed' 6 6 5
    $empty = Write-Trx 'empty' 0 0 0

    Assert-Succeeds @{ Path = @($allPassed); MinimumPassed = 6 } 'every selected test ran and passed'
    Assert-Fails @{ Path = @($skipped) } 'tests that skip themselves' 'did not run'
    Assert-Fails @{ Path = @($failed) } 'a failed test' 'did not pass'
    Assert-Fails @{ Path = @($empty) } 'a filter that matches nothing' 'Only 0 tests passed'
    Assert-Fails @{ Path = @($allPassed); MinimumPassed = 7 } 'fewer tests than expected' 'expected at least 7'
    Assert-Fails @{ Path = @((Join-Path $fixture 'missing.trx')) } 'a run without results' 'does not exist'
    Assert-Succeeds @{ Path = @($allPassed, $allPassed); MinimumPassed = 12 } 'results summed across files'

    Write-Host "Test result guard self-tests passed ($checks checks)."
}
finally {
    Remove-Item -LiteralPath $fixture -Recurse -Force -ErrorAction SilentlyContinue
}
