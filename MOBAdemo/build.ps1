#requires -Version 7.0
<#
.SYNOPSIS
    Builds the MOBAdemo frontend and prepares it as the web root of the MOBAdemo host (issue #430).

.DESCRIPTION
    Runs `npm ci` and `npm run build` in MOBAdemo/app, writes a Brotli (quality 11) and a gzip sibling next to
    every JS, CSS, HTML and SVG file, and then replaces the output folder with the result, so nothing from an
    earlier build stays behind. Runs on Windows and Linux. The host never runs npm or Node; this script does.

    The committed placeholder MOBAdemo/Host/wwwroot/index.html is never changed.

.PARAMETER Configuration
    Release (default) builds the production bundle; Debug builds with Vite's development mode.

.PARAMETER OutputPath
    Folder that receives the web root. Default: MOBAdemo/Host/wwwroot-build (ignored by Git). The host project
    publishes and runs from that folder when it exists. The folder is deleted and recreated.

.EXAMPLE
    pwsh MOBAdemo/build.ps1
#>
[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Release',

    [string] $OutputPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$demoRoot = $PSScriptRoot
$repositoryRoot = Split-Path -Parent $demoRoot
$appRoot = Join-Path $demoRoot 'app'
$hostRoot = Join-Path $demoRoot 'Host'

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $hostRoot 'wwwroot-build'
}

$output = [IO.Path]::GetFullPath($ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputPath))
$output = $output.TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)

function Test-IsSameOrParent([string] $Candidate, [string] $Path) {
    $separator = [IO.Path]::DirectorySeparatorChar
    $comparison = if ($IsWindows) { [StringComparison]::OrdinalIgnoreCase } else { [StringComparison]::Ordinal }
    $candidatePath = $Candidate.TrimEnd($separator, [IO.Path]::AltDirectorySeparatorChar)
    $fullPath = $Path.TrimEnd($separator, [IO.Path]::AltDirectorySeparatorChar)
    return $fullPath.Equals($candidatePath, $comparison) -or
        $fullPath.StartsWith($candidatePath + $separator, $comparison)
}

# The output folder is deleted, so inside the repository only the ignored default folder is allowed.
$defaultOutput = [IO.Path]::GetFullPath((Join-Path $hostRoot 'wwwroot-build'))
if ((Test-IsSameOrParent $output $repositoryRoot) -or
    ((Test-IsSameOrParent $repositoryRoot $output) -and -not (Test-IsSameOrParent $defaultOutput $output))) {
    throw "Refusing to use '$output' as the output folder: inside the repository only MOBAdemo/Host/wwwroot-build may be replaced."
}

foreach ($tool in 'node', 'npm') {
    if ($null -eq (Get-Command $tool -ErrorAction SilentlyContinue)) {
        throw "'$tool' was not found on the PATH. Install Node.js 24 (see MOBAdemo/app/.nvmrc)."
    }
}

function Invoke-Native([string] $Description, [scriptblock] $Command) {
    Write-Host "==> $Description"
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

    # Replace the output only after the build and the compression succeeded: a failed build keeps the old folder.
    if (Test-Path -LiteralPath $output) {
        Remove-Item -LiteralPath $output -Recurse -Force
    }

    New-Item -ItemType Directory -Path $output -Force | Out-Null
    Copy-Item -Path (Join-Path $distPath '*') -Destination $output -Recurse -Force
}
finally {
    Pop-Location
}

Write-Host "MOBAdemo web root written to $output"
