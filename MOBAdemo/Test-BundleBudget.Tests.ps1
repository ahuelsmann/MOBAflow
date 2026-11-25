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
    if (-not [string]::IsNullOrEmpty($IndexHtml)) {
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

    # The default limit is 200,000 bytes: a 300 KB incompressible script (about 225 KB compressed) fails, 150 KB (about 112 KB) passes.
    $oversized = New-WebRoot 'oversized' $index @{
        'assets/app-1.js' = New-IncompressibleText 300000 1
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
        'assets/pages/signal-box-2.js' = New-IncompressibleText 300000 3
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
    Assert-Result 'index.html without a script tag fails' (Invoke-Budget @('-Path', $noScripts)) 1 'entry point'
    $extensionlessEntry = New-WebRoot 'extensionless-entry' '<html><body><script src="./assets/loader"></script></body></html>' @{
        'assets/loader' = New-IncompressibleText 300000 12
        'assets/small.js' = 'console.log(1);'
    }
    Assert-Result 'oversized extensionless entry script is measured' (Invoke-Budget @('-Path', $extensionlessEntry)) 1 'exceeds'
    $dataSrc = New-WebRoot 'data-src' '<html><body><script data-src="./assets/app-1.js"></script></body></html>' @{ 'assets/app-1.js' = 'console.log(1);' }
    Assert-Result 'data-src is not a script entry point' (Invoke-Budget @('-Path', $dataSrc)) 1 'entry point'
    $commented = New-WebRoot 'commented' '<html><body><!-- <script src="./assets/app-1.js"></script> --></body></html>' @{ 'assets/app-1.js' = 'console.log(1);' }
    Assert-Result 'script tag inside an HTML comment is not an entry point' (Invoke-Budget @('-Path', $commented)) 1 'entry point'
    $external = New-WebRoot 'external' '<html><body><script src="https://cdn.example.test/lib.js"></script></body></html>' @{ 'assets/app-1.js' = 'console.log("demo");' }
    Assert-Result 'external script fails' (Invoke-Budget @('-Path', $external)) 1 'external'
    $outside = New-WebRoot 'outside' '<html><body><script src="../secret.js"></script></body></html>' @{ 'assets/app-1.js' = 'console.log("demo");' }
    Assert-Result 'reference leaving the web root fails' (Invoke-Budget @('-Path', $outside)) 1 'outside'
    Assert-Result 'non-positive limit fails' (Invoke-Budget @('-Path', $small, '-LimitBytes', 0)) 1 'LimitBytes'

    # Entry point and tag parsing: unquoted attributes count, an orphan asset without a script tag does not pass.
    $unquoted = New-WebRoot 'unquoted' '<html><head><link rel=stylesheet href=./assets/app-1.css></head><body><script type=module src=./assets/app-1.js></script></body></html>' @{
        'assets/app-1.js' = New-IncompressibleText 300000 8
        'assets/app-1.css' = 'body{margin:0}'
    }
    Assert-Result 'unquoted references are measured' (Invoke-Budget @('-Path', $unquoted)) 1 'exceeds'
    $noEntry = New-WebRoot 'no-entry' '<html><body>no script tag</body></html>' @{ 'assets/orphan.js' = 'console.log("demo");' }
    Assert-Result 'orphan asset without a script entry point fails' (Invoke-Budget @('-Path', $noEntry)) 1 'entry point'
    $extensionless = New-WebRoot 'extensionless' '<html><body><script src="./assets/app-1.js"></script><script src="./assets/loader"></script></body></html>' @{ 'assets/app-1.js' = 'console.log("demo");' }
    Assert-Result 'extensionless script reference that is missing fails' (Invoke-Budget @('-Path', $extensionless)) 1 'loader'

    # Hidden assets count.
    $hidden = New-WebRoot 'hidden' $index @{
        'assets/app-1.js' = 'console.log("demo");' * 50
        'assets/app-1.css' = 'body{margin:0}' * 50
        'assets/.hidden-chunk.js' = New-IncompressibleText 300000 9
    }
    Assert-Result 'hidden chunk counts toward the budget' (Invoke-Budget @('-Path', $hidden)) 1 'exceeds'

    # A symbolic link in the web root is refused (skipped when the platform does not allow creating one).
    $linked = New-WebRoot 'linked' $index @{
        'assets/app-1.js' = 'console.log("demo");'
        'assets/app-1.css' = 'body{margin:0}'
    }
    $outsideFile = Join-Path $workRoot 'outside-target.js'
    Set-Content -LiteralPath $outsideFile -Value 'console.log("outside");' -Encoding utf8NoBOM
    $linkCreated = $false
    try {
        New-Item -ItemType SymbolicLink -Path (Join-Path $linked 'assets/linked.js') -Target $outsideFile -ErrorAction Stop | Out-Null
        $linkCreated = $true
    }
    catch {
        Write-Host "Symbolic link case skipped: $($_.Exception.Message)"
    }
    if ($linkCreated) {
        Assert-Result 'symbolic link in the web root fails' (Invoke-Budget @('-Path', $linked)) 1 'symbolic link'
    }

    # Exact boundary with the default limit: build a script whose Brotli size is exactly 200,000 bytes, then one more.
    function Get-BrotliLength([byte[]] $Bytes) {
        $buffer = [IO.MemoryStream]::new()
        $brotli = [IO.Compression.BrotliStream]::new($buffer, [IO.Compression.CompressionLevel]::SmallestSize, $true)
        $brotli.Write($Bytes, 0, $Bytes.Length)
        $brotli.Dispose()
        $length = $buffer.Length
        $buffer.Dispose()
        return $length
    }

    function Find-ScriptBytes([long] $TargetSize) {
        $random = [Random]::new(11)
        $pool = [byte[]]::new(260000)
        $random.NextBytes($pool)
        $length = [int] $TargetSize - 10
        for ($attempt = 0; $attempt -lt 60; $attempt++) {
            $candidate = [byte[]]::new($length)
            [Array]::Copy($pool, $candidate, $length)
            $size = Get-BrotliLength $candidate
            if ($size -eq $TargetSize) {
                return $candidate
            }

            $length += [int] ($TargetSize - $size)
        }

        throw "Could not build a script with a Brotli size of exactly $TargetSize bytes."
    }

    foreach ($case in @(@{ Size = 200000; ExitCode = 0; Text = 'within' }, @{ Size = 200001; ExitCode = 1; Text = 'exceeds' })) {
        $boundary = New-WebRoot "boundary-$($case.Size)" '<html><body><script src="./assets/app-1.js"></script></body></html>' @{}
        New-Item -ItemType Directory -Path (Join-Path $boundary 'assets') -Force | Out-Null
        [IO.File]::WriteAllBytes((Join-Path $boundary 'assets/app-1.js'), (Find-ScriptBytes $case.Size))
        Assert-Result "default limit with exactly $($case.Size) Brotli bytes" (Invoke-Budget @('-Path', $boundary)) $case.ExitCode $case.Text
    }
}
finally {
    Remove-Item -LiteralPath $workRoot -Recurse -Force -ErrorAction SilentlyContinue
}

# The failing child runs leave $LASTEXITCODE at 1; a CI step that dot-sources this file would report it.
$global:LASTEXITCODE = 0
Write-Host "MOBAdemo bundle budget self-tests passed ($checks checks)."
