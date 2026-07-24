[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("Issue", "PullRequest")]
    [string] $Mode,

    [string] $IssueEventPath,

    [string] $BaseRef,

    [string] $RepositoryRoot = (Split-Path -Parent $PSScriptRoot),

    [string[]] $ChangedFiles
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Resolve-RepositoryPath(
    [string] $Root,
    [string] $RelativePath) {
    $normalizedRoot = [IO.Path]::GetFullPath($Root)
    $fullPath = [IO.Path]::GetFullPath((Join-Path $normalizedRoot $RelativePath))
    $rootPrefix = $normalizedRoot.TrimEnd(
        [IO.Path]::DirectorySeparatorChar,
        [IO.Path]::AltDirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    if (-not $fullPath.StartsWith($rootPrefix, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Path '$RelativePath' resolves outside the repository."
    }

    return $fullPath
}

function Get-MarkdownSectionValue(
    [string] $Body,
    [string] $Heading) {
    if ([string]::IsNullOrWhiteSpace($Body)) {
        return ""
    }

    $escapedHeading = [Text.RegularExpressions.Regex]::Escape($Heading)
    $pattern = "(?ms)^###\s+$escapedHeading\s*\r?\n+(?<value>.*?)(?=^###\s+|\z)"
    $match = [Text.RegularExpressions.Regex]::Match($Body, $pattern)
    if (-not $match.Success) {
        return ""
    }

    return $match.Groups["value"].Value.Trim()
}

function Test-TraceabilityReference([string] $Value) {
    if ([string]::IsNullOrWhiteSpace($Value) -or $Value -eq "_No response_") {
        return $false
    }

    return $Value -match "(?i)(?:^|\s)(?:specs/\d{3}-[a-z0-9-]+(?:/[\w./-]+)?|#\d+|https://github\.com/[^/\s]+/[^/\s]+/issues/\d+)(?:\s|$)"
}

function Get-IssueGovernanceErrors([string] $EventPath) {
    if ([string]::IsNullOrWhiteSpace($EventPath)) {
        throw "IssueEventPath is required in Issue mode."
    }

    $event = Get-Content -Raw -LiteralPath $EventPath | ConvertFrom-Json
    $title = [string] $event.issue.title
    $body = [string] $event.issue.body

    if ($title -match "^T\d{3}:") {
        return @()
    }

    $workflow = Get-MarkdownSectionValue -Body $body -Heading "Spec Kit workflow"
    $errors = [Collections.Generic.List[string]]::new()
    if ([string]::IsNullOrWhiteSpace($workflow) -or $workflow -eq "_No response_") {
        $errors.Add("Issue is missing the required 'Spec Kit workflow' classification.")
        return @($errors)
    }

    if ($workflow -match "^(?i:Existing Spec Kit)") {
        $reference = Get-MarkdownSectionValue -Body $body -Heading "Spec Kit reference"
        if (-not (Test-TraceabilityReference $reference)) {
            $errors.Add(
                "Issue declares existing Spec Kit work but has no valid specs/ path or GitHub issue reference.")
        }
    }
    elseif ($workflow -notmatch "^(?i:Required before implementation|Not applicable)") {
        $errors.Add("Issue has an unsupported Spec Kit workflow classification: '$workflow'.")
    }

    return @($errors)
}

function Get-ChangedRepositoryFiles(
    [string] $Root,
    [string] $ComparisonRef,
    [string[]] $ExplicitFiles) {
    if ($null -ne $ExplicitFiles -and $ExplicitFiles.Count -gt 0) {
        return @($ExplicitFiles | ForEach-Object { $_.Replace('\', '/') })
    }

    if ([string]::IsNullOrWhiteSpace($ComparisonRef)) {
        throw "BaseRef is required in PullRequest mode when ChangedFiles is not provided."
    }

    $output = & git -C $Root diff --name-only --diff-filter=ACMRT "$ComparisonRef...HEAD"
    if ($LASTEXITCODE -ne 0) {
        throw "Unable to determine changed files against '$ComparisonRef'."
    }

    return @($output | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
        ForEach-Object { $_.Replace('\', '/') })
}

function Get-FileText(
    [string] $Root,
    [string] $RelativePath) {
    $fullPath = Resolve-RepositoryPath -Root $Root -RelativePath $RelativePath
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        throw "Changed file '$RelativePath' does not exist."
    }

    return Get-Content -Raw -LiteralPath $fullPath
}

function Test-GitHubIssueMetadata([string] $Text) {
    return $Text -match "(?im)^\*\*(?:GitHub Issue|Source Issue)\*\*:\s*(?:#\d+|https://github\.com/[^/\s]+/[^/\s]+/issues/\d+)\s*$"
}

function Get-StandalonePlanErrors(
    [string] $Root,
    [string] $RelativePath) {
    $text = Get-FileText -Root $Root -RelativePath $RelativePath
    $errors = [Collections.Generic.List[string]]::new()

    if (-not (Test-GitHubIssueMetadata $text)) {
        $errors.Add("$RelativePath must reference an authoritative GitHub issue.")
    }
    if ($text -notmatch "(?im)^\*\*Spec Kit\*\*:\s*(?:Required|Feature:\s*specs/\d{3}-[a-z0-9-]+|Not applicable\s*-\s*.+)\s*$") {
        $errors.Add(
            "$RelativePath must declare '**Spec Kit**: Required', a feature path, or a reasoned not-applicable status.")
    }
    if ($text -match "(?im)^\*\*Status\*\*:\s*(?:Complete|Completed|Done|Closed)\s*$") {
        $errors.Add("$RelativePath is completed and must be deleted instead of retained.")
    }

    return @($errors)
}

function Get-FeatureArtifactErrors(
    [string] $Root,
    [string] $RelativePath) {
    $text = Get-FileText -Root $Root -RelativePath $RelativePath
    $errors = [Collections.Generic.List[string]]::new()
    $segments = $RelativePath.Split('/')
    if ($segments.Count -lt 3 -or $segments[1] -notmatch "^\d{3}-[a-z0-9-]+$") {
        $errors.Add("$RelativePath must use specs/NNN-feature-name/.")
        return @($errors)
    }

    $fileName = $segments[-1]
    switch ($fileName) {
        "spec.md" {
            if (-not (Test-GitHubIssueMetadata $text)) {
                $errors.Add("$RelativePath must reference its source GitHub issue.")
            }
            if ($text -notmatch "(?m)^## Governance and Traceability") {
                $errors.Add("$RelativePath is missing the Governance and Traceability section.")
            }
            if ($text -notmatch "(?im)^\*\*Spec Kit\*\*:\s*Required\s*$") {
                $errors.Add("$RelativePath must declare Spec Kit as required.")
            }
        }
        "plan.md" {
            if (-not (Test-GitHubIssueMetadata $text)) {
                $errors.Add("$RelativePath must reference its authoritative GitHub issue.")
            }
            if ($text -notmatch "(?m)^## Constitution Check") {
                $errors.Add("$RelativePath is missing the Constitution Check.")
            }
            if ($text -notmatch "(?m)^## Validation Strategy") {
                $errors.Add("$RelativePath is missing the Validation Strategy.")
            }
            if ($text -notmatch "(?im)^\*\*Spec Kit\*\*:\s*Required\s*$") {
                $errors.Add("$RelativePath must declare Spec Kit as required.")
            }
        }
        "tasks.md" {
            if ($text -notmatch "(?im)^- \[[ xX]\] T\d{3}.*\b(?:test|tests|testing|validate|validation)\b") {
                $errors.Add("$RelativePath must contain an explicit automated test or validation task.")
            }
            if ($text -notmatch "(?im)^- \[[ xX]\] T\d{3}.*\bSonar\b") {
                $errors.Add("$RelativePath must contain an explicit Sonar quality-gate task.")
            }
        }
    }

    return @($errors)
}

function Get-IssueTemplateErrors([string] $Root) {
    $templateRoot = Join-Path $Root ".github/ISSUE_TEMPLATE"
    if (-not (Test-Path -LiteralPath $templateRoot -PathType Container)) {
        return @()
    }

    $errors = [Collections.Generic.List[string]]::new()
    $templates = Get-ChildItem -LiteralPath $templateRoot -File -Filter "*.yml" |
        Where-Object { $_.Name -ne "config.yml" }
    foreach ($template in $templates) {
        $text = Get-Content -Raw -LiteralPath $template.FullName
        if ($text -notmatch "(?m)^\s+id:\s*spec_kit\s*$" -or
            $text -notmatch "(?m)^\s+label:\s*Spec Kit workflow\s*$") {
            $relativePath = [IO.Path]::GetRelativePath($Root, $template.FullName).Replace('\', '/')
            $errors.Add("$relativePath must require a Spec Kit workflow classification.")
        }
    }

    return @($errors)
}

function Add-ValidationErrors(
    [Collections.Generic.List[string]] $Target,
    [object[]] $Source) {
    foreach ($errorMessage in $Source) {
        $Target.Add([string] $errorMessage)
    }
}

function Get-PullRequestGovernanceErrors(
    [string] $Root,
    [string] $ComparisonRef,
    [string[]] $ExplicitFiles) {
    $changed = Get-ChangedRepositoryFiles `
        -Root $Root `
        -ComparisonRef $ComparisonRef `
        -ExplicitFiles $ExplicitFiles
    $errors = [Collections.Generic.List[string]]::new()

    foreach ($relativePath in $changed) {
        if ($relativePath -match "(?i)^docs/.*(?:plan|roadmap).*\.md$") {
            $errors.Add("$relativePath is a standalone plan and belongs in plans/.")
            continue
        }
        if ($relativePath -match "(?i)^plans/.+\.md$") {
            Add-ValidationErrors `
                -Target $errors `
                -Source @(Get-StandalonePlanErrors -Root $Root -RelativePath $relativePath)
            continue
        }
        if ($relativePath -match "(?i)^specs/[^/]+/(?:spec|plan|tasks)\.md$") {
            Add-ValidationErrors `
                -Target $errors `
                -Source @(Get-FeatureArtifactErrors -Root $Root -RelativePath $relativePath)
        }
    }

    Add-ValidationErrors -Target $errors -Source @(Get-IssueTemplateErrors -Root $Root)
    return @($errors)
}

$resolvedRoot = [IO.Path]::GetFullPath($RepositoryRoot)
$validationErrors = @(
    if ($Mode -eq "Issue") {
        Get-IssueGovernanceErrors -EventPath $IssueEventPath
    }
    else {
        Get-PullRequestGovernanceErrors `
            -Root $resolvedRoot `
            -ComparisonRef $BaseRef `
            -ExplicitFiles $ChangedFiles
    }
)

if ($validationErrors.Count -gt 0) {
    throw "Spec Kit governance validation failed:`n - $($validationErrors -join "`n - ")"
}

Write-Host "Spec Kit governance validation passed for $Mode mode."
