[CmdletBinding()]
param(
    # Repository-relative paths to classify. When omitted, the paths changed between BaseRef and HeadRef are used.
    [string[]] $Path,

    [string] $BaseRef,

    [string] $HeadRef = 'HEAD'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Paths that no build, test, analyzer or firmware job reads. Anything else, including an empty change set,
# keeps the full Quality workflow. Markdown inside project directories is excluded on purpose because
# projects such as Sound package it as content.
$documentationOnlyPatterns = @(
    '^docs/',
    '^\.agents/references/',
    '^assets/office/',
    '^plans/',
    '^specs/',
    '^\.specify/',
    '^\.github/instructions/',
    '^\.github/ISSUE_TEMPLATE/',
    '^\.github/[^/]+\.md$',
    '^[^/]+\.md$',
    '^LICENSE$'
)

# The firmware build and its native tests read only the PlatformIO project (`lib_extra_dirs = lib` stays inside it).
$firmwareInputPatterns = @(
    '^MOBAdisplay/esp32/'
)

# Shared .NET build and Stryker tool inputs: a change here requires the .NET build and all 7 mutation lanes.
$sharedMutationInputPatterns = @(
    '^\.config/dotnet-tools\.json$',
    '^global\.json$',
    '^version\.json$',
    '^NuGet\.Config$',
    '^\.editorconfig$',
    '^Directory\.(Build|Packages|Solution)\.(props|targets)$'
)

# Transitive ProjectReference groups shared across multiple Stryker mutation hosts.
$contractsClosurePatterns = @(
    '^Contracts/',
    '^Domain/'
)
$commonClosurePatterns = @(
    '^Common/'
) + $contractsClosurePatterns
$speechBaseClosurePatterns = @(
    '^Speech\.Base/'
) + $commonClosurePatterns
$backendClosurePatterns = @(
    '^Backend/',
    '^Sound/'
) + $speechBaseClosurePatterns
$sharedUiClosurePatterns = @(
    '^SharedUI/',
    '^MOBAdisplay/(?!esp32/)',
    '^TrackPlan\.Renderer/',
    '^TrackPlan\.Editor/',
    '^TrackLibrary\.Base/'
) + $backendClosurePatterns

# Per-lane mutation inputs matching each MutationTest/**/*.csproj transitive ProjectReference, <Compile>,
# <None>/<Content> CopyToOutputDirectory closure, and Stryker/host configuration.
# Get-QualityChangeScope.Tests.ps1 checks every active lane against the actual project dependency graph.
$mutationLaneInputPatterns = [ordered] @{
    Mutation = @(
        '^Domain/',
        '^Test/Domain/',
        '^Test/TestFile/',
        '^Test/Unit/(DomainDefaultsTests|NewSolutionTests|SolutionInstanceTests|SolutionTest)\.cs$',
        '^MutationTest/Domain\.MutationTests\.csproj$',
        '^MutationTest/stryker-config\.json$'
    )
    MutationContracts = $contractsClosurePatterns + @(
        '^Test/Contracts/',
        '^MutationTest/Contracts/',
        '^MutationTest/stryker-contracts-config\.json$'
    )
    MutationApi = @(
        '^MOBApi/',
        '^Test/MOBApi/',
        '^MutationTest/Api/',
        '^MutationTest/stryker-api-config\.json$'
    ) + $commonClosurePatterns
    MutationSpeechBase = $backendClosurePatterns + @(
        '^Test/Speech/(AudioCacheTests|CachedSpeechOutputTests|SpeechOutputTests|WaveAudioTests|TestSpeechCache)\.cs$',
        '^Test/MOBAdisplay/ManualTimeProvider\.cs$',
        '^MutationTest/Speech\.Base/',
        '^MutationTest/stryker-speech-base-config\.json$'
    )
    MutationCommon = $sharedUiClosurePatterns + @(
        '^Test/Common/',
        '^Test/Backend/Recording[^/]*\.cs$',
        '^Test/Backend/MultiplexerCommandResolverTests\.cs$',
        '^Test/Speech/SpeechSettingsTests\.cs$',
        '^Test/TestData/Recording/',
        '^MutationTest/Common/',
        '^MutationTest/stryker-common-config\.json$'
    )
    MutationBackend = $sharedUiClosurePatterns + @(
        '^Test/Backend/',
        '^Test/Integration/WorkflowExecutionEndToEndTests\.cs$',
        '^Test/Z21DccCommandDecoderTests\.cs$',
        '^Test/GlobalUsings\.cs$',
        '^Test/Helpers/(ManualTimeProvider|ServiceRegistrations|TestProjectRuntimeHost|TestRuntimes|TestWorkflowServices)\.cs$',
        '^Test/MOBAdisplay/ManualTimeProvider\.cs$',
        '^Test/Mocks/FakeUdpClientWrapper\.cs$',
        '^Test/TestData/Z21Packets\.cs$',
        '^Test/TestData/Recording/',
        '^Test/TestFile/solution\.json$',
        '^MutationTest/Backend/',
        '^MutationTest/stryker-backend-config\.json$'
    )
    MutationSharedUI = $sharedUiClosurePatterns + @(
        '^TrackLibrary\.PikoA/',
        '^Test/SharedUI/',
        '^Test/TrackPlanRenderer/TrackLayoutArchitectureTests[^/]*\.cs$',
        '^Test/GlobalUsings\.cs$',
        '^Test/Helpers/',
        '^MutationTest/SharedUI/',
        '^MutationTest/stryker-shared-ui-config\.json$'
    )
}

$mutationLaneOutputs = [ordered] @{
    Mutation           = 'mutation'
    MutationCommon     = 'mutation_common'
    MutationContracts  = 'mutation_contracts'
    MutationSpeechBase = 'mutation_speech_base'
    MutationBackend    = 'mutation_backend'
    MutationApi        = 'mutation_api'
    MutationSharedUI   = 'mutation_shared_ui'
}

# Known .NET repository areas. A path outside every listed area (workflows, scripts, repository configuration,
# new top-level folders) fails open and runs every scoped job except Android.
$knownDotNetPatterns = @(
    '^Domain/',
    '^Common/',
    '^Contracts/',
    '^Backend/',
    '^SharedUI/',
    '^Speech\.Base/',
    '^Speech\.Piper/',
    '^Speech\.SystemSpeech/',
    '^MOBAflow/',
    '^MOBAsmart/',
    '^MOBApi/',
    '^MOBAdisplay/',
    '^Sound/',
    '^TrackLibrary\.Base/',
    '^TrackLibrary\.PikoA/',
    '^TrackPlan\.Editor/',
    '^TrackPlan\.Renderer/',
    '^MOBAdemo/',
    '^MutationTest/',
    '^Test/',
    '^quality/',
    '^Moba\.slnx$',
    '^Moba\.sln\.DotSettings$'
)

# The Android Release AAB job runs on a pull request only for these paths (maintainer decision of 2026-11-08,
# issue #360): the MOBAsmart project and the analyzer baseline that only this job checks. Shared projects that
# MOBAsmart compiles, repository-wide build inputs and the workflow itself do not select it; the manual
# validate_android dispatch covers them. Unlike the other scopes, an unknown path or an empty change set does
# not select the Android job.
$androidInputPatterns = @(
    '^MOBAsmart/',
    '^quality/analyzer-baseline\.android\.json$'
)

# The MOBAdemo CI job (issue #430) builds the frontend and the host and runs the MOBAdemo tests. It runs for the
# demo itself and for the shared inputs of that build; documentation, firmware, Android and the other .NET areas
# do not select it. A path outside every known area runs it too (fail open, like the other scopes).
$mobaDemoInputPatterns = @(
    '^MOBAdemo/',
    '^Test/MOBAdemo/',
    '^Test/Test\.csproj$',
    '^Moba\.slnx$',
    '^global\.json$',
    '^version\.json$',
    '^NuGet\.Config$',
    '^\.editorconfig$',
    '^Directory\.(Build|Packages|Solution)\.(props|targets)$'
)

function Test-MatchesAny([string] $RelativePath, [string[]] $Patterns) {
    foreach ($pattern in $Patterns) {
        if ($RelativePath -cmatch $pattern) {
            return $true
        }
    }

    return $false
}

function New-PathScope(
    [bool] $Code,
    [bool] $Firmware,
    [bool] $Android,
    [bool] $MobaDemo,
    [hashtable] $MutationLanes) {
    $properties = [ordered] @{
        Code     = $Code
        Firmware = $Firmware
        Mutation = [bool] $MutationLanes['Mutation']
        Android  = $Android
        MobaDemo = $MobaDemo
    }
    foreach ($laneKey in $mutationLaneOutputs.Keys) {
        if ($laneKey -eq 'Mutation') { continue }
        $properties[$laneKey] = [bool] $MutationLanes[$laneKey]
    }
    return [pscustomobject] $properties
}

function New-UniformMutationMap([bool] $Value) {
    $map = @{}
    foreach ($laneKey in $mutationLaneOutputs.Keys) {
        $map[$laneKey] = $Value
    }
    return $map
}

# Returns which optional jobs a changed path needs. Order matters: documentation, firmware and shared build
# inputs are checked before the broader .NET areas that contain them.
function Get-PathScope([string] $RelativePath) {
    $normalizedPath = $RelativePath.Trim().Replace('\', '/')
    $android = Test-MatchesAny $normalizedPath $androidInputPatterns
    $mobaDemo = Test-MatchesAny $normalizedPath $mobaDemoInputPatterns
    if (Test-MatchesAny $normalizedPath $documentationOnlyPatterns) {
        return New-PathScope -Code $false -Firmware $false -Android $android -MobaDemo $false -MutationLanes (New-UniformMutationMap $false)
    }

    if (Test-MatchesAny $normalizedPath $firmwareInputPatterns) {
        return New-PathScope -Code $true -Firmware $true -Android $android -MobaDemo $mobaDemo -MutationLanes (New-UniformMutationMap $false)
    }

    if (Test-MatchesAny $normalizedPath $sharedMutationInputPatterns) {
        return New-PathScope -Code $true -Firmware $false -Android $android -MobaDemo $mobaDemo -MutationLanes (New-UniformMutationMap $true)
    }

    if (Test-MatchesAny $normalizedPath $knownDotNetPatterns) {
        $lanes = @{}
        $anyMutationMatch = $false
        foreach ($laneKey in $mutationLaneInputPatterns.Keys) {
            $matched = Test-MatchesAny $normalizedPath $mutationLaneInputPatterns[$laneKey]
            $lanes[$laneKey] = $matched
            if ($matched) { $anyMutationMatch = $true }
        }
        # Shared or newly added files under MutationTest/ (such as mutation-lanes.json) fail open for all 7 lanes.
        if ((-not $anyMutationMatch) -and ($normalizedPath -cmatch '^MutationTest/')) {
            $lanes = New-UniformMutationMap $true
        }
        return New-PathScope -Code $true -Firmware $false -Android $android -MobaDemo $mobaDemo -MutationLanes $lanes
    }

    return New-PathScope -Code $true -Firmware $true -Android $android -MobaDemo $true -MutationLanes (New-UniformMutationMap $true)
}

function Get-ChangedPaths([string] $Base, [string] $Head) {
    $changedPaths = @(git diff --name-only --no-renames $Base $Head)
    if ($LASTEXITCODE -ne 0) {
        throw "git diff between '$Base' and '$Head' failed."
    }

    return $changedPaths
}

if ($null -eq $Path) {
    if ([string]::IsNullOrWhiteSpace($BaseRef)) {
        throw 'Provide either -Path or -BaseRef.'
    }

    $Path = Get-ChangedPaths -Base $BaseRef -Head $HeadRef
}

$changedPaths = @($Path | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
$pathScopes = @($changedPaths | ForEach-Object { Get-PathScope $_ })

# An empty change set cannot be classified; run everything.
$empty = $changedPaths.Count -eq 0
$scopeProperties = [ordered] @{
    Code     = $empty -or @($pathScopes | Where-Object Code).Count -gt 0
    Firmware = $empty -or @($pathScopes | Where-Object Firmware).Count -gt 0
    Mutation = $empty -or @($pathScopes | Where-Object Mutation).Count -gt 0
    Android  = @($pathScopes | Where-Object Android).Count -gt 0
    MobaDemo = $empty -or @($pathScopes | Where-Object MobaDemo).Count -gt 0
}
foreach ($laneKey in $mutationLaneOutputs.Keys) {
    if ($laneKey -eq 'Mutation') { continue }
    $scopeProperties[$laneKey] = $empty -or @($pathScopes | Where-Object { $_.$laneKey }).Count -gt 0
}
$scope = [pscustomobject] $scopeProperties

if ($scope.Code) {
    Write-Host ("Code-relevant changes found; firmware=$($scope.Firmware), mutation=$($scope.Mutation), " +
        "mutation_common=$($scope.MutationCommon), mutation_contracts=$($scope.MutationContracts), " +
        "mutation_speech_base=$($scope.MutationSpeechBase), mutation_backend=$($scope.MutationBackend), " +
        "mutation_api=$($scope.MutationApi), mutation_shared_ui=$($scope.MutationSharedUI), " +
        "android=$($scope.Android), mobademo=$($scope.MobaDemo).")
    @($changedPaths | Where-Object { (Get-PathScope $_).Code }) | Select-Object -First 20 | ForEach-Object { Write-Host "  $_" }
}
else {
    Write-Host "All $($changedPaths.Count) changed paths are documentation only; skipping build, test and firmware jobs."
}

if (-not [string]::IsNullOrEmpty($env:GITHUB_OUTPUT)) {
    $outputLines = @(
        "code=$($scope.Code.ToString().ToLowerInvariant())"
        "firmware=$($scope.Firmware.ToString().ToLowerInvariant())"
        "mutation=$($scope.Mutation.ToString().ToLowerInvariant())"
        "mutation_common=$($scope.MutationCommon.ToString().ToLowerInvariant())"
        "mutation_contracts=$($scope.MutationContracts.ToString().ToLowerInvariant())"
        "mutation_speech_base=$($scope.MutationSpeechBase.ToString().ToLowerInvariant())"
        "mutation_backend=$($scope.MutationBackend.ToString().ToLowerInvariant())"
        "mutation_api=$($scope.MutationApi.ToString().ToLowerInvariant())"
        "mutation_shared_ui=$($scope.MutationSharedUI.ToString().ToLowerInvariant())"
        "android=$($scope.Android.ToString().ToLowerInvariant())"
        "mobademo=$($scope.MobaDemo.ToString().ToLowerInvariant())"
    )
    Add-Content -LiteralPath $env:GITHUB_OUTPUT -Encoding utf8NoBOM -Value $outputLines
}

return $scope
