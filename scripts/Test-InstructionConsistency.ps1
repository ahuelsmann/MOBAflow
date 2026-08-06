[CmdletBinding()]
param(
    [string] $RepositoryRoot = (Split-Path -Parent $PSScriptRoot)
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Read-RepositoryFile([string] $RelativePath) {
    $path = Join-Path $RepositoryRoot $RelativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required repository instruction file is missing: $RelativePath"
    }

    return Get-Content -Raw -LiteralPath $path
}

function Assert-Contains(
    [string] $Text,
    [string] $Expected,
    [string] $Context) {
    if (-not $Text.Contains($Expected, [StringComparison]::Ordinal)) {
        throw "$Context is missing required text: $Expected"
    }
}

$copilot = Read-RepositoryFile ".github/copilot-instructions.md"
$agents = Read-RepositoryFile "AGENTS.md"
$index = Read-RepositoryFile ".github/instructions/instructions-index.md"
$worktree = Read-RepositoryFile ".github/instructions/git-worktree-isolation.instructions.md"
$qualityWorkflow = Read-RepositoryFile ".github/workflows/quality.yml"

$absoluteRules = [regex]::Match(
    $copilot,
    "(?ms)^## .+Absolute Rules.*?\r?\n(?<body>.*?)(?=^---\s*$)")
if (-not $absoluteRules.Success) {
    throw "Unable to find the Absolute Rules section in .github/copilot-instructions.md."
}

$ruleNumbers = @(
    [regex]::Matches($absoluteRules.Groups["body"].Value, "(?m)^(?<number>\d+)\.\s+\*\*") |
        ForEach-Object { [int] $_.Groups["number"].Value }
)
if ($ruleNumbers.Count -eq 0) {
    throw "The Absolute Rules section contains no numbered rules."
}

for ($indexPosition = 0; $indexPosition -lt $ruleNumbers.Count; $indexPosition++) {
    $expectedNumber = $indexPosition + 1
    if ($ruleNumbers[$indexPosition] -ne $expectedNumber) {
        throw "Absolute Rules must be sequential. Expected $expectedNumber, found $($ruleNumbers[$indexPosition])."
    }
}

$agentsCount = [regex]::Match($agents, "Absolute Rules \((?<count>\d+) rules\)")
if (-not $agentsCount.Success) {
    throw "AGENTS.md does not declare the Absolute Rules count."
}
if ([int] $agentsCount.Groups["count"].Value -ne $ruleNumbers.Count) {
    throw "AGENTS.md declares $($agentsCount.Groups['count'].Value) Absolute Rules, but copilot-instructions.md contains $($ruleNumbers.Count)."
}

$canonicalPath = ".github/instructions/git-worktree-isolation.instructions.md"
Assert-Contains $copilot $canonicalPath ".github/copilot-instructions.md"
Assert-Contains $agents $canonicalPath "AGENTS.md"
Assert-Contains $index "git-worktree-isolation.instructions.md" "instructions-index.md"
Assert-Contains $qualityWorkflow "./scripts/Test-InstructionConsistency.ps1" "quality.yml"

@(
    "SonarQube before PR review",
    "Balanced secrets scanning",
    "Never launch MOBAflow without explicit user approval",
    "Write answers for junior developers",
    "Isolate independent write tasks with Git worktrees"
) | ForEach-Object {
    Assert-Contains $copilot $_ ".github/copilot-instructions.md"
}

@(
    "git worktree list --porcelain",
    "git status --short --branch",
    "## Bootstrap exception",
    "codex/",
    "GitHub-only"
) | ForEach-Object {
    Assert-Contains $worktree $_ "git-worktree-isolation.instructions.md"
}

if ($worktree -match "(?i)\bshould\b|\bmight\b|\bpossibly\b") {
    throw "git-worktree-isolation.instructions.md contains ambiguous instruction wording."
}

Write-Host "Repository instruction consistency passed with $($ruleNumbers.Count) Absolute Rules."
