[CmdletBinding()]
param(
    [int] $PullRequest = 0,

    [string] $Repository = '',

    [switch] $SetStatus,

    [string] $RepositoryRoot = (Split-Path -Parent $PSScriptRoot)
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Informational until #566 (separate agent identities): this context is NOT a required ruleset check.
# The declared (shared account) tier may only satisfy this informational context.
$script:StatusContext = 'Independent final review (informational)'

function Get-SliceHeaderField {
    [CmdletBinding()]
    param(
        [AllowNull()]
        [AllowEmptyString()]
        [string] $Body,

        [Parameter(Mandatory = $true)]
        [string] $FieldName
    )

    if ([string]::IsNullOrWhiteSpace($Body) -or [string]::IsNullOrWhiteSpace($FieldName)) {
        return $null
    }

    $escaped = [Text.RegularExpressions.Regex]::Escape($FieldName.Trim())
    $pattern = "(?im)^\s*[-*]\s+\*{0,2}$escaped\*{0,2}\s*:\s*(?<value>.*)$"
    $match = [Text.RegularExpressions.Regex]::Match($Body, $pattern)
    if (-not $match.Success) {
        return $null
    }

    $value = [Text.RegularExpressions.Regex]::Replace($match.Groups['value'].Value, '<!--.*?-->', '').Trim()
    if ([string]::IsNullOrWhiteSpace($value)) {
        return $null
    }

    return $value
}

function ConvertTo-ModelFamily {
    [CmdletBinding()]
    param(
        [AllowNull()]
        [AllowEmptyString()]
        [string] $Value
    )

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return $null
    }

    $trimmed = $Value.Trim()
    if ($trimmed -match '\|' -or $trimmed -match '^<.*>$') {
        return $null
    }

    $lower = $trimmed.ToLowerInvariant()
    if ($lower -in @('claude', 'openai', 'gemini')) {
        return $lower
    }

    $matchesOpenAi = $lower -match '(?:codex|openai|\bgpt\b|gpt[-_\s]?\d)'
    $matchesClaude = $lower -match '(?:claude|opus|sonnet|haiku|anthropic)'
    $matchesGemini = $lower -match '(?:gemini|argon|antigravity)'

    $matched = [Collections.Generic.List[string]]::new()
    if ($matchesOpenAi) { $matched.Add('openai') }
    if ($matchesClaude) { $matched.Add('claude') }
    if ($matchesGemini) { $matched.Add('gemini') }

    if ($matched.Count -eq 1) {
        return $matched[0]
    }

    # Antigravity can host built-in Claude models (for example "Antigravity Opus").
    if ($matched.Count -eq 2 -and $matchesClaude -and $matchesGemini -and -not $matchesOpenAi -and $lower -match '(?:claude|opus|sonnet|haiku)') {
        return 'claude'
    }

    return $null
}

function Get-ObjectField {
    param(
        [AllowNull()]
        [object] $Source,

        [Parameter(Mandatory = $true)]
        [string[]] $Names
    )

    if ($null -eq $Source) {
        return $null
    }

    foreach ($name in $Names) {
        if ($Source -is [System.Collections.IDictionary] -and $Source.Contains($name)) {
            return $Source[$name]
        }
        if ($null -ne $Source.PSObject.Properties[$name]) {
            return $Source.PSObject.Properties[$name].Value
        }
    }

    return $null
}

function ConvertFrom-IssueComment {
    param(
        [AllowNull()]
        [object] $Comment,

        [int] $Index
    )

    if ($null -eq $Comment) {
        return $null
    }

    if ($Comment -is [string]) {
        return [pscustomobject] @{
            Index = $Index
            HasCreatedAt = $false
            CreatedAt = [DateTimeOffset]::MinValue
            CommentId = $null
            Author = $null
            Body = $Comment
        }
    }

    $rawCreated = [string] (Get-ObjectField -Source $Comment -Names @('created_at', 'CreatedAt'))
    $parsedDate = [DateTimeOffset]::MinValue
    $hasDate = (-not [string]::IsNullOrWhiteSpace($rawCreated)) -and ([DateTimeOffset]::TryParse($rawCreated, [ref] $parsedDate))

    $userObj = Get-ObjectField -Source $Comment -Names @('user', 'User')
    $author = [string] (Get-ObjectField -Source $userObj -Names @('login', 'Login'))
    if ([string]::IsNullOrWhiteSpace($author)) {
        $author = [string] (Get-ObjectField -Source $Comment -Names @('author', 'Author'))
    }

    return [pscustomobject] @{
        Index = $Index
        HasCreatedAt = $hasDate
        CreatedAt = if ($hasDate) { $parsedDate } else { [DateTimeOffset]::MinValue }
        CommentId = (Get-ObjectField -Source $Comment -Names @('id', 'Id'))
        Author = if ([string]::IsNullOrWhiteSpace($author)) { $null } else { $author.Trim() }
        Body = [string] (Get-ObjectField -Source $Comment -Names @('body', 'Body'))
    }
}

function Get-OrderedComments {
    param(
        [AllowNull()]
        [object[]] $Comments = @()
    )

    $entries = [Collections.Generic.List[pscustomobject]]::new()
    for ($i = 0; $i -lt $Comments.Count; $i++) {
        $entry = ConvertFrom-IssueComment -Comment $Comments[$i] -Index $i
        if ($null -ne $entry) {
            $entries.Add($entry)
        }
    }

    if ($entries.Count -eq 0) {
        return @()
    }

    $missingDates = @($entries | Where-Object { -not $_.HasCreatedAt }).Count
    if ($missingDates -eq 0) {
        return @($entries | Sort-Object -Property @{ Expression = { $_.CreatedAt }; Descending = $false }, @{ Expression = { $_.Index }; Descending = $false })
    }

    return @($entries | Sort-Object -Property Index)
}

