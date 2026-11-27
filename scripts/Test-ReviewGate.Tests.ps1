[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$scriptPath = Join-Path $PSScriptRoot 'Test-ReviewGate.ps1'
. $scriptPath

$checks = 0

function Assert-Equal($Expected, $Actual, [string] $Scenario) {
    if ($Expected -ne $Actual) {
        throw "Expected '$Expected' for '$Scenario', but got '$Actual'."
    }
    $script:checks++
}

function Assert-True([bool] $Condition, [string] $Message) {
    if (-not $Condition) {
        throw $Message
    }
    $script:checks++
}

$headA = '1111111111111111111111111111111111111111'
$headB = '2222222222222222222222222222222222222222'
$baseA = 'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa'
$wrongBase = 'bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb'

$defaultIdentities = @{
    trustedReviewers = @{
        'chatgpt-codex-connector[bot]' = 'openai'
        'claude[bot]' = 'claude'
        'gemini-code-assist[bot]' = 'gemini'
    }
    allowDeclaredReviews = $true
}

$strictIdentities = @{
    trustedReviewers = @{
        'chatgpt-codex-connector[bot]' = 'openai'
        'claude[bot]' = 'claude'
        'gemini-code-assist[bot]' = 'gemini'
    }
    allowDeclaredReviews = $false
}

$standardBody = @"
## Slice header

- Issue: #561
- Risk class (H/M/L): M
- Locked areas: scripts/Test-ReviewGate.ps1
- Writer family: claude
- Writer: Claude Opus 5.5
- Orchestrator: Opus Head
"@

# 1. Slice header extraction and model-family normalization.
Assert-Equal 'claude' (Get-SliceHeaderField -Body $standardBody -FieldName 'writer family') 'case-insensitive Writer family extraction'
Assert-Equal 'Opus Head' (Get-SliceHeaderField -Body $standardBody -FieldName 'ORCHESTRATOR') 'case-insensitive Orchestrator extraction'
Assert-Equal 'openai' (ConvertTo-ModelFamily 'Codex') 'keyword fallback Codex -> openai'
Assert-Equal 'openai' (ConvertTo-ModelFamily 'GPT-6.2 Sol') 'keyword fallback GPT -> openai'
Assert-Equal 'claude' (ConvertTo-ModelFamily 'Claude Opus 5.5') 'keyword fallback Claude -> claude'
Assert-Equal 'gemini' (ConvertTo-ModelFamily 'Gemini 4 Carbon') 'keyword fallback Gemini -> gemini'
Assert-Equal 'gemini' (ConvertTo-ModelFamily 'Antigravity') 'keyword fallback Antigravity -> gemini'
Assert-Equal $null (ConvertTo-ModelFamily 'unknown-model') 'unknown model returns null'
Assert-Equal $null (ConvertTo-ModelFamily 'claude|openai|gemini') 'unfilled template returns null'

# 2. Marker parsing tolerance (requires base=, tolerant of case/spacing/quotes/order).
$tolerantComment = @{
    id = 10
    created_at = '2026-11-26T10:00:00Z'
    user = @{ login = 'ahuelsmann' }
    body = "Review summary`n<!--   MOBA-FINAL-REVIEW   dispatcher = `"Codex-TD`"  base = $baseA  reviewer-model=gpt-6.2-sol   reviewer-family = OpenAI   head = $headA   verdict = pass  -->"
}
$parsedMarkers = @(Get-FinalReviewMarkers -Comments @($tolerantComment))
Assert-Equal 1 $parsedMarkers.Count 'tolerant marker parsed'
Assert-Equal 'PASS' $parsedMarkers[0].Verdict 'verdict normalized to uppercase'
Assert-Equal $baseA $parsedMarkers[0].Base 'base extracted and lowercased'
Assert-Equal 'openai' $parsedMarkers[0].ReviewerFamily 'reviewer-family normalized to lowercase'
Assert-Equal 'Codex-TD' $parsedMarkers[0].Dispatcher 'quoted dispatcher extracted'

# 3. Missing base= in marker is rejected.
$missingBaseComment = @{
    id = 11
    created_at = '2026-11-26T10:00:00Z'
    user = @{ login = 'ahuelsmann' }
    body = "<!-- moba-final-review verdict=PASS head=$headA reviewer-family=openai reviewer-model=gpt-6.2-sol dispatcher=Codex-TD -->"
}
Assert-Equal 0 @(Get-FinalReviewMarkers -Comments @($missingBaseComment)).Count 'marker without base= is rejected'
$resMissingBase = Get-ReviewGateResult `
    -PullRequestAuthor 'ahuelsmann' `
    -PullRequestBody $standardBody `
    -HeadSha $headA `
    -BaseSha $baseA `
    -Comments @($missingBaseComment) `
    -IdentitiesConfig $defaultIdentities
Assert-Equal 'failure' $resMissingBase.State 'marker missing base= fails gate'
Assert-True ($resMissingBase.Description -like '*base=*') 'missing base= explanation mentions base='

# 4. Wrong base= in marker fails gate.
$wrongBaseComment = @{
    id = 12
    created_at = '2026-11-26T10:00:00Z'
    user = @{ login = 'ahuelsmann' }
    body = "<!-- moba-final-review verdict=PASS head=$headA base=$wrongBase reviewer-family=openai reviewer-model=gpt-6.2-sol dispatcher=Codex-TD -->"
}
$resWrongBase = Get-ReviewGateResult `
    -PullRequestAuthor 'ahuelsmann' `
    -PullRequestBody $standardBody `
    -HeadSha $headA `
    -BaseSha $baseA `
    -Comments @($wrongBaseComment) `
    -IdentitiesConfig $defaultIdentities
Assert-Equal 'failure' $resWrongBase.State 'mismatched base= fails gate'
Assert-True ($resWrongBase.Description -like '*does not match PR base*') 'mismatched base explanation clear'

# 5. Missing marker -> failure.
$resMissing = Get-ReviewGateResult `
    -PullRequestAuthor 'ahuelsmann' `
    -PullRequestBody $standardBody `
    -HeadSha $headA `
    -BaseSha $baseA `
    -Comments @(@{ id = 1; created_at = '2026-11-26T10:00:00Z'; body = 'LGTM without marker' }) `
    -IdentitiesConfig $defaultIdentities
Assert-Equal 'failure' $resMissing.State 'missing marker fails'
Assert-True ($resMissing.Description.Length -le 140) 'missing marker description <= 140 chars'

# 6. Declared vs Verified tiers (allowDeclaredReviews=true vs false, and trusted bot login).
$resDeclaredAccepted = Get-ReviewGateResult `
    -PullRequestAuthor 'ahuelsmann' `
    -PullRequestBody $standardBody `
    -HeadSha $headA `
    -BaseSha $baseA `
    -Comments @($tolerantComment) `
    -IdentitiesConfig $defaultIdentities
Assert-Equal 'success' $resDeclaredAccepted.State 'declared review accepted when allowDeclaredReviews=true'
Assert-True ($resDeclaredAccepted.Description.Contains('declared (shared account)')) 'declared description includes declared (shared account)'

$resDeclaredRejected = Get-ReviewGateResult `
    -PullRequestAuthor 'ahuelsmann' `
    -PullRequestBody $standardBody `
    -HeadSha $headA `
    -BaseSha $baseA `
    -Comments @($tolerantComment) `
    -IdentitiesConfig $strictIdentities
Assert-Equal 'failure' $resDeclaredRejected.State 'declared review rejected when allowDeclaredReviews=false'
Assert-True ($resDeclaredRejected.Description -like '*disabled*') 'strict mode rejection explains declared reviews are disabled'

$trustedBotMarker = @{
    id = 13
    created_at = '2026-11-26T10:00:00Z'
    user = @{ login = 'chatgpt-codex-connector[bot]' }
    body = "<!-- moba-final-review verdict=PASS head=$headA base=$baseA reviewer-family=openai reviewer-model=gpt-6.2 dispatcher=Codex-TD -->"
}
$resTrustedBotMarker = Get-ReviewGateResult `
    -PullRequestAuthor 'ahuelsmann' `
    -PullRequestBody $standardBody `
    -HeadSha $headA `
    -BaseSha $baseA `
    -Comments @($trustedBotMarker) `
    -IdentitiesConfig $strictIdentities
Assert-Equal 'success' $resTrustedBotMarker.State 'trusted bot marker succeeds even when allowDeclaredReviews=false'
Assert-True ($resTrustedBotMarker.Description.Contains('verified')) 'trusted bot description includes verified'

# 7. chatgpt-codex-connector[bot] review: explicit PASS marker bound to head and base, wrong SHA, unresolved thread.
$codexPassMarker = "<!-- moba-final-review verdict=PASS head=$headA base=$baseA reviewer-family=openai reviewer-model=gpt-6.2 dispatcher=Codex-TD -->"
$codexPassReview = @{
    user = @{ login = 'chatgpt-codex-connector[bot]' }
    commit_id = $headA
    state = 'COMMENTED'
    submitted_at = '2026-11-26T10:05:00Z'
    body = "Review summary`n$codexPassMarker"
}
$resCodexPass = Get-ReviewGateResult `
    -PullRequestAuthor 'ahuelsmann' `
    -PullRequestBody $standardBody `
    -HeadSha $headA `
    -BaseSha $baseA `
    -Reviews @($codexPassReview) `
    -ReviewThreads @(@{ isResolved = $true }) `
    -IdentitiesConfig $strictIdentities
Assert-Equal 'success' $resCodexPass.State 'chatgpt-codex-connector[bot] PASS on exact head with resolved threads succeeds'
Assert-True ($resCodexPass.Description.Contains('verified')) 'codex connector description contains verified'

$codexWrongShaReview = @{
    user = @{ login = 'chatgpt-codex-connector[bot]' }
    commit_id = $headB
    state = 'APPROVED'
    submitted_at = '2026-11-26T10:05:00Z'
    body = $codexPassMarker
}
$resCodexWrongSha = Get-ReviewGateResult `
    -PullRequestAuthor 'ahuelsmann' `
    -PullRequestBody $standardBody `
    -HeadSha $headA `
    -BaseSha $baseA `
    -Reviews @($codexWrongShaReview) `
    -ReviewThreads @() `
    -IdentitiesConfig $strictIdentities
Assert-Equal 'failure' $resCodexWrongSha.State 'chatgpt-codex-connector[bot] wrong SHA fails'

$resCodexUnresolved = Get-ReviewGateResult `
    -PullRequestAuthor 'ahuelsmann' `
    -PullRequestBody $standardBody `
    -HeadSha $headA `
    -BaseSha $baseA `
    -Reviews @($codexPassReview) `
    -ReviewThreads @(@{ isResolved = $false }) `
    -IdentitiesConfig $strictIdentities
Assert-Equal 'failure' $resCodexUnresolved.State 'chatgpt-codex-connector[bot] with unresolved review thread fails'
Assert-True ($resCodexUnresolved.Description -like '*Unresolved review threads*') 'unresolved thread failure clear'

function Get-ConnectorResult([hashtable] $Review) {
    return Get-ReviewGateResult `
        -PullRequestAuthor 'ahuelsmann' `
        -PullRequestBody $standardBody `
        -HeadSha $headA `
        -BaseSha $baseA `
        -Reviews @($Review) `
        -ReviewThreads @() `
        -IdentitiesConfig $strictIdentities
}

