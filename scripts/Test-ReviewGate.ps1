[CmdletBinding()]
param(
    [int] $PullRequest = 0,

    [string] $Repository = '',

    [switch] $SetStatus,

    [string] $RepositoryRoot = (Split-Path -Parent $PSScriptRoot)
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

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

    # Antigravity can host built-in Claude models (for example "Antigravity Opus"); treat explicit Claude model names as claude unless OpenAI also matched.
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

    return [pscustomobject] @{
        Index = $Index
        HasCreatedAt = $hasDate
        CreatedAt = if ($hasDate) { $parsedDate } else { [DateTimeOffset]::MinValue }
        CommentId = (Get-ObjectField -Source $Comment -Names @('id', 'Id'))
        Author = if ([string]::IsNullOrWhiteSpace($author)) { $null } else { $author }
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

    foreach ($requiredKey in @('verdict', 'head', 'reviewer-family', 'reviewer-model', 'dispatcher')) {
        if (-not $attrs.ContainsKey($requiredKey) -or [string]::IsNullOrWhiteSpace($attrs[$requiredKey])) {
            return $null
        }
    }

    $verdict = $attrs['verdict'].ToUpperInvariant()
    if ($verdict -notin @('PASS', 'CHANGES')) {
        return $null
    }

    $head = $attrs['head'].ToLowerInvariant()
    if ($head -notmatch '^[0-9a-f]{40}$') {
        return $null
    }

    $reviewerFamily = $attrs['reviewer-family'].ToLowerInvariant()
    if ($reviewerFamily -notin @('claude', 'openai', 'gemini')) {
        return $null
    }

    return [pscustomobject] @{
        Verdict = $verdict
        Head = $head
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

    $orderedComments = Get-OrderedComments -Comments $Comments
    if ($orderedComments.Count -eq 0) {
        return @()
    }

    $markerRegex = [Text.RegularExpressions.Regex]::new(
        '<!--\s*moba-final-review\b(?<attrs>.*?)-->',
        [Text.RegularExpressions.RegexOptions]::IgnoreCase -bor [Text.RegularExpressions.RegexOptions]::Singleline)
    $attrRegex = [Text.RegularExpressions.Regex]::new(
        '\b(?<key>verdict|head|reviewer-family|reviewer-model|dispatcher)\s*=\s*(?:"(?<qval>[^"]*)"|''(?<sval>[^'']*)''|(?<uval>.*?))(?=\s+(?:verdict|head|reviewer-family|reviewer-model|dispatcher)\s*=|\s*$)',
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

function Get-GitMergeBase {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Sha,

        [Parameter(Mandatory = $true)]
        [string] $BaseRef,

        [Parameter(Mandatory = $true)]
        [string] $RepositoryRoot
    )

    $start = New-CleanGitProcessStartInfo -FileName 'git' -Arguments @('-C', $RepositoryRoot, 'merge-base', $BaseRef, $Sha)
    $proc = [Diagnostics.Process]::Start($start)
    try {
        $out = $proc.StandardOutput.ReadToEndAsync()
        $null = $proc.StandardError.ReadToEndAsync()
        $proc.WaitForExit()
        if ($proc.ExitCode -ne 0) {
            return $null
        }
        $value = $out.Result.Trim()
        if ([string]::IsNullOrWhiteSpace($value)) {
            return $null
        }
        return $value
    }
    finally {
        $proc.Dispose()
    }
}

function Get-GitDiffPatchId {
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

    $diffStart = New-CleanGitProcessStartInfo -FileName 'git' -Arguments @('-C', $RepositoryRoot, 'diff', $mergeBase, $Sha)
    $patchStart = New-CleanGitProcessStartInfo -FileName 'git' -Arguments @('-C', $RepositoryRoot, 'patch-id', '--stable')
    $patchStart.RedirectStandardInput = $true

    $diffProc = [Diagnostics.Process]::Start($diffStart)
    $patchProc = [Diagnostics.Process]::Start($patchStart)
    try {
        $null = $diffProc.StandardError.ReadToEndAsync()
        $patchOut = $patchProc.StandardOutput.ReadToEndAsync()
        $null = $patchProc.StandardError.ReadToEndAsync()

        $diffProc.StandardOutput.BaseStream.CopyTo($patchProc.StandardInput.BaseStream)
        $patchProc.StandardInput.Close()

        $diffProc.WaitForExit()
        $patchProc.WaitForExit()

        if ($diffProc.ExitCode -ne 0 -or $patchProc.ExitCode -ne 0) {
            return $null
        }

        $patchText = $patchOut.Result.Trim()
        if ([string]::IsNullOrWhiteSpace($patchText)) {
            return ''
        }

        return ($patchText -split '\s+')[0].ToLowerInvariant()
    }
    finally {
        $diffProc.Dispose()
        $patchProc.Dispose()
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

    $markerPatchId = Get-GitDiffPatchId -Sha $MarkerHead -BaseRef $BaseRef -RepositoryRoot $RepositoryRoot
    if ([string]::IsNullOrEmpty($markerPatchId)) {
        return $false
    }

    $currentPatchId = Get-GitDiffPatchId -Sha $CurrentHead -BaseRef $BaseRef -RepositoryRoot $RepositoryRoot
    if ([string]::IsNullOrEmpty($currentPatchId)) {
        return $false
    }

    return $markerPatchId.Equals($currentPatchId, [StringComparison]::OrdinalIgnoreCase)
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
        [string] $Orchestrator = $null
    )

    return [pscustomobject] @{
        State = $State
        Description = Format-StatusDescription $Reason
        Marker = $Marker
        WriterFamily = $WriterFamily
        Orchestrator = $Orchestrator
    }
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
    if ($Sha.Length -ge 8) {
        return $Sha.Substring(0, 8)
    }
    return $Sha
}

function Resolve-MarkerHeadMatchResult {
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
        [string] $BaseRef,

        [Parameter(Mandatory = $true)]
        [string] $RepositoryRoot,

        [AllowNull()]
        [scriptblock] $PatchEquivalentEvaluator = $null
    )

    $shortHead = Get-ShortSha $HeadSha
    $shortMarkerHead = Get-ShortSha $Marker.Head

    if ($Marker.Head.Equals($HeadSha, [StringComparison]::OrdinalIgnoreCase)) {
        return New-GateResult `
            -State 'success' `
            -Reason "Verified $($Marker.ReviewerFamily) ($($Marker.ReviewerModel)) PASS on $shortHead." `
            -Marker $Marker `
            -WriterFamily $WriterFamily `
            -Orchestrator $Orchestrator
    }

    $isEquivalent = if ($null -ne $PatchEquivalentEvaluator) {
        [bool] (& $PatchEquivalentEvaluator $Marker.Head $HeadSha $BaseRef $RepositoryRoot)
    }
    else {
        Test-PatchEquivalent -MarkerHead $Marker.Head -CurrentHead $HeadSha -BaseRef $BaseRef -RepositoryRoot $RepositoryRoot
    }

    if ($isEquivalent) {
        return New-GateResult `
            -State 'success' `
            -Reason "Verified $($Marker.ReviewerFamily) PASS (patch-equivalent to $shortMarkerHead)." `
            -Marker $Marker `
            -WriterFamily $WriterFamily `
            -Orchestrator $Orchestrator
    }

    return New-GateResult `
        -State 'failure' `
        -Reason "Review marker for $shortMarkerHead is stale for $shortHead." `
        -Marker $Marker `
        -WriterFamily $WriterFamily `
        -Orchestrator $Orchestrator
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

        [AllowNull()]
        [object[]] $Comments = @(),

        [string] $BaseRef = 'origin/main',

        [string] $RepositoryRoot = (Get-Location).Path,

        [AllowNull()]
        [scriptblock] $PatchEquivalentEvaluator = $null
    )

    $authorTrimmed = if ($null -eq $PullRequestAuthor) { '' } else { $PullRequestAuthor.Trim() }
    if ($authorTrimmed -in @('dependabot[bot]', 'app/dependabot')) {
        return New-GateResult -State 'success' -Reason 'exempt: dependency bot'
    }

    $markers = @(Get-FinalReviewMarkers -Comments $Comments)
    if ($markers.Count -eq 0) {
        $reason = if (Test-HasMalformedReviewMarker -Comments $Comments) {
            'Invalid moba-final-review marker: check required fields.'
        }
        else {
            'Missing valid moba-final-review marker.'
        }
        return New-GateResult -State 'failure' -Reason $reason
    }

    $newest = $markers[0]
    if ($newest.Verdict -eq 'CHANGES') {
        return New-GateResult -State 'failure' -Reason "Final review verdict is CHANGES for $(Get-ShortSha $newest.Head)." -Marker $newest
    }

    $writerResolution = Resolve-WriterFamily -PullRequestBody $PullRequestBody
    if ($null -eq $writerResolution.Family) {
        return New-GateResult -State 'failure' -Reason $writerResolution.Error -Marker $newest
    }

    $writerFamily = $writerResolution.Family
    $orchestrator = Get-SliceHeaderField -Body $PullRequestBody -FieldName 'Orchestrator'
    if ([string]::IsNullOrWhiteSpace($orchestrator) -or $orchestrator -match '^<.*>$') {
        return New-GateResult -State 'failure' -Reason 'Missing Orchestrator in PR slice header.' -Marker $newest -WriterFamily $writerFamily -Orchestrator $orchestrator
    }

    if ($newest.ReviewerFamily -eq $writerFamily) {
        return New-GateResult -State 'failure' -Reason "Reviewer family '$($newest.ReviewerFamily)' matches writer family '$writerFamily'." -Marker $newest -WriterFamily $writerFamily -Orchestrator $orchestrator
    }

    if ((Get-NormalizedIdentifier $newest.Dispatcher) -eq (Get-NormalizedIdentifier $orchestrator)) {
        return New-GateResult -State 'failure' -Reason 'Final review dispatcher must differ from PR Orchestrator.' -Marker $newest -WriterFamily $writerFamily -Orchestrator $orchestrator
    }

    return Resolve-MarkerHeadMatchResult `
        -Marker $newest `
        -HeadSha $HeadSha `
        -WriterFamily $writerFamily `
        -Orchestrator $orchestrator `
        -BaseRef $BaseRef `
        -RepositoryRoot $RepositoryRoot `
        -PatchEquivalentEvaluator $PatchEquivalentEvaluator
}

function Get-GitHubPullRequestComments {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Repository,

        [Parameter(Mandatory = $true)]
        [int] $PullRequest
    )

    $commentsJson = gh api --paginate --slurp "repos/$Repository/issues/$PullRequest/comments"
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to read comments for PR #$PullRequest from $Repository."
    }

    $pages = $commentsJson | ConvertFrom-Json
    $comments = [Collections.Generic.List[object]]::new()
    foreach ($page in @($pages)) {
        if ($page -is [System.Collections.IEnumerable] -and $page -isnot [string]) {
            foreach ($item in $page) {
                $comments.Add($item)
            }
        }
        elseif ($null -ne $page) {
            $comments.Add($page)
        }
    }

    return @($comments)
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
$comments = Get-GitHubPullRequestComments -Repository $Repository -PullRequest $PullRequest

$headSha = [string] $pr.head.sha
$baseRef = "origin/$($pr.base.ref)"
$result = Get-ReviewGateResult `
    -PullRequestAuthor ([string] $pr.user.login) `
    -PullRequestBody ([string] $pr.body) `
    -HeadSha $headSha `
    -Comments $comments `
    -BaseRef $baseRef `
    -RepositoryRoot $RepositoryRoot

Write-Host "Review gate PR #$PullRequest ($headSha): state=$($result.State) description=$($result.Description)"

if ($SetStatus) {
    gh api "repos/$Repository/statuses/$headSha" `
        -f "state=$($result.State)" `
        -f "context=Independent final review" `
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