function Get-AttributeValue([Text.RegularExpressions.Match] $AttrMatch) {
    if ($AttrMatch.Groups['qval'].Success) {
        return $AttrMatch.Groups['qval'].Value.Trim()
    }
    if ($AttrMatch.Groups['sval'].Success) {
        return $AttrMatch.Groups['sval'].Value.Trim()
    }
    return $AttrMatch.Groups['uval'].Value.Trim()
}

function ConvertFrom-ReviewMarkerMatch {
    param(
        [Parameter(Mandatory = $true)]
        [Text.RegularExpressions.Match] $Match,

        [Parameter(Mandatory = $true)]
        [Text.RegularExpressions.Regex] $AttrRegex,

        [Parameter(Mandatory = $true)]
        [pscustomobject] $CommentEntry
    )

    $attrs = @{}
    foreach ($attrMatch in $AttrRegex.Matches($Match.Groups['attrs'].Value.Trim())) {
        $key = $attrMatch.Groups['key'].Value.ToLowerInvariant()
        $attrs[$key] = Get-AttributeValue -AttrMatch $attrMatch
    }

    foreach ($requiredKey in @('verdict', 'head', 'base', 'reviewer-family', 'reviewer-model', 'dispatcher')) {
        if (-not $attrs.ContainsKey($requiredKey) -or [string]::IsNullOrWhiteSpace($attrs[$requiredKey])) {
            return $null
        }
    }

    $verdict = $attrs['verdict'].ToUpperInvariant()
    if ($verdict -notin @('PASS', 'CHANGES')) {
        return $null
    }

    $head = $attrs['head'].ToLowerInvariant()
    $base = $attrs['base'].ToLowerInvariant()
    if ($head -notmatch '^[0-9a-f]{40}$' -or $base -notmatch '^[0-9a-f]{40}$') {
        return $null
    }

    $reviewerFamily = $attrs['reviewer-family'].ToLowerInvariant()
    if ($reviewerFamily -notin @('claude', 'openai', 'gemini')) {
        return $null
    }

    return [pscustomobject] @{
        Verdict = $verdict
        Head = $head
        Base = $base
        ReviewerFamily = $reviewerFamily
        ReviewerModel = $attrs['reviewer-model']
        Dispatcher = $attrs['dispatcher']
        CommentId = $CommentEntry.CommentId
        CreatedAt = if ($CommentEntry.HasCreatedAt) { $CommentEntry.CreatedAt } else { $null }
        Author = $CommentEntry.Author
    }
}

function Get-FinalReviewMarkers {
    [CmdletBinding()]
    param(
        [AllowNull()]
        [object[]] $Comments = @()
    )

    if ($null -eq $Comments -or $Comments.Count -eq 0) {
        return @()
    }

    $orderedComments = @(Get-OrderedComments -Comments $Comments)
    if ($orderedComments.Count -eq 0) {
        return @()
    }

    $markerRegex = [Text.RegularExpressions.Regex]::new(
        '<!--\s*moba-final-review\b(?<attrs>.*?)-->',
        [Text.RegularExpressions.RegexOptions]::IgnoreCase -bor [Text.RegularExpressions.RegexOptions]::Singleline)
    $attrRegex = [Text.RegularExpressions.Regex]::new(
        '\b(?<key>verdict|head|base|reviewer-family|reviewer-model|dispatcher)\s*=\s*(?:"(?<qval>[^"]*)"|''(?<sval>[^'']*)''|(?<uval>.*?))(?=\s+(?:verdict|head|base|reviewer-family|reviewer-model|dispatcher)\s*=|\s*$)',
        [Text.RegularExpressions.RegexOptions]::IgnoreCase -bor [Text.RegularExpressions.RegexOptions]::Singleline)

    $chronological = [Collections.Generic.List[pscustomobject]]::new()
    foreach ($entry in $orderedComments) {
        if ([string]::IsNullOrWhiteSpace($entry.Body)) {
            continue
        }

        foreach ($match in $markerRegex.Matches($entry.Body)) {
            $parsed = ConvertFrom-ReviewMarkerMatch -Match $match -AttrRegex $attrRegex -CommentEntry $entry
            if ($null -ne $parsed) {
                $chronological.Add($parsed)
            }
        }
    }

    if ($chronological.Count -eq 0) {
        return @()
    }

    $newestFirst = [Collections.Generic.List[pscustomobject]]::new()
    for ($i = $chronological.Count - 1; $i -ge 0; $i--) {
        $newestFirst.Add($chronological[$i])
    }

    return @($newestFirst)
}

function New-CleanGitProcessStartInfo {
    param(
        [Parameter(Mandatory = $true)]
        [string] $FileName,

        [Parameter(Mandatory = $true)]
        [string[]] $Arguments,

        [string] $WorkingDirectory
    )

    $start = [Diagnostics.ProcessStartInfo]::new($FileName)
    $start.UseShellExecute = $false
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    if (-not [string]::IsNullOrWhiteSpace($WorkingDirectory)) {
        $start.WorkingDirectory = $WorkingDirectory
    }
    foreach ($key in @('GIT_DIR', 'GIT_WORK_TREE', 'GIT_INDEX_FILE', 'GIT_COMMON_DIR')) {
        [void] $start.Environment.Remove($key)
    }
    foreach ($arg in $Arguments) {
        $start.ArgumentList.Add($arg)
    }
    return $start
}

