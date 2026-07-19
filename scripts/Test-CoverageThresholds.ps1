param(
    [Parameter(Mandatory = $true)]
    [string] $CoveragePath,

    [string] $ThresholdsPath = "Test/coverage-thresholds.json"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function ConvertTo-Percent([object] $Rate) {
    $parsedRate = [double]::Parse(
        [string] $Rate,
        [Globalization.CultureInfo]::InvariantCulture)
    return $parsedRate * 100
}

$resolvedCoveragePath = Resolve-Path -LiteralPath $CoveragePath
$resolvedThresholdsPath = Resolve-Path -LiteralPath $ThresholdsPath
[xml] $coverage = Get-Content -Raw -LiteralPath $resolvedCoveragePath
$thresholds = Get-Content -Raw -LiteralPath $resolvedThresholdsPath | ConvertFrom-Json

$failures = [Collections.Generic.List[string]]::new()
$globalLinePercent = ConvertTo-Percent $coverage.coverage."line-rate"
$globalBranchPercent = ConvertTo-Percent $coverage.coverage."branch-rate"

if ($globalLinePercent -lt $thresholds.global."minimum-line-percent") {
    $failures.Add(
        "Global line coverage $($globalLinePercent.ToString('F2'))% is below $($thresholds.global.'minimum-line-percent')%.")
}

if ($globalBranchPercent -lt $thresholds.global."minimum-branch-percent") {
    $failures.Add(
        "Global branch coverage $($globalBranchPercent.ToString('F2'))% is below $($thresholds.global.'minimum-branch-percent')%.")
}

$packagesByName = @{}
foreach ($package in $coverage.coverage.packages.package) {
    $packagesByName[[string] $package.name] = $package
}

$projectResults = foreach ($property in $thresholds.projects.PSObject.Properties) {
    if (-not $packagesByName.ContainsKey($property.Name)) {
        $failures.Add("Coverage report does not contain project '$($property.Name)'.")
        continue
    }

    $linePercent = ConvertTo-Percent $packagesByName[$property.Name]."line-rate"
    $minimumPercent = [double] $property.Value
    if ($linePercent -lt $minimumPercent) {
        $failures.Add(
            "$($property.Name) line coverage $($linePercent.ToString('F2'))% is below $($minimumPercent.ToString('F2'))%.")
    }

    [pscustomobject]@{
        Project = $property.Name
        LinePercent = $linePercent.ToString("F2")
        MinimumPercent = $minimumPercent.ToString("F2")
    }
}

$projectResults | Sort-Object Project | Format-Table -AutoSize
Write-Host "Global line coverage: $($globalLinePercent.ToString('F2'))%"
Write-Host "Global branch coverage: $($globalBranchPercent.ToString('F2'))%"

if ($failures.Count -gt 0) {
    throw "Coverage threshold failures:`n - $($failures -join "`n - ")"
}
