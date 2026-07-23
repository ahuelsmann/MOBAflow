Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$scriptPath = Join-Path $PSScriptRoot "Test-AnalyzerBaseline.ps1"
$testRoot = Join-Path $repositoryRoot ".agent-build/analyzer-baseline-tests/$([Guid]::NewGuid())"
$sarifDirectory = Join-Path $testRoot "Sample/obj/analyzers/Release/net10.0"
$sarifPath = Join-Path $sarifDirectory "Sample.sarif"
$baselinePath = Join-Path $testRoot "baseline.json"

function Write-SampleSarif([object[]] $Results) {
    $document = [ordered]@{
        version = "2.1.0"
        runs = @(
            [ordered]@{
                tool = [ordered]@{
                    driver = [ordered]@{
                        name = "Sample"
                    }
                }
                results = $Results
            }
        )
    }
    $document | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $sarifPath -Encoding utf8NoBOM
}

function New-SampleResult(
    [string] $RuleId,
    [string] $Path,
    [string] $Message) {
    return [ordered]@{
        ruleId = $RuleId
        message = [ordered]@{
            text = $Message
        }
        locations = @(
            [ordered]@{
                physicalLocation = [ordered]@{
                    artifactLocation = [ordered]@{
                        uri = $Path
                    }
                }
            }
        )
    }
}

function Assert-Succeeds([scriptblock] $Action, [string] $Scenario) {
    try {
        & $Action
    }
    catch {
        throw "Expected success for '$Scenario', but failed: $($_.Exception.Message)"
    }
}

function Assert-Fails([scriptblock] $Action, [string] $Scenario) {
    $failed = $false
    try {
        & $Action
    }
    catch {
        $failed = $true
    }

    if (-not $failed) {
        throw "Expected failure for '$Scenario'."
    }
}

try {
    New-Item -ItemType Directory -Path $sarifDirectory -Force | Out-Null
    $firstResult = New-SampleResult `
        -RuleId "CA1001" `
        -Path (Join-Path $repositoryRoot "Common/Sample.cs") `
        -Message "Dispose  the resource in $repositoryRoot."
    $generatedResult = New-SampleResult `
        -RuleId "CA1812" `
        -Path (Join-Path $repositoryRoot "Common/obj/Generated.g.cs") `
        -Message "Generated diagnostic."
    Write-SampleSarif @($firstResult, $generatedResult)

    Assert-Succeeds {
        & $scriptPath -SarifRoot $testRoot -BaselinePath $baselinePath -UpdateBaseline
    } "baseline creation"
    $baselineText = Get-Content -Raw -LiteralPath $baselinePath
    if ($baselineText.Contains($repositoryRoot, [StringComparison]::OrdinalIgnoreCase)) {
        throw "The baseline contains an absolute repository path."
    }
    $baseline = $baselineText | ConvertFrom-Json
    if (@($baseline.diagnostics).Count -ne 1) {
        throw "Expected generated diagnostics to be excluded from the baseline."
    }
    if ($null -ne $baseline.diagnostics[0].PSObject.Properties["message"]) {
        throw "Expected the baseline identity to exclude localized diagnostic messages."
    }
    Assert-Succeeds {
        & $scriptPath -SarifRoot $testRoot -BaselinePath $baselinePath
    } "matching baseline"

    $localizedResult = New-SampleResult `
        -RuleId "CA1001" `
        -Path (Join-Path $repositoryRoot "Common/Sample.cs") `
        -Message "Die Ressource muss verworfen werden."
    Write-SampleSarif @($localizedResult)
    Assert-Succeeds {
        & $scriptPath -SarifRoot $testRoot -BaselinePath $baselinePath
    } "localized diagnostic message"

    Write-SampleSarif @($firstResult, $firstResult)
    Assert-Fails {
        & $scriptPath -SarifRoot $testRoot -BaselinePath $baselinePath
    } "increased diagnostic count"

    $secondResult = New-SampleResult `
        -RuleId "CA5394" `
        -Path (Join-Path $repositoryRoot "Common/SecuritySample.cs") `
        -Message "Use a cryptographically secure random number generator."
    Write-SampleSarif @($firstResult, $secondResult)
    Assert-Fails {
        & $scriptPath -SarifRoot $testRoot -BaselinePath $baselinePath
    } "new diagnostic"

    Write-SampleSarif @()
    Assert-Fails {
        & $scriptPath -SarifRoot $testRoot -BaselinePath $baselinePath
    } "removed diagnostic without baseline refresh"

    Set-Content -LiteralPath $sarifPath -Value "{ invalid" -Encoding utf8NoBOM
    Assert-Fails {
        & $scriptPath -SarifRoot $testRoot -BaselinePath $baselinePath
    } "malformed SARIF"

    Write-SampleSarif @($firstResult)
    $documentWithoutRuns = [ordered]@{
        version = "2.1.0"
    }
    $documentWithoutRuns |
        ConvertTo-Json -Depth 4 |
        Set-Content -LiteralPath $sarifPath -Encoding utf8NoBOM
    Assert-Fails {
        & $scriptPath -SarifRoot $testRoot -BaselinePath $baselinePath
    } "SARIF without runs"

    Write-Host "Analyzer baseline self-tests passed."
}
finally {
    if (Test-Path -LiteralPath $testRoot) {
        Remove-Item -LiteralPath $testRoot -Recurse -Force
    }
}
