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
    publishes and runs from that folder when it exists. The folder is replaced. Inside the repository only the
    default folder may be used, and a path that is or runs through a link (symbolic link, junction) into the
    repository is refused.

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

$demoRoot = $PSScriptRoot
$repositoryRoot = Split-Path -Parent $demoRoot
$appRoot = if ([string]::IsNullOrWhiteSpace($AppPath)) { Join-Path $demoRoot 'app' } else { Resolve-Path -LiteralPath $AppPath | Select-Object -ExpandProperty ProviderPath }
$hostRoot = Join-Path $demoRoot 'Host'

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $hostRoot 'wwwroot-build'
}

$output = [IO.Path]::GetFullPath($ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputPath))
$output = $output.TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)

# Returns the path with every symbolic link and junction on it resolved, so the guard compares real locations.
function Resolve-RealPath([string] $Path, [int] $Depth = 0) {
    if ($Depth -gt 16) {
        throw "Refusing to use '$Path': too many nested links."
    }

    $full = [IO.Path]::GetFullPath($Path)
    $current = [IO.Path]::GetPathRoot($full)
    $separators = [char[]] @([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
    foreach ($part in $full.Substring($current.Length).Split($separators, [StringSplitOptions]::RemoveEmptyEntries)) {
        $current = [IO.Path]::Combine($current, $part)
        $info = [IO.DirectoryInfo] $current
        if ($null -ne $info.LinkTarget) {
            $current = Resolve-RealPath $info.ResolveLinkTarget($true).FullName ($Depth + 1)
        }
    }

    return $current.TrimEnd($separators)
}

function Test-IsSameOrParent([string] $Candidate, [string] $Path) {
    $separator = [IO.Path]::DirectorySeparatorChar
    $comparison = if ($IsWindows) { [StringComparison]::OrdinalIgnoreCase } else { [StringComparison]::Ordinal }
    $candidatePath = $Candidate.TrimEnd($separator, [IO.Path]::AltDirectorySeparatorChar)
    $fullPath = $Path.TrimEnd($separator, [IO.Path]::AltDirectorySeparatorChar)
    return $fullPath.Equals($candidatePath, $comparison) -or
        $fullPath.StartsWith($candidatePath + $separator, $comparison)
}

# The output folder is replaced, so inside the repository only the ignored default folder is allowed. The check
# runs on resolved paths: a link outside the repository that points into it must not pass, and a link as the
# output folder itself is never replaced.
$realOutput = Resolve-RealPath $output
$realRepository = Resolve-RealPath $repositoryRoot
$realDefault = Resolve-RealPath (Join-Path $hostRoot 'wwwroot-build')
$outputIsLink = $null -ne ([IO.DirectoryInfo] $output).LinkTarget
if ($outputIsLink -or
    (Test-IsSameOrParent $realOutput $realRepository) -or
    ((Test-IsSameOrParent $realRepository $realOutput) -and -not (Test-IsSameOrParent $realDefault $realOutput))) {
    throw "Refusing to use '$output' as the output folder: it is or runs through a link, or lies inside the repository where only MOBAdemo/Host/wwwroot-build may be replaced."
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
        Copy-Item -Path (Join-Path $distPath '*') -Destination $staging -Recurse -Force

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
