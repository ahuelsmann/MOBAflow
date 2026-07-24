Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$scriptPath = Join-Path $PSScriptRoot "Test-SpecKitGovernance.ps1"
$testRoot = Join-Path $repositoryRoot ".agent-build/spec-kit-governance-tests/$([Guid]::NewGuid())"

function Assert-Succeeds(
    [hashtable] $Parameters,
    [string] $Scenario) {
    try {
        & $scriptPath @Parameters
    }
    catch {
        throw "Expected success for '$Scenario', but failed: $($_.Exception.Message)"
    }
}

function Assert-Fails(
    [hashtable] $Parameters,
    [string] $Scenario) {
    $failed = $false
    try {
        & $scriptPath @Parameters
    }
    catch {
        $failed = $true
    }

    if (-not $failed) {
        throw "Expected failure for '$Scenario'."
    }
}

function Write-IssueEvent(
    [string] $Title,
    [string] $Body) {
    $path = Join-Path $testRoot "issue-event.json"
    [ordered]@{
        issue = [ordered]@{
            title = $Title
            body = $Body
        }
    } | ConvertTo-Json -Depth 4 |
        Set-Content -LiteralPath $path -Encoding utf8NoBOM
    return $path
}

function Write-TestFile(
    [string] $RelativePath,
    [string] $Content) {
    $path = Join-Path $testRoot $RelativePath
    $directory = Split-Path -Parent $path
    New-Item -ItemType Directory -Path $directory -Force | Out-Null
    Set-Content -LiteralPath $path -Value $Content -Encoding utf8NoBOM
}

try {
    New-Item -ItemType Directory -Path $testRoot -Force | Out-Null

    $taskIssue = Write-IssueEvent -Title "T001: Add safe routing" -Body ""
    Assert-Succeeds @{
        Mode = "Issue"
        IssueEventPath = $taskIssue
        RepositoryRoot = $testRoot
    } "Spec Kit task issue"

    $requiredIssue = Write-IssueEvent `
        -Title "[Feature]: Safe routing" `
        -Body "### Spec Kit workflow`n`nRequired before implementation"
    Assert-Succeeds @{
        Mode = "Issue"
        IssueEventPath = $requiredIssue
        RepositoryRoot = $testRoot
    } "issue requiring a future specification"

    $missingClassification = Write-IssueEvent -Title "[Bug]: Missing metadata" -Body "No form metadata"
    Assert-Fails @{
        Mode = "Issue"
        IssueEventPath = $missingClassification
        RepositoryRoot = $testRoot
    } "issue without Spec Kit classification"

    $missingReference = Write-IssueEvent `
        -Title "[Feature]: Existing work" `
        -Body "### Spec Kit workflow`n`nExisting Spec Kit feature`n`n### Spec Kit reference`n`n_No response_"
    Assert-Fails @{
        Mode = "Issue"
        IssueEventPath = $missingReference
        RepositoryRoot = $testRoot
    } "existing feature without traceability"

    $existingReference = Write-IssueEvent `
        -Title "[Feature]: Existing work" `
        -Body "### Spec Kit workflow`n`nExisting Spec Kit feature`n`n### Spec Kit reference`n`nspecs/001-safe-routing"
    Assert-Succeeds @{
        Mode = "Issue"
        IssueEventPath = $existingReference
        RepositoryRoot = $testRoot
    } "existing feature with traceability"

    Write-TestFile -RelativePath "plans/active-plan.md" -Content @"
# Active plan

**GitHub Issue**: #42
**Spec Kit**: Not applicable - repository maintenance only
**Status**: Active
"@
    Assert-Succeeds @{
        Mode = "PullRequest"
        ChangedFiles = @("plans/active-plan.md")
        RepositoryRoot = $testRoot
    } "valid standalone plan"

    Write-TestFile -RelativePath "plans/completed-plan.md" -Content @"
# Completed plan

**GitHub Issue**: #42
**Spec Kit**: Required
**Status**: Completed
"@
    Assert-Fails @{
        Mode = "PullRequest"
        ChangedFiles = @("plans/completed-plan.md")
        RepositoryRoot = $testRoot
    } "completed standalone plan"

    Write-TestFile -RelativePath "docs/refactoring-plan.md" -Content "# Misplaced plan"
    Assert-Fails @{
        Mode = "PullRequest"
        ChangedFiles = @("docs/refactoring-plan.md")
        RepositoryRoot = $testRoot
    } "standalone plan below docs"

    Write-TestFile -RelativePath "specs/001-safe-routing/spec.md" -Content @"
# Feature Specification: Safe routing

**Source Issue**: #42
**Spec Kit**: Required

## Governance and Traceability
"@
    Assert-Succeeds @{
        Mode = "PullRequest"
        ChangedFiles = @("specs/001-safe-routing/spec.md")
        RepositoryRoot = $testRoot
    } "valid feature specification"

    Write-TestFile -RelativePath "specs/001-safe-routing/plan.md" -Content @"
# Implementation Plan: Safe routing

**GitHub Issue**: #42
**Spec Kit**: Required

## Constitution Check

## Validation Strategy
"@
    Assert-Succeeds @{
        Mode = "PullRequest"
        ChangedFiles = @("specs/001-safe-routing/plan.md")
        RepositoryRoot = $testRoot
    } "valid Spec Kit plan"

    Write-TestFile -RelativePath "specs/001-safe-routing/tasks.md" -Content @"
# Tasks

- [ ] T001 Add automated routing tests in Test/Domain/RoutingTests.cs
- [ ] T002 Run Sonar analysis against the PR base
"@
    Assert-Succeeds @{
        Mode = "PullRequest"
        ChangedFiles = @("specs/001-safe-routing/tasks.md")
        RepositoryRoot = $testRoot
    } "valid Spec Kit tasks"

    Write-TestFile -RelativePath "specs/002-invalid/tasks.md" -Content @"
# Tasks

- [ ] T001 Add automated routing tests in Test/Domain/RoutingTests.cs
"@
    Assert-Fails @{
        Mode = "PullRequest"
        ChangedFiles = @("specs/002-invalid/tasks.md")
        RepositoryRoot = $testRoot
    } "tasks without Sonar gate"

    Write-TestFile -RelativePath ".github/ISSUE_TEMPLATE/feature.yml" -Content @"
name: Feature
body:
  - type: dropdown
    id: spec_kit
    attributes:
      label: Spec Kit workflow
"@
    Assert-Succeeds @{
        Mode = "PullRequest"
        ChangedFiles = @(".github/ISSUE_TEMPLATE/feature.yml")
        RepositoryRoot = $testRoot
    } "issue template with Spec Kit classification"

    Write-TestFile -RelativePath ".github/ISSUE_TEMPLATE/bug.yml" -Content @"
name: Bug
body: []
"@
    Assert-Fails @{
        Mode = "PullRequest"
        ChangedFiles = @(".github/ISSUE_TEMPLATE/bug.yml")
        RepositoryRoot = $testRoot
    } "issue template without Spec Kit classification"

    Write-Host "Spec Kit governance self-tests passed."
}
finally {
    if (Test-Path -LiteralPath $testRoot) {
        Remove-Item -LiteralPath $testRoot -Recurse -Force
    }
}
