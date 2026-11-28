[CmdletBinding()]
param(
    # Sanitized, validated publish directory that was compressed into the archive.
    [Parameter(Mandatory = $true)]
    [string]$PublishDirectory,

    # The finished release ZIP; its SHA-256 becomes the root of the SBOM.
    [Parameter(Mandatory = $true)]
    [string]$Archive,

    [Parameter(Mandatory = $true)]
    [string]$Tag,

    [Parameter(Mandatory = $true)]
    [string]$Commit,

    [Parameter(Mandatory = $true)]
    [string]$Repository,

    [string]$RunUrl = '',

    # NuGet restore outputs (obj/project.assets.json) of the projects that were published.
    [Parameter(Mandatory = $true)]
    [string[]]$AssetsFile,

    [Parameter(Mandatory = $true)]
    [string]$OutputPath,

    [string]$CoverageOutputPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $PublishDirectory -PathType Container)) { throw "Publish directory does not exist: $PublishDirectory" }
if (-not (Test-Path -LiteralPath $Archive -PathType Leaf)) { throw "Release archive does not exist: $Archive" }
if ($Commit -notmatch '^[0-9a-f]{40}$') { throw 'Commit must be a full 40-character lowercase SHA.' }

$publishRoot = (Resolve-Path -LiteralPath $PublishDirectory).Path.TrimEnd('\', '/')

function Get-Sha256([string]$Path) { (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant() }

function ConvertTo-SpdxId([string]$Text) {
    'SPDXRef-' + ($Text -replace '[^A-Za-z0-9.\-]', '-')
}

# --- NuGet package inventory (direct and transitive) from the restore assets ---------------------
$packages = @{}
$projectNames = @{}
foreach ($assets in $AssetsFile) {
    if (-not (Test-Path -LiteralPath $assets -PathType Leaf)) { throw "NuGet assets file does not exist: $assets" }
    $json = Get-Content -LiteralPath $assets -Raw | ConvertFrom-Json
    foreach ($property in $json.libraries.PSObject.Properties) {
        $library = $property.Value
        if ($library.type -eq 'project') { $projectNames[($property.Name -split '/', 2)[0].ToLowerInvariant()] = $true; continue }
        if ($library.type -ne 'package') { continue }
        $name, $version = $property.Name -split '/', 2
        $key = "$name/$version".ToLowerInvariant()
        if ($packages.ContainsKey($key)) { continue }
        $hex = $null
        if ($library.PSObject.Properties['sha512'] -and $library.sha512) {
            $hex = -join ([Convert]::FromBase64String($library.sha512) | ForEach-Object { $_.ToString('x2') })
        }
        $packages[$key] = [pscustomobject]@{ Name = $name; Version = $version; Sha512 = $hex }
    }
}

# --- Classification of every shipped file ----------------------------------------------------------
$packageNames = @{}
foreach ($package in $packages.Values) { $packageNames[$package.Name.ToLowerInvariant()] = $true }

function Get-Category([string]$RelativePath) {
    $leaf = [IO.Path]::GetFileName($RelativePath)
    $stem = [IO.Path]::GetFileNameWithoutExtension($leaf)
    $extension = [IO.Path]::GetExtension($leaf).ToLowerInvariant()

    if ($stem -match '^(MOBA|Moba)' -and $extension -in '.dll', '.exe', '.pdb', '.json', '.xbf', '.pri') { return 'own-code' }
    if ($extension -eq '.pdb' -or $extension -eq '.xml') { return 'own-code' }
    if ($extension -in '.dll', '.exe') {
        if ($projectNames.ContainsKey($stem.ToLowerInvariant())) { return 'own-code' }
        if ($packageNames.ContainsKey($stem.ToLowerInvariant())) { return 'nuget-package' }
        if ($stem -match '^(System|Microsoft|WindowsBase|mscorlib|netstandard|PresentationNative|clrjit|coreclr|hostfxr|hostpolicy|clretwrc|mscordaccore|mscordbi|msquic|createdump|api-ms-win|ucrtbase|D3DCompiler|vcruntime|msvcp|concrt|clrgc|aspnetcorev2|mscorrc)') { return 'dotnet-runtime-or-platform' }
        return 'native-or-unclassified-binary'
    }
    if ($RelativePath -match '(?i)^(Assets|Resources|Strings)/' -or $extension -in '.png', '.jpg', '.ico', '.svg', '.xbf', '.pri', '.wav', '.mp3') { return 'own-resource-or-asset' }
    if ($extension -in '.json', '.config', '.txt', '.md') { return 'configuration-or-metadata' }
    return 'other-unclassified'
}

$files = Get-ChildItem -LiteralPath $publishRoot -Recurse -File | Sort-Object { $_.FullName.Substring($publishRoot.Length + 1).Replace('\', '/') }
$fileEntries = foreach ($file in $files) {
    $relative = $file.FullName.Substring($publishRoot.Length + 1).Replace('\', '/')
    [pscustomobject]@{ Path = $relative; Sha256 = (Get-Sha256 $file.FullName); Size = $file.Length; Category = (Get-Category $relative) }
}

# --- SPDX 2.3 document -------------------------------------------------------------------------------
$archiveName = Split-Path -Leaf $Archive
$archiveSha = Get-Sha256 $Archive
$rootId = 'SPDXRef-Package-MOBAflow-win-x64'
$archiveId = 'SPDXRef-Archive'

$spdxFiles = New-Object System.Collections.Generic.List[object]
$spdxFiles.Add([ordered]@{
        SPDXID = $archiveId
        fileName = $archiveName
        checksums = @([ordered]@{ algorithm = 'SHA256'; checksumValue = $archiveSha })
        licenseConcluded = 'NOASSERTION'
        copyrightText = 'NOASSERTION'
        comment = 'Shipped release archive. Every file entry below is the content of this archive.'
    })

$index = 0
$relationships = New-Object System.Collections.Generic.List[object]
$relationships.Add([ordered]@{ spdxElementId = 'SPDXRef-DOCUMENT'; relationshipType = 'DESCRIBES'; relatedSpdxElement = $archiveId })
$relationships.Add([ordered]@{ spdxElementId = 'SPDXRef-DOCUMENT'; relationshipType = 'DESCRIBES'; relatedSpdxElement = $rootId })
$relationships.Add([ordered]@{ spdxElementId = $archiveId; relationshipType = 'CONTAINS'; relatedSpdxElement = $rootId })
foreach ($entry in $fileEntries) {
    $index++
    $id = 'SPDXRef-File-' + $index
    $spdxFiles.Add([ordered]@{
            SPDXID = $id
            fileName = './' + $entry.Path
            checksums = @([ordered]@{ algorithm = 'SHA256'; checksumValue = $entry.Sha256 })
            licenseConcluded = 'NOASSERTION'
            copyrightText = 'NOASSERTION'
            comment = "category=$($entry.Category)"
        })
    $relationships.Add([ordered]@{ spdxElementId = $rootId; relationshipType = 'CONTAINS'; relatedSpdxElement = $id })
}

$spdxPackages = New-Object System.Collections.Generic.List[object]
$spdxPackages.Add([ordered]@{
        SPDXID = $rootId
        name = 'MOBAflow-win-x64'
        versionInfo = $Tag
        downloadLocation = "https://github.com/$Repository/releases/tag/$Tag"
        filesAnalyzed = $false
        supplier = 'NOASSERTION'
        licenseConcluded = 'NOASSERTION'
        licenseDeclared = 'NOASSERTION'
        copyrightText = 'NOASSERTION'
        sourceInfo = "Built from commit $Commit of $Repository"
        comment = 'Self-contained win-x64 MOBAflow desktop application with bundled MOBApi.'
    })
foreach ($package in ($packages.Values | Sort-Object Name, Version)) {
    $id = ConvertTo-SpdxId "Package-NuGet-$($package.Name)-$($package.Version)"
    $entry = [ordered]@{
        SPDXID = $id
        name = $package.Name
        versionInfo = $package.Version
        downloadLocation = "https://www.nuget.org/packages/$($package.Name)/$($package.Version)"
        filesAnalyzed = $false
        supplier = 'NOASSERTION'
        licenseConcluded = 'NOASSERTION'
        licenseDeclared = 'NOASSERTION'
        copyrightText = 'NOASSERTION'
        externalRefs = @([ordered]@{ referenceCategory = 'PACKAGE-MANAGER'; referenceType = 'purl'; referenceLocator = "pkg:nuget/$($package.Name)@$($package.Version)" })
    }
    if ($package.Sha512) { $entry['checksums'] = @([ordered]@{ algorithm = 'SHA512'; checksumValue = $package.Sha512 }) }
    $spdxPackages.Add($entry)
    $relationships.Add([ordered]@{ spdxElementId = $rootId; relationshipType = 'DEPENDS_ON'; relatedSpdxElement = $id })
}

$creationComment = "tag=$Tag; commit=$Commit; repository=$Repository; archive=$archiveName; archiveSha256=$archiveSha"
if ($RunUrl) { $creationComment += "; run=$RunUrl" }

$document = [ordered]@{
    spdxVersion = 'SPDX-2.3'
    dataLicense = 'CC0-1.0'
    SPDXID = 'SPDXRef-DOCUMENT'
    name = "MOBAflow-$Tag-win-x64"
    documentNamespace = "https://github.com/$Repository/spdx/$Tag/$archiveSha"
    creationInfo = [ordered]@{
        created = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
        creators = @('Tool: MOBAflow-New-ReleaseSbom')
        comment = $creationComment
    }
    packages = $spdxPackages.ToArray()
    files = $spdxFiles.ToArray()
    relationships = $relationships.ToArray()
}

$outputFull = [IO.Path]::GetFullPath($OutputPath)
[void][IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($outputFull))
$text = ($document | ConvertTo-Json -Depth 20) + "`n"
[IO.File]::WriteAllText($outputFull, $text, (New-Object System.Text.UTF8Encoding($false)))

# --- Coverage report: shipped surface vs. SBOM, with explicit gaps and owners ----------------------
$byCategory = $fileEntries | Group-Object Category | Sort-Object Name | ForEach-Object { [ordered]@{ category = $_.Name; files = $_.Count } }
$gapOwners = @{
    'native-or-unclassified-binary' = 'Binary matches no NuGet package or known runtime prefix. Owner: release maintainer; check: identify the origin of each listed file before publishing.'
    'other-unclassified' = 'File type has no classification rule. Owner: release maintainer; check: review the listed files.'
}
$gaps = foreach ($category in $gapOwners.Keys | Sort-Object) {
    $paths = @($fileEntries | Where-Object Category -eq $category | ForEach-Object Path)
    if ($paths.Count -gt 0) { [ordered]@{ category = $category; count = $paths.Count; ownerAndRemainingCheck = $gapOwners[$category]; files = $paths } }
}
$coverage = [ordered]@{
    tag = $Tag
    commit = $Commit
    archive = $archiveName
    archiveSha256 = $archiveSha
    sbomFile = (Split-Path -Leaf $outputFull)
    sbomSha256 = (Get-Sha256 $outputFull)
    shippedFiles = @($fileEntries).Count
    sbomFileEntries = $spdxFiles.Count - 1
    nugetPackages = $packages.Count
    categories = @($byCategory)
    gaps = @($gaps)
    notes = @(
        'Licenses are NOASSERTION: this SBOM inventories components, it does not conclude licenses.',
        'NuGet packages come from restore assets; the file-to-package mapping is by file name only.',
        'Provenance and an SBOM prove origin and content, not product readiness or the absence of vulnerabilities.'
    )
}
if ($CoverageOutputPath) {
    $coverageFull = [IO.Path]::GetFullPath($CoverageOutputPath)
    [void][IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($coverageFull))
    [IO.File]::WriteAllText($coverageFull, (($coverage | ConvertTo-Json -Depth 20) + "`n"), (New-Object System.Text.UTF8Encoding($false)))
}

Write-Host "SBOM written: $($fileEntries.Count) files, $($packages.Count) NuGet packages, $(@($gaps).Count) gap categories."
