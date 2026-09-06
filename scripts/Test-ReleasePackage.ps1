[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$PublishDirectory,

    [string]$Archive,

    [string]$ChecksumFile
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $PublishDirectory -PathType Container)) {
    throw "Publish directory does not exist: $PublishDirectory"
}

$requiredFiles = @(
    'MOBAflow.exe',
    'appsettings.json',
    'MOBApi/MOBApi.exe'
)

foreach ($relativePath in $requiredFiles) {
    $path = Join-Path $PublishDirectory $relativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required release file is missing: $relativePath"
    }
}

$settingsPath = Join-Path $PublishDirectory 'appsettings.json'
$settings = Get-Content -LiteralPath $settingsPath -Raw | ConvertFrom-Json
$sensitiveValues = @(
    $settings.Z21.CurrentIpAddress,
    $settings.Speech.PiperExecutablePath,
    $settings.Speech.PiperModelPath,
    $settings.Speech.PiperConfigPath,
    $settings.Application.LastSolutionPath,
    $settings.Application.PhotoStoragePath
)

if ($sensitiveValues | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) }) {
    throw 'Release appsettings.json contains a machine-local address or path.'
}

$localPathPattern = '(?i)([a-z]:\\Users\\|/home/[^/]+/|/Users/[^/]+/)'
$leaks = Get-ChildItem -LiteralPath $PublishDirectory -Recurse -File -Include *.json,*.config,*.txt |
    Select-String -Pattern $localPathPattern
if ($leaks) {
    throw "Release files contain machine-local paths: $($leaks.Path -join ', ')"
}

if ($Archive) {
    if (-not (Test-Path -LiteralPath $Archive -PathType Leaf)) {
        throw "Release archive does not exist: $Archive"
    }
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $zip = [IO.Compression.ZipFile]::OpenRead((Resolve-Path -LiteralPath $Archive))
    try {
        foreach ($relativePath in $requiredFiles) {
            $entryName = $relativePath.Replace('\\', '/')
            if (-not ($zip.Entries | Where-Object FullName -eq $entryName)) {
                throw "Release archive is missing: $entryName"
            }
        }
    }
    finally {
        $zip.Dispose()
    }
}

if ($Archive -and $ChecksumFile) {
    if (-not (Test-Path -LiteralPath $ChecksumFile -PathType Leaf)) {
        throw "Checksum file does not exist: $ChecksumFile"
    }
    $expected = ((Get-Content -LiteralPath $ChecksumFile -Raw).Trim() -split '\s+')[0]
    $actual = (Get-FileHash -LiteralPath $Archive -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($expected.ToLowerInvariant() -ne $actual) {
        throw 'SHA-256 checksum does not match the release archive.'
    }
}

Write-Host 'Release package validation passed.'
