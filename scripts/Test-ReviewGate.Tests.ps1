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

# 7. chatgpt-codex-connector[bot] review matching on exact head SHA (no base= needed), wrong SHA, and unresolved thread.
$codexPassReview = @{
    user = @{ login = 'chatgpt-codex-connector[bot]' }
    commit_id = $headA
    state = 'COMMENTED'
    submitted_at = '2026-11-26T10:05:00Z'
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
Assert-True $loadedIdentities.AllowDeclaredReviews 'repo identities file enables allowDeclaredReviews'

$workflowText = Get-Content -Raw -LiteralPath (Join-Path $repositoryRoot '.github/workflows/review-gate.yml')
Assert-True ($workflowText.Contains('pull_request_target:')) 'review-gate.yml uses pull_request_target'
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