function New-ConnectorReview([string] $State, [AllowNull()][string] $Body) {
    $review = @{
        user = @{ login = 'chatgpt-codex-connector[bot]' }
        commit_id = $headA
        state = $State
        submitted_at = '2026-11-26T10:05:00Z'
    }
    if ($null -ne $Body) {
        $review.body = $Body
    }
    return $review
}

function New-ConnectorMarker([string] $Verdict, [string] $Head, [string] $Base, [string] $Family) {
    $baseAttr = if ([string]::IsNullOrEmpty($Base)) { '' } else { " base=$Base" }
    return "<!-- moba-final-review verdict=$Verdict head=$Head$baseAttr reviewer-family=$Family reviewer-model=gpt-6.2 dispatcher=Codex-TD -->"
}

# 7b. Negative: the connector path never passes without an explicit, head- and base-bound PASS marker.
$resPlainCommented = Get-ConnectorResult (New-ConnectorReview 'COMMENTED' 'Looks fine.')
Assert-Equal 'failure' $resPlainCommented.State 'plain COMMENTED connector review is never PASS'
Assert-True ($resPlainCommented.Description -like '*explicit moba-final-review PASS marker*') 'plain COMMENTED failure names the missing marker'
Assert-Equal 'failure' (Get-ConnectorResult (New-ConnectorReview 'COMMENTED' $null)).State 'connector review without body is never PASS'
Assert-Equal 'failure' (Get-ConnectorResult (New-ConnectorReview 'APPROVED' '')).State 'plain APPROVED connector review without marker is never PASS'
Assert-Equal 'failure' (Get-ConnectorResult (New-ConnectorReview 'COMMENTED' (New-ConnectorMarker 'PASS' $headA '' 'openai'))).State 'connector PASS marker without base= fails'
$resConnWrongBase = Get-ConnectorResult (New-ConnectorReview 'COMMENTED' (New-ConnectorMarker 'PASS' $headA $wrongBase 'openai'))
Assert-Equal 'failure' $resConnWrongBase.State 'connector PASS marker with wrong base fails'
Assert-True ($resConnWrongBase.Description -like '*does not match PR base*') 'connector wrong base explained'
Assert-Equal 'failure' (Get-ConnectorResult (New-ConnectorReview 'COMMENTED' (New-ConnectorMarker 'PASS' $headB $baseA 'openai'))).State 'connector PASS marker for a different head fails'
Assert-Equal 'failure' (Get-ConnectorResult (New-ConnectorReview 'COMMENTED' (New-ConnectorMarker 'CHANGES' $headA $baseA 'openai'))).State 'connector CHANGES marker fails'
Assert-Equal 'failure' (Get-ConnectorResult (New-ConnectorReview 'COMMENTED' (New-ConnectorMarker 'PASS' $headA $baseA 'gemini'))).State 'connector marker family differing from the trusted mapping fails'
Assert-Equal 'success' (Get-ConnectorResult (New-ConnectorReview 'COMMENTED' (New-ConnectorMarker 'PASS' $headA $baseA 'openai'))).State 'connector review with explicit head- and base-bound PASS marker succeeds'

