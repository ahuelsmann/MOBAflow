# MOBAflow managed line-ending checker
[CmdletBinding()]
param(
    [string] $RepositoryRoot = (Split-Path -Parent $PSScriptRoot),
    [string[]] $Path = @(),
    [switch] $Staged,
    [switch] $Fix
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
if ($Staged -and $Fix) {
    throw 'Fix files explicitly, review them, and stage them again; -Staged cannot be combined with -Fix.'
}
if ($Staged -and $Path.Count -gt 0) {
    throw 'Choose either -Staged or -Path.'
}
$RepositoryRoot = (Resolve-Path -LiteralPath $RepositoryRoot).Path

function Invoke-GitText([string[]] $Arguments) {
    $start = [Diagnostics.ProcessStartInfo]::new('git')
    $start.WorkingDirectory = $RepositoryRoot
    $start.UseShellExecute = $false
    $start.RedirectStandardOutput = $true
    $start.StandardOutputEncoding = [Text.Encoding]::UTF8
    foreach ($argument in $Arguments) { $start.ArgumentList.Add($argument) }
    $process = [Diagnostics.Process]::Start($start)
    try {
        $output = $process.StandardOutput.ReadToEnd()
        $process.WaitForExit()
        if ($process.ExitCode -ne 0) { throw "git $($Arguments[0]) failed: $($process.ExitCode)" }
        return $output
    }
    finally { $process.Dispose() }
}

# NUL-delimited Git output preserves spaces, tabs, Unicode and newlines in filenames.
$selection = $null
if ($Staged) {
    $selection = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $names = Invoke-GitText @('diff', '--cached', '--name-only', '--diff-filter=ACMRT', '-z')
    foreach ($name in $names.Split([char]0, [StringSplitOptions]::RemoveEmptyEntries)) {
        [void] $selection.Add($name)
    }
}
$arguments = @('ls-files', '--cached', '--others', '--exclude-standard', '--eol', '-z', '--') + $Path
$entries = Invoke-GitText $arguments
$failures = [Collections.Generic.List[string]]::new()
$checked = 0
$fixed = 0
foreach ($entry in $entries.Split([char]0, [StringSplitOptions]::RemoveEmptyEntries)) {
    if ($entry -notmatch '^i/(?<index>\S*)\s+w/(?<working>\S*)\s+attr/(?<attributes>[^\t]*)\t(?<name>[\s\S]+)$') {
        throw 'Unexpected git ls-files --eol output.'
    }
    $name = $Matches.name
    $working = $Matches.working
    $index = $Matches.index
    $attributes = $Matches.attributes
    if ($null -ne $selection -and -not $selection.Contains($name)) { continue }
    # Git classifies UTF-16 and other NUL-containing files as binary; never rewrite them.
    if ($attributes -match '(^|\s)-text(\s|$)' -or $working -eq '-text') { continue }
    $absolutePath = Join-Path $RepositoryRoot $name
    if (-not (Test-Path -LiteralPath $absolutePath -PathType Leaf)) { continue }
    if ((Get-Item -LiteralPath $absolutePath -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) { continue }
    $checked++
    if ($index -in @('mixed', 'cr') -and $Staged) {
        $failures.Add("$name (index: $index; normalize and stage again)")
    }
    $expected = if ($attributes -match '(^|\s)eol=(lf|crlf)(\s|$)') { $Matches[2] } else { 'crlf' }
    if ($working -in @('', 'none', $expected)) { continue }
    if (-not $Fix) {
        $failures.Add("$name (working directory: $working; expected $expected)")
        continue
    }
    # Latin-1 is a reversible byte mapping: only CR/LF bytes change, including for UTF-8 with a BOM.
    $bytes = [IO.File]::ReadAllBytes($absolutePath)
    $text = [Text.Encoding]::Latin1.GetString($bytes)
    $normalized = $text.Replace("`r`n", "`n").Replace("`r", "`n")
    if ($expected -eq 'crlf') { $normalized = $normalized.Replace("`n", "`r`n") }
    [IO.File]::WriteAllBytes($absolutePath, [Text.Encoding]::Latin1.GetBytes($normalized))
    $fixed++
    Write-Host "Normalized: $name ($expected)"
}
if ($Path.Count -gt 0 -and $checked -eq 0) {
    throw 'No text files matched -Path; check the paths and Git ignore rules.'
}
if ($failures.Count -gt 0) {
    foreach ($failure in $failures) { Write-Host "Invalid line endings: $failure" }
    Write-Host 'Run ./scripts/Test-LineEndings.ps1 -Path <paths> -Fix, review, and stage the files again.'
    exit 1
}
Write-Host "Line endings passed: $checked text files checked; $fixed normalized."
