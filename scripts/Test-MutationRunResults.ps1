[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $ReportPath,
    [Parameter(Mandatory)] [string[]] $BaselinePath,
    [Parameter(Mandatory)] [string[]] $TestFilePath,
    [Parameter(Mandatory)] [string] $ProductionRoot,
    [Parameter(Mandatory)] [datetime] $StartedAt
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $ReportPath -PathType Leaf)) { throw "Mutation report does not exist: $ReportPath" }
if ((Get-Item -LiteralPath $ReportPath).LastWriteTimeUtc -lt $StartedAt.ToUniversalTime()) {
    throw 'Mutation report is older than this run.'
}
& (Join-Path $PSScriptRoot 'Test-TestRunResults.ps1') -Path $BaselinePath
$baselineCount = 0
$baselineNames = [Collections.Generic.List[string]]::new()
function Convert-TestName([string] $Name) {
    # NUnit/TRX escapes surrogate pairs in parameter display names; Stryker emits their Unicode characters.
    return [regex]::Replace($Name, '\\u([0-9a-fA-F]{4})', [Text.RegularExpressions.MatchEvaluator] {
        param($match)
        return [string] [char] [Convert]::ToInt32($match.Groups[1].Value, 16)
    })
}
foreach ($path in $BaselinePath) {
    if ((Get-Item -LiteralPath $path).LastWriteTimeUtc -lt $StartedAt.ToUniversalTime()) {
        throw 'Ordinary test baseline is older than this run.'
    }
    [xml] $trx = Get-Content -Raw -LiteralPath $path
    $baselineCount += [int] $trx.TestRun.ResultSummary.Counters.passed
    foreach ($test in $trx.TestRun.TestDefinitions.UnitTest) {
        $baselineNames.Add((Convert-TestName ($test.TestMethod.className + '.' + $test.TestMethod.name)))
    }
}
$report = Get-Content -Raw -LiteralPath $ReportPath | ConvertFrom-Json
if ([IO.Path]::GetFullPath($report.projectRoot) -ne [IO.Path]::GetFullPath($ProductionRoot)) {
    throw 'Mutation report belongs to a different production project.'
}
$reportedFiles = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
$testIds = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
$reportNames = [Collections.Generic.List[string]]::new()
foreach ($file in $report.testFiles.PSObject.Properties) {
    $tests = @($file.Value.tests)
    if ($tests.Count -eq 0) { continue }
    [void] $reportedFiles.Add([IO.Path]::GetFullPath($file.Name))
    foreach ($test in $tests) {
        $reportNames.Add((Convert-TestName $test.name))
        if ([string]::IsNullOrWhiteSpace($test.id) -or -not $testIds.Add($test.id)) {
            throw 'Mutation report has empty or duplicate test IDs.'
        }
    }
}
$missing = @($TestFilePath | Where-Object { -not $reportedFiles.Contains([IO.Path]::GetFullPath($_)) })
if ($missing.Count -gt 0) { throw "Registered test files missing from mutation report: $($missing -join ', ')" }
if ($testIds.Count -eq 0 -or $testIds.Count -ne $baselineCount) {
    throw "Mutation report contains $($testIds.Count) tests; ordinary baseline passed $baselineCount."
}
if ($baselineNames.Count -ne $baselineCount -or
    @(Compare-Object @($baselineNames | Sort-Object -CaseSensitive) @($reportNames | Sort-Object -CaseSensitive) -CaseSensitive).Count -gt 0) {
    throw 'Mutation report test cases do not match the ordinary baseline.'
}
$mutants = @($report.files.PSObject.Properties | ForEach-Object { $_.Value.mutants })
$allowedStatuses = @('Killed', 'Survived', 'Timeout', 'NoCoverage', 'CompileError', 'Ignored')
$unfinished = @($mutants | Where-Object { $_.status -notin $allowedStatuses })
if ($unfinished.Count -gt 0) { throw 'Mutation report has unfinished or unknown mutant statuses.' }
$executed = @($mutants | Where-Object { $_.status -in @('Killed', 'Survived', 'Timeout') })
if ($executed.Count -eq 0) { throw 'No mutants were executed against tests.' }
foreach ($mutant in $mutants) {
    foreach ($property in @('coveredBy', 'killedBy')) {
        $references = $mutant.PSObject.Properties[$property]
        if ($null -eq $references) { continue }
        foreach ($id in @($references.Value)) {
            if (-not $testIds.Contains($id)) { throw 'Mutant references a test absent from the report.' }
        }
    }
    if ($mutant.status -eq 'Killed' -and
        ($null -eq $mutant.PSObject.Properties['killedBy'] -or @($mutant.killedBy).Count -eq 0)) {
        throw 'Killed mutant has no test execution evidence.'
    }
}
Write-Host "Verified fresh mutation report: $($testIds.Count) tests, $($executed.Count) executed mutants."
$mutants | Group-Object status | Select-Object Name, Count | Format-Table -AutoSize | Out-Host