function Invoke-GitCapture {
    param(
        [Parameter(Mandatory = $true)]
        [string] $RepositoryRoot,

        [Parameter(Mandatory = $true)]
        [string[]] $Arguments
    )

    $start = New-CleanGitProcessStartInfo -FileName 'git' -Arguments (@('-C', $RepositoryRoot) + $Arguments)
    $proc = [Diagnostics.Process]::Start($start)
    try {
        $out = $proc.StandardOutput.ReadToEndAsync()
        $null = $proc.StandardError.ReadToEndAsync()
        $proc.WaitForExit()
        return [pscustomobject] @{
            ExitCode = $proc.ExitCode
            Output = $out.Result
        }
    }
    finally {
        $proc.Dispose()
    }
}

function Get-GitMergeBase {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Sha,

        [Parameter(Mandatory = $true)]
        [string] $BaseRef,

        [Parameter(Mandatory = $true)]
        [string] $RepositoryRoot
    )

    $result = Invoke-GitCapture -RepositoryRoot $RepositoryRoot -Arguments @('merge-base', $BaseRef, $Sha)
    if ($result.ExitCode -ne 0) {
        return $null
    }

    $value = $result.Output.Trim()
    if ([string]::IsNullOrWhiteSpace($value)) {
        return $null
    }

    return $value
}

function Test-GitIsAncestor {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Ancestor,

        [Parameter(Mandatory = $true)]
        [string] $Descendant,

        [Parameter(Mandatory = $true)]
        [string] $RepositoryRoot
    )

    $result = Invoke-GitCapture -RepositoryRoot $RepositoryRoot -Arguments @('merge-base', '--is-ancestor', $Ancestor, $Descendant)
    return ($result.ExitCode -eq 0)
}

function Test-AllNonMergeCommitsReachable {
    param(
        [Parameter(Mandatory = $true)]
        [string] $MarkerHead,

        [Parameter(Mandatory = $true)]
        [string] $CurrentHead,

        [Parameter(Mandatory = $true)]
        [string] $BaseRef,

        [Parameter(Mandatory = $true)]
        [string] $RepositoryRoot
    )

    $result = Invoke-GitCapture -RepositoryRoot $RepositoryRoot -Arguments @('rev-list', '--no-merges', "$MarkerHead..$CurrentHead", '--not', $BaseRef)
    if ($result.ExitCode -ne 0) {
        return $false
    }

    return [string]::IsNullOrWhiteSpace($result.Output)
}

function Get-GitRawDiffHash {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Sha,

        [Parameter(Mandatory = $true)]
        [string] $BaseRef,

        [Parameter(Mandatory = $true)]
        [string] $RepositoryRoot
    )

    $mergeBase = Get-GitMergeBase -Sha $Sha -BaseRef $BaseRef -RepositoryRoot $RepositoryRoot
    if ($null -eq $mergeBase) {
        return $null
    }

    $diffStart = New-CleanGitProcessStartInfo -FileName 'git' -Arguments @('-C', $RepositoryRoot, 'diff', '--no-color', $mergeBase, $Sha)
    $diffProc = [Diagnostics.Process]::Start($diffStart)
    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    try {
        $null = $diffProc.StandardError.ReadToEndAsync()
        $hashBytes = $sha256.ComputeHash($diffProc.StandardOutput.BaseStream)
        $diffProc.WaitForExit()

        if ($diffProc.ExitCode -ne 0) {
            return $null
        }

        return [Convert]::ToHexString($hashBytes).ToLowerInvariant()
    }
    finally {
        $sha256.Dispose()
        $diffProc.Dispose()
    }
}

function Test-PatchEquivalent {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string] $MarkerHead,

        [Parameter(Mandatory = $true)]
        [string] $CurrentHead,

        [string] $BaseRef = 'origin/main',

        [string] $RepositoryRoot = (Get-Location).Path
    )

    if ($MarkerHead -notmatch '^[0-9a-fA-F]{40}$' -or $CurrentHead -notmatch '^[0-9a-fA-F]{40}$') {
        return $false
    }

    if ($MarkerHead.Equals($CurrentHead, [StringComparison]::OrdinalIgnoreCase)) {
        return $true
    }

    if (-not (Test-GitIsAncestor -Ancestor $MarkerHead -Descendant $CurrentHead -RepositoryRoot $RepositoryRoot)) {
        return $false
    }

    if (-not (Test-AllNonMergeCommitsReachable -MarkerHead $MarkerHead -CurrentHead $CurrentHead -BaseRef $BaseRef -RepositoryRoot $RepositoryRoot)) {
        return $false
    }

    $markerHash = Get-GitRawDiffHash -Sha $MarkerHead -BaseRef $BaseRef -RepositoryRoot $RepositoryRoot
    $currentHash = Get-GitRawDiffHash -Sha $CurrentHead -BaseRef $BaseRef -RepositoryRoot $RepositoryRoot
    if ([string]::IsNullOrEmpty($markerHash) -or [string]::IsNullOrEmpty($currentHash)) {
        return $false
    }

    return $markerHash.Equals($currentHash, [StringComparison]::OrdinalIgnoreCase)
}

function Format-StatusDescription([string] $Message) {
    $clean = ($Message -replace '[\r\n]+', ' ').Trim()
    if ($clean.Length -le 140) {
        return $clean
    }
    return $clean.Substring(0, 137) + '...'
}

function Get-NormalizedIdentifier([string] $Value) {
    if ([string]::IsNullOrWhiteSpace($Value)) {
        return ''
    }
    return ($Value -replace '\s+', '').ToLowerInvariant()
}