# 7c. Newest verdict wins across sources: a newer CHANGES (marker or CHANGES_REQUESTED) overrides an earlier PASS.
$resNewerChangesRequested = Get-ReviewGateResult `
    -PullRequestAuthor 'ahuelsmann' `
    -PullRequestBody $standardBody `
    -HeadSha $headA `
    -BaseSha $baseA `
    -Comments @($tolerantComment) `
    -Reviews @(@{ user = @{ login = 'chatgpt-codex-connector[bot]' }; commit_id = $headA; state = 'CHANGES_REQUESTED'; submitted_at = '2026-11-26T10:30:00Z'; body = '' }) `
    -ReviewThreads @() `
    -IdentitiesConfig $defaultIdentities
Assert-Equal 'failure' $resNewerChangesRequested.State 'newer connector CHANGES_REQUESTED overrides earlier issue-comment PASS'

$newerChangesConnector = New-ConnectorReview 'COMMENTED' (New-ConnectorMarker 'CHANGES' $headA $baseA 'openai')
$newerChangesConnector.submitted_at = '2026-11-26T10:30:00Z'
$resNewerConnectorChangesMarker = Get-ReviewGateResult `
    -PullRequestAuthor 'ahuelsmann' `
    -PullRequestBody $standardBody `
    -HeadSha $headA `
    -BaseSha $baseA `
    -Comments @($tolerantComment) `
    -Reviews @($newerChangesConnector) `
    -ReviewThreads @() `
    -IdentitiesConfig $defaultIdentities
