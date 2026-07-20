[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$BundlePath
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $BundlePath -PathType Leaf)) {
    throw "Android App Bundle does not exist: $BundlePath"
}

$bundle = Get-Item -LiteralPath $BundlePath
if ($bundle.Length -eq 0) {
    throw "Android App Bundle is empty: $BundlePath"
}

Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [IO.Compression.ZipFile]::OpenRead($bundle.FullName)
try {
    $entryNames = @($archive.Entries | ForEach-Object FullName)
    $requiredEntries = @(
        'base/manifest/AndroidManifest.xml',
        'base/resources.pb',
        'base/dex/classes.dex'
    )

    foreach ($requiredEntry in $requiredEntries) {
        if ($requiredEntry -notin $entryNames) {
            throw "Android App Bundle is missing required entry: $requiredEntry"
        }
    }

    $actualAbis = @(
        $entryNames |
            ForEach-Object {
                if ($_ -match '^base/lib/([^/]+)/[^/]+\.so$') {
                    $Matches[1]
                }
            } |
            Sort-Object -Unique
    )
    $expectedAbis = @('arm64-v8a', 'x86_64')
    if ($actualAbis.Count -eq 0) {
        throw 'Android App Bundle contains no native ABI libraries.'
    }
    $abiDifference = @(Compare-Object -ReferenceObject $expectedAbis -DifferenceObject $actualAbis)
    if ($abiDifference.Count -ne 0) {
        throw "Android App Bundle ABIs must be exactly '$($expectedAbis -join ', ')'; found '$($actualAbis -join ', ')'."
    }
}
finally {
    $archive.Dispose()
}

Write-Host "Android App Bundle validation passed: $($bundle.FullName)"