function New-GateResult {
    param(
        [Parameter(Mandatory = $true)]
        [string] $State,

        [Parameter(Mandatory = $true)]
        [string] $Reason,

        [AllowNull()]
        [object] $Marker = $null,

        [AllowNull()]
        [string] $WriterFamily = $null,

        [AllowNull()]
        [string] $Orchestrator = $null,

        [AllowNull()]
        [string] $Tier = $null
    )

    return [pscustomobject] @{
        State = $State
        Description = Format-StatusDescription $Reason
        Marker = $Marker
        WriterFamily = $WriterFamily
        Orchestrator = $Orchestrator
        Tier = $Tier
    }
}

function Get-IdentitiesConfig {
    param(
        [string] $RepositoryRoot = (Get-Location).Path,

        [string] $IdentitiesPath = '',

        [AllowNull()]
        [object] $IdentitiesConfig = $null
    )

    $source = $IdentitiesConfig
    if ($null -eq $source) {
        $resolvedPath = if ([string]::IsNullOrWhiteSpace($IdentitiesPath)) {
            Join-Path $RepositoryRoot '.github/review-gate-identities.json'
        }
        else {
            $IdentitiesPath
        }

        if (-not (Test-Path -LiteralPath $resolvedPath -PathType Leaf)) {
            return [pscustomobject] @{
                TrustedReviewers = @{}
                AllowDeclaredReviews = $false
            }
        }

        $source = Get-Content -Raw -LiteralPath $resolvedPath | ConvertFrom-Json
    }

    $trusted = @{}
    $rawTrusted = Get-ObjectField -Source $source -Names @('trustedReviewers', 'TrustedReviewers')
    if ($rawTrusted -is [System.Collections.IDictionary]) {
        foreach ($key in $rawTrusted.Keys) {
            $trusted[([string] $key).Trim().ToLowerInvariant()] = ([string] $rawTrusted[$key]).Trim().ToLowerInvariant()
        }
    }
    elseif ($null -ne $rawTrusted) {
        foreach ($prop in $rawTrusted.PSObject.Properties) {
            $trusted[$prop.Name.Trim().ToLowerInvariant()] = ([string] $prop.Value).Trim().ToLowerInvariant()
        }
    }

    $allowRaw = Get-ObjectField -Source $source -Names @('allowDeclaredReviews', 'AllowDeclaredReviews')
    $allowDeclared = ($null -ne $allowRaw) -and [bool] $allowRaw

    return [pscustomobject] @{
        TrustedReviewers = $trusted
        AllowDeclaredReviews = $allowDeclared
    }
}

function Get-BranchPrefixFamily([string] $HeadRefName) {
    if ([string]::IsNullOrWhiteSpace($HeadRefName)) {
        return $null
    }

    $lower = $HeadRefName.Trim().ToLowerInvariant()
    if ($lower -match '^(?:claude|opus|sonnet|haiku)/') {
        return 'claude'
    }
    if ($lower -match '^(?:codex|openai|gpt)/') {
        return 'openai'
    }
    if ($lower -match '^(?:antigravity|gemini|argon)/') {
        return 'gemini'
    }

    return $null
}

function Get-CommitInferredFamily([object] $Commit) {
    if ($null -eq $Commit) {
        return $null
    }

    $parents = Get-ObjectField -Source $Commit -Names @('parents', 'Parents')
    if ($null -ne $parents -and @($parents).Count -gt 1) {
        return $null
    }

    $commitNode = Get-ObjectField -Source $Commit -Names @('commit', 'Commit')
    $message = [string] (Get-ObjectField -Source $commitNode -Names @('message', 'Message'))
    if ([string]::IsNullOrWhiteSpace($message)) {
        $message = [string] (Get-ObjectField -Source $Commit -Names @('message', 'Message'))
    }

    $families = [Collections.Generic.List[string]]::new()
    foreach ($line in ($message -split "\r?\n")) {
        if ($line -match '(?i)^\s*Co-Authored-By\s*:\s*(?<trailer>.+)$') {
            $family = ConvertTo-ModelFamily -Value $Matches['trailer']
            if ($null -ne $family -and $families -notcontains $family) {
                $families.Add($family)
            }
        }
    }

    return @($families)
}

function Get-InferredWriterFamilies {
    param(
        [AllowNull()]
        [string] $HeadRefName,

        [AllowNull()]
        [object[]] $Commits = @()
    )

    $families = [Collections.Generic.List[string]]::new()
    $branchFamily = Get-BranchPrefixFamily -HeadRefName $HeadRefName
    if ($null -ne $branchFamily) {
        $families.Add($branchFamily)
    }

    foreach ($commit in @($Commits)) {
        foreach ($commitFamily in @(Get-CommitInferredFamily -Commit $commit)) {
            if ($null -ne $commitFamily -and $families -notcontains $commitFamily) {
                $families.Add($commitFamily)
            }
        }
    }

    return @($families)
}

function Test-WriterCrossCheckError {
    param(
        [Parameter(Mandatory = $true)]
        [string] $ReviewerFamily,

        [AllowNull()]
        [string] $HeaderWriterFamily,

        [AllowNull()]
        [string[]] $InferredWriterFamilies = @()
    )

    if (@($InferredWriterFamilies) -contains $ReviewerFamily) {
        return "writer family mismatch: commit or branch provenance identifies '$ReviewerFamily'."
    }

    if (-not [string]::IsNullOrWhiteSpace($HeaderWriterFamily) -and
        $InferredWriterFamilies.Count -eq 1 -and
        $InferredWriterFamilies[0] -ne $HeaderWriterFamily) {
        return "writer family mismatch: header '$HeaderWriterFamily' conflicts with commit/branch '$($InferredWriterFamilies[0])'."
    }

    return $null
}

