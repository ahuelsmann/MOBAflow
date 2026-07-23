param(
    [string] $SarifRoot = "artifacts/analyzers/Release/net10.0",

    [string] $BaselinePath = "quality/analyzer-baseline.json",

    [switch] $UpdateBaseline
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Resolve-InputPath([string] $RepositoryRoot, [string] $Path) {
    if ([IO.Path]::IsPathRooted($Path)) {
        return [IO.Path]::GetFullPath($Path)
    }

    return [IO.Path]::GetFullPath((Join-Path $RepositoryRoot $Path))
}

function Normalize-Message(
    [string] $Message,
    [string] $RepositoryRoot) {
    $normalizedMessage = [Text.RegularExpressions.Regex]::Replace(
        $Message.Trim(),
        "\s+",
        " ")
    $normalizedMessage = $normalizedMessage.Replace(
        $RepositoryRoot,
        "<repo>",
        [StringComparison]::OrdinalIgnoreCase)
    $normalizedRepositoryRoot = $RepositoryRoot.Replace('\', '/')
    return $normalizedMessage.Replace(
        $normalizedRepositoryRoot,
        "<repo>",
        [StringComparison]::OrdinalIgnoreCase)
}

function Get-RepositoryRelativePath(
    [string] $RepositoryRoot,
    [string] $ArtifactUri) {
    if ([string]::IsNullOrWhiteSpace($ArtifactUri)) {
        return "<no-location>"
    }

    $decodedPath = [Uri]::UnescapeDataString($ArtifactUri)
    if ($decodedPath.StartsWith("file:", [StringComparison]::OrdinalIgnoreCase)) {
        $decodedPath = ([Uri] $decodedPath).LocalPath
    }

    $decodedPath = $decodedPath.Replace('/', [IO.Path]::DirectorySeparatorChar)
    $fullPath = if ([IO.Path]::IsPathRooted($decodedPath)) {
        [IO.Path]::GetFullPath($decodedPath)
    }
    else {
        [IO.Path]::GetFullPath((Join-Path $RepositoryRoot $decodedPath))
    }

    $relativePath = [IO.Path]::GetRelativePath($RepositoryRoot, $fullPath).Replace('\', '/')
    if ($relativePath.StartsWith("../", [StringComparison]::Ordinal)) {
        return "<external>/$([IO.Path]::GetFileName($fullPath))"
    }

    return $relativePath
}

function Get-ResultArtifactUri([object] $Result) {
    $locationsProperty = $Result.PSObject.Properties["locations"]
    if ($null -eq $locationsProperty -or @($locationsProperty.Value).Count -eq 0) {
        return ""
    }

    $physicalLocation = @($locationsProperty.Value)[0].physicalLocation
    if ($null -eq $physicalLocation) {
        return ""
    }

    $artifactLocation = $physicalLocation.artifactLocation
    if ($null -eq $artifactLocation) {
        return ""
    }

    return [string] $artifactLocation.uri
}

function Test-IsGeneratedPath([string] $Path) {
    return $Path -match "(^|/)(bin|obj)/" -or
        $Path.EndsWith(".g.cs", [StringComparison]::OrdinalIgnoreCase) -or
        $Path.EndsWith(".generated.cs", [StringComparison]::OrdinalIgnoreCase)
}

function Get-SarifFiles([string] $ResolvedSarifRoot) {
    $sarifFiles = @(
        Get-ChildItem -LiteralPath $ResolvedSarifRoot -Recurse -Filter "*.sarif" -File |
            Sort-Object FullName
    )

    if ($sarifFiles.Count -eq 0) {
        throw "No analyzer SARIF files were found below '$ResolvedSarifRoot'."
    }

    return $sarifFiles
}

function Read-SarifDocument([IO.FileInfo] $SarifFile) {
    try {
        return Get-Content -Raw -LiteralPath $SarifFile.FullName | ConvertFrom-Json
    }
    catch {
        throw "Invalid SARIF file '$($SarifFile.FullName)': $($_.Exception.Message)"
    }
}

function Get-SarifRuns(
    [object] $Document,
    [string] $SarifFilePath) {
    $runsProperty = $Document.PSObject.Properties["runs"]
    if ($null -eq $runsProperty -or @($runsProperty.Value).Count -eq 0) {
        throw "SARIF file '$SarifFilePath' has no runs."
    }

    return @($runsProperty.Value)
}

function Get-SarifRunResults(
    [object] $Run,
    [string] $SarifFilePath) {
    if ($null -eq $Run) {
        throw "SARIF file '$SarifFilePath' contains an empty run."
    }

    $resultsProperty = $Run.PSObject.Properties["results"]
    if ($null -eq $resultsProperty) {
        return @()
    }

    return @($resultsProperty.Value)
}

function ConvertTo-SarifDiagnostic(
    [object] $Result,
    [string] $SarifFilePath,
    [string] $RepositoryRoot,
    [string] $Project,
    [string] $TargetFramework) {
    if ($null -eq $Result) {
        throw "SARIF file '$SarifFilePath' contains an empty result."
    }

    $path = Get-RepositoryRelativePath `
        -RepositoryRoot $RepositoryRoot `
        -ArtifactUri (Get-ResultArtifactUri $Result)
    if (Test-IsGeneratedPath $path) {
        return $null
    }

    if ([string]::IsNullOrWhiteSpace([string] $Result.ruleId)) {
        throw "SARIF result in '$SarifFilePath' has no rule id."
    }

    $messageObject = $Result.PSObject.Properties["message"]
    if ($null -eq $messageObject -or $null -eq $messageObject.Value) {
        throw "SARIF result '$($Result.ruleId)' in '$SarifFilePath' has no message."
    }

    $messageProperty = $messageObject.Value.PSObject.Properties["text"]
    if ($null -eq $messageProperty -or
        [string]::IsNullOrWhiteSpace([string] $messageProperty.Value)) {
        throw "SARIF result '$($Result.ruleId)' in '$SarifFilePath' has no text message."
    }

    return [pscustomobject]@{
        project = $Project
        targetFramework = $TargetFramework
        ruleId = [string] $Result.ruleId
        path = $path
        message = Normalize-Message `
            -Message ([string] $messageProperty.Value) `
            -RepositoryRoot $RepositoryRoot
    }
}

function Get-SarifDiagnostics(
    [string] $RepositoryRoot,
    [string] $ResolvedSarifRoot) {
    $diagnostics = foreach ($sarifFile in Get-SarifFiles $ResolvedSarifRoot) {
        $document = Read-SarifDocument $sarifFile
        $targetFramework = $sarifFile.Directory.Name
        $project = $sarifFile.BaseName

        foreach ($run in Get-SarifRuns -Document $document -SarifFilePath $sarifFile.FullName) {
            foreach ($result in Get-SarifRunResults -Run $run -SarifFilePath $sarifFile.FullName) {
                $diagnostic = ConvertTo-SarifDiagnostic `
                    -Result $result `
                    -SarifFilePath $sarifFile.FullName `
                    -RepositoryRoot $RepositoryRoot `
                    -Project $project `
                    -TargetFramework $targetFramework
                if ($null -ne $diagnostic) {
                    $diagnostic
                }
            }
        }
    }

    return @($diagnostics)
}

function ConvertTo-BaselineEntries([object[]] $Diagnostics) {
    $groups = $Diagnostics |
        Group-Object project, targetFramework, ruleId, path, message |
        Sort-Object {
            $first = $_.Group[0]
            "$($first.project)|$($first.targetFramework)|$($first.ruleId)|$($first.path)|$($first.message)"
        }

    return @(
        foreach ($group in $groups) {
            $first = $group.Group[0]
            [ordered]@{
                project = $first.project
                targetFramework = $first.targetFramework
                ruleId = $first.ruleId
                path = $first.path
                message = $first.message
                count = $group.Count
            }
        }
    )
}

function Get-EntryKey([object] $Entry) {
    return "$($Entry.project)|$($Entry.targetFramework)|$($Entry.ruleId)|$($Entry.path)|$($Entry.message)"
}

function ConvertTo-EntryCountMap([object[]] $Entries) {
    $countsByKey = @{}
    foreach ($entry in $Entries) {
        $countsByKey[(Get-EntryKey $entry)] = [int] $entry.count
    }

    return $countsByKey
}

function Get-EntryCount(
    [hashtable] $CountsByKey,
    [string] $Key) {
    if ($CountsByKey.ContainsKey($Key)) {
        return $CountsByKey[$Key]
    }

    return 0
}

function Get-BaselineMismatch(
    [string] $Key,
    [hashtable] $ExpectedByKey,
    [hashtable] $CurrentByKey) {
    $expected = Get-EntryCount -CountsByKey $ExpectedByKey -Key $Key
    $current = Get-EntryCount -CountsByKey $CurrentByKey -Key $Key
    if ($expected -eq $current) {
        return $null
    }

    $direction = if ($current -gt $expected) { "new or increased" } else { "removed or decreased" }
    return "$direction diagnostic: expected=$expected current=$current $Key"
}

function Write-Baseline(
    [string] $ResolvedBaselinePath,
    [object[]] $Entries) {
    $baselineDirectory = Split-Path -Parent $ResolvedBaselinePath
    if (-not (Test-Path -LiteralPath $baselineDirectory)) {
        New-Item -ItemType Directory -Path $baselineDirectory | Out-Null
    }

    $document = [ordered]@{
        schemaVersion = 1
        diagnostics = $Entries
    }
    $json = $document | ConvertTo-Json -Depth 8
    [IO.File]::WriteAllText(
        $ResolvedBaselinePath,
        $json + [Environment]::NewLine,
        [Text.UTF8Encoding]::new($false))
}

function Compare-Baseline(
    [string] $ResolvedBaselinePath,
    [object[]] $CurrentEntries) {
    if (-not (Test-Path -LiteralPath $ResolvedBaselinePath)) {
        throw "Analyzer baseline '$ResolvedBaselinePath' does not exist. Run with -UpdateBaseline."
    }

    $baseline = Get-Content -Raw -LiteralPath $ResolvedBaselinePath | ConvertFrom-Json
    if ([int] $baseline.schemaVersion -ne 1) {
        throw "Unsupported analyzer baseline schema version '$($baseline.schemaVersion)'."
    }

    $expectedByKey = ConvertTo-EntryCountMap @($baseline.diagnostics)
    $currentByKey = ConvertTo-EntryCountMap $CurrentEntries
    $keys = @($expectedByKey.Keys + $currentByKey.Keys | Sort-Object -Unique)
    $failures = @(
        foreach ($key in $keys) {
            $mismatch = Get-BaselineMismatch `
                -Key $key `
                -ExpectedByKey $expectedByKey `
                -CurrentByKey $currentByKey
            if ($null -ne $mismatch) {
                $mismatch
            }
        }
    )

    if ($failures.Count -gt 0) {
        throw "Analyzer baseline mismatch. Refresh the baseline in the same reviewed change:`n - $($failures -join "`n - ")"
    }
}

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$resolvedSarifRoot = Resolve-InputPath -RepositoryRoot $repositoryRoot -Path $SarifRoot
$resolvedBaselinePath = Resolve-InputPath -RepositoryRoot $repositoryRoot -Path $BaselinePath
$diagnostics = Get-SarifDiagnostics `
    -RepositoryRoot $repositoryRoot `
    -ResolvedSarifRoot $resolvedSarifRoot
$entries = ConvertTo-BaselineEntries $diagnostics

if ($UpdateBaseline) {
    Write-Baseline -ResolvedBaselinePath $resolvedBaselinePath -Entries $entries
    Write-Host "Wrote analyzer baseline with $($diagnostics.Count) diagnostics in $($entries.Count) groups."
}
else {
    Compare-Baseline -ResolvedBaselinePath $resolvedBaselinePath -CurrentEntries $entries
    Write-Host "Analyzer baseline matches $($diagnostics.Count) diagnostics in $($entries.Count) groups."
}
