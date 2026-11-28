[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Sbom,

    [Parameter(Mandatory = $true)]
    [string]$Archive,

    # The SBOM must bind to exactly this tag and commit.
    [Parameter(Mandatory = $true)]
    [string]$Tag,

    [Parameter(Mandatory = $true)]
    [string]$Commit,

    # SHA256SUMS.txt that must list both the archive and the SBOM.
    [string]$ChecksumFile
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

foreach ($path in @($Sbom, $Archive)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "File does not exist: $path" }
}

$raw = Get-Content -LiteralPath $Sbom -Raw
$document = $raw | ConvertFrom-Json
if ($document.spdxVersion -ne 'SPDX-2.3') { throw 'SBOM is not an SPDX 2.3 document.' }

$archiveName = Split-Path -Leaf $Archive
$archiveSha = (Get-FileHash -LiteralPath $Archive -Algorithm SHA256).Hash.ToLowerInvariant()
$archiveEntry = @($document.files | Where-Object { $_.SPDXID -eq 'SPDXRef-Archive' })
if ($archiveEntry.Count -ne 1 -or $archiveEntry[0].fileName -ne $archiveName) { throw 'SBOM does not describe the release archive.' }
if ($archiveEntry[0].checksums[0].checksumValue -ne $archiveSha) { throw 'SBOM archive digest does not match the release archive.' }

$comment = [string]$document.creationInfo.comment
if ($comment -notlike "*tag=$Tag;*") { throw 'SBOM is not bound to the expected tag.' }
if ($comment -notlike "*commit=$Commit;*") { throw 'SBOM is not bound to the expected commit.' }
if ($document.documentNamespace -notlike "*/$Tag/$archiveSha") { throw 'SBOM document namespace is not bound to the archive digest.' }

# Every archive entry must be listed with an identical digest, and the SBOM must list nothing else.
$expected = @{}
foreach ($file in ($document.files | Where-Object { $_.SPDXID -ne 'SPDXRef-Archive' })) {
    $expected[$file.fileName.Substring(2)] = $file.checksums[0].checksumValue
}
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [IO.Compression.ZipFile]::OpenRead((Resolve-Path -LiteralPath $Archive).Path)
try {
    $seen = 0
    foreach ($entry in $zip.Entries) {
        if ($entry.FullName.EndsWith('/')) { continue }
        $seen++
        if (-not $expected.ContainsKey($entry.FullName)) { throw "Archive file is missing from the SBOM: $($entry.FullName)" }
        $stream = $entry.Open()
        try {
            $sha = [Security.Cryptography.SHA256]::Create()
            $actual = -join ($sha.ComputeHash($stream) | ForEach-Object { $_.ToString('x2') })
        }
        finally { $stream.Dispose() }
        if ($actual -ne $expected[$entry.FullName]) { throw "SBOM digest differs from archive content: $($entry.FullName)" }
    }
    if ($seen -ne $expected.Count) { throw "SBOM lists $($expected.Count) files but the archive contains $seen." }
}
finally { $zip.Dispose() }

if ($raw -match '(?i)([a-z]:\\|/home/[^/]+/|/Users/[^/]+/|ghp_|github_pat_)') {
    throw 'SBOM contains a local path or credential-like value.'
}

if ($ChecksumFile) {
    if (-not (Test-Path -LiteralPath $ChecksumFile -PathType Leaf)) { throw "Checksum file does not exist: $ChecksumFile" }
    $sbomName = Split-Path -Leaf $Sbom
    $sbomSha = (Get-FileHash -LiteralPath $Sbom -Algorithm SHA256).Hash.ToLowerInvariant()
    $lines = Get-Content -LiteralPath $ChecksumFile
    foreach ($pair in @(@($archiveName, $archiveSha), @($sbomName, $sbomSha))) {
        if ($lines -notcontains "$($pair[1])  $($pair[0])") { throw "SHA256SUMS.txt has no matching line for $($pair[0])." }
    }
}

Write-Host 'Release SBOM validation passed.'
