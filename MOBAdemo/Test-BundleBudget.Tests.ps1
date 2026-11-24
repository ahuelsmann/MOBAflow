#requires -Version 7.0
<#
.SYNOPSIS
    Self-tests of MOBAdemo/Test-BundleBudget.ps1 on synthetic web roots (issue #430, FR-076). Runs in the MOBAdemo CI job.
#>
[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$budgetScript = Join-Path $PSScriptRoot 'Test-BundleBudget.ps1'
$pwsh = (Get-Process -Id $PID).Path
$workRoot = Join-Path ([IO.Path]::GetTempPath()) ("mobademo-budget-" + [Guid]::NewGuid().ToString('N'))
$checks = 0

function New-IncompressibleText([int] $Bytes, [int] $Seed) {
    # Base64 of seeded random bytes: Brotli cannot shrink it below about three quarters of its length.
    $random = [Random]::new($Seed)
    $buffer = [byte[]]::new([int] [Math]::Ceiling($Bytes * 3 / 4))
    $random.NextBytes($buffer)
    return '/*' + [Convert]::ToBase64String($buffer).Substring(0, $Bytes - 4) + '*/'
}

function New-WebRoot([string] $Name, [string] $IndexHtml, [hashtable] $Files) {
    $root = Join-Path $workRoot $Name
    New-Item -ItemType Directory -Path $root -Force | Out-Null
    if ($null -ne $IndexHtml) {
        Set-Content -LiteralPath (Join-Path $root 'index.html') -Value $IndexHtml -Encoding utf8NoBOM
    }
    foreach ($relativePath in $Files.Keys) {
        $target = Join-Path $root $relativePath
        New-Item -ItemType Directory -Path (Split-Path -Parent $target) -Force | Out-Null
        Set-Content -LiteralPath $target -Value $Files[$relativePath] -Encoding utf8NoBOM -NoNewline
    }

    return $root
}

function Invoke-Budget([string[]] $Arguments) {
    $output = & $pwsh -NoProfile -NonInteractive -File $budgetScript @Arguments 2>&1 | Out-String
    return [pscustomobject] @{ ExitCode = $LASTEXITCODE; Output = $output }
}

function Get-TotalBytes([string] $Output) {
    if ($Output -notmatch 'Total Brotli bytes: (\d+)') {
        throw "No total in the budget output: $Output"
    }

    return [long] $Matches[1]
}

function Assert-Result([string] $Scenario, $Result, [int] $ExpectedExitCode, [string] $ExpectedText) {
    if ($Result.ExitCode -ne $ExpectedExitCode) {
        throw "Scenario '$Scenario': expected exit code $ExpectedExitCode but got $($Result.ExitCode). Output: $($Result.Output)"
    }
    if (-not [string]::IsNullOrEmpty($ExpectedText) -and $Result.Output -notmatch [regex]::Escape($ExpectedText)) {
        throw "Scenario '$Scenario': expected the text '$ExpectedText'. Output: $($Result.Output)"
    }

    $script:checks++
}

$index = '<!doctype html><html><head><link rel="stylesheet" href="./assets/app-1.css"></head><body><script type="module" src="./assets/app-1.js"></script></body></html>'

try {
    $small = New-WebRoot 'small' $index @{
        'assets/app-1.js' = 'console.log("demo");' * 50
        'assets/app-1.css' = 'body{margin:0}' * 50
    }
    $smallResult = Invoke-Budget @('-Path', $small)
    Assert-Result 'small bundle passes the default limit' $smallResult 0 'within'
    $total = Get-TotalBytes $smallResult.Output
    if ($total -le 0) {
        throw 'The synthetic bundle must have a positive Brotli size.'
    }

    # Boundary: exactly the limit passes, one byte less fails.
    Assert-Result 'total equal to the limit passes' (Invoke-Budget @('-Path', $small, '-LimitBytes', $total)) 0 'within'
    Assert-Result 'total one byte over the limit fails' (Invoke-Budget @('-Path', $small, '-LimitBytes', ($total - 1))) 1 'exceeds'

    # The default limit is 200,000 bytes: a 250 KB incompressible script fails, 150 KB passes.
    $oversized = New-WebRoot 'oversized' $index @{
        'assets/app-1.js' = New-IncompressibleText 250000 1
        'assets/app-1.css' = 'body{margin:0}'
    }
    Assert-Result 'oversized bundle fails' (Invoke-Budget @('-Path', $oversized)) 1 'exceeds'
    $large = New-WebRoot 'large' $index @{
        'assets/app-1.js' = New-IncompressibleText 150000 2
        'assets/app-1.css' = 'body{margin:0}'
    }
    Assert-Result 'bundle under the default limit passes' (Invoke-Budget @('-Path', $large)) 0 'within'

    # A lazy chunk that index.html does not reference still counts, and so do nested folders.
    $lazy = New-WebRoot 'lazy' $index @{
        'assets/app-1.js' = 'console.log("demo");' * 50
        'assets/app-1.css' = 'body{margin:0}' * 50
        'assets/pages/signal-box-2.js' = New-IncompressibleText 250000 3
    }
    Assert-Result 'lazy chunk counts toward the budget' (Invoke-Budget @('-Path', $lazy)) 1 'exceeds'

    # Precompressed siblings and other files are not scripts or style sheets.
    $siblings = New-WebRoot 'siblings' $index @{
        'assets/app-1.js' = 'console.log("demo");' * 50
        'assets/app-1.css' = 'body{margin:0}' * 50
        'assets/app-1.js.br' = New-IncompressibleText 250000 4
        'assets/app-1.js.gz' = New-IncompressibleText 250000 5
        'assets/photo.svg' = New-IncompressibleText 250000 6
    }
    $siblingsResult = Invoke-Budget @('-Path', $siblings)
    Assert-Result 'precompressed siblings and images are ignored' $siblingsResult 0 'within'
    if ((Get-TotalBytes $siblingsResult.Output) -ne $total) {
        throw 'Siblings must not change the total.'
    }

    # Missing inputs fail instead of passing an empty measurement.
    Assert-Result 'missing folder fails' (Invoke-Budget @('-Path', (Join-Path $workRoot 'does-not-exist'))) 1 'does not exist'
    $noIndex = New-WebRoot 'no-index' $null @{ 'assets/app-1.js' = 'console.log("demo");' }
    Assert-Result 'missing index.html fails' (Invoke-Budget @('-Path', $noIndex)) 1 'index.html'
    $missingRef = New-WebRoot 'missing-reference' $index @{ 'assets/app-1.js' = 'console.log("demo");' }
    Assert-Result 'referenced style sheet that is missing fails' (Invoke-Budget @('-Path', $missingRef)) 1 'app-1.css'
    $noScripts = New-WebRoot 'no-scripts' '<!doctype html><html><body>x</body></html>' @{ 'assets/readme.txt' = 'x' }
    Assert-Result 'no script or style sheet fails' (Invoke-Budget @('-Path', $noScripts)) 1 'No script or style sheet'
    $external = New-WebRoot 'external' '<html><body><script src="https://cdn.example.test/lib.js"></script></body></html>' @{ 'assets/app-1.js' = 'console.log("demo");' }
    Assert-Result 'external script fails' (Invoke-Budget @('-Path', $external)) 1 'external'
    $outside = New-WebRoot 'outside' '<html><body><script src="../secret.js"></script></body></html>' @{ 'assets/app-1.js' = 'console.log("demo");' }
    Assert-Result 'reference leaving the web root fails' (Invoke-Budget @('-Path', $outside)) 1 'outside'
    Assert-Result 'non-positive limit fails' (Invoke-Budget @('-Path', $small, '-LimitBytes', 0)) 1 'LimitBytes'
}
finally {
    Remove-Item -LiteralPath $workRoot -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host "MOBAdemo bundle budget self-tests passed ($checks checks)."
