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

    $value = $match.Groups['value'].Value
    $value = [Text.RegularExpressions.Regex]::Replace($value, '<!--.*?-->', '').Trim()
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

function Get-FinalReviewMarkers {
    [CmdletBinding()]
    param(
        [AllowNull()]
        [object[]] $Comments = @()
    )

    if ($null -eq $Comments -or $Comments.Count -eq 0) {
        return @()
    }

    $indexed = [Collections.Generic.List[pscustomobject]]::new()
    $index = 0
    foreach ($comment in $Comments) {
        if ($null -eq $comment) {
            $index++
            continue
        }

        $body = $null
        $createdAt = $null
        $hasCreatedAt = $false
        $commentId = $null
        $author = $null

        if ($comment -is [string]) {
            $body = $comment
        }
        elseif ($comment -is [System.Collections.IDictionary]) {
            if ($comment.Contains('body')) { $body = [string] $comment['body'] }
            elseif ($comment.Contains('Body')) { $body = [string] $comment['Body'] }
            if ($comment.Contains('created_at') -and -not [string]::IsNullOrWhiteSpace([string] $comment['created_at'])) {
                $parsed = [DateTimeOffset]::MinValue
                if ([DateTimeOffset]::TryParse([string] $comment['created_at'], [ref] $parsed)) {
                    $createdAt = $parsed
                    $hasCreatedAt = $true
                }
            }
            elseif ($comment.Contains('CreatedAt') -and -not [string]::IsNullOrWhiteSpace([string] $comment['CreatedAt'])) {
                $parsed = [DateTimeOffset]::MinValue
                if ([DateTimeOffset]::TryParse([string] $comment['CreatedAt'], [ref] $parsed)) {
                    $createdAt = $parsed
                    $hasCreatedAt = $true
                }
            }
            if ($comment.Contains('id')) { $commentId = $comment['id'] }
            if ($comment.Contains('user') -and $null -ne $comment['user']) { $author = [string] $comment['user'].login }
        }
        else {
            if ($null -ne $comment.PSObject.Properties['body']) { $body = [string] $comment.body }
            if ($null -ne $comment.PSObject.Properties['created_at'] -and -not [string]::IsNullOrWhiteSpace([string] $comment.created_at)) {
                $parsed = [DateTimeOffset]::MinValue
                if ([DateTimeOffset]::TryParse([string] $comment.created_at, [ref] $parsed)) {
                    $createdAt = $parsed
                    $hasCreatedAt = $true
                }
            }
            if ($null -ne $comment.PSObject.Properties['id']) { $commentId = $comment.id }
            if ($null -ne $comment.PSObject.Properties['user'] -and $null -ne $comment.user -and $null -ne $comment.user.PSObject.Properties['login']) {
                $author = [string] $comment.user.login
            }
        }

        $indexed.Add([pscustomobject] @{
            Index = $index
            HasCreatedAt = $hasCreatedAt
            CreatedAt = if ($hasCreatedAt) { $createdAt } else { [DateTimeOffset]::MinValue }
            CommentId = $commentId
            Author = $author
            Body = $body
        })
        $index++
    }

    $allHaveTimestamps = $indexed.Count -gt 0 -and (@($indexed | Where-Object { -not $_.HasCreatedAt }).Count -eq 0)
    $orderedComments = if ($allHaveTimestamps) {
        @($indexed | Sort-Object -Property @{ Expression = { $_.CreatedAt }; Descending = $false }, @{ Expression = { $_.Index }; Descending = $false })
    }
    else {
        @($indexed | Sort-Object -Property Index)
    }

    $markerRegex = [Text.RegularExpressions.Regex]::new(
        '<!--\s*moba-final-review\b(?<attrs>.*?)-->',
        [Text.RegularExpressions.RegexOptions]::IgnoreCase -bor [Text.RegularExpressions.RegexOptions]::Singleline)
    $attrRegex = [Text.RegularExpressions.Regex]::new(
        '\b(?<key>verdict|head|reviewer-family|reviewer-model|dispatcher)\s*=\s*(?:"(?<qval>[^"]*)"|''(?<sval>[^'']*)''|(?<uval>.*?))(?=\s+(?:verdict|head|reviewer-family|reviewer-model|dispatcher)\s*=|\s*$)',
        [Text.RegularExpressions.RegexOptions]::IgnoreCase -bor [Text.RegularExpressions.RegexOptions]::Singleline)

    $chronologicalMarkers = [Collections.Generic.List[pscustomobject]]::new()
    foreach ($entry in $orderedComments) {
        if ([string]::IsNullOrWhiteSpace($entry.Body)) {
            continue
        }

        foreach ($match in $markerRegex.Matches($entry.Body)) {
            $attrsText = $match.Groups['attrs'].Value.Trim()
            $attrs = @{}
            foreach ($attrMatch in $attrRegex.Matches($attrsText)) {
                $key = $attrMatch.Groups['key'].Value.ToLowerInvariant()
                $val = if ($attrMatch.Groups['qval'].Success) {
                    $attrMatch.Groups['qval'].Value
                }
                elseif ($attrMatch.Groups['sval'].Success) {
                    $attrMatch.Groups['sval'].Value
                }
                else {
                    $attrMatch.Groups['uval'].Value
                }
                $attrs[$key] = $val.Trim()
            }

            if (-not $attrs.ContainsKey('verdict') -or
                -not $attrs.ContainsKey('head') -or
                -not $attrs.ContainsKey('reviewer-family') -or
                -not $attrs.ContainsKey('reviewer-model') -or
                -not $attrs.ContainsKey('dispatcher')) {
                continue
            }

            $verdict = $attrs['verdict'].ToUpperInvariant()
            if ($verdict -notin @('PASS', 'CHANGES')) {
                continue
            }

            $head = $attrs['head'].ToLowerInvariant()
            if ($head -notmatch '^[0-9a-f]{40}$') {
                continue
            }

            $reviewerFamily = $attrs['reviewer-family'].ToLowerInvariant()
            if ($reviewerFamily -notin @('claude', 'openai', 'gemini')) {
                continue
            }

            $reviewerModel = $attrs['reviewer-model']
            $dispatcher = $attrs['dispatcher']
            if ([string]::IsNullOrWhiteSpace($reviewerModel) -or [string]::IsNullOrWhiteSpace($dispatcher)) {
                continue
            }

            $chronologicalMarkers.Add([pscustomobject] @{
                Verdict = $verdict
                Head = $head
                ReviewerFamily = $reviewerFamily
                ReviewerModel = $reviewerModel
                Dispatcher = $dispatcher
                CommentId = $entry.CommentId
                CreatedAt = if ($entry.HasCreatedAt) { $entry.CreatedAt } else { $null }
                Author = $entry.Author
            })
        }
    }

    if ($chronologicalMarkers.Count -eq 0) {
        return @()
    }

    $newestFirst = [Collections.Generic.List[pscustomobject]]::new()
    for ($i = $chronologicalMarkers.Count - 1; $i -ge 0; $i--) {
        $newestFirst.Add($chronologicalMarkers[$i])
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

function Get-GitDiffPatchId {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Sha,

        [Parameter(Mandatory = $true)]
        [string] $BaseRef,

        [Parameter(Mandatory = $true)]
        [string] $RepositoryRoot
    )

    $mergeBaseStart = New-CleanGitProcessStartInfo -FileName 'git' -Arguments @('-C', $RepositoryRoot, 'merge-base', $BaseRef, $Sha)
    $mergeBaseProc = [Diagnostics.Process]::Start($mergeBaseStart)
    try {
        $mbOut = $mergeBaseProc.StandardOutput.ReadToEndAsync()
        $mbErr = $mergeBaseProc.StandardError.ReadToEndAsync()
        $mergeBaseProc.WaitForExit()
        if ($mergeBaseProc.ExitCode -ne 0) {
            return $null
        }
        $mergeBase = $mbOut.Result.Trim()
    }
    finally {
        $mergeBaseProc.Dispose()
    }

    if ([string]::IsNullOrWhiteSpace($mergeBase)) {
        return $null
    }

    $diffStart = New-CleanGitProcessStartInfo -FileName 'git' -Arguments @('-C', $RepositoryRoot, 'diff', $mergeBase, $Sha)
    $patchStart = New-CleanGitProcessStartInfo -FileName 'git' -Arguments @('-C', $RepositoryRoot, 'patch-id', '--stable')
    $patchStart.RedirectStandardInput = $true

    $diffProc = [Diagnostics.Process]::Start($diffStart)
    $patchProc = [Diagnostics.Process]::Start($patchStart)
    try {
        $diffErr = $diffProc.StandardError.ReadToEndAsync()
        $patchOut = $patchProc.StandardOutput.ReadToEndAsync()
        $patchErr = $patchProc.StandardError.ReadToEndAsync()

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

        $firstToken = ($patchText -split '\s+')[0]
        return $firstToken.ToLowerInvariant()
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
    if ($null -eq $markerPatchId -or $markerPatchId -eq '') {
        return $false
    }

    $currentPatchId = Get-GitDiffPatchId -Sha $CurrentHead -BaseRef $BaseRef -RepositoryRoot $RepositoryRoot
    if ($null -eq $currentPatchId -or $currentPatchId -eq '') {
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
        return [pscustomobject] @{
            State = 'success'
            Description = Format-StatusDescription 'exempt: dependency bot'
            Marker = $null
            WriterFamily = $null
            Orchestrator = $null
        }
    }

    $markers = @(Get-FinalReviewMarkers -Comments $Comments)
    if ($markers.Count -eq 0) {
        $hasRawMarker = $false
        foreach ($comment in @($Comments)) {
            $bodyText = if ($comment -is [string]) { $comment }
                elseif ($comment -is [System.Collections.IDictionary] -and $comment.Contains('body')) { [string] $comment['body'] }
                elseif ($null -ne $comment -and $null -ne $comment.PSObject.Properties['body']) { [string] $comment.body }
                else { '' }
            if ($bodyText -match '<!--\s*moba-final-review\b') {
                $hasRawMarker = $true
                break
            }
        }

        $reason = if ($hasRawMarker) {
            'Invalid moba-final-review marker: check required fields.'
        }
        else {
            'Missing valid moba-final-review marker.'
        }

        return [pscustomobject] @{
            State = 'failure'
            Description = Format-StatusDescription $reason
            Marker = $null
            WriterFamily = $null
            Orchestrator = $null
        }
    }

    $newest = $markers[0]
    $shortHead = if ($HeadSha.Length -ge 8) { $HeadSha.Substring(0, 8) } else { $HeadSha }
    $shortMarkerHead = if ($newest.Head.Length -ge 8) { $newest.Head.Substring(0, 8) } else { $newest.Head }

    if ($newest.Verdict -eq 'CHANGES') {
        return [pscustomobject] @{
            State = 'failure'
            Description = Format-StatusDescription "Final review verdict is CHANGES for $shortMarkerHead."
            Marker = $newest
            WriterFamily = $null
            Orchestrator = $null
        }
    }

    $writerFamilyField = Get-SliceHeaderField -Body $PullRequestBody -FieldName 'Writer family'
    $writerField = Get-SliceHeaderField -Body $PullRequestBody -FieldName 'Writer'

    $writerFamily = $null
    if (-not [string]::IsNullOrWhiteSpace($writerFamilyField) -and $writerFamilyField -notmatch '\|' -and $writerFamilyField -notmatch '^<.*>$') {
        $writerFamily = ConvertTo-ModelFamily -Value $writerFamilyField
        if ($null -eq $writerFamily) {
            return [pscustomobject] @{
                State = 'failure'
                Description = Format-StatusDescription "Unresolved Writer family '$writerFamilyField' in PR slice header."
                Marker = $newest
                WriterFamily = $null
                Orchestrator = $null
            }
        }
    }
    elseif (-not [string]::IsNullOrWhiteSpace($writerField)) {
        $writerFamily = ConvertTo-ModelFamily -Value $writerField
    }

    if ($null -eq $writerFamily) {
        return [pscustomobject] @{
            State = 'failure'
            Description = Format-StatusDescription 'Unable to resolve writer model family from PR slice header.'
            Marker = $newest
            WriterFamily = $null
            Orchestrator = $null
        }
    }

    $orchestrator = Get-SliceHeaderField -Body $PullRequestBody -FieldName 'Orchestrator'
    if ([string]::IsNullOrWhiteSpace($orchestrator) -or $orchestrator -match '^<.*>$') {
        return [pscustomobject] @{
            State = 'failure'
            Description = Format-StatusDescription 'Missing Orchestrator in PR slice header.'
            Marker = $newest
            WriterFamily = $writerFamily
            Orchestrator = $orchestrator
        }
    }

    if ($newest.ReviewerFamily -eq $writerFamily) {
        return [pscustomobject] @{
            State = 'failure'
            Description = Format-StatusDescription "Reviewer family '$($newest.ReviewerFamily)' matches writer family '$writerFamily'."
            Marker = $newest
            WriterFamily = $writerFamily
            Orchestrator = $orchestrator
        }
    }

    if ((Get-NormalizedIdentifier $newest.Dispatcher) -eq (Get-NormalizedIdentifier $orchestrator)) {
        return [pscustomobject] @{
            State = 'failure'
            Description = Format-StatusDescription 'Final review dispatcher must differ from PR Orchestrator.'
            Marker = $newest
            WriterFamily = $writerFamily
            Orchestrator = $orchestrator
        }
    }

    if ($newest.Head.Equals($HeadSha, [StringComparison]::OrdinalIgnoreCase)) {
        return [pscustomobject] @{
            State = 'success'
            Description = Format-StatusDescription "Verified $($newest.ReviewerFamily) ($($newest.ReviewerModel)) PASS on $shortHead."
            Marker = $newest
            WriterFamily = $writerFamily
            Orchestrator = $orchestrator
        }
    }

    $isEquivalent = if ($null -ne $PatchEquivalentEvaluator) {
        [bool] (& $PatchEquivalentEvaluator $newest.Head $HeadSha $BaseRef $RepositoryRoot)
    }
    else {
        Test-PatchEquivalent -MarkerHead $newest.Head -CurrentHead $HeadSha -BaseRef $BaseRef -RepositoryRoot $RepositoryRoot
    }

    if ($isEquivalent) {
        return [pscustomobject] @{
            State = 'success'
            Description = Format-StatusDescription "Verified $($newest.ReviewerFamily) PASS (patch-equivalent to $shortMarkerHead)."
            Marker = $newest
            WriterFamily = $writerFamily
            Orchestrator = $orchestrator
        }
    }

    return [pscustomobject] @{
        State = 'failure'
        Description = Format-StatusDescription "Review marker for $shortMarkerHead is stale for $shortHead."
        Marker = $newest
        WriterFamily = $writerFamily
        Orchestrator = $orchestrator
    }
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

$commentsJson = gh api --paginate --slurp "repos/$Repository/issues/$PullRequest/comments"
if ($LASTEXITCODE -ne 0) {
    throw "Failed to read comments for PR #$PullRequest from $Repository."
}
$commentPages = $commentsJson | ConvertFrom-Json
$comments = [Collections.Generic.List[object]]::new()
foreach ($page in @($commentPages)) {
    if ($page -is [System.Collections.IEnumerable] -and $page -isnot [string]) {
        foreach ($item in $page) {
            $comments.Add($item)
        }
    }
    elseif ($null -ne $page) {
        $comments.Add($page)
    }
}

$headSha = [string] $pr.head.sha
$baseRef = "origin/$($pr.base.ref)"
$result = Get-ReviewGateResult `
    -PullRequestAuthor ([string] $pr.user.login) `
    -PullRequestBody ([string] $pr.body) `
    -HeadSha $headSha `
    -Comments @($comments) `
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
