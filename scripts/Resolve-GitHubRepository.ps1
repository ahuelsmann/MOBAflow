[CmdletBinding()]
param([string] $RepositoryRoot = (Get-Location).Path)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-GitValue([string[]] $Arguments, [switch] $Optional) {
    $value = & git -C $RepositoryRoot @Arguments 2>$null
    if ($LASTEXITCODE -ne 0) {
        if ($Optional) { return $null }
        throw 'Unable to inspect the active Git repository.'
    }
    return ($value -join "`n").Trim()
}

function Get-GitHubTarget([string] $Remote) {
    $url = Get-GitValue @('remote', 'get-url', $Remote)
    $pattern = '^(?:https://github\.com/|git@github\.com:|ssh://git@github\.com/)(?<owner>[A-Za-z0-9_.-]+)/(?<repo>[A-Za-z0-9_.-]+?)(?:\.git)?/?$'
    if ($url -notmatch $pattern) { return $null }
    if ($Matches.owner -in @('.', '..') -or $Matches.repo -in @('.', '..')) { return $null }
    return [ordered]@{
        remote = $Remote
        repository = "$($Matches.owner)/$($Matches.repo)"
        url = "https://github.com/$($Matches.owner)/$($Matches.repo)"
        root = $script:root
    }
}

$script:root = Get-GitValue @('rev-parse', '--show-toplevel')
$branch = Get-GitValue @('symbolic-ref', '--quiet', '--short', 'HEAD') -Optional
$tracking = if ($branch) { Get-GitValue @('config', '--get', "branch.$branch.remote") -Optional }
# '.' means the branch tracks another local branch, which names no remote.
if ($tracking -and $tracking -ne '.') {
    $target = Get-GitHubTarget $tracking
    if (-not $target) { throw 'The tracking remote is not a supported GitHub URL. Resolve the destination explicitly.' }
} else {
    $remotes = @(Get-GitValue @('remote') | ForEach-Object { $_ -split "`n" } | Where-Object { $_ })
    $targets = @($remotes | ForEach-Object { Get-GitHubTarget $_ } | Where-Object { $null -ne $_ })
    if ($targets.Count -ne 1) { throw 'Expected exactly one GitHub remote without tracking; destination is missing or ambiguous.' }
    $target = $targets[0]
}
$target | ConvertTo-Json -Compress
