[CmdletBinding()]
param(
    [Parameter(Mandatory)] [ValidateSet('common', 'domain')] [string] $LaneName,
    [Parameter(Mandatory)] [string] $OutputPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$outputRoot = [IO.Path]::GetFullPath($OutputPath, $repositoryRoot)
# A fresh directory prevents an old successful report/TRX from masking a missing or interrupted run.
if (Test-Path -LiteralPath $outputRoot) { throw "Mutation output directory already exists: $outputRoot" }
$inputs = & (Join-Path $PSScriptRoot 'Test-MutationLaneCoverage.ps1') -LaneName $LaneName -PassThru
$startedAt = [datetime]::UtcNow
[void] [IO.Directory]::CreateDirectory($outputRoot)
$baselinePaths = @()
Push-Location $repositoryRoot
try {
    for ($index = 0; $index -lt $inputs.TestProjects.Count; $index++) {
        $baselineDirectory = Join-Path $outputRoot "baseline/$index"
        & dotnet test $inputs.TestProjects[$index] --configuration $inputs.Configuration --framework $inputs.Framework `
            --logger 'trx;LogFileName=baseline.trx' --results-directory $baselineDirectory
        if ($LASTEXITCODE -ne 0) { throw "Ordinary tests failed for mutation lane '$LaneName'." }
        $baselinePaths += Join-Path $baselineDirectory 'baseline.trx'
    }
    & (Join-Path $PSScriptRoot 'Test-TestRunResults.ps1') -Path $baselinePaths
    Push-Location (Split-Path -Parent $inputs.ConfigPath)
    try {
        & dotnet stryker --config-file $inputs.ConfigPath --skip-version-check --output $outputRoot
        if ($LASTEXITCODE -ne 0) { throw "Stryker failed for mutation lane '$LaneName' (exit $LASTEXITCODE)." }
    }
    finally { Pop-Location }
    $reports = @(Get-ChildItem -LiteralPath $outputRoot -Recurse -File -Filter 'mutation-report.json')
    if ($reports.Count -ne 1) { throw "Expected one fresh mutation report; found $($reports.Count)." }
    & (Join-Path $PSScriptRoot 'Test-MutationRunResults.ps1') -ReportPath $reports[0].FullName `
        -BaselinePath $baselinePaths -TestFilePath $inputs.TestFiles -ProductionRoot $inputs.ProductionRoot -StartedAt $startedAt
}
finally { Pop-Location }
