param(
    [string] $RegistryPath = "MutationTest/mutation-lanes.json",
    [switch] $RequireActive
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$resolvedRegistryPath = Resolve-Path -LiteralPath (Join-Path $repositoryRoot $RegistryPath)
$registry = Get-Content -Raw -LiteralPath $resolvedRegistryPath | ConvertFrom-Json
$testRoot = Join-Path $repositoryRoot "Test"
$testFiles = Get-ChildItem -LiteralPath $testRoot -Recurse -Filter "*.cs" -File |
    Where-Object {
        $_.FullName -notmatch "[/\\](bin|obj|TestResults|StrykerOutput)[/\\]" -and
        (Get-Content -Raw -LiteralPath $_.FullName) -match "\[(Test|TestCase|TestCaseSource|TestFixture)(\(|\])"
    }

$laneDirectories = foreach ($lane in $registry.lanes) {
    foreach ($testDirectory in $lane."test-directories") {
        [pscustomobject]@{
            Lane = [string] $lane.name
            Status = [string] $lane.status
            Directory = [IO.Path]::GetFullPath((Join-Path $repositoryRoot $testDirectory)).TrimEnd('\', '/')
        }
    }
}

$laneFiles = foreach ($lane in $registry.lanes) {
    $testFilesProperty = $lane.PSObject.Properties["test-files"]
    if ($null -eq $testFilesProperty) {
        continue
    }

    foreach ($testFile in @($testFilesProperty.Value)) {
        if ([string]::IsNullOrWhiteSpace([string] $testFile)) {
            continue
        }

        [pscustomobject]@{
            Lane = [string] $lane.name
            Status = [string] $lane.status
            File = [IO.Path]::GetFullPath((Join-Path $repositoryRoot $testFile))
        }
    }
}

$unmappedFiles = [Collections.Generic.List[string]]::new()
$inactiveFiles = [Collections.Generic.List[string]]::new()
$laneCounts = @{}

foreach ($testFile in $testFiles) {
    $matches = @($laneDirectories | Where-Object {
        $testFile.FullName.StartsWith($_.Directory + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)
    })
    $matches += @($laneFiles | Where-Object {
        $testFile.FullName.Equals($_.File, [StringComparison]::OrdinalIgnoreCase)
    })

    if ($matches.Count -eq 0) {
        $unmappedFiles.Add($testFile.FullName)
        continue
    }

    foreach ($match in $matches) {
        if (-not $laneCounts.ContainsKey($match.Lane)) {
            $laneCounts[$match.Lane] = 0
        }
        $laneCounts[$match.Lane]++

        if ($RequireActive -and $match.Status -ne "active") {
            $inactiveFiles.Add("$($testFile.FullName) -> $($match.Lane)")
        }
    }
}

$registry.lanes | ForEach-Object {
    [pscustomobject]@{
        Lane = $_.name
        Status = $_.status
        TestFiles = if ($laneCounts.ContainsKey([string] $_.name)) { $laneCounts[[string] $_.name] } else { 0 }
    }
} | Format-Table -AutoSize

if ($unmappedFiles.Count -gt 0) {
    throw "Test files without a mutation lane:`n - $($unmappedFiles -join "`n - ")"
}

if ($inactiveFiles.Count -gt 0) {
    throw "Test files assigned only to planned mutation lanes:`n - $($inactiveFiles -join "`n - ")"
}

Write-Host "All $($testFiles.Count) test files are registered in mutation lanes."