function Test-HasMalformedReviewMarker {
    param(
        [AllowNull()]
        [object[]] $Comments = @()
    )

    foreach ($comment in @($Comments)) {
        $entry = ConvertFrom-IssueComment -Comment $comment -Index 0
        if ($null -ne $entry -and $entry.Body -match '<!--\s*moba-final-review\b') {
            return $true
        }
    }

    return $false
}

function Resolve-WriterFamily {
    param(
        [AllowNull()]
        [AllowEmptyString()]
        [string] $PullRequestBody
    )

    $writerFamilyField = Get-SliceHeaderField -Body $PullRequestBody -FieldName 'Writer family'
    if (-not [string]::IsNullOrWhiteSpace($writerFamilyField) -and $writerFamilyField -notmatch '\|' -and $writerFamilyField -notmatch '^<.*>$') {
        $resolved = ConvertTo-ModelFamily -Value $writerFamilyField
        if ($null -eq $resolved) {
            return [pscustomobject] @{
                Family = $null
                Error = "Unresolved Writer family '$writerFamilyField' in PR slice header."
            }
        }
        return [pscustomobject] @{ Family = $resolved; Error = $null }
    }

    $writerField = Get-SliceHeaderField -Body $PullRequestBody -FieldName 'Writer'
    if (-not [string]::IsNullOrWhiteSpace($writerField)) {
        $fallback = ConvertTo-ModelFamily -Value $writerField
        if ($null -ne $fallback) {
            return [pscustomobject] @{ Family = $fallback; Error = $null }
        }
    }

    return [pscustomobject] @{
        Family = $null
        Error = 'Unable to resolve writer model family from PR slice header.'
    }
}

function Get-ShortSha([string] $Sha) {
    if ([string]::IsNullOrWhiteSpace($Sha)) {
        return ''
    }
    if ($Sha.Length -ge 8) {
        return $Sha.Substring(0, 8)
    }
    return $Sha
}

function Test-MarkerBaseMatches {
    param(
        [Parameter(Mandatory = $true)]
        [pscustomobject] $Marker,

        [string] $BaseSha = '',

        [Parameter(Mandatory = $true)]
        [string] $BaseRef,

        [Parameter(Mandatory = $true)]
        [string] $RepositoryRoot,

        # Current-head verdicts must bind to the exact current PR base SHA. Only carry-forward
        # (historical marker head) may validate the marker base through the git merge base.
        [switch] $AllowHistoricalBase
    )

    if (-not [string]::IsNullOrWhiteSpace($BaseSha) -and $Marker.Base.Equals($BaseSha, [StringComparison]::OrdinalIgnoreCase)) {
        return $true
    }

    if (-not $AllowHistoricalBase) {
        return $false
    }

    $markerMergeBase = Get-GitMergeBase -Sha $Marker.Head -BaseRef $BaseRef -RepositoryRoot $RepositoryRoot
    if ($null -ne $markerMergeBase -and $Marker.Base.Equals($markerMergeBase, [StringComparison]::OrdinalIgnoreCase)) {
        return $true
    }

    return $false
}

function Test-HasUnresolvedReviewThreads {
    param(
        [AllowNull()]
        [object[]] $ReviewThreads = @()
    )

    foreach ($thread in @($ReviewThreads)) {
        if ($null -eq $thread) {
            continue
        }
        $resolved = Get-ObjectField -Source $thread -Names @('isResolved', 'IsResolved')
        if ($null -ne $resolved -and -not [bool] $resolved) {
            return $true
        }
    }

    return $false
}

function Test-MarkerEligible {
    param(
        [Parameter(Mandatory = $true)]
        [pscustomobject] $Marker,

        [Parameter(Mandatory = $true)]
        [string] $HeadSha,

        [string] $BaseSha = '',

        [string] $BaseRef = 'origin/main',

        [string] $RepositoryRoot = (Get-Location).Path,

        [AllowNull()]
        [scriptblock] $CarryForwardEvaluator = $null
    )

    # Only a verdict naming the PR head with the exact current PR base, or a validated carry-forward
    # (historical base plus identical patch), takes part in verdict ordering. Anything else is ignored.
    if ($Marker.Head.Equals($HeadSha, [StringComparison]::OrdinalIgnoreCase)) {
        return (Test-MarkerBaseMatches -Marker $Marker -BaseSha $BaseSha -BaseRef $BaseRef -RepositoryRoot $RepositoryRoot)
    }

    try {
        if (-not (Test-MarkerBaseMatches -Marker $Marker -BaseSha $BaseSha -BaseRef $BaseRef -RepositoryRoot $RepositoryRoot -AllowHistoricalBase)) {
            return $false
        }

        if ($null -ne $CarryForwardEvaluator) {
            return [bool] (& $CarryForwardEvaluator $Marker.Head $HeadSha $BaseRef $RepositoryRoot)
        }
        return [bool] (Test-PatchEquivalent -MarkerHead $Marker.Head -CurrentHead $HeadSha -BaseRef $BaseRef -RepositoryRoot $RepositoryRoot)
    }
    catch {
        return $false
    }
}

