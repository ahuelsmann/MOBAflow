[CmdletBinding()]
param(
    # Repository-relative paths to classify. When omitted, the paths changed between BaseRef and HeadRef are used.
    [string[]] $Path,

    [string] $BaseRef,

    [string] $HeadRef = 'HEAD'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Paths that no build, test, analyzer or firmware job reads. Anything else, including an empty change set,
# keeps the full Quality workflow. Markdown inside project directories is excluded on purpose because
# projects such as Sound package it as content.
$documentationOnlyPatterns = @(
    '^docs/',
    '^plans/',
    '^specs/',
    '^\.specify/',
    '^\.github/instructions/',
    '^\.github/ISSUE_TEMPLATE/',
    '^\.github/[^/]+\.md$',
    '^[^/]+\.md$',
    '^LICENSE$'
)

function Test-IsDocumentationOnlyPath([string] $RelativePath) {
    $normalizedPath = $RelativePath.Trim().Replace('\', '/')
    foreach ($pattern in $documentationOnlyPatterns) {
        if ($normalizedPath -cmatch $pattern) {
            return $true
        }
    }

    return $false
}

function Get-ChangedPaths([string] $Base, [string] $Head) {
    $changedPaths = @(git diff --name-only --no-renames $Base $Head)
    if ($LASTEXITCODE -ne 0) {
        throw "git diff between '$Base' and '$Head' failed."
    }

    return $changedPaths
}

if ($null -eq $Path) {
    if ([string]::IsNullOrWhiteSpace($BaseRef)) {
        throw 'Provide either -Path or -BaseRef.'
    }

    $Path = Get-ChangedPaths -Base $BaseRef -Head $HeadRef
}

$changedPaths = @($Path | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
$codePaths = @($changedPaths | Where-Object { -not (Test-IsDocumentationOnlyPath $_) })
$code = ($changedPaths.Count -eq 0) -or ($codePaths.Count -gt 0)
$codeValue = $code.ToString().ToLowerInvariant()

if ($code) {
    Write-Host "Code-relevant changes found; running the full Quality workflow."
    $codePaths | Select-Object -First 20 | ForEach-Object { Write-Host "  $_" }
}
else {
    Write-Host "All $($changedPaths.Count) changed paths are documentation only; skipping build, test and firmware jobs."
}

if (-not [string]::IsNullOrEmpty($env:GITHUB_OUTPUT)) {
    Add-Content -LiteralPath $env:GITHUB_OUTPUT -Value "code=$codeValue" -Encoding utf8NoBOM
}

return $code
