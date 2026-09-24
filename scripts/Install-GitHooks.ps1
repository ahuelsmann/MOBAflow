[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$commonDirectory = git -C $repositoryRoot rev-parse --path-format=absolute --git-common-dir
if ($LASTEXITCODE -ne 0) { throw 'Cannot find the Git common directory.' }
$hooksDirectory = Join-Path $commonDirectory 'hooks'
$configured = git -C $repositoryRoot config --get core.hooksPath
if ($LASTEXITCODE -notin @(0, 1)) { throw 'Cannot read core.hooksPath.' }
if ($configured -and $configured -ne '.git/hooks' -and $configured -ne $hooksDirectory) {
    throw "Existing custom core.hooksPath must be integrated manually: $configured"
}
$destination = Join-Path $hooksDirectory 'pre-commit'
$fallback = Join-Path $hooksDirectory 'mobaflow-line-endings.ps1'
if ((Test-Path -LiteralPath $fallback) -and
    -not ([IO.File]::ReadAllText($fallback)).Contains('# MOBAflow managed line-ending checker')) {
    throw "Existing hook support file must be integrated manually: $fallback"
}
if (Test-Path -LiteralPath $destination) {
    $existing = [IO.File]::ReadAllText($destination)
    if (-not $existing.Contains('# MOBAflow managed line-ending hook')) {
        throw "Existing pre-commit hook must be integrated manually: $destination"
    }
}
# Keep all other hooks. An absolute path also works in linked worktrees, where .git is a file.
[void] [IO.Directory]::CreateDirectory($hooksDirectory)
Copy-Item -LiteralPath (Join-Path $repositoryRoot '.githooks/pre-commit') -Destination $destination
Copy-Item -LiteralPath (Join-Path $repositoryRoot 'scripts/Test-LineEndings.ps1') -Destination $fallback
if (-not $IsWindows) {
    & chmod +x $destination
    if ($LASTEXITCODE -ne 0) { throw 'Could not make the hook executable.' }
}
git -C $repositoryRoot config --local core.hooksPath $hooksDirectory
if ($LASTEXITCODE -ne 0) { throw 'Could not configure core.hooksPath.' }
Write-Host "Installed line-ending pre-commit hook: $destination"
