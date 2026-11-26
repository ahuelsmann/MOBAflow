#requires -Version 7.0
<#
.SYNOPSIS
    Builds the MOBAdemo frontend and prepares it as the web root of the MOBAdemo host (issue #430).

.DESCRIPTION
    Runs `npm ci` and `npm run build` in MOBAdemo/app, writes a Brotli (quality 11) and a gzip sibling next to
    every JS, CSS, HTML and SVG file, stages the result in a sibling folder and then swaps it for the output
    folder, so nothing from an earlier build stays behind and a failed run keeps the previous output. Runs on Windows and Linux. The host never runs npm or Node; this script does.

    The committed placeholder MOBAdemo/Host/wwwroot/index.html is never changed.

.PARAMETER Configuration
    Release (default) builds the production bundle; Debug builds with Vite's development mode.

.PARAMETER OutputPath
    Folder that receives the web root. Default: MOBAdemo/Host/wwwroot-build (ignored by Git). The host project
    publishes and runs from that folder when it exists. The folder is replaced when it does not exist, is empty, or
    carries the .mobademo-build-owned marker written by this script; any pre-existing non-empty directory or file
    without that marker is refused. Inside the repository only the default folder may be used, and any path that is
    or traverses a symbolic link, junction, or reparse point (including the default folder or any parent) is refused.

.PARAMETER AppPath
    Frontend folder to build. Default: MOBAdemo/app. Tests build a copy of the app with it.

.PARAMETER FailBeforePublish
    Test switch: fails after the old output was moved aside and before the staged output is moved in, to prove
    that the previous output is restored.

.EXAMPLE
    pwsh MOBAdemo/build.ps1
#>
[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Release',

    [string] $OutputPath,

    [string] $AppPath,

    [Parameter(DontShow)]
    [switch] $FailBeforePublish
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
if (Test-Path -LiteralPath 'Variable:\PSNativeCommandUseErrorActionPreference') {
    $PSNativeCommandUseErrorActionPreference = $false
}

$demoRoot = $PSScriptRoot
$repositoryRoot = Split-Path -Parent $demoRoot
$appRoot = if ([string]::IsNullOrWhiteSpace($AppPath)) { Join-Path $demoRoot 'app' } else { Resolve-Path -LiteralPath $AppPath | Select-Object -ExpandProperty ProviderPath }
$hostRoot = Join-Path $demoRoot 'Host'
$defaultOutputPath = Join-Path $hostRoot 'wwwroot-build'
$ownershipMarkerName = '.mobademo-build-owned'

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = $defaultOutputPath
}

$rawOutput = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputPath)
$output = [IO.Path]::TrimEndingDirectorySeparator([IO.Path]::GetFullPath($rawOutput))
$defaultOutput = [IO.Path]::TrimEndingDirectorySeparator([IO.Path]::GetFullPath($defaultOutputPath))
$repositoryFull = [IO.Path]::TrimEndingDirectorySeparator([IO.Path]::GetFullPath($repositoryRoot))

# Checks whether an existing filesystem segment is a symbolic link, junction, or other reparse point.
function Test-SegmentIsReparsePoint([string] $SegmentPath) {
    $item = Get-Item -LiteralPath $SegmentPath -Force -ErrorAction SilentlyContinue
    if ($null -ne $item) {
        if ($null -ne $item.LinkTarget) {
            return $true
        }

        return (([int] $item.Attributes) -band ([int] [IO.FileAttributes]::ReparsePoint)) -ne 0
    }

    # Dangling links may return $null from Get-Item on some platforms; inspect DirectoryInfo metadata directly.
    $dirInfo = [IO.DirectoryInfo]::new($SegmentPath)
    if ($null -ne $dirInfo.LinkTarget) {
        return $true
    }

    $attrs = [int] $dirInfo.Attributes
    return ($attrs -ne -1) -and (($attrs -band ([int] [IO.FileAttributes]::ReparsePoint)) -ne 0)
}

