param(
    [string] $RegistryPath = "MutationTest/mutation-lanes.json",
    [switch] $RequireActive,
    [string] $RepositoryRoot = (Split-Path -Parent $PSScriptRoot),
    [string] $LaneName,
    [switch] $PassThru
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repositoryRoot = [IO.Path]::GetFullPath($RepositoryRoot)
$resolvedRegistryPath = Resolve-Path -LiteralPath (Join-Path $repositoryRoot $RegistryPath)
$registry = Get-Content -Raw -LiteralPath $resolvedRegistryPath | ConvertFrom-Json
$testRoot = Join-Path $repositoryRoot "Test"
$testFiles = @(Get-ChildItem -LiteralPath $testRoot -Recurse -Filter "*.cs" -File |
    Where-Object {
        $_.FullName -notmatch "[/\\](bin|obj|TestResults|StrykerOutput)[/\\]" -and
        (Get-Content -Raw -LiteralPath $_.FullName) -match "\[(Test|TestCase|TestCaseSource|TestFixture)(\(|\])"
    })

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
$activeFiles = @{}

foreach ($testFile in $testFiles) {
    $laneMatches = @($laneDirectories | Where-Object {
        $testFile.FullName.StartsWith($_.Directory + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)
    })
    $laneMatches += @($laneFiles | Where-Object {
        $testFile.FullName.Equals($_.File, [StringComparison]::OrdinalIgnoreCase)
    })

    if ($laneMatches.Count -eq 0) {
        $unmappedFiles.Add($testFile.FullName)
        continue
    }

    foreach ($match in $laneMatches) {
        if (-not $laneCounts.ContainsKey($match.Lane)) {
            $laneCounts[$match.Lane] = 0
        }
        $laneCounts[$match.Lane]++

        if ($match.Status -eq "active") {
            if (-not $activeFiles.ContainsKey($match.Lane)) {
                $activeFiles[$match.Lane] = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
            }
            [void] $activeFiles[$match.Lane].Add($testFile.FullName)
        }
    }
    if ($RequireActive -and @($laneMatches | Where-Object Status -eq 'active').Count -eq 0) {
        $inactiveFiles.Add($testFile.FullName)
    }
}

$registry.lanes | ForEach-Object {
    [pscustomobject]@{
        Lane = $_.name
        Status = $_.status
        TestFiles = if ($laneCounts.ContainsKey([string] $_.name)) { $laneCounts[[string] $_.name] } else { 0 }
    }
} | Format-Table -AutoSize | Out-Host

if ($unmappedFiles.Count -gt 0) {
    throw "Test files without a mutation lane:`n - $($unmappedFiles -join "`n - ")"
}

if ($inactiveFiles.Count -gt 0) {
    throw "Test files assigned only to planned mutation lanes:`n - $($inactiveFiles -join "`n - ")"
}

$selectedLanes = @($registry.lanes | Where-Object { $_.status -eq 'active' -and (-not $LaneName -or $_.name -eq $LaneName) })
if ($LaneName -and $selectedLanes.Count -ne 1) {
    throw "Expected one active mutation lane named '$LaneName'."
}

foreach ($selected in $selectedLanes) {
    $configProperty = $selected.PSObject.Properties['stryker-config']
    if ($null -eq $configProperty) { throw "Active lane '$($selected.name)' has no Stryker configuration." }
    $configPath = (Resolve-Path -LiteralPath (Join-Path $repositoryRoot $configProperty.Value)).Path
    $config = (Get-Content -Raw -LiteralPath $configPath | ConvertFrom-Json).'stryker-config'
    foreach ($property in @('target-framework', 'configuration')) {
        if (-not $config.PSObject.Properties[$property] -or [string]::IsNullOrWhiteSpace($config.$property)) {
            throw "Active lane '$($selected.name)' must specify '$property'."
        }
    }
    if ($config.PSObject.Properties['test-case-filter'] -and $config.'test-case-filter') {
        throw "Active lane '$($selected.name)' must not filter out test cases."
    }
    $productionProjects = @($selected.'production-projects')
    if ($productionProjects.Count -ne 1 -or (Split-Path -Leaf $productionProjects[0]) -ne $config.project) {
        throw "Stryker production project does not match lane '$($selected.name)'."
    }
    $productionPath = (Resolve-Path -LiteralPath (Join-Path $repositoryRoot $productionProjects[0])).Path
    $compiledFiles = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    $testProjects = @($config.'test-projects' | ForEach-Object {
        (Resolve-Path -LiteralPath (Join-Path (Split-Path -Parent $configPath) $_)).Path
    })
    if ($testProjects.Count -eq 0) { throw "Active lane '$($selected.name)' has no test projects." }
    $referencesProduction = $false
    foreach ($project in $testProjects) {
        # Evaluate effective items, including imported/conditional Compile Remove entries, not raw XML/globs.
        $evaluation = & dotnet msbuild $project -nologo "-p:TargetFramework=$($config.'target-framework')" `
            "-p:Configuration=$($config.configuration)" -getItem:Compile,ProjectReference
        if ($LASTEXITCODE -ne 0) { throw "MSBuild evaluation failed for '$project'." }
        $items = ($evaluation -join "`n" | ConvertFrom-Json).Items
        foreach ($item in $items.Compile) { [void] $compiledFiles.Add([IO.Path]::GetFullPath($item.FullPath)) }
        foreach ($item in $items.ProjectReference) {
            if ([IO.Path]::GetFullPath($item.FullPath) -eq $productionPath) { $referencesProduction = $true }
        }
    }
    if (-not $referencesProduction) { throw "Lane '$($selected.name)' does not reference its production project." }
    if (-not $activeFiles.ContainsKey($selected.name)) { throw "Active lane '$($selected.name)' has no test files." }
    $expectedFiles = @($activeFiles[$selected.name])
    $missing = @($expectedFiles | Where-Object { -not $compiledFiles.Contains($_) })
    if ($missing.Count -gt 0) {
        throw "Registered tests missing from Stryker host '$($selected.name)':`n - $($missing -join "`n - ")"
    }
    Write-Host "Active lane '$($selected.name)': all $($expectedFiles.Count) registered test files are compiled."
    if ($PassThru) {
        [pscustomobject] @{
            Name = $selected.name
            ConfigPath = $configPath
            TestProjects = $testProjects
            TestFiles = $expectedFiles
            ProductionRoot = Split-Path -Parent $productionPath
            Framework = $config.'target-framework'
            Configuration = $config.configuration
        }
    }
}
Write-Host "All $($testFiles.Count) test files are registered; planned lanes are not mutation-tested."