Assert-Equal 'failure' $resNewerConnectorChangesMarker.State 'newer connector CHANGES marker overrides earlier issue-comment PASS'

$olderChangesConnector = New-ConnectorReview 'CHANGES_REQUESTED' ''
$olderChangesConnector.submitted_at = '2026-11-26T09:00:00Z'
$resOlderConnectorChanges = Get-ReviewGateResult `
    -PullRequestAuthor 'ahuelsmann' `
    -PullRequestBody $standardBody `
    -HeadSha $headA `
    -BaseSha $baseA `
    -Comments @($tolerantComment) `
    -Reviews @($olderChangesConnector) `
    -ReviewThreads @() `
    -IdentitiesConfig $defaultIdentities
Assert-Equal 'success' $resOlderConnectorChanges.State 'older connector CHANGES_REQUESTED does not override newer issue-comment PASS'

# 7d. Connector path applies the same dispatcher/orchestrator checks as the issue-comment path.
$sameDispatcherBody = $standardBody -replace 'Orchestrator: Opus Head', 'Orchestrator: Codex-TD'
$resConnectorSameDispatcher = Get-ReviewGateResult `
    -PullRequestAuthor 'ahuelsmann' `
    -PullRequestBody $sameDispatcherBody `
    -HeadSha $headA `
    -BaseSha $baseA `
    -Reviews @($codexPassReview) `
    -ReviewThreads @() `
    -IdentitiesConfig $strictIdentities
Assert-Equal 'failure' $resConnectorSameDispatcher.State 'connector marker dispatcher equal to PR Orchestrator fails'
Assert-True ($resConnectorSameDispatcher.Description -like '*dispatcher must differ*') 'connector dispatcher failure explained'

$noOrchestratorBody = ($standardBody -split "`n" | Where-Object { $_ -notmatch 'Orchestrator' }) -join "`n"
$resConnectorNoOrchestrator = Get-ReviewGateResult `
    -PullRequestAuthor 'ahuelsmann' `
    -PullRequestBody $noOrchestratorBody `
    -HeadSha $headA `
    -BaseSha $baseA `
    -Reviews @($codexPassReview) `
    -ReviewThreads @() `
    -IdentitiesConfig $strictIdentities
Assert-Equal 'failure' $resConnectorNoOrchestrator.State 'connector path fails when PR Orchestrator is missing'

