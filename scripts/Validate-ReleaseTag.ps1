# Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Tag,

    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[^/]+/[^/]+$')]
    [string]$Repository,

    [Parameter(Mandatory = $true)]
    [string]$Token
)

$ErrorActionPreference = 'Stop'

if ($Tag -notmatch '^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?$') {
    throw "Tag '$Tag' is not a plain Semantic Version such as 0.2.0 or 0.2.0-rc.1."
}

$headers = @{
    Accept = 'application/vnd.github+json'
    Authorization = "Bearer $Token"
    'X-GitHub-Api-Version' = '2022-11-28'
}

$escapedTag = [Uri]::EscapeDataString($Tag)
$referenceUri = "https://api.github.com/repos/$Repository/git/ref/tags/$escapedTag"
$reference = Invoke-RestMethod -Uri $referenceUri -Headers $headers

if ($reference.object.type -ne 'tag') {
    throw "Tag '$Tag' is lightweight. MOBAflow releases require an annotated signed tag."
}

$tagObjectUri = "https://api.github.com/repos/$Repository/git/tags/$($reference.object.sha)"
$tagObject = Invoke-RestMethod -Uri $tagObjectUri -Headers $headers

if (-not $tagObject.verification.verified) {
    $reason = $tagObject.verification.reason
    throw "GitHub could not verify the signature for tag '$Tag' (reason: $reason)."
}

$checkedOutCommit = (git rev-parse HEAD).Trim()
$tagCommit = (git rev-list -n 1 $Tag).Trim()
if ($LASTEXITCODE -ne 0 -or $checkedOutCommit -ne $tagCommit) {
    throw "The checked-out commit does not match tag '$Tag'."
}

Write-Host "Validated signed release tag $Tag at commit $checkedOutCommit."
