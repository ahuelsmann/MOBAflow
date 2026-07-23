param(
    [string] $SarifRoot = ".",

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

function Get-SarifDiagnostics(
    [string] $RepositoryRoot,
    [string] $ResolvedSarifRoot) {
    $sarifFiles = @(
        Get-ChildItem -LiteralPath $ResolvedSarifRoot -Recurse -Filter "*.sarif" -File |
            Where-Object {
                $_.FullName -match "[/\\]obj[/\\]analyzers[/\\]"
            } |
            Sort-Object FullName
    )

    if ($sarifFiles.Count -eq 0) {
        throw "No analyzer SARIF files were found below '$ResolvedSarifRoot'."
    }

    $diagnostics = foreach ($sarifFile in $sarifFiles) {
        try {
            $document = Get-Content -Raw -LiteralPath $sarifFile.FullName | ConvertFrom-Json
        }
        catch {
            throw "Invalid SARIF file '$($sarifFile.FullName)': $($_.Exception.Message)"
        }

        $targetFramework = $sarifFile.Directory.Name
        $project = $sarifFile.BaseName

        $runsProperty = $document.PSObject.Properties["runs"]
        if ($null -eq $runsProperty -or @($runsProperty.Value).Count -eq 0) {
            throw "SARIF file '$($sarifFile.FullName)' has no runs."
        }

        foreach ($run in @($runsProperty.Value)) {
            if ($null -eq $run) {
                throw "SARIF file '$($sarifFile.FullName)' contains an empty run."
            }

            $resultsProperty = $run.PSObject.Properties["results"]
            if ($null -eq $resultsProperty) {
                continue
            }

            foreach ($result in @($resultsProperty.Value)) {
                if ($null -eq $result) {
                    throw "SARIF file '$($sarifFile.FullName)' contains an empty result."
                }

                $path = Get-RepositoryRelativePath `
                    -RepositoryRoot $RepositoryRoot `
                    -ArtifactUri (Get-ResultArtifactUri $result)
                if (Test-IsGeneratedPath $path) {
                    continue
                }

                if ([string]::IsNullOrWhiteSpace([string] $result.ruleId)) {
                    throw "SARIF result in '$($sarifFile.FullName)' has no rule id."
                }

                $messageObject = $result.PSObject.Properties["message"]
                if ($null -eq $messageObject -or $null -eq $messageObject.Value) {
                    throw "SARIF result '$($result.ruleId)' in '$($sarifFile.FullName)' has no message."
                }

                $messageProperty = $messageObject.Value.PSObject.Properties["text"]
                if ($null -eq $messageProperty -or
                    [string]::IsNullOrWhiteSpace([string] $messageProperty.Value)) {
                    throw "SARIF result '$($result.ruleId)' in '$($sarifFile.FullName)' has no text message."
                }

                [pscustomobject]@{
                    project = $project
                    targetFramework = $targetFramework
                    ruleId = [string] $result.ruleId
                    path = $path
                    message = Normalize-Message `
                        -Message ([string] $messageProperty.Value) `
                        -RepositoryRoot $RepositoryRoot
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

    $expectedByKey = @{}
    foreach ($entry in @($baseline.diagnostics)) {
        $expectedByKey[(Get-EntryKey $entry)] = [int] $entry.count
    }

    $currentByKey = @{}
    foreach ($entry in $CurrentEntries) {
        $currentByKey[(Get-EntryKey $entry)] = [int] $entry.count
    }

    $failures = [Collections.Generic.List[string]]::new()
    foreach ($key in @($expectedByKey.Keys + $currentByKey.Keys | Sort-Object -Unique)) {
        $expected = if ($expectedByKey.ContainsKey($key)) { $expectedByKey[$key] } else { 0 }
        $current = if ($currentByKey.ContainsKey($key)) { $currentByKey[$key] } else { 0 }
        if ($expected -ne $current) {
            $direction = if ($current -gt $expected) { "new or increased" } else { "removed or decreased" }
            $failures.Add("$direction diagnostic: expected=$expected current=$current $key")
        }
    }

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