# 7e. Review-thread pagination reads every page until an unresolved thread is found.
$script:pageCalls = 0
$pagedUnresolved = Read-ReviewThreadPages -FetchPage {
    param($cursor)
    $script:pageCalls++
    if ($null -eq $cursor) {
        return [pscustomobject] @{ nodes = @(1..100 | ForEach-Object { @{ isResolved = $true } }); hasNextPage = $true; endCursor = 'c1' }
    }
    return [pscustomobject] @{ nodes = @(@{ isResolved = $false }); hasNextPage = $false; endCursor = $null }
}
Assert-Equal 2 $script:pageCalls 'pagination fetches the second page'
Assert-True (Test-HasUnresolvedReviewThreads -ReviewThreads $pagedUnresolved) 'unresolved thread beyond the first 100 is detected'

$script:pageCalls = 0
$pagedResolved = Read-ReviewThreadPages -FetchPage {
    param($cursor)
    $script:pageCalls++
    $next = $script:pageCalls -lt 3
    return [pscustomobject] @{ nodes = @(@{ isResolved = $true }); hasNextPage = $next; endCursor = "c$script:pageCalls" }
}
Assert-Equal 3 $script:pageCalls 'pagination reads all pages when every thread is resolved'
Assert-Equal 3 @($pagedResolved).Count 'pagination returns the threads of every page'

# 8. Writer-family cross-check against PR commits / branch prefix ("writer family mismatch").
$codexBranchBody = @"
- Writer family: claude
- Writer: Claude Opus 5.5
- Orchestrator: Opus Head
"@
$resBranchMismatch = Get-ReviewGateResult `
    -PullRequestAuthor 'ahuelsmann' `
    -PullRequestBody $codexBranchBody `
    -HeadSha $headA `
    -BaseSha $baseA `
    -HeadRefName 'codex/fake-claude-header' `
    -Comments @($tolerantComment) `
    -IdentitiesConfig $defaultIdentities
Assert-Equal 'failure' $resBranchMismatch.State 'codex branch prefix conflicts with openai reviewer -> fails'
Assert-True ($resBranchMismatch.Description -like '*writer family mismatch*') 'branch mismatch reports writer family mismatch'

$coAuthorCommits = @(
    @{
        parents = @(@{ sha = $baseA })
        commit = @{ message = "feat: something`n`nCo-Authored-By: ChatGPT Codex <codex@openai.com>" }
    }
)
$resCommitMismatch = Get-ReviewGateResult `
    -PullRequestAuthor 'ahuelsmann' `
    -PullRequestBody $standardBody `
    -HeadSha $headA `
    -BaseSha $baseA `
    -HeadRefName 'opus/review-gate' `
    -Commits $coAuthorCommits `
    -Reviews @($codexPassReview) `
    -ReviewThreads @() `
    -IdentitiesConfig $strictIdentities
Assert-Equal 'failure' $resCommitMismatch.State 'commit Co-Authored-By openai conflicts with codex connector -> fails'
Assert-True ($resCommitMismatch.Description -like '*writer family mismatch*') 'commit mismatch reports writer family mismatch'

# 9. Same reviewer-family, dispatcher==orchestrator, CHANGES override, dependabot, and Writer fallback.
$sameFamilyComment = @{
    id = 2
    created_at = '2026-11-26T10:01:00Z'
    user = @{ login = 'ahuelsmann' }
    body = "<!-- moba-final-review verdict=PASS head=$headA base=$baseA reviewer-family=claude reviewer-model=claude-opus-5-5 dispatcher=Codex-TD -->"
}
$resSameFamily = Get-ReviewGateResult `
    -PullRequestAuthor 'ahuelsmann' `
    -PullRequestBody $standardBody `
    -HeadSha $headA `
    -BaseSha $baseA `
    -Comments @($sameFamilyComment) `
    -IdentitiesConfig $defaultIdentities
Assert-Equal 'failure' $resSameFamily.State 'same reviewer-family as writer-family fails'