function Resolve-MarkerTier {
    param(
        [Parameter(Mandatory = $true)]
        [pscustomobject] $Marker,

        [Parameter(Mandatory = $true)]
        [pscustomobject] $Identities,

        [string] $RepositoryOwner = ''
    )

    $authorKey = if ([string]::IsNullOrWhiteSpace($Marker.Author)) { '' } else { $Marker.Author.Trim().ToLowerInvariant() }
    if ($Identities.TrustedReviewers.ContainsKey($authorKey)) {
        $trustedFamily = $Identities.TrustedReviewers[$authorKey]
        if ($trustedFamily -ne $Marker.ReviewerFamily) {
            return [pscustomobject] @{
                IsValid = $false
                Tier = 'verified'
                Error = "Trusted reviewer '$($Marker.Author)' is mapped to '$trustedFamily', not '$($Marker.ReviewerFamily)'."
            }
        }
        return [pscustomobject] @{ IsValid = $true; Tier = 'verified'; Error = $null }
    }

    if (-not $Identities.AllowDeclaredReviews) {
        return [pscustomobject] @{
            IsValid = $false
            Tier = 'declared'
            Error = 'Declared (shared account) reviews are disabled; a verified bot review is required.'
        }
    }

    # Declared tier: only the repository owner account may declare a review (shared-account transition).
    if ([string]::IsNullOrWhiteSpace($RepositoryOwner) -or
        -not $authorKey.Equals($RepositoryOwner.Trim().ToLowerInvariant(), [StringComparison]::Ordinal)) {
        return [pscustomobject] @{
            IsValid = $false
            Tier = 'declared'
            Error = 'Declared reviews are accepted only from the repository owner account.'
        }
    }

    return [pscustomobject] @{ IsValid = $true; Tier = 'declared'; Error = $null }
}

function Resolve-MarkerHeadOrCarryForward {
    param(
        [Parameter(Mandatory = $true)]
        [pscustomobject] $Marker,

        [Parameter(Mandatory = $true)]
        [string] $HeadSha,

        [Parameter(Mandatory = $true)]
        [string] $WriterFamily,

        [Parameter(Mandatory = $true)]
        [string] $Orchestrator,

        [Parameter(Mandatory = $true)]
        [string] $Tier,

        [Parameter(Mandatory = $true)]
        [string] $BaseRef,

        [Parameter(Mandatory = $true)]
        [string] $RepositoryRoot,

        [AllowNull()]
        [scriptblock] $CarryForwardEvaluator = $null
    )

    $shortHead = Get-ShortSha $HeadSha
    $shortMarkerHead = Get-ShortSha $Marker.Head
    $tierLabel = if ($Tier -eq 'verified') { 'verified' } else { 'declared (shared account)' }

    if ($Marker.Head.Equals($HeadSha, [StringComparison]::OrdinalIgnoreCase)) {
        return New-GateResult -State 'success' -Reason "Review $tierLabel $($Marker.ReviewerFamily) ($($Marker.ReviewerModel)) PASS on $shortHead." -Marker $Marker -WriterFamily $WriterFamily -Orchestrator $Orchestrator -Tier $Tier
    }

    $isEquivalent = if ($null -ne $CarryForwardEvaluator) {
        [bool] (& $CarryForwardEvaluator $Marker.Head $HeadSha $BaseRef $RepositoryRoot)
    }
    else {
        Test-PatchEquivalent -MarkerHead $Marker.Head -CurrentHead $HeadSha -BaseRef $BaseRef -RepositoryRoot $RepositoryRoot
    }

    if ($isEquivalent) {
        return New-GateResult -State 'success' -Reason "Review $tierLabel $($Marker.ReviewerFamily) PASS (main-merge carry-forward from $shortMarkerHead)." -Marker $Marker -WriterFamily $WriterFamily -Orchestrator $Orchestrator -Tier $Tier
    }

    return New-GateResult -State 'failure' -Reason "Review marker for $shortMarkerHead is stale for $shortHead." -Marker $Marker -WriterFamily $WriterFamily -Orchestrator $Orchestrator -Tier $Tier
}

