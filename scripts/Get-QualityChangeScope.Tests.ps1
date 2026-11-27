[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptPath = Join-Path $PSScriptRoot 'Get-QualityChangeScope.ps1'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$checks = 0

$lanePropertyByName = [ordered] @{
    'domain'              = 'Mutation'
    'common'              = 'MutationCommon'
    'contracts'           = 'MutationContracts'
    'speech-base'         = 'MutationSpeechBase'
    'backend'             = 'MutationBackend'
    'api'                 = 'MutationApi'
    'shared-ui'           = 'MutationSharedUI'
    'track-plan-renderer' = 'MutationTrackPlanRenderer'
}

$laneOutputByName = [ordered] @{
    'domain'              = 'mutation'
    'common'              = 'mutation_common'
    'contracts'           = 'mutation_contracts'
    'speech-base'         = 'mutation_speech_base'
    'backend'             = 'mutation_backend'
    'api'                 = 'mutation_api'
    'shared-ui'           = 'mutation_shared_ui'
    'track-plan-renderer' = 'mutation_track_plan_renderer'
}

$laneJobByName = [ordered] @{
    'domain'              = 'mutation-domain'
    'common'              = 'mutation-common'
    'contracts'           = 'mutation-contracts'
    'speech-base'         = 'mutation-speech-base'
    'backend'             = 'mutation-backend'
    'api'                 = 'mutation-api'
    'shared-ui'           = 'mutation-shared-ui'
    'track-plan-renderer' = 'mutation-track-plan-renderer'
}

function Get-Scope([string[]] $Paths) {
    $savedOutput = $env:GITHUB_OUTPUT
    $env:GITHUB_OUTPUT = $null
    try {
        return & $scriptPath -Path $Paths 6> $null
    }
    finally {
        $env:GITHUB_OUTPUT = $savedOutput
    }
}

function Assert-Scope(
    [string[]] $Paths,
    [bool] $Code,
    [bool] $Firmware,
    [bool] $Mutation,
    [string] $Scenario) {
    $actual = Get-Scope $Paths
    if ($actual.Code -ne $Code -or $actual.Firmware -ne $Firmware -or $actual.Mutation -ne $Mutation) {
        throw ("Expected code=$Code firmware=$Firmware mutation=$Mutation for '$Scenario', " +
            "but got code=$($actual.Code) firmware=$($actual.Firmware) mutation=$($actual.Mutation).")
    }

    $script:checks++
}

function Assert-MutationLanes(
    [string[]] $Paths,
    [string[]] $ExpectedActiveLanes,
    [string] $Scenario) {
    $actual = Get-Scope $Paths
    foreach ($laneName in $lanePropertyByName.Keys) {
        $property = $lanePropertyByName[$laneName]
        $expected = $ExpectedActiveLanes -contains $laneName
        $actualValue = [bool] $actual.$property
        if ($actualValue -ne $expected) {
            throw ("Expected $property=$expected for '$Scenario', " +
                "but got $property=$actualValue (expected active lanes: [$($ExpectedActiveLanes -join ', ')]).")
        }
    }

    $script:checks++
}

function Resolve-RepoRelativeSamplePath([string] $BaseDirectory, [string] $IncludePath) {
    $normalized = $IncludePath.Trim().Replace('\', '/') -replace '\*\*/', '' -replace '\*', 'Sample'
    $combined = Join-Path $BaseDirectory $normalized
    $full = [IO.Path]::GetFullPath($combined)
    $rootPrefix = ([IO.Path]::GetFullPath($repositoryRoot)).TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
    if (-not $full.StartsWith($rootPrefix, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Path '$IncludePath' resolved outside repository root: '$full'."
    }
    return $full.Substring($rootPrefix.Length).Replace('\', '/')
}

# Documentation only: nothing runs.
Assert-Scope @('docs/developer/ARCHITECTURE.md') $false $false $false 'documentation page'
Assert-Scope @('.agents/references/engineering/tdd.md', 'assets/office/MOBAnews.docx') $false $false $false 'reference pages and Office files'
Assert-Scope @('plans/QUALITY-AND-REFACTORING-PLAN.md', 'specs/007-x/spec.md', '.specify/memory/constitution.md') $false $false $false 'plans and specifications'
Assert-Scope @('README.md', 'AGENTS.md', 'CHANGELOG.md', 'LICENSE') $false $false $false 'root documentation'
Assert-Scope @('.github/instructions/quick-reference.md', '.github/ISSUE_TEMPLATE/bug.yml', '.github/PULL_REQUEST_TEMPLATE.md') $false $false $false 'GitHub guidance'
Assert-Scope @('docs\index.html') $false $false $false 'Windows path separators'
Assert-MutationLanes @('docs/developer/ARCHITECTURE.md') @() 'documentation skips all 8 mutation lanes'

# Unknown or shared inputs run everything (fail open).
Assert-Scope @() $true $true $true 'empty change set'
Assert-Scope @('.github/workflows/quality.yml') $true $true $true 'workflow change'
Assert-Scope @('scripts/Test-AnalyzerBaseline.ps1') $true $true $true 'quality script change'
Assert-Scope @('scripts/Get-QualityChangeScope.ps1') $true $true $true 'change-scope rules change'
Assert-Scope @('.gitattributes') $true $true $true 'repository configuration'
Assert-Scope @('NewTopLevel/Feature.cs') $true $true $true 'new top-level folder'
Assert-Scope @('Docs/README.md') $true $true $true 'case-sensitive directory match'
Assert-Scope @('Sound/Resources/Sounds/Station/README.md') $true $false $false 'Markdown packaged by a project'

$allLanes = @($lanePropertyByName.Keys)
Assert-MutationLanes @() $allLanes 'empty change set runs all 8 mutation lanes'
Assert-MutationLanes @('.github/workflows/quality.yml') $allLanes 'workflow change runs all 8 mutation lanes'
Assert-MutationLanes @('scripts/Get-QualityChangeScope.ps1') $allLanes 'change-scope script change runs all 8 mutation lanes'
Assert-MutationLanes @('scripts/Invoke-MutationLane.ps1') $allLanes 'mutation runner change runs all 8 mutation lanes'
Assert-MutationLanes @('NewTopLevel/Feature.cs') $allLanes 'unknown path runs all 8 mutation lanes'
Assert-MutationLanes @('MutationTest/mutation-lanes.json') $allLanes 'shared mutation lane registry runs all 8 mutation lanes'
foreach ($sharedFile in @(
    '.config/dotnet-tools.json',
    'global.json',
    'version.json',
    'NuGet.Config',
    '.editorconfig',
    'Directory.Build.props',
    'Directory.Build.targets',
    'Directory.Packages.props',
    'Directory.Solution.props'
)) {
    Assert-Scope @($sharedFile) $true $false $true "shared build/tool input '$sharedFile'"
    Assert-MutationLanes @($sharedFile) $allLanes "shared build/tool input '$sharedFile' runs all 8 mutation lanes"
}

# .NET areas outside the firmware and the Domain mutation graph.
Assert-Scope @('docs/developer/ARCHITECTURE.md', 'Backend/Service/Z21.cs') $true $false $false 'mixed documentation and backend code'
Assert-Scope @('SharedUI/ViewModel/MainWindowViewModel.cs', 'MOBAflow/View/SolutionPage.xaml') $true $false $false 'desktop code'
Assert-Scope @('MOBAdisplay/Protocol/DisplayPayloadCodec.cs') $true $false $false 'display host code'
Assert-Scope @('Contracts/Runtime/MobaRuntimeSnapshot.cs', 'TrackPlan.Editor/EditableTrackPlan.cs') $true $false $false 'wire contracts and track-plan editor code'
Assert-Scope @('quality/analyzer-baseline.json') $true $false $false 'analyzer baseline change'
Assert-Scope @('Test/SharedUI/SolutionSessionTests.cs') $true $false $false 'tests outside the Domain lane'

# Android Release AAB job: only MOBAsmart and the Android analyzer baseline select it (issue #360).
function Assert-Android([string[]] $Paths, [bool] $Android, [string] $Scenario) {
    $actual = Get-Scope $Paths
    if ($actual.Android -ne $Android) {
        throw "Expected android=$Android for '$Scenario', but got android=$($actual.Android)."
    }

    $script:checks++
}

Assert-Android @('MOBAsmart/View/ControlPage.xaml') $true 'MOBAsmart source'
Assert-Android @('MOBAsmart\MOBAsmart.csproj') $true 'MOBAsmart project with Windows path separators'
Assert-Android @('quality/analyzer-baseline.android.json') $true 'Android analyzer baseline'
Assert-Android @('docs/developer/ARCHITECTURE.md', 'Backend/Z21.cs', 'MOBAsmart/MauiProgram.cs') $true 'MOBAsmart among other changes'
Assert-Android @('SharedUI/ViewModel/MainWindowViewModel.cs') $false 'SharedUI only'
Assert-Android @('MOBAflow/View/SolutionPage.xaml', 'Test/MOBAflow/SomeTests.cs') $false 'desktop only'
Assert-Android @('docs/developer/BUILD-PERFORMANCE.md', 'plans/360-ci-platform-matrix.md') $false 'documentation only'
Assert-Android @('quality/analyzer-baseline.json') $false 'desktop analyzer baseline'
Assert-Android @('Test/MOBAsmart/SomeTests.cs') $false 'MOBAsmart tests outside the project folder'
Assert-Android @('Directory.Packages.props', 'global.json') $false 'repository-wide build inputs'
Assert-Android @('.github/workflows/quality.yml', 'scripts/Get-QualityChangeScope.ps1') $false 'workflow and scope rules'
Assert-Android @() $false 'empty change set'
Assert-Scope @('MOBAsmart/View/ControlPage.xaml') $true $false $false 'MOBAsmart keeps the other scopes unchanged'

# MOBAdemo CI job (issue #430): the frontend, its tests and the shared build inputs select it; unknown paths and an
# empty change set fail open like every other scope. Documentation-only, firmware, Android and other .NET areas do not.
function Assert-MobaDemo([string[]] $Paths, [bool] $MobaDemo, [string] $Scenario) {
    $actual = Get-Scope $Paths
    if ($actual.MobaDemo -ne $MobaDemo) {
        throw "Expected mobademo=$MobaDemo for '$Scenario', but got mobademo=$($actual.MobaDemo)."
    }

    $script:checks++
}

Assert-MobaDemo @('MOBAdemo/app/src/main.tsx') $true 'frontend source'
Assert-MobaDemo @('MOBAdemo/Host/MobaDemoHost.cs') $true 'host source'
Assert-MobaDemo @('MOBAdemo\build.ps1') $true 'build script with Windows path separators'
Assert-Scope @('MOBAdemo\build.ps1') $true $false $false 'Windows separators keep MOBAdemo a .NET area (no unknown-path fallback)'
Assert-MobaDemo @('Test/MOBAdemo/Build/BuildScriptTests.cs') $true 'MOBAdemo tests'
Assert-MobaDemo @('Test/Test.csproj', 'Moba.slnx') $true 'test project and solution'
Assert-MobaDemo @('global.json') $true 'SDK pin'
Assert-MobaDemo @('Directory.Packages.props') $true 'central package versions'
Assert-MobaDemo @('NuGet.Config') $true 'NuGet configuration'
Assert-MobaDemo @('version.json') $true 'version file'
Assert-MobaDemo @('.editorconfig') $true 'editor configuration'
Assert-MobaDemo @('.github/workflows/quality.yml') $true 'workflow change'
Assert-MobaDemo @('scripts/Get-QualityChangeScope.ps1') $true 'change-scope rules change'
Assert-MobaDemo @('NewTopLevel/Feature.cs') $true 'unknown path fails open'
Assert-MobaDemo @() $true 'empty change set fails open'
Assert-MobaDemo @('docs/developer/ARCHITECTURE.md', 'MOBAdemo/app/src/App.tsx') $true 'MOBAdemo among documentation'
Assert-MobaDemo @('docs/developer/ARCHITECTURE.md', 'specs/020-web-demo/spec.md') $false 'documentation only'
Assert-MobaDemo @('Backend/Service/Z21.cs', 'SharedUI/ViewModel/MainWindowViewModel.cs') $false 'other .NET areas'
Assert-MobaDemo @('MOBAdisplay/esp32/src/main.cpp') $false 'firmware only'
Assert-MobaDemo @('MOBAsmart/View/ControlPage.xaml') $false 'Android only'
Assert-MobaDemo @('Domain/Project.cs', 'Test/Domain/ProjectTests.cs') $false 'Domain mutation inputs only'
Assert-MobaDemo @('Test/SharedUI/SolutionSessionTests.cs') $false 'tests of other areas'
# MOBAdemo is a .NET area of its own: it must not wake the firmware or any mutation lane.
Assert-Scope @('MOBAdemo/app/src/main.tsx', 'Test/MOBAdemo/Build/BuildScriptTests.cs') $true $false $false 'MOBAdemo wakes neither firmware nor mutation'

# Firmware inputs skip all 8 mutation lanes.
Assert-Scope @('MOBAdisplay/esp32/src/main.cpp') $true $true $false 'firmware source'
Assert-Scope @('MOBAdisplay/esp32/platformio.ini') $true $true $false 'firmware build configuration'
Assert-Scope @('MOBAdisplay/esp32/lib/MobaDisplayProtocol/protocol.h', 'Backend/Z21.cs') $true $true $false 'firmware and backend code'
Assert-MutationLanes @('MOBAdisplay/esp32/src/main.cpp') @() 'ESP32 firmware source skips all 8 mutation lanes'
Assert-MutationLanes @('MOBAdisplay/esp32/platformio.ini') @() 'ESP32 firmware config skips all 8 mutation lanes'

# Negative exclusions: .NET areas outside all 8 mutation lanes run desktop/portable/analyzer gates without Stryker.
foreach ($excludedPath in @(
    'MOBAdemo/Host/MobaDemoHost.cs',
    'MOBAdemo/app/src/main.tsx',
    'Test/MOBAdemo/Build/BuildScriptTests.cs',
    'MOBAflow/View/SolutionPage.xaml',
    'MOBAflow/ViewModel/Shell.cs',
    'Test/MOBAflow/NavigationTests.cs',
    'Test/WinUI/ThemeConverterTests.cs',
    'MOBAsmart/View/ControlPage.xaml',
    'quality/analyzer-baseline.json',
    'quality/analyzer-baseline.windows.json',
    'quality/analyzer-baseline.android.json',
    'Speech.Piper/LocalSpeechProvider.cs',
    'Speech.SystemSpeech/SystemSpeechProvider.cs',
    'Test/Speech/LocalSpeechProviderTests.cs',
    'Test/TrackPlanRenderer/TrackPlanSnapHelperTests.cs',
    'Test/Integration/MobApiProcessTests.cs',
    'Test/Sound/NullSoundPlayerTests.cs',
    'Test/Unit/UnrelatedUnitTests.cs',
    'Test/Test.csproj',
    'Moba.slnx'
)) {
    Assert-Scope @($excludedPath) $true $false $false "excluded .NET area '$excludedPath'"
    Assert-MutationLanes @($excludedPath) @() "excluded .NET area '$excludedPath' skips all 8 mutation lanes"
}

# Exact transitive dependency graph and per-lane isolation across all 8 Stryker lanes (issue #539, #497).
Assert-MutationLanes @('Domain/Project.cs') $allLanes 'Domain feeds all 8 mutation lanes'
Assert-MutationLanes @('Contracts/Runtime/MobaRuntimeSnapshot.cs') @('common', 'contracts', 'speech-base', 'backend', 'api', 'shared-ui') 'Contracts feeds 6 downstream lanes'
Assert-MutationLanes @('Common/Configuration/AppSettings.cs') @('common', 'speech-base', 'backend', 'api', 'shared-ui') 'Common feeds 5 downstream lanes'
Assert-MutationLanes @('Speech.Base/ISpeechEngine.cs') @('speech-base', 'common', 'backend', 'shared-ui') 'Speech.Base feeds 4 downstream lanes'
Assert-MutationLanes @('Sound/NullSoundPlayer.cs') @('speech-base', 'common', 'backend', 'shared-ui') 'Sound feeds 4 downstream lanes'
Assert-MutationLanes @('Backend/Service/Z21.cs') @('speech-base', 'common', 'backend', 'shared-ui') 'Backend feeds 4 downstream lanes'
Assert-MutationLanes @('SharedUI/ViewModel/MainWindowViewModel.cs') @('common', 'backend', 'shared-ui') 'SharedUI feeds common, backend, and shared-ui'
Assert-MutationLanes @('TrackPlan.Renderer/SvgExporter.cs') @('common', 'backend', 'shared-ui', 'track-plan-renderer') 'TrackPlan.Renderer feeds common, backend, shared-ui, and track-plan-renderer'
Assert-MutationLanes @('TrackPlan.Editor/EditableTrackPlan.cs') @('common', 'backend', 'shared-ui', 'track-plan-renderer') 'TrackPlan.Editor feeds common, backend, shared-ui, and track-plan-renderer'
Assert-MutationLanes @('TrackLibrary.Base/Segment.cs') @('common', 'backend', 'shared-ui', 'track-plan-renderer') 'TrackLibrary.Base feeds common, backend, shared-ui, and track-plan-renderer'
Assert-MutationLanes @('MOBAdisplay/Protocol/DisplayPayloadCodec.cs') @('common', 'backend', 'shared-ui') 'MOBAdisplay C# host feeds common, backend, and shared-ui'
Assert-MutationLanes @('TrackLibrary.PikoA/R1.cs') @('shared-ui', 'track-plan-renderer') 'TrackLibrary.PikoA feeds shared-ui and track-plan-renderer'
Assert-MutationLanes @('MOBApi/Endpoints/RuntimeEndpoints.cs') @('api') 'MOBApi feeds api only'

# Test, fixture, and Stryker configuration isolation per lane.
Assert-MutationLanes @('Test/Domain/ProjectTests.cs') @('domain') 'Domain test'
Assert-MutationLanes @('Test/Unit/DomainDefaultsTests.cs') @('domain') 'Domain unit test'
Assert-MutationLanes @('Test/TestFile/germany-stations.json') @('domain') 'Domain-only test fixture'
Assert-MutationLanes @('Test/TestFile/solution.json') @('domain', 'backend') 'Domain and Backend shared fixture'
Assert-MutationLanes @('Test/Contracts/MobApiEndpointTests.cs') @('contracts') 'Contracts test'
Assert-MutationLanes @('Test/MOBApi/MobApiTests.cs') @('api') 'MOBApi test'
Assert-MutationLanes @('Test/Common/EventBusTests.cs') @('common') 'Common test'
Assert-MutationLanes @('Test/Speech/SpeechSettingsTests.cs') @('common') 'SpeechSettings test linked by Common'
Assert-MutationLanes @('Test/Speech/AudioCacheTests.cs') @('speech-base') 'Speech.Base cache test'
Assert-MutationLanes @('Test/MOBAdisplay/ManualTimeProvider.cs') @('speech-base', 'backend') 'ManualTimeProvider linked by Speech.Base and Backend'
Assert-MutationLanes @('Test/Backend/Z21UnitTests.cs') @('backend') 'Backend-only test'
Assert-MutationLanes @('Test/Backend/RecordingSessionServiceTests.cs') @('common', 'backend') 'Recording test linked by Common and Backend'
Assert-MutationLanes @('Test/Backend/MultiplexerCommandResolverTests.cs') @('common', 'backend') 'Multiplexer test linked by Common and Backend'
Assert-MutationLanes @('Test/TestData/Recording/recording-v1-golden.json') @('common', 'backend') 'Recording fixture linked by Common and Backend'
Assert-MutationLanes @('Test/Integration/WorkflowExecutionEndToEndTests.cs') @('backend') 'Backend workflow integration test'
Assert-MutationLanes @('Test/SharedUI/SolutionSessionTests.cs') @('shared-ui') 'SharedUI test'
Assert-MutationLanes @('Test/TrackPlanRenderer/TrackLayoutArchitectureTests.cs') @('shared-ui') 'TrackLayoutArchitectureTests linked by SharedUI'
Assert-MutationLanes @('Test/TrackPlanRenderer/PathToSvgConverterTests.cs') @('track-plan-renderer') 'PathToSvgConverterTests linked by TrackPlan.Renderer'
Assert-MutationLanes @('Test/TrackPlanRenderer/PlacedTrackPlanEditorTests.cs') @('track-plan-renderer') 'PlacedTrackPlanEditorTests linked by TrackPlan.Renderer'
Assert-MutationLanes @('Test/TrackPlanRenderer/SvgExporterTests.cs') @('track-plan-renderer') 'SvgExporterTests linked by TrackPlan.Renderer'
Assert-MutationLanes @('Test/Helpers/ServiceRegistrations.cs') @('backend', 'shared-ui') 'Shared test helper linked by Backend and SharedUI'
Assert-MutationLanes @('Test/Helpers/TrackPlanTestGeometry.cs') @('shared-ui', 'track-plan-renderer') 'TrackPlanTestGeometry linked by SharedUI and TrackPlan.Renderer'
Assert-MutationLanes @('Test/Helpers/ViewModelHelpers.cs') @('shared-ui') 'Test helper linked by SharedUI only'
Assert-MutationLanes @('Test/GlobalUsings.cs') @('backend', 'shared-ui') 'GlobalUsings linked by Backend and SharedUI'
Assert-MutationLanes @('MutationTest/stryker-config.json') @('domain') 'Domain Stryker config'
Assert-MutationLanes @('MutationTest/stryker-contracts-config.json') @('contracts') 'Contracts Stryker config'
Assert-MutationLanes @('MutationTest/stryker-common-config.json') @('common') 'Common Stryker config'
Assert-MutationLanes @('MutationTest/stryker-speech-base-config.json') @('speech-base') 'Speech.Base Stryker config'
Assert-MutationLanes @('MutationTest/stryker-backend-config.json') @('backend') 'Backend Stryker config'
Assert-MutationLanes @('MutationTest/stryker-api-config.json') @('api') 'Api Stryker config'
Assert-MutationLanes @('MutationTest/stryker-shared-ui-config.json') @('shared-ui') 'SharedUI Stryker config'
Assert-MutationLanes @('MutationTest/stryker-track-plan-renderer-config.json') @('track-plan-renderer') 'TrackPlan.Renderer Stryker config'

# Dynamically validate every active lane in MutationTest/mutation-lanes.json against its full transitive
# ProjectReference, <Compile>, and copied <None>/<Content> closure.
$registry = Get-Content -Raw -LiteralPath (Join-Path $repositoryRoot 'MutationTest/mutation-lanes.json') | ConvertFrom-Json
$activeLanes = @($registry.lanes | Where-Object { $_.status -eq 'active' })
if ($activeLanes.Count -ne $lanePropertyByName.Count) {
    throw "Expected $($lanePropertyByName.Count) active lanes in mutation-lanes.json; found $($activeLanes.Count)."
}

$projectItemXPath = @(
    '//Compile/@Include'
    '//ProjectReference/@Include'
    '//None[(CopyToOutputDirectory and CopyToOutputDirectory != "Never") or (@CopyToOutputDirectory and @CopyToOutputDirectory != "Never")]/@Include'
    '//None[(CopyToOutputDirectory and CopyToOutputDirectory != "Never") or (@CopyToOutputDirectory and @CopyToOutputDirectory != "Never")]/@Update'
    '//Content[(CopyToOutputDirectory and CopyToOutputDirectory != "Never") or (@CopyToOutputDirectory and @CopyToOutputDirectory != "Never")]/@Include'
    '//Content[(CopyToOutputDirectory and CopyToOutputDirectory != "Never") or (@CopyToOutputDirectory and @CopyToOutputDirectory != "Never")]/@Update'
) -join ' | '

foreach ($lane in $activeLanes) {
    $laneName = [string] $lane.name
    if (-not $lanePropertyByName.Contains($laneName)) {
        throw "Active lane '$laneName' is missing from Get-QualityChangeScope.Tests.ps1 lane map."
    }
    $laneProperty = $lanePropertyByName[$laneName]
    $configRelPath = [string] $lane.'stryker-config'
    $configFullPath = Join-Path $repositoryRoot $configRelPath
    $configDir = Split-Path -Parent $configFullPath
    $config = (Get-Content -Raw -LiteralPath $configFullPath | ConvertFrom-Json).'stryker-config'

    $configScope = Get-Scope @($configRelPath)
    if (-not $configScope.Code -or $configScope.Firmware -or -not $configScope.$laneProperty) {
        throw "Stryker config '$configRelPath' did not enable lane '$laneName'."
    }
    $checks++

    $queue = [Collections.Generic.Queue[string]]::new()
    $visited = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($testProj in @($config.'test-projects')) {
        $testProjRel = Resolve-RepoRelativeSamplePath $configDir $testProj
        if ($visited.Add($testProjRel)) {
            $queue.Enqueue($testProjRel)
        }
    }

    while ($queue.Count -gt 0) {
        $csprojRel = $queue.Dequeue()
        $csprojScope = Get-Scope @($csprojRel)
        if (-not $csprojScope.Code -or $csprojScope.Firmware -or -not $csprojScope.$laneProperty) {
            throw "Project file '$csprojRel' in transitive closure of '$laneName' did not enable $laneProperty."
        }
        $checks++

        $csprojFull = Join-Path $repositoryRoot $csprojRel
        $csprojDir = Split-Path -Parent $csprojFull
        [xml] $projectXml = Get-Content -Raw -LiteralPath $csprojFull

        foreach ($refAttr in @($projectXml.SelectNodes('//ProjectReference/@Include'))) {
            $refRel = Resolve-RepoRelativeSamplePath $csprojDir $refAttr.Value
            if ($visited.Add($refRel)) {
                $queue.Enqueue($refRel)
            }
            $refDir = (Split-Path -Parent $refRel).Replace('\', '/')
            $sampleCodePath = if ([string]::IsNullOrEmpty($refDir)) { 'Sample.cs' } else { "$refDir/Sample.cs" }
            $refScope = Get-Scope @($sampleCodePath)
            if (-not $refScope.Code -or $refScope.Firmware -or -not $refScope.$laneProperty) {
                throw "Transitive project source '$sampleCodePath' (from '$refRel') did not enable '$laneName'."
            }
            $checks++
        }

        foreach ($itemAttr in @($projectXml.SelectNodes($projectItemXPath))) {
            if ($itemAttr.OwnerElement.LocalName -eq 'ProjectReference') { continue }
            $samplePath = Resolve-RepoRelativeSamplePath $csprojDir $itemAttr.Value
            $itemScope = Get-Scope @($samplePath)
            if (-not $itemScope.Code -or $itemScope.Firmware -or -not $itemScope.$laneProperty) {
                throw "Project input '$($itemAttr.Value)' -> '$samplePath' (in '$csprojRel') did not enable '$laneName'."
            }
            $checks++
        }
    }
}

# A failed change detection leaves every output empty. Each job condition must then run its job rather than
# report a skipped (passing) required check, so quality.yml compares the outputs only with != 'false'.
$workflowText = Get-Content -Raw -LiteralPath (Join-Path $repositoryRoot '.github/workflows/quality.yml')
$scopeReads = @([regex]::Matches($workflowText, "needs\.changes\.outputs\.(\w+)(\s*[!=]=\s*'[^']*')?"))
$failClosedReads = @($scopeReads | Where-Object { $_.Groups[2].Value -notmatch "^\s*!=\s*'false'$" })
if ($failClosedReads.Count -gt 0) {
    throw "quality.yml must read change-scope outputs with != 'false'; found: $(($failClosedReads | ForEach-Object Value) -join ', ')."
}
$readOutputs = @($scopeReads | ForEach-Object { $_.Groups[1].Value })
$expectedWorkflowOutputs = @(
    'code', 'firmware', 'mutation',
    'mutation_common', 'mutation_contracts', 'mutation_speech_base',
    'mutation_backend', 'mutation_api', 'mutation_shared_ui',
    'mutation_track_plan_renderer', 'android', 'mobademo'
)
foreach ($output in $expectedWorkflowOutputs) {
    if ($readOutputs -notcontains $output) {
        throw "Expected quality.yml to read the '$output' change-scope output."
    }
}
$checks++

# Without a status-check function GitHub adds an implicit success() to a job condition, so a failed change
# detection would skip the job whatever the output comparison says. Every job that needs `changes` and reads
# its outputs must therefore keep !cancelled() or always() in its job-level condition.
function Get-JobValue([string[]] $JobLines, [string] $Key) {
    # Returns the job-level value of $Key, including folded continuation lines, or $null.
    $value = $null
    foreach ($line in $JobLines) {
        if ($null -eq $value) {
            if ($line -match "^    $Key\s*:(.*)$") {
                $value = $Matches[1]
            }
        }
        elseif ($line -match '^\s{6,}\S' -or $line -match '^\s*$') {
            $value += "`n$line"
        }
        else {
            break
        }
    }

    return $value
}

$workflowLines = $workflowText -split "\r?\n"
$jobsStart = [array]::IndexOf($workflowLines, 'jobs:')
if ($jobsStart -lt 0) {
    throw 'Expected a top-level jobs: section in quality.yml.'
}
$jobs = [ordered] @{}
$currentJob = $null
foreach ($line in $workflowLines[($jobsStart + 1)..($workflowLines.Count - 1)]) {
    if ($line -match '^\S') {
        break
    }
    if ($line -match '^  ([\w-]+):\s*$') {
        $currentJob = $Matches[1]
        $jobs[$currentJob] = [Collections.Generic.List[string]]::new()
    }
    elseif ($null -ne $currentJob) {
        $jobs[$currentJob].Add($line)
    }
}

$guardedJobs = @()
foreach ($job in $jobs.Keys) {
    $jobLines = [string[]] $jobs[$job]
    $needs = Get-JobValue $jobLines 'needs'
    if ($null -eq $needs -or @([regex]::Matches($needs, '[\w-]+') | ForEach-Object Value) -notcontains 'changes') {
        continue
    }
    if (-not ($jobLines -join "`n").Contains('needs.changes.outputs.')) {
        continue
    }
    $condition = Get-JobValue $jobLines 'if'
    if ($null -eq $condition -or $condition -notmatch '!\s*cancelled\(\)|\balways\(\)') {
        throw ("quality.yml job '$job' reads the change scope, but its condition lacks !cancelled() or always(); " +
            'a failed change detection would skip it.')
    }
    $guardedJobs += $job
    $checks++
}
foreach ($job in @(
    'desktop', 'display-protocol', 'portable-linux', 'android-release', 'mobademo',
    'mutation-domain', 'mutation-common', 'mutation-contracts', 'mutation-speech-base',
    'mutation-backend', 'mutation-api', 'mutation-shared-ui', 'mutation-track-plan-renderer'
)) {
    if ($guardedJobs -notcontains $job) {
        throw "Expected quality.yml job '$job' to read the change scope with a fail-open condition."
    }
}
foreach ($laneName in $laneJobByName.Keys) {
    $jobName = $laneJobByName[$laneName]
    $expectedOutput = $laneOutputByName[$laneName]
    $condition = Get-JobValue ([string[]] $jobs[$jobName]) 'if'
    $expectedPattern = "needs\.changes\.outputs\.$expectedOutput\s*!=\s*'false'"
    if ($condition -notmatch $expectedPattern) {
        throw "Expected quality.yml job '$jobName' to gate on '$expectedOutput != ''false'''; found '$condition'."
    }
    $checks++
}

$outputFile = [IO.Path]::GetTempFileName()
$savedOutput = $env:GITHUB_OUTPUT
try {
    $env:GITHUB_OUTPUT = $outputFile
    & $scriptPath -Path @('MOBAdisplay/esp32/src/main.cpp') 6> $null | Out-Null
    $written = (Get-Content -Raw -LiteralPath $outputFile).Trim() -replace "`r`n", "`n"
    $expectedFirmwareOutput = @(
        'code=true'
        'firmware=true'
        'mutation=false'
        'mutation_common=false'
        'mutation_contracts=false'
        'mutation_speech_base=false'
        'mutation_backend=false'
        'mutation_api=false'
        'mutation_shared_ui=false'
        'mutation_track_plan_renderer=false'
        'android=false'
        'mobademo=false'
    ) -join "`n"
    if ($written -ne $expectedFirmwareOutput) {
        throw "Unexpected GITHUB_OUTPUT content for firmware:`n$written"
    }
    $checks++

    [IO.File]::WriteAllText($outputFile, '')
    & $scriptPath -Path @('Contracts/Runtime/MobaRuntimeSnapshot.cs') 6> $null | Out-Null
    $writtenContracts = (Get-Content -Raw -LiteralPath $outputFile).Trim() -replace "`r`n", "`n"
    $expectedContractsOutput = @(
        'code=true'
        'firmware=false'
        'mutation=false'
        'mutation_common=true'
        'mutation_contracts=true'
        'mutation_speech_base=true'
        'mutation_backend=true'
        'mutation_api=true'
        'mutation_shared_ui=true'
        'mutation_track_plan_renderer=false'
        'android=false'
        'mobademo=false'
    ) -join "`n"
    if ($writtenContracts -ne $expectedContractsOutput) {
        throw "Unexpected GITHUB_OUTPUT content for Contracts:`n$writtenContracts"
    }
    $checks++
}
finally {
    $env:GITHUB_OUTPUT = $savedOutput
    Remove-Item -LiteralPath $outputFile -Force -ErrorAction SilentlyContinue
}

Write-Host "Quality change scope self-tests passed ($checks checks)."
