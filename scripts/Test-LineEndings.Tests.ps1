[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$sourceRoot = Split-Path -Parent $PSScriptRoot
$fixture = Join-Path ([IO.Path]::GetTempPath()) ('mobaflow-line-endings-' + [guid]::NewGuid().ToString('N'))
[void] [IO.Directory]::CreateDirectory($fixture)
$checks = 0

function Assert-True([bool] $Condition, [string] $Message) {
    if (-not $Condition) { throw $Message }
    $script:checks++
}

function Invoke-Process([string] $Program, [string[]] $Arguments) {
    $start = [Diagnostics.ProcessStartInfo]::new($Program)
    $start.WorkingDirectory = $fixture
    $start.UseShellExecute = $false
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    # Tests may themselves run from a Git hook; never reuse the parent's index or Git directory.
    foreach ($key in @('GIT_DIR', 'GIT_WORK_TREE', 'GIT_INDEX_FILE', 'GIT_COMMON_DIR')) {
        [void] $start.Environment.Remove($key)
    }
    foreach ($argument in $Arguments) { $start.ArgumentList.Add($argument) }
    $process = [Diagnostics.Process]::Start($start)
    try {
        $stdout = $process.StandardOutput.ReadToEndAsync()
        $stderr = $process.StandardError.ReadToEndAsync()
        $process.WaitForExit()
        return @{ Code = $process.ExitCode; Output = $stdout.Result + $stderr.Result }
    }
    finally { $process.Dispose() }
}

function Git([string[]] $Arguments) {
    $result = Invoke-Process 'git' $Arguments
    if ($result.Code -ne 0) { throw $result.Output }
    return $result.Output
}

function Check([string[]] $Arguments = @()) {
    return Invoke-Process 'pwsh' (@('-NoProfile', '-File', (Join-Path $fixture 'scripts/Test-LineEndings.ps1'),
        '-RepositoryRoot', $fixture) + $Arguments)
}

function Write-Text([string] $Name, [string] $Text) {
    [IO.File]::WriteAllText((Join-Path $fixture $Name), $Text, [Text.UTF8Encoding]::new($false))
}

try {
    $null = Git @('init', '--quiet')
    $null = Git @('config', 'core.autocrlf', 'true')
    $null = Git @('config', 'user.name', 'Line ending tests')
    $null = Git @('config', 'user.email', 'line-endings@example.invalid')
    [void] [IO.Directory]::CreateDirectory((Join-Path $fixture 'scripts'))
    [void] [IO.Directory]::CreateDirectory((Join-Path $fixture '.githooks'))
    foreach ($name in @('Test-LineEndings.ps1', 'Install-GitHooks.ps1')) {
        Copy-Item -LiteralPath (Join-Path $sourceRoot "scripts/$name") -Destination (Join-Path $fixture "scripts/$name")
    }
    Copy-Item -LiteralPath (Join-Path $sourceRoot '.githooks/pre-commit') -Destination (Join-Path $fixture '.githooks/pre-commit')
    Write-Text '.gitattributes' "* text=auto eol=crlf`r`n*.sh text eol=lf`r`n.githooks/* text eol=lf`r`n*.bin binary`r`n"
    Write-Text 'example space ü.cs' "first`r`nsecond`nlast"
    $result = Check
    Assert-True ($result.Code -eq 1 -and $result.Output.Contains('working directory: mixed')) 'Mixed working file was not rejected.'
    $result = Check @('-Fix')
    Assert-True ($result.Code -eq 0) $result.Output
    Assert-True ([IO.File]::ReadAllText((Join-Path $fixture 'example space ü.cs')) -ceq "first`r`nsecond`r`nlast") 'Fix changed content or added a final newline.'
    Write-Text 'script.sh' "first`r`nsecond`n"
    $bomBytes = [byte[]] (0xEF, 0xBB, 0xBF) + [Text.Encoding]::UTF8.GetBytes("Grüße`r`nnext`n")
    [IO.File]::WriteAllBytes((Join-Path $fixture 'bom.cs'), $bomBytes)
    $binaryBytes = [byte[]] (0, 13, 10, 10, 255)
    [IO.File]::WriteAllBytes((Join-Path $fixture 'asset.bin'), $binaryBytes)
    $result = Check @('-Fix')
    Assert-True ($result.Code -eq 0) $result.Output
    Assert-True ([IO.File]::ReadAllText((Join-Path $fixture 'script.sh')) -ceq "first`nsecond`n") 'LF exception was not preserved.'
    $expectedBom = [byte[]] (0xEF, 0xBB, 0xBF) + [Text.Encoding]::UTF8.GetBytes("Grüße`r`nnext`r`n")
    Assert-True ([Convert]::ToBase64String([IO.File]::ReadAllBytes((Join-Path $fixture 'bom.cs'))) -ceq [Convert]::ToBase64String($expectedBom)) 'BOM or Unicode bytes changed.'
    Assert-True ([Convert]::ToBase64String([IO.File]::ReadAllBytes((Join-Path $fixture 'asset.bin'))) -ceq [Convert]::ToBase64String($binaryBytes)) 'Binary bytes changed.'
    $before = (Get-FileHash -LiteralPath (Join-Path $fixture 'bom.cs')).Hash
    $result = Check @('-Fix')
    Assert-True ($result.Code -eq 0 -and (Get-FileHash -LiteralPath (Join-Path $fixture 'bom.cs')).Hash -eq $before) 'Repeated fix was not idempotent.'
    $result = Check @('-Path', 'missing.cs')
    Assert-True ($result.Code -ne 0) 'An unmatched explicit path passed.'

    # Git cleans the index, but leaves the mixed working copy untouched.
    Write-Text 'example space ü.cs' "first`r`nsecond`nlast"
    $null = Git @('add', '.')
    $eol = Git @('ls-files', '--eol', '--', 'example space ü.cs')
    Assert-True ($eol -match 'i/lf\s+w/mixed') 'Fixture did not reproduce clean index plus mixed working copy.'
    $result = Check @('-Staged')
    Assert-True ($result.Code -eq 1 -and $result.Output.Contains('working directory: mixed')) 'Staged check ignored the working copy.'
    $result = Check @('-Staged', '-Fix')
    Assert-True ($result.Code -ne 0) 'Hook mode unexpectedly allowed writes.'

    $hookPath = Join-Path $fixture '.git/hooks/pre-commit'
    Write-Text '.git/hooks/pre-commit' '# Existing user hook'
    $installArgs = @('-NoProfile', '-File', (Join-Path $fixture 'scripts/Install-GitHooks.ps1'))
    $result = Invoke-Process 'pwsh' $installArgs
    Assert-True ($result.Code -ne 0 -and [IO.File]::ReadAllText($hookPath) -ceq '# Existing user hook') 'Installer overwrote an existing hook.'
    Remove-Item -LiteralPath $hookPath
    $result = Invoke-Process 'pwsh' $installArgs
    Assert-True ($result.Code -eq 0) $result.Output
    $result = Invoke-Process 'git' @('commit', '-m', 'test: reject mixed working copy')
    Assert-True ($result.Code -ne 0 -and $result.Output.Contains('Invalid line endings:')) 'Git did not execute the installed hook.'
    $result = Check @('-Fix')
    Assert-True ($result.Code -eq 0) $result.Output
    $result = Invoke-Process 'git' @('commit', '-m', 'test: accept normalized working copy')
    Assert-True ($result.Code -eq 0) $result.Output

    # Preserve a deliberately malformed index blob even after the working file has been fixed.
    [IO.File]::AppendAllText((Join-Path $fixture '.gitattributes'), "raw.cs -text`r`n")
    Write-Text 'raw.cs' "first`r`nsecond`n"
    $null = Git @('add', 'raw.cs')
    Write-Text '.gitattributes' "* text=auto eol=crlf`r`n*.sh text eol=lf`r`n.githooks/* text eol=lf`r`n*.bin binary`r`n"
    $result = Check @('-Path', 'raw.cs', '-Fix')
    Assert-True ($result.Code -eq 0) $result.Output
    $result = Check @('-Staged')
    Assert-True ($result.Code -eq 1 -and $result.Output.Contains('index: mixed')) 'Malformed staged bytes were not rejected.'
    $null = Git @('add', 'raw.cs')
    $result = Check @('-Staged')
    Assert-True ($result.Code -eq 0) $result.Output
    $null = Git @('commit', '-m', 'test: add normalized file')
    $null = Git @('rm', 'raw.cs')
    $result = Check @('-Staged')
    Assert-True ($result.Code -eq 0) 'Staged deletion failed.'
    # The installed checker also protects older worktrees lacking the repository script.
    Remove-Item -LiteralPath (Join-Path $fixture 'scripts/Test-LineEndings.ps1')
    Write-Text 'example space ü.cs' "first`r`nsecond`nchanged"
    $null = Git @('add', 'example space ü.cs')
    $result = Invoke-Process 'git' @('commit', '-m', 'test: fallback checker rejects mixed working copy')
    Assert-True ($result.Code -ne 0 -and $result.Output.Contains('Invalid line endings:')) 'Installed fallback checker was not executed.'
    Write-Host "Line-ending regression tests passed: $checks assertions."
}
finally {
    # This exact GUID directory was created by this test; never delete a caller-supplied path.
    $resolved = [IO.Path]::GetFullPath($fixture)
    $temporaryRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
    if ($resolved.StartsWith($temporaryRoot, [StringComparison]::OrdinalIgnoreCase) -and
        [IO.Path]::GetFileName($resolved) -match '^mobaflow-line-endings-[a-f0-9]{32}$') {
        Remove-Item -LiteralPath $resolved -Recurse -Force
    }
}
