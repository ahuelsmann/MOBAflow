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

# The shared policy now lives in AGENTS.md; Copilot delegates instead of duplicating a numbered rule list.
Assert-Contains $copilot "[AGENTS.md](../AGENTS.md)" ".github/copilot-instructions.md"
Assert-Contains $index "[AGENTS.md](../../AGENTS.md)" "instructions-index.md"
if ($copilot -match "(?m)^## .*(Absolute Rules|Working agreement|Repository policies)") {
    throw "Keep repository-wide policies in AGENTS.md rather than duplicating them in the Copilot entry point."
}

$canonicalPath = ".github/instructions/git-worktree-isolation.instructions.md"
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
    Assert-Contains $agents $_ "AGENTS.md"
}

@(
    "sonarqube-pre-pr.instructions.md",
    "spec-kit-governance.instructions.md"
) | ForEach-Object {
    Assert-Contains $agents $_ "AGENTS.md"
    Assert-Contains $index $_ "instructions-index.md"
    $null = Read-RepositoryFile ".github/instructions/$_"
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

Write-Host "Repository instruction consistency passed with AGENTS.md as the shared policy source."