function Resolve-MarkerGateResult {
    param(
        [Parameter(Mandatory = $true)]
        [pscustomobject] $Marker,

        [Parameter(Mandatory = $true)]
        [string] $HeadSha,

        [string] $BaseSha = '',

        [AllowNull()]
        [AllowEmptyString()]
        [string] $PullRequestBody,

        [AllowNull()]
        [string[]] $InferredWriterFamilies = @(),

        [Parameter(Mandatory = $true)]
        [pscustomobject] $Identities,

        [string] $RepositoryOwner = '',

        [Parameter(Mandatory = $true)]
        [string] $BaseRef,

        [Parameter(Mandatory = $true)]
        [string] $RepositoryRoot,

        [AllowNull()]
        [scriptblock] $CarryForwardEvaluator = $null
    )

    if ($Marker.Verdict -eq 'CHANGES') {
        return New-GateResult -State 'failure' -Reason "Final review verdict is CHANGES for $(Get-ShortSha $Marker.Head)." -Marker $Marker
    }

    $tierInfo = Resolve-MarkerTier -Marker $Marker -Identities $Identities -RepositoryOwner $RepositoryOwner
    if (-not $tierInfo.IsValid) {
        return New-GateResult -State 'failure' -Reason $tierInfo.Error -Marker $Marker -Tier $tierInfo.Tier
    }

    $isCurrentHead = $Marker.Head.Equals($HeadSha, [StringComparison]::OrdinalIgnoreCase)
    if (-not (Test-MarkerBaseMatches -Marker $Marker -BaseSha $BaseSha -BaseRef $BaseRef -RepositoryRoot $RepositoryRoot -AllowHistoricalBase:(-not $isCurrentHead))) {
        return New-GateResult -State 'failure' -Reason "Review marker base $(Get-ShortSha $Marker.Base) does not match PR base." -Marker $Marker -Tier $tierInfo.Tier
    }

    $writerResolution = Resolve-WriterFamily -PullRequestBody $PullRequestBody
    if ($null -eq $writerResolution.Family) {
        return New-GateResult -State 'failure' -Reason $writerResolution.Error -Marker $Marker -Tier $tierInfo.Tier
    }

    $writerFamily = $writerResolution.Family
    $crossCheckError = Test-WriterCrossCheckError -ReviewerFamily $Marker.ReviewerFamily -HeaderWriterFamily $writerFamily -InferredWriterFamilies $InferredWriterFamilies
    if ($null -ne $crossCheckError) {
        return New-GateResult -State 'failure' -Reason $crossCheckError -Marker $Marker -WriterFamily $writerFamily -Tier $tierInfo.Tier
    }

    $orchestrator = Get-SliceHeaderField -Body $PullRequestBody -FieldName 'Orchestrator'
    if ([string]::IsNullOrWhiteSpace($orchestrator) -or $orchestrator -match '^<.*>$') {
        return New-GateResult -State 'failure' -Reason 'Missing Orchestrator in PR slice header.' -Marker $Marker -WriterFamily $writerFamily -Orchestrator $orchestrator -Tier $tierInfo.Tier
    }

    if ($Marker.ReviewerFamily -eq $writerFamily) {
        return New-GateResult -State 'failure' -Reason "Reviewer family '$($Marker.ReviewerFamily)' matches writer family '$writerFamily'." -Marker $Marker -WriterFamily $writerFamily -Orchestrator $orchestrator -Tier $tierInfo.Tier
    }

    if ((Get-NormalizedIdentifier $Marker.Dispatcher) -eq (Get-NormalizedIdentifier $orchestrator)) {
        return New-GateResult -State 'failure' -Reason 'Final review dispatcher must differ from PR Orchestrator.' -Marker $Marker -WriterFamily $writerFamily -Orchestrator $orchestrator -Tier $tierInfo.Tier
    }

    return Resolve-MarkerHeadOrCarryForward `
        -Marker $Marker `
        -HeadSha $HeadSha `
        -WriterFamily $writerFamily `
        -Orchestrator $orchestrator `
        -Tier $tierInfo.Tier `
        -BaseRef $BaseRef `
        -RepositoryRoot $RepositoryRoot `
        -CarryForwardEvaluator $CarryForwardEvaluator
}

function Get-ReviewGateResult {
    [CmdletBinding()]
    param(
        [AllowNull()]
        [AllowEmptyString()]
        [string] $PullRequestAuthor,

        [AllowNull()]
        [AllowEmptyString()]
        [string] $PullRequestBody,

        [Parameter(Mandatory = $true)]
        [string] $HeadSha,

        [string] $BaseSha = '',

        [string] $HeadRefName = '',

        [AllowNull()]
        [object[]] $Commits = @(),

        [AllowNull()]
        [object[]] $Comments = @(),

        [AllowNull()]
        [object[]] $ReviewThreads = @(),

        [string] $BaseRef = 'origin/main',

        [string] $RepositoryRoot = (Get-Location).Path,

        [string] $RepositoryOwner = '',

        [string] $IdentitiesPath = '',

        [AllowNull()]
        [object] $IdentitiesConfig = $null,

        [AllowNull()]
        [scriptblock] $CarryForwardEvaluator = $null
    )

    $authorTrimmed = if ($null -eq $PullRequestAuthor) { '' } else { $PullRequestAuthor.Trim() }
    if ($authorTrimmed -in @('dependabot[bot]', 'app/dependabot')) {
        return New-GateResult -State 'success' -Reason 'exempt: dependency bot'
    }

    $identities = Get-IdentitiesConfig -RepositoryRoot $RepositoryRoot -IdentitiesPath $IdentitiesPath -IdentitiesConfig $IdentitiesConfig
    $inferredWriterFamilies = @(Get-InferredWriterFamilies -HeadRefName $HeadRefName -Commits $Commits)
    $markers = @(Get-FinalReviewMarkers -Comments $Comments)

    # Unresolved review threads fail the gate regardless of any verdict.
    if (Test-HasUnresolvedReviewThreads -ReviewThreads $ReviewThreads) {
        return New-GateResult -State 'failure' -Reason 'Unresolved review threads block the final review.'
    }

    # Only issue-comment markers count. Keep the eligible ones (current head and exact current base, or a
    # validated carry-forward); ineligible markers never hide an eligible older one.
    $eligible = @($markers | Where-Object {
            Test-MarkerEligible -Marker $_ -HeadSha $HeadSha -BaseSha $BaseSha -BaseRef $BaseRef -RepositoryRoot $RepositoryRoot -CarryForwardEvaluator $CarryForwardEvaluator
        })

    if ($eligible.Count -eq 0) {
        $reason = if ($markers.Count -gt 0) {
            'No moba-final-review marker matches the PR head and base (stale review).'
        }
        elseif (Test-HasMalformedReviewMarker -Comments $Comments) {
            'Invalid moba-final-review marker: check required fields (including base=).'
        }
        else {
            'Missing valid moba-final-review marker.'
        }
        return New-GateResult -State 'failure' -Reason $reason
    }

    # The newest eligible marker wins; on equal timestamps CHANGES wins.
    # $markers is ordered newest first.
    $newest = $eligible[0]
    $winner = $newest
    foreach ($candidate in $eligible) {
        if ($candidate.CreatedAt -eq $newest.CreatedAt -and $candidate.Verdict -eq 'CHANGES') {
            $winner = $candidate
            break
        }
    }

    return Resolve-MarkerGateResult `
        -Marker $winner `
        -HeadSha $HeadSha `
        -BaseSha $BaseSha `
        -PullRequestBody $PullRequestBody `
        -InferredWriterFamilies $inferredWriterFamilies `
        -Identities $identities `
        -RepositoryOwner $RepositoryOwner `
        -BaseRef $BaseRef `
        -RepositoryRoot $RepositoryRoot `
        -CarryForwardEvaluator $CarryForwardEvaluator
}

