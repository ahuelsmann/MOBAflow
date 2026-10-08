[CmdletBinding()]
param(
    # TRX result files written by `dotnet test --logger trx`.
    [Parameter(Mandatory)]
    [string[]] $Path,

    # The least number of tests that must have run and passed across all files.
    [ValidateRange(1, [int]::MaxValue)]
    [int] $MinimumPassed = 1
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# A filtered `dotnet test` run succeeds when the filter matches nothing or every test skips itself.
# Mandatory CI lanes use this check so missing prerequisites fail instead of passing silently.
$total = 0
$passed = 0
$notRun = 0
foreach ($file in $Path) {
    if (-not (Test-Path -LiteralPath $file -PathType Leaf)) {
        throw "Test result file '$file' does not exist; the test run did not produce results."
    }

    [xml] $trx = Get-Content -LiteralPath $file -Raw
    $counters = $trx.TestRun.ResultSummary.Counters
    if ($null -eq $counters) {
        throw "Test result file '$file' has no result counters."
    }

    $total += [int] $counters.total
    $passed += [int] $counters.passed
    $notRun += [int] $counters.total - [int] $counters.executed
}

if ($notRun -gt 0) {
    throw "$notRun of $total tests did not run (skipped or ignored); a mandatory lane must run every selected test."
}

if ($passed -ne $total) {
    throw "$($total - $passed) of $total tests did not pass."
}

if ($passed -lt $MinimumPassed) {
    throw "Only $passed tests passed; expected at least $MinimumPassed. Check the test filter and prerequisites."
}

Write-Host "Test results passed: $passed of $total tests ran and passed."
