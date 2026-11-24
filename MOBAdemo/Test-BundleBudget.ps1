#requires -Version 7.0
<#
.SYNOPSIS
    Checks that the MOBAdemo scripts and style sheets stay within the Brotli budget (issue #430, FR-076).

.DESCRIPTION
    Measures every .js, .mjs and .css file below the web root, lazy chunks and nested folders included, by
    compressing it with Brotli at the smallest-size level (quality 11), and adds the sizes up. The precompressed
    .br and .gz siblings written by MOBAdemo/build.ps1 are not trusted and not counted; images and other files
    do not belong to the budget. Fails (exit code 1) when the total exceeds the limit, when the web root, its
    index.html or a script or style sheet that index.html references is missing, when index.html references an
    external or out-of-root script or style sheet, or when no script or style sheet exists at all.

    200 KB is taken as 200,000 bytes, the conservative reading of FR-076.

.PARAMETER Path
    Web root to measure. In CI: MOBAdemo/Host/wwwroot-build, the default output of MOBAdemo/build.ps1.

.PARAMETER LimitBytes
    Budget for the Brotli-compressed total. Default: 200000.

.EXAMPLE
    pwsh MOBAdemo/build.ps1
    pwsh MOBAdemo/Test-BundleBudget.ps1 -Path MOBAdemo/Host/wwwroot-build
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string] $Path,

    [ValidateRange(1, 1000000000)]
    [long] $LimitBytes = 200000
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-BrotliSize([string] $FilePath) {
    $bytes = [IO.File]::ReadAllBytes($FilePath)
    $buffer = [IO.MemoryStream]::new()
    try {
        $brotli = [IO.Compression.BrotliStream]::new($buffer, [IO.Compression.CompressionLevel]::SmallestSize, $true)
        try {
            $brotli.Write($bytes, 0, $bytes.Length)
        }
        finally {
            $brotli.Dispose()
        }

        return $buffer.Length
    }
    finally {
        $buffer.Dispose()
    }
}

try {
    if (-not (Test-Path -LiteralPath $Path -PathType Container)) {
        throw "The web root '$Path' does not exist."
    }

    $root = [IO.Path]::TrimEndingDirectorySeparator([IO.Path]::GetFullPath((Resolve-Path -LiteralPath $Path).ProviderPath))
    $indexPath = Join-Path $root 'index.html'
    if (-not (Test-Path -LiteralPath $indexPath -PathType Leaf)) {
        throw "The web root '$Path' has no index.html; build it with MOBAdemo/build.ps1 first."
    }

    # Every script and style sheet that index.html references must exist inside the web root.
    $html = Get-Content -Raw -LiteralPath $indexPath
    $references = @([regex]::Matches($html, '(?:src|href)\s*=\s*["'']([^"'']+)["'']', 'IgnoreCase') |
        ForEach-Object { $_.Groups[1].Value } |
        Where-Object { ($_ -split '[?#]')[0] -match '\.(js|mjs|css)$' })
    foreach ($reference in $references) {
        if ($reference -match '^(?:[a-z][a-z0-9+.-]*:)?//' -or $reference -match '^[a-z][a-z0-9+.-]*:') {
            throw "index.html references the external script or style sheet '$reference'."
        }

        $relative = ($reference -split '[?#]')[0].TrimStart('/')
        $target = [IO.Path]::GetFullPath((Join-Path $root $relative))
        if (-not $target.StartsWith($root + [IO.Path]::DirectorySeparatorChar, [StringComparison]::Ordinal)) {
            throw "index.html references '$reference', which lies outside the web root."
        }
        if (-not (Test-Path -LiteralPath $target -PathType Leaf)) {
            throw "index.html references '$reference', but '$relative' is missing from the web root."
        }
    }

    $files = @(Get-ChildItem -LiteralPath $root -Recurse -File |
        Where-Object { $_.Extension -in '.js', '.mjs', '.css' } |
        Sort-Object FullName)
    if ($files.Count -eq 0) {
        throw "No script or style sheet found below '$Path'."
    }

    $total = [long] 0
    foreach ($file in $files) {
        $size = Get-BrotliSize $file.FullName
        $total += $size
        $relativeName = [IO.Path]::GetRelativePath($root, $file.FullName).Replace('\', '/')
        Write-Host ("{0,10:N0}  {1}" -f $size, $relativeName)
    }

    Write-Host "Total Brotli bytes: $total (limit $LimitBytes, $($files.Count) files)"
    if ($total -gt $LimitBytes) {
        throw "The total of scripts and style sheets exceeds the budget by $($total - $LimitBytes) bytes (FR-076)."
    }

    Write-Host "Scripts and style sheets are within the budget: $total of $LimitBytes bytes."
}
catch {
    Write-Host "::error title=MOBAdemo bundle budget::$($_.Exception.Message)"
    Write-Host $_.Exception.Message
    exit 1
}
