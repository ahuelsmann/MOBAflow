[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$generator = Join-Path $PSScriptRoot 'New-ReleaseSbom.ps1'
$verifier = Join-Path $PSScriptRoot 'Test-ReleaseSbom.ps1'
$work = Join-Path ([IO.Path]::GetTempPath()) ('mobaflow-sbom-' + [guid]::NewGuid().ToString('N'))
$checks = 0
$tag = '9.9.9-test.1'
$commit = 'a' * 40

function Assert-Fails([scriptblock] $Action, [string] $Expected, [string] $Scenario) {
    try { & $Action | Out-Null } catch {
        if ($_.Exception.Message -notlike "*$Expected*") { throw "Unexpected failure for '$Scenario': $($_.Exception.Message)" }
        $script:checks++
        return
    }
    throw "Expected failure for '$Scenario', but it passed."
}

function Assert-True([bool] $Condition, [string] $Scenario) {
    if (-not $Condition) { throw "Assertion failed: $Scenario" }
    $script:checks++
}

try {
    $publish = Join-Path $work 'publish'
    [void][IO.Directory]::CreateDirectory((Join-Path $publish 'MOBApi'))
    [IO.File]::WriteAllText((Join-Path $publish 'MOBAflow.exe'), 'exe')
    [IO.File]::WriteAllText((Join-Path $publish 'MOBAflow.dll'), 'own')
    [IO.File]::WriteAllText((Join-Path $publish 'Newtonsoft.Json.dll'), 'nuget')
    [IO.File]::WriteAllText((Join-Path $publish 'System.Private.CoreLib.dll'), 'runtime')
    [IO.File]::WriteAllText((Join-Path $publish 'mystery.dll'), 'native')
    [IO.File]::WriteAllText((Join-Path $publish 'appsettings.json'), '{}')
    [IO.File]::WriteAllText((Join-Path $publish 'MOBApi/MOBApi.exe'), 'api')

    $assets = Join-Path $work 'project.assets.json'
    $sha512 = [Convert]::ToBase64String((1..64 | ForEach-Object { [byte]$_ }))
    @{ libraries = @{ 'Newtonsoft.Json/13.0.3' = @{ type = 'package'; sha512 = $sha512 }; 'MOBAflow.Domain/1.0.0' = @{ type = 'project' } } } |
        ConvertTo-Json -Depth 5 | Set-Content $assets

    $archive = Join-Path $work "MOBAflow-$tag-win-x64.zip"
    Compress-Archive -Path (Join-Path $publish '*') -DestinationPath $archive
    $sbom = Join-Path $work "MOBAflow-$tag-win-x64.spdx.json"
    $coverage = Join-Path $work 'coverage.json'

    & $generator -PublishDirectory $publish -Archive $archive -Tag $tag -Commit $commit -Repository 'owner/repo' `
        -RunUrl 'https://github.com/owner/repo/actions/runs/1/attempts/1' -AssetsFile $assets -OutputPath $sbom -CoverageOutputPath $coverage 6> $null
    $document = Get-Content $sbom -Raw | ConvertFrom-Json
    $report = Get-Content $coverage -Raw | ConvertFrom-Json

    Assert-True ($document.spdxVersion -eq 'SPDX-2.3') 'SPDX 2.3 document'
    Assert-True (@($document.files).Count -eq 8) 'archive plus seven shipped files are listed'
    Assert-True (@($document.packages | Where-Object name -eq 'Newtonsoft.Json').Count -eq 1) 'NuGet package is listed'
    Assert-True (@($document.packages | Where-Object name -eq 'MOBAflow.Domain').Count -eq 0) 'project references are not third-party packages'
    Assert-True ($report.shippedFiles -eq 7) 'coverage counts shipped files'
    Assert-True (@($report.gaps | Where-Object category -eq 'native-or-unclassified-binary').Count -eq 1) 'unknown binary is reported as an explicit gap'
    Assert-True ((Get-Content $sbom -Raw) -notmatch '(?i)[a-z]:\\') 'no local paths in SBOM'

    $sums = Join-Path $work 'SHA256SUMS.txt'
    @($archive, $sbom) | ForEach-Object { "$((Get-FileHash $_ -Algorithm SHA256).Hash.ToLowerInvariant())  $(Split-Path -Leaf $_)" } | Set-Content $sums
    & $verifier -Sbom $sbom -Archive $archive -Tag $tag -Commit $commit -ChecksumFile $sums 6> $null
    $checks++

    Assert-Fails { & $verifier -Sbom $sbom -Archive $archive -Tag '0.0.1' -Commit $commit 6> $null } 'expected tag' 'wrong tag binding'
    Assert-Fails { & $verifier -Sbom $sbom -Archive $archive -Tag $tag -Commit ('b' * 40) 6> $null } 'expected commit' 'wrong commit binding'

    [void][IO.Directory]::CreateDirectory((Join-Path $work 'bad')); $tampered = Join-Path $work "bad/MOBAflow-$tag-win-x64.zip"
    Copy-Item $archive $tampered
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $zip = [IO.Compression.ZipFile]::Open($tampered, 'Update')
    try { @($zip.Entries) | Where-Object FullName -eq 'MOBAflow.dll' | ForEach-Object { $_.Delete() }; $e = $zip.CreateEntry('MOBAflow.dll'); $w = New-Object IO.StreamWriter($e.Open()); $w.Write('evil'); $w.Dispose() }
    finally { $zip.Dispose() }
    Assert-Fails { & $verifier -Sbom $sbom -Archive $tampered -Tag $tag -Commit $commit 6> $null } 'archive digest' 'tampered archive'

    $badSums = Join-Path $work 'bad-sums.txt'
    "$((Get-FileHash $archive -Algorithm SHA256).Hash.ToLowerInvariant())  $(Split-Path -Leaf $archive)" | Set-Content $badSums
    Assert-Fails { & $verifier -Sbom $sbom -Archive $archive -Tag $tag -Commit $commit -ChecksumFile $badSums 6> $null } 'SHA256SUMS.txt has no matching line' 'checksum file without SBOM'

    Write-Host "Release SBOM self-tests passed ($checks checks)."
}
finally {
    Remove-Item -LiteralPath $work -Recurse -Force -ErrorAction SilentlyContinue
}
