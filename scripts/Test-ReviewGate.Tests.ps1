[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

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

# 2. Marker parsing tolerance (case, spacing, quotes, attribute order).
$tolerantComment = @{
    id = 10
    created_at = '2026-11-26T10:00:00Z'
    body = "Review summary`n<!--   MOBA-FINAL-REVIEW   dispatcher = `"Codex Head`"  reviewer-model=gpt-6.2-sol   reviewer-family = OpenAI   head = $headA   verdict = pass  -->"
}
$parsedMarkers = @(Get-FinalReviewMarkers -Comments @($tolerantComment))
Assert-Equal 1 $parsedMarkers.Count 'tolerant marker parsed'
Assert-Equal 'PASS' $parsedMarkers[0].Verdict 'verdict normalized to uppercase'
Assert-Equal 'openai' $parsedMarkers[0].ReviewerFamily 'reviewer-family normalized to lowercase'
Assert-Equal 'Codex Head' $parsedMarkers[0].Dispatcher 'quoted dispatcher extracted'

# 3. Missing marker -> failure.
$resMissing = Get-ReviewGateResult `
    -PullRequestAuthor 'ahuelsmann' `
    -PullRequestBody $standardBody `
    -HeadSha $headA `
    -Comments @(@{ id = 1; created_at = '2026-11-26T10:00:00Z'; body = 'LGTM without marker' })
Assert-Equal 'failure' $resMissing.State 'missing marker fails'
Assert-True ($resMissing.Description.Length -le 140) 'missing marker description <= 140 chars'

# 4. Invalid / incomplete marker -> failure.
$resInvalid = Get-ReviewGateResult `
    -PullRequestAuthor 'ahuelsmann' `
    -PullRequestBody $standardBody `
    -HeadSha $headA `
    -Comments @(@{ id = 1; created_at = '2026-11-26T10:00:00Z'; body = '<!-- moba-final-review verdict=PASS head=short reviewer-family=openai -->' })
Assert-Equal 'failure' $resInvalid.State 'invalid marker fails'
Assert-True ($resInvalid.Description.Length -le 140) 'invalid marker description <= 140 chars'

# 5. Same reviewer-family as writer-family -> failure.
$sameFamilyComment = @{
    id = 2
    created_at = '2026-11-26T10:01:00Z'
    body = "<!-- moba-final-review verdict=PASS head=$headA reviewer-family=claude reviewer-model=claude-opus-5-5 dispatcher=Codex Head -->"
}
$resSameFamily = Get-ReviewGateResult `
    -PullRequestAuthor 'ahuelsmann' `
    -PullRequestBody $standardBody `
    -HeadSha $headA `
    -Comments @($sameFamilyComment)
Assert-Equal 'failure' $resSameFamily.State 'same reviewer-family as writer-family fails'
Assert-True ($resSameFamily.Description -like '*matches writer family*') 'same family explanation clear'
Assert-True ($resSameFamily.Description.Length -le 140) 'same family description <= 140 chars'

# 6. dispatcher == orchestrator (different case and spacing) -> failure.
$sameDispatcherComment = @{
    id = 3
    created_at = '2026-11-26T10:02:00Z'
    body = "<!-- moba-final-review verdict=PASS head=$headA reviewer-family=openai reviewer-model=gpt-6.2-sol dispatcher=`"  opus   head  `" -->"
}
$resSameDispatcher = Get-ReviewGateResult `
    -PullRequestAuthor 'ahuelsmann' `
    -PullRequestBody $standardBody `
    -HeadSha $headA `
    -Comments @($sameDispatcherComment)
Assert-Equal 'failure' $resSameDispatcher.State 'dispatcher == orchestrator (case/spaces) fails'
Assert-True ($resSameDispatcher.Description -like '*Orchestrator*') 'dispatcher == orchestrator explanation clear'
Assert-True ($resSameDispatcher.Description.Length -le 140) 'dispatcher == orchestrator description <= 140 chars'

# 7. Newer CHANGES overrides earlier PASS -> failure.
$passComment = @{
    id = 4
    created_at = '2026-11-26T10:00:00Z'
    body = "<!-- moba-final-review verdict=PASS head=$headA reviewer-family=openai reviewer-model=gpt-6.2-sol dispatcher=Codex Head -->"
}
$changesComment = @{
    id = 5
    created_at = '2026-11-26T10:05:00Z'
    body = "<!-- moba-final-review verdict=CHANGES head=$headA reviewer-family=openai reviewer-model=gpt-6.2-sol dispatcher=Codex Head -->"
}
$resChanges = Get-ReviewGateResult `
    -PullRequestAuthor 'ahuelsmann' `
    -PullRequestBody $standardBody `
    -HeadSha $headA `
    -Comments @($passComment, $changesComment)
Assert-Equal 'failure' $resChanges.State 'newer CHANGES overrides earlier PASS'
Assert-True ($resChanges.Description -like '*CHANGES*') 'CHANGES explanation clear'

# 8. dependabot[bot] exemption -> success.
$resDependabot = Get-ReviewGateResult `
    -PullRequestAuthor 'dependabot[bot]' `
    -PullRequestBody 'Bumps Foo from 1.0 to 2.0' `
    -HeadSha $headA `
    -Comments @()
Assert-Equal 'success' $resDependabot.State 'dependabot[bot] is exempt'
Assert-Equal 'exempt: dependency bot' $resDependabot.Description 'dependabot[bot] description exact'

# 9. Missing Writer family uses Writer keyword fallback (Codex -> openai; unknown -> failure).
$bodyWriterCodex = @"
- Writer: Codex (GPT-6.2 Sol)
- Orchestrator: Opus Head
"@
$claudePassComment = @{
    id = 6
    created_at = '2026-11-26T10:06:00Z'
    body = "<!-- moba-final-review verdict=PASS head=$headA reviewer-family=claude reviewer-model=claude-opus-5-5 dispatcher=Antigravity Head -->"
}
$resFallbackCodex = Get-ReviewGateResult `
    -PullRequestAuthor 'ahuelsmann' `
    -PullRequestBody $bodyWriterCodex `
    -HeadSha $headA `
    -Comments @($claudePassComment)
Assert-Equal 'success' $resFallbackCodex.State 'missing Writer family falls back to Writer keyword Codex -> openai'

$bodyWriterUnknown = @"
- Writer: MysteryAgent
- Orchestrator: Opus Head
"@
$resFallbackUnknown = Get-ReviewGateResult `
    -PullRequestAuthor 'ahuelsmann' `
    -PullRequestBody $bodyWriterUnknown `
    -HeadSha $headA `
    -Comments @($claudePassComment)
Assert-Equal 'failure' $resFallbackUnknown.State 'unknown Writer without Writer family fails'
Assert-True ($resFallbackUnknown.Description.Length -le 140) 'unknown writer description <= 140 chars'

# 10. Real temporary git repository tests for Test-PatchEquivalent (pure main merge vs changed diff).
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

    # Create origin/main ref at base.
    $baseMainSha = Invoke-FixtureGit @('rev-parse', 'HEAD')
    $null = Invoke-FixtureGit @('update-ref', 'refs/remotes/origin/main', $baseMainSha)

    # Feature branch makes a change and gets reviewed at $reviewedHead.
    $null = Invoke-FixtureGit @('checkout', '-b', 'feature/slice')
    [IO.File]::WriteAllText((Join-Path $fixture 'app.txt'), "base`nfeature line`n")
    $null = Invoke-FixtureGit @('add', 'app.txt')
    $null = Invoke-FixtureGit @('commit', '-m', 'feat: slice change')
    $reviewedHead = Invoke-FixtureGit @('rev-parse', 'HEAD')

    # Main advances on an unrelated file, and origin/main updates.
    $null = Invoke-FixtureGit @('checkout', 'main')
    [IO.File]::WriteAllText((Join-Path $fixture 'other.txt'), "other base`nmain advance`n")
    $null = Invoke-FixtureGit @('add', 'other.txt')
    $null = Invoke-FixtureGit @('commit', '-m', 'chore: main advance')
    $newMainSha = Invoke-FixtureGit @('rev-parse', 'HEAD')
    $null = Invoke-FixtureGit @('update-ref', 'refs/remotes/origin/main', $newMainSha)

    # Merge origin/main cleanly into feature branch -> pure main merge head.
    $null = Invoke-FixtureGit @('checkout', 'feature/slice')
    $null = Invoke-FixtureGit @('merge', '--no-edit', 'origin/main')
    $pureMergeHead = Invoke-FixtureGit @('rev-parse', 'HEAD')

    Assert-True (Test-PatchEquivalent -MarkerHead $reviewedHead -CurrentHead $pureMergeHead -BaseRef 'origin/main' -RepositoryRoot $fixture) 'Test-PatchEquivalent preserves pure main merge'

    $reviewedComment = @{
        id = 7
        created_at = '2026-11-26T10:10:00Z'
        body = "<!-- moba-final-review verdict=PASS head=$reviewedHead reviewer-family=openai reviewer-model=gpt-6.2-sol dispatcher=Codex Head -->"
    }
    $resPureMerge = Get-ReviewGateResult `
        -PullRequestAuthor 'ahuelsmann' `
        -PullRequestBody $standardBody `
        -HeadSha $pureMergeHead `
        -Comments @($reviewedComment) `
        -BaseRef 'origin/main' `
        -RepositoryRoot $fixture
    Assert-Equal 'success' $resPureMerge.State 'stale head with pure main merge succeeds'
    Assert-True ($resPureMerge.Description -like '*patch-equivalent*') 'pure main merge description mentions patch-equivalent'

    # Now push a real code change on top of the PR branch -> stale changed diff must fail.
    [IO.File]::WriteAllText((Join-Path $fixture 'app.txt'), "base`nfeature line`nextra edit`n")
    $null = Invoke-FixtureGit @('add', 'app.txt')
    $null = Invoke-FixtureGit @('commit', '-m', 'fix: unreviewed edit')
    $changedDiffHead = Invoke-FixtureGit @('rev-parse', 'HEAD')

    Assert-True (-not (Test-PatchEquivalent -MarkerHead $reviewedHead -CurrentHead $changedDiffHead -BaseRef 'origin/main' -RepositoryRoot $fixture)) 'Test-PatchEquivalent rejects changed diff'

    $resStaleChanged = Get-ReviewGateResult `
        -PullRequestAuthor 'ahuelsmann' `
        -PullRequestBody $standardBody `
        -HeadSha $changedDiffHead `
        -Comments @($reviewedComment) `
        -BaseRef 'origin/main' `
        -RepositoryRoot $fixture
    Assert-Equal 'failure' $resStaleChanged.State 'stale head with changed diff fails'
    Assert-True ($resStaleChanged.Description -like '*stale*') 'stale diff description mentions stale'
    Assert-True ($resStaleChanged.Description.Length -le 140) 'stale diff description <= 140 chars'

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