$sameDispatcherComment = @{
    id = 3
    created_at = '2026-11-26T10:02:00Z'
    user = @{ login = 'ahuelsmann' }
    body = "<!-- moba-final-review verdict=PASS head=$headA base=$baseA reviewer-family=openai reviewer-model=gpt-6.2-sol dispatcher=`"  opus   head  `" -->"
}
$resSameDispatcher = Get-ReviewGateResult `
    -PullRequestAuthor 'ahuelsmann' `
    -PullRequestBody $standardBody `
    -HeadSha $headA `
    -BaseSha $baseA `
    -Comments @($sameDispatcherComment) `
    -IdentitiesConfig $defaultIdentities
Assert-Equal 'failure' $resSameDispatcher.State 'dispatcher == orchestrator (case/spaces) fails'

$changesComment = @{
    id = 5
    created_at = '2026-11-26T10:05:00Z'
    user = @{ login = 'ahuelsmann' }
    body = "<!-- moba-final-review verdict=CHANGES head=$headA base=$baseA reviewer-family=openai reviewer-model=gpt-6.2-sol dispatcher=Codex-TD -->"
}
$resChanges = Get-ReviewGateResult `
    -PullRequestAuthor 'ahuelsmann' `
    -PullRequestBody $standardBody `
    -HeadSha $headA `
    -BaseSha $baseA `
    -Comments @($tolerantComment, $changesComment) `
    -IdentitiesConfig $defaultIdentities
Assert-Equal 'failure' $resChanges.State 'newer CHANGES overrides earlier PASS'

$resDependabot = Get-ReviewGateResult `
    -PullRequestAuthor 'dependabot[bot]' `
    -PullRequestBody 'Bumps Foo from 1.0 to 2.0' `
    -HeadSha $headA `
    -BaseSha $baseA `
    -Comments @() `
    -IdentitiesConfig $strictIdentities
Assert-Equal 'success' $resDependabot.State 'dependabot[bot] is exempt'
Assert-Equal 'exempt: dependency bot' $resDependabot.Description 'dependabot[bot] description exact'

$bodyWriterCodex = @"
- Writer: Codex (GPT-6.2 Sol)
- Orchestrator: Opus Head
"@
$claudePassComment = @{
    id = 6
    created_at = '2026-11-26T10:06:00Z'
    user = @{ login = 'ahuelsmann' }
    body = "<!-- moba-final-review verdict=PASS head=$headA base=$baseA reviewer-family=claude reviewer-model=claude-opus-5-5 dispatcher=Antigravity-Head -->"
}
$resFallbackCodex = Get-ReviewGateResult `
    -PullRequestAuthor 'ahuelsmann' `
    -PullRequestBody $bodyWriterCodex `
    -HeadSha $headA `
    -BaseSha $baseA `
    -Comments @($claudePassComment) `
    -IdentitiesConfig $defaultIdentities
Assert-Equal 'success' $resFallbackCodex.State 'missing Writer family falls back to Writer keyword Codex -> openai'

$bodyWriterUnknown = @"
- Writer: MysteryAgent
- Orchestrator: Opus Head
"@
$resFallbackUnknown = Get-ReviewGateResult `
    -PullRequestAuthor 'ahuelsmann' `
    -PullRequestBody $bodyWriterUnknown `
    -HeadSha $headA `
    -BaseSha $baseA `
    -Comments @($claudePassComment) `
    -IdentitiesConfig $defaultIdentities
Assert-Equal 'failure' $resFallbackUnknown.State 'unknown Writer without Writer family fails'

# 10. Validate committed .github/review-gate-identities.json and bootstrap behavior in .github/workflows/review-gate.yml.
$loadedIdentities = Get-IdentitiesConfig -RepositoryRoot $repositoryRoot
Assert-Equal 'openai' $loadedIdentities.TrustedReviewers['chatgpt-codex-connector[bot]'] 'repo identities file maps chatgpt-codex-connector[bot]'
Assert-Equal 'claude' $loadedIdentities.TrustedReviewers['claude[bot]'] 'repo identities file maps claude[bot]'
Assert-Equal 'gemini' $loadedIdentities.TrustedReviewers['gemini-code-assist[bot]'] 'repo identities file maps gemini-code-assist[bot]'
Assert-True $loadedIdentities.AllowDeclaredReviews 'repo identities file enables allowDeclaredReviews (informational until #566)'

$workflowText = Get-Content -Raw -LiteralPath (Join-Path $repositoryRoot '.github/workflows/review-gate.yml')
# Trusted publisher: the job with statuses: write must never be triggered by PR-defined workflow events.
Assert-True ($workflowText -notmatch '(?m)^\s*pull_request(_review|_target)?:') 'publisher workflow has no pull_request, pull_request_review or pull_request_target trigger'
Assert-True ($workflowText -match '(?m)^\s*issue_comment:') 'publisher workflow listens to issue_comment'
Assert-True ($workflowText -match '(?m)^\s*workflow_run:') 'publisher workflow listens to workflow_run'
Assert-True ($workflowText -match 'statuses: write') 'publisher workflow holds statuses: write'
Assert-True ($workflowText -notmatch 'actions/checkout') 'publisher workflow never uses a PR checkout action'
$testsWorkflowText = Get-Content -Raw -LiteralPath (Join-Path $repositoryRoot '.github/workflows/review-gate-tests.yml')
Assert-True ($testsWorkflowText -notmatch 'statuses:\s*write') 'PR-head self-test workflow has no statuses: write'
Assert-True ($testsWorkflowText -notmatch 'secrets\.') 'PR-head self-test workflow uses no secrets'
Assert-True ((Get-Content -Raw -LiteralPath $scriptPath).Contains('Independent final review (informational)')) 'status context is the informational name'
Assert-True ($workflowText -notmatch 'gate not yet installed') 'bootstrap path does not emit success commit status'
Assert-True ($workflowText -match 'exit 0') 'bootstrap path exits cleanly without posting status'

# 11. Real temporary git repository tests for exact raw-diff carry-forward:
#     (a) pure main merge -> success
#     (b) extra non-main commit (even empty) -> failure
#     (c) whitespace-only diff change -> failure
$fixture = Join-Path ([IO.Path]::GetTempPath()) ('mobaflow-review-gate-' + [guid]::NewGuid().ToString('N'))
[void] [IO.Directory]::CreateDirectory($fixture)

function Invoke-FixtureGit([string[]] $Arguments) {
    $start = [Diagnostics.ProcessStartInfo]::new('git')
    $start.WorkingDirectory = $fixture
    $start.UseShellExecute = $false
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    foreach ($key in @('GIT_DIR', 'GIT_WORK_TREE', 'GIT_INDEX_FILE', 'GIT_COMMON_DIR')) {
        [void] $start.Environment.Remove($key)
    }
    foreach ($arg in $Arguments) { $start.ArgumentList.Add($arg) }
    $proc = [Diagnostics.Process]::Start($start)
    try {
        $out = $proc.StandardOutput.ReadToEndAsync()
        $err = $proc.StandardError.ReadToEndAsync()
        $proc.WaitForExit()
        if ($proc.ExitCode -ne 0) {
            throw "git $($Arguments -join ' ') failed: $($out.Result)$($err.Result)"
        }
        return $out.Result.Trim()
    }
    finally {
        $proc.Dispose()
    }
}

try {
    $null = Invoke-FixtureGit @('init', '--quiet', '-b', 'main')
    $null = Invoke-FixtureGit @('config', 'user.name', 'Review Gate Tests')
    $null = Invoke-FixtureGit @('config', 'user.email', 'review-gate@example.invalid')

    [IO.File]::WriteAllText((Join-Path $fixture 'app.txt'), "base`n")
    [IO.File]::WriteAllText((Join-Path $fixture 'other.txt'), "other base`n")
    $null = Invoke-FixtureGit @('add', '.')
    $null = Invoke-FixtureGit @('commit', '-m', 'chore: base')

    $baseMainSha = Invoke-FixtureGit @('rev-parse', 'HEAD')
    $null = Invoke-FixtureGit @('update-ref', 'refs/remotes/origin/main', $baseMainSha)

    $null = Invoke-FixtureGit @('checkout', '-b', 'opus/slice')
    [IO.File]::WriteAllText((Join-Path $fixture 'app.txt'), "base`nfeature line`n")
    $null = Invoke-FixtureGit @('add', 'app.txt')
    $null = Invoke-FixtureGit @('commit', '-m', 'feat: slice change')
    $reviewedHead = Invoke-FixtureGit @('rev-parse', 'HEAD')

    # Main advances on an unrelated file.
    $null = Invoke-FixtureGit @('checkout', 'main')
    [IO.File]::WriteAllText((Join-Path $fixture 'other.txt'), "other base`nmain advance`n")
    $null = Invoke-FixtureGit @('add', 'other.txt')
    $null = Invoke-FixtureGit @('commit', '-m', 'chore: main advance')
    $newMainSha = Invoke-FixtureGit @('rev-parse', 'HEAD')
    $null = Invoke-FixtureGit @('update-ref', 'refs/remotes/origin/main', $newMainSha)

    # Pure main merge on PR branch.
    $null = Invoke-FixtureGit @('checkout', 'opus/slice')
    $null = Invoke-FixtureGit @('merge', '--no-edit', 'origin/main')
    $pureMergeHead = Invoke-FixtureGit @('rev-parse', 'HEAD')

    Assert-True (Test-PatchEquivalent -MarkerHead $reviewedHead -CurrentHead $pureMergeHead -BaseRef 'origin/main' -RepositoryRoot $fixture) 'carry-forward succeeds on pure main merge'

    $reviewedComment = @{
        id = 7
        created_at = '2026-11-26T10:10:00Z'
        user = @{ login = 'ahuelsmann' }
        body = "<!-- moba-final-review verdict=PASS head=$reviewedHead base=$baseMainSha reviewer-family=openai reviewer-model=gpt-6.2-sol dispatcher=Codex-TD -->"
    }
    $resPureMerge = Get-ReviewGateResult `
        -PullRequestAuthor 'ahuelsmann' `
        -PullRequestBody $standardBody `
        -HeadSha $pureMergeHead `
        -BaseSha $newMainSha `
        -HeadRefName 'opus/slice' `
        -Comments @($reviewedComment) `
        -BaseRef 'origin/main' `
        -RepositoryRoot $fixture `
        -IdentitiesConfig $defaultIdentities
    Assert-Equal 'success' $resPureMerge.State 'stale head with pure main merge succeeds'
    Assert-True ($resPureMerge.Description -like '*main-merge carry-forward*') 'pure main merge description mentions main-merge carry-forward'

    # Current-head verdict whose base is only the historical merge base (not the exact current PR base) must fail.
    $resHistoricalBaseOnCurrentHead = Get-ReviewGateResult `
        -PullRequestAuthor 'ahuelsmann' `
        -PullRequestBody $standardBody `
        -HeadSha $reviewedHead `
        -BaseSha $newMainSha `
        -HeadRefName 'opus/slice' `
        -Comments @($reviewedComment) `
        -BaseRef 'origin/main' `
        -RepositoryRoot $fixture `
        -IdentitiesConfig $defaultIdentities
    Assert-Equal 'failure' $resHistoricalBaseOnCurrentHead.State 'current-head marker with merge-base-only base fails'
    Assert-True ($resHistoricalBaseOnCurrentHead.Description -like '*does not match PR base*') 'merge-base-only base explained'

    $connectorHistoricalBase = @{
        user = @{ login = 'chatgpt-codex-connector[bot]' }
        commit_id = $reviewedHead
        state = 'COMMENTED'
        submitted_at = '2026-11-26T10:05:00Z'
        body = "<!-- moba-final-review verdict=PASS head=$reviewedHead base=$baseMainSha reviewer-family=openai reviewer-model=gpt-6.2 dispatcher=Codex-TD -->"
    }
    $resConnectorHistoricalBase = Get-ReviewGateResult `
        -PullRequestAuthor 'ahuelsmann' `
        -PullRequestBody $standardBody `
        -HeadSha $reviewedHead `
        -BaseSha $newMainSha `
        -HeadRefName 'opus/slice' `
        -Reviews @($connectorHistoricalBase) `
        -ReviewThreads @() `
        -BaseRef 'origin/main' `
        -RepositoryRoot $fixture `
        -IdentitiesConfig $strictIdentities
    Assert-Equal 'failure' $resConnectorHistoricalBase.State 'connector marker with merge-base-only base fails on current head'

    # Extra non-main empty commit (identical diff bytes, but not reachable from origin/main) -> must fail.
    $null = Invoke-FixtureGit @('commit', '--allow-empty', '-m', 'chore: extra non-main commit')
    $extraEmptyCommitHead = Invoke-FixtureGit @('rev-parse', 'HEAD')
    Assert-True (-not (Test-PatchEquivalent -MarkerHead $reviewedHead -CurrentHead $extraEmptyCommitHead -BaseRef 'origin/main' -RepositoryRoot $fixture)) 'carry-forward rejects extra non-main commit'

    # Whitespace-only diff change on PR branch -> must fail raw SHA-256 check.
    [IO.File]::WriteAllText((Join-Path $fixture 'app.txt'), "base`nfeature   line`n")
    $null = Invoke-FixtureGit @('add', 'app.txt')
    $null = Invoke-FixtureGit @('commit', '-m', 'style: whitespace change')
    $whitespaceHead = Invoke-FixtureGit @('rev-parse', 'HEAD')
    Assert-True (-not (Test-PatchEquivalent -MarkerHead $reviewedHead -CurrentHead $whitespaceHead -BaseRef 'origin/main' -RepositoryRoot $fixture)) 'carry-forward rejects whitespace-only diff change'

    $resWhitespaceStale = Get-ReviewGateResult `
        -PullRequestAuthor 'ahuelsmann' `
        -PullRequestBody $standardBody `
        -HeadSha $whitespaceHead `
        -BaseSha $newMainSha `
        -HeadRefName 'opus/slice' `
        -Comments @($reviewedComment) `
        -BaseRef 'origin/main' `
        -RepositoryRoot $fixture `
        -IdentitiesConfig $defaultIdentities
    Assert-Equal 'failure' $resWhitespaceStale.State 'whitespace diff change fails gate'
    Assert-True ($resWhitespaceStale.Description -like '*stale*') 'stale diff description mentions stale'

    Write-Host "Review gate self-tests passed ($checks assertions)."
}
finally {
    $resolved = [IO.Path]::GetFullPath($fixture)
    $tempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
    if ($resolved.StartsWith($tempRoot, [StringComparison]::OrdinalIgnoreCase) -and
        [IO.Path]::GetFileName($resolved) -match '^mobaflow-review-gate-[a-f0-9]{32}$') {
        Remove-Item -LiteralPath $resolved -Recurse -Force -ErrorAction SilentlyContinue
    }
}