function Get-GitHubPaginatedList {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Endpoint
    )

    $rawJson = gh api --paginate --slurp $Endpoint
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to read GitHub API endpoint '$Endpoint'."
    }

    $pages = $rawJson | ConvertFrom-Json
    $items = [Collections.Generic.List[object]]::new()
    foreach ($page in @($pages)) {
        if ($page -is [System.Collections.IEnumerable] -and $page -isnot [string]) {
            foreach ($item in $page) {
                $items.Add($item)
            }
        }
        elseif ($null -ne $page) {
            $items.Add($page)
        }
    }

    return @($items)
}

function Read-ReviewThreadPages {
    param(
        # Called with the end cursor (or $null for the first page); returns an object with
        # nodes, hasNextPage and endCursor.
        [Parameter(Mandatory = $true)]
        [scriptblock] $FetchPage
    )

    $threads = [Collections.Generic.List[object]]::new()
    $cursor = $null
    do {
        $page = & $FetchPage $cursor
        foreach ($node in @($page.nodes)) {
            $threads.Add($node)
        }

        # An unresolved thread already blocks the gate; no need to read further pages.
        if (Test-HasUnresolvedReviewThreads -ReviewThreads @($page.nodes)) {
            break
        }

        $hasNext = [bool] $page.hasNextPage
        $cursor = $page.endCursor
    } while ($hasNext)

    return @($threads)
}

function Get-GitHubReviewThreads {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Repository,

        [Parameter(Mandatory = $true)]
        [int] $PullRequest
    )

    $parts = $Repository.Split('/', 2)
    if ($parts.Count -ne 2) {
        throw "Invalid Repository '$Repository'; expected owner/name."
    }

    $query = 'query($owner:String!,$name:String!,$number:Int!,$cursor:String){repository(owner:$owner,name:$name){pullRequest(number:$number){reviewThreads(first:100,after:$cursor){pageInfo{hasNextPage endCursor} nodes{isResolved isOutdated}}}}}'
    $fetch = {
        param($cursor)
        $ghArgs = @('api', 'graphql', '-f', "query=$query", '-f', "owner=$($parts[0])", '-f', "name=$($parts[1])", '-F', "number=$PullRequest")
        if (-not [string]::IsNullOrEmpty($cursor)) {
            $ghArgs += @('-f', "cursor=$cursor")
        }
        $raw = & gh @ghArgs
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to query review threads for PR #$PullRequest in $Repository."
        }
        $data = $raw | ConvertFrom-Json
        $threadsNode = $data.data.repository.pullRequest.reviewThreads
        return [pscustomobject] @{
            nodes = @($threadsNode.nodes)
            hasNextPage = [bool] $threadsNode.pageInfo.hasNextPage
            endCursor = $threadsNode.pageInfo.endCursor
        }
    }.GetNewClosure()

    return Read-ReviewThreadPages -FetchPage $fetch
}

if ($MyInvocation.InvocationName -eq '.') {
    return
}

if ($PullRequest -le 0) {
    throw 'PullRequest must be a positive pull request number.'
}
if ([string]::IsNullOrWhiteSpace($Repository)) {
    throw 'Repository is required (owner/name).'
}

$prJson = gh api "repos/$Repository/pulls/$PullRequest"
if ($LASTEXITCODE -ne 0) {
    throw "Failed to read PR #$PullRequest from $Repository."
}
$pr = $prJson | ConvertFrom-Json

$comments = Get-GitHubPaginatedList -Endpoint "repos/$Repository/issues/$PullRequest/comments"
$commits = Get-GitHubPaginatedList -Endpoint "repos/$Repository/pulls/$PullRequest/commits"
$threads = Get-GitHubReviewThreads -Repository $Repository -PullRequest $PullRequest

$headSha = [string] $pr.head.sha
$baseSha = [string] $pr.base.sha
$baseRef = "origin/$($pr.base.ref)"

$result = Get-ReviewGateResult `
    -PullRequestAuthor ([string] $pr.user.login) `
    -PullRequestBody ([string] $pr.body) `
    -HeadSha $headSha `
    -BaseSha $baseSha `
    -HeadRefName ([string] $pr.head.ref) `
    -Commits $commits `
    -Comments $comments `
    -ReviewThreads $threads `
    -RepositoryOwner ([string] $pr.base.repo.owner.login) `
    -BaseRef $baseRef `
    -RepositoryRoot $RepositoryRoot

Write-Host "Review gate PR #$PullRequest ($headSha): state=$($result.State) tier=$($result.Tier) description=$($result.Description)"

if ($SetStatus) {
    gh api "repos/$Repository/statuses/$headSha" `
        -f "state=$($result.State)" `
        -f "context=$script:StatusContext" `
        -f "description=$($result.Description)" | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to publish commit status for PR #$PullRequest ($headSha)."
    }
    exit 0
}

if ($result.State -ne 'success') {
    exit 1
}
exit 0