# Walks every segment from the path root/anchor to the leaf and returns the first link/reparse point found, or $null.
function Find-ReparsePointSegment([string] $Path) {
    $full = [IO.Path]::GetFullPath($Path)
    $root = [IO.Path]::GetPathRoot($full)
    if (-not [string]::IsNullOrEmpty($root) -and (Test-SegmentIsReparsePoint $root)) {
        return $root
    }

    $current = $root
    $separators = [char[]] @([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
    $suffix = if ([string]::IsNullOrEmpty($root)) { $full } else { $full.Substring($root.Length) }
    foreach ($part in $suffix.Split($separators, [StringSplitOptions]::RemoveEmptyEntries)) {
        $current = if ([string]::IsNullOrEmpty($current)) { $part } else { [IO.Path]::Combine($current, $part) }
        if (Test-SegmentIsReparsePoint $current) {
            return $current
        }
    }

    return $null
}

# Returns the path with every symbolic link and junction on it resolved, so the guard compares real locations.
function Resolve-RealPath([string] $Path, [int] $Depth = 0) {
    if ($Depth -gt 16) {
        throw "Refusing to use '$Path': too many nested links."
    }

    $full = [IO.Path]::GetFullPath($Path)
    $current = [IO.Path]::GetPathRoot($full)
    $separators = [char[]] @([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
    $suffix = if ([string]::IsNullOrEmpty($current)) { $full } else { $full.Substring($current.Length) }
    foreach ($part in $suffix.Split($separators, [StringSplitOptions]::RemoveEmptyEntries)) {
        $current = if ([string]::IsNullOrEmpty($current)) { $part } else { [IO.Path]::Combine($current, $part) }
        $info = [IO.DirectoryInfo] $current
        if ($null -ne $info.LinkTarget) {
            $target = $info.ResolveLinkTarget($true)
            if ($null -ne $target) {
                $current = Resolve-RealPath $target.FullName ($Depth + 1)
            }
        }
    }

    return [IO.Path]::TrimEndingDirectorySeparator($current)
}

function Test-IsSameOrParent([string] $Candidate, [string] $Path) {
    $separator = [IO.Path]::DirectorySeparatorChar
    $comparison = if ($IsWindows) { [StringComparison]::OrdinalIgnoreCase } else { [StringComparison]::Ordinal }
    $candidatePath = [IO.Path]::TrimEndingDirectorySeparator($Candidate)
    $fullPath = [IO.Path]::TrimEndingDirectorySeparator($Path)
    return $fullPath.Equals($candidatePath, $comparison) -or
        $fullPath.StartsWith($candidatePath + $separator, $comparison)
}

function Test-OutputDirectorySafe([string] $TargetPath, [string] $MarkerName) {
    $existing = Get-Item -LiteralPath $TargetPath -Force -ErrorAction SilentlyContinue
    if ($null -eq $existing) {
        return $true
    }

    if (-not $existing.PSIsContainer) {
        return $false
    }

    if (-not [System.Linq.Enumerable]::Any([IO.Directory]::EnumerateFileSystemEntries($TargetPath))) {
        return $true
    }

    $markerPath = Join-Path $TargetPath $MarkerName
    $markerItem = Get-Item -LiteralPath $markerPath -Force -ErrorAction SilentlyContinue
    return ($null -ne $markerItem) -and
        (-not $markerItem.PSIsContainer) -and
        ($null -eq $markerItem.LinkTarget) -and
        ((([int] $markerItem.Attributes) -band ([int] [IO.FileAttributes]::ReparsePoint)) -eq 0)
}

# The output folder is replaced, so:
# 1. Any path segment that is a link, junction, or reparse point (including the default folder or any parent) is refused.
# 2. Inside the repository only the unlinked default folder MOBAdemo/Host/wwwroot-build may be replaced.
# 3. Any pre-existing target must be either an empty directory or carry our ownership marker file.
$parentDirectory = Split-Path -Parent $output
if ([string]::IsNullOrWhiteSpace($parentDirectory)) {
    throw "Refusing to use '$output' as the output folder: root paths cannot be replaced."
}

$outputLink = Find-ReparsePointSegment $output
$defaultLink = Find-ReparsePointSegment $defaultOutput
$realOutput = Resolve-RealPath $output
$realRepository = Resolve-RealPath $repositoryFull
$comparison = if ($IsWindows) { [StringComparison]::OrdinalIgnoreCase } else { [StringComparison]::Ordinal }
$isAllowedDefault = ($null -eq $defaultLink) -and $realOutput.Equals((Resolve-RealPath $defaultOutput), $comparison)

if ($null -ne $outputLink) {
    throw "Refusing to use '$output' as the output folder: path traverses link '$outputLink' (only unlinked external folders or unlinked MOBAdemo/Host/wwwroot-build may be replaced)."
}

if ((Test-IsSameOrParent $realOutput $realRepository) -or
    ((Test-IsSameOrParent $realRepository $realOutput) -and -not $isAllowedDefault)) {
    if ($null -ne $defaultLink) {
        throw "Refusing to use '$output' as the output folder: repository path '$defaultOutput' traverses link '$defaultLink', so MOBAdemo/Host/wwwroot-build cannot be replaced."
    }

    throw "Refusing to use '$output' as the output folder: it lies inside the repository ('$realRepository') where only MOBAdemo/Host/wwwroot-build may be replaced."
}

if (-not (Test-OutputDirectorySafe -TargetPath $output -MarkerName $ownershipMarkerName)) {
    throw "Refusing to use '$output' as the output folder: existing non-empty directory or file lacks the '$ownershipMarkerName' ownership marker."
}
foreach ($tool in 'node', 'npm') {
    if ($null -eq (Get-Command $tool -ErrorAction SilentlyContinue)) {
        throw "'$tool' was not found on the PATH. Install Node.js 24 (see MOBAdemo/app/.nvmrc)."
    }
}

function Invoke-Native([string] $Description, [scriptblock] $Command) {
    Write-Information "==> $Description" -InformationAction Continue
    & $Command
    if ($LASTEXITCODE -ne 0) {
        throw "$Description failed with exit code $LASTEXITCODE."
    }
}

Push-Location $appRoot
try {
    Invoke-Native 'npm ci' { npm ci }

    $distPath = Join-Path $appRoot 'dist'
    if (Test-Path -LiteralPath $distPath) {
        Remove-Item -LiteralPath $distPath -Recurse -Force
    }

    if ($Configuration -eq 'Debug') {
        Invoke-Native 'npm run build (development mode)' { npm run build -- --mode development }
    }
    else {
        Invoke-Native 'npm run build' { npm run build }
    }

    Invoke-Native 'compress text assets (Brotli q11, gzip)' { npm run compress -- dist }

    # Stage the complete output next to the target, then swap: a failure at any point keeps the previous output.
    $parent = Split-Path -Parent $output
    $suffix = [Guid]::NewGuid().ToString('N').Substring(0, 8)
    $staging = Join-Path $parent ".$(Split-Path -Leaf $output).new-$suffix"
    $backup = Join-Path $parent ".$(Split-Path -Leaf $output).old-$suffix"
    $movedOld = $false
    try {
        New-Item -ItemType Directory -Path $staging -Force | Out-Null
        Get-ChildItem -LiteralPath $distPath -Force | ForEach-Object {
            Copy-Item -LiteralPath $_.FullName -Destination $staging -Recurse -Force
        }
        [IO.File]::WriteAllText(
            (Join-Path $staging $ownershipMarkerName),
            "MOBAdemo build output$([Environment]::NewLine)",
            [Text.UTF8Encoding]::new($false))

        if (Test-Path -LiteralPath $output) {
            Move-Item -LiteralPath $output -Destination $backup
            $movedOld = $true
        }

        if ($FailBeforePublish) {
            throw 'Simulated failure before the staged output is moved in.'
        }

        Move-Item -LiteralPath $staging -Destination $output
    }
    catch {
        if ($movedOld -and -not (Test-Path -LiteralPath $output)) {
            Move-Item -LiteralPath $backup -Destination $output
            $movedOld = $false
        }

        throw
    }
    finally {
        # The backup is the only copy of the old output until the new one is in place: keep it when the restore failed.
        $leftovers = if (Test-Path -LiteralPath $output) { @($staging, $backup) } else { @($staging) }
        foreach ($leftover in $leftovers) {
            if (Test-Path -LiteralPath $leftover) {
                Remove-Item -LiteralPath $leftover -Recurse -Force
            }
        }
    }
}
finally {
    Pop-Location
}

Write-Output "MOBAdemo web root written to $output"
