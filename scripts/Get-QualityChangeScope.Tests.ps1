[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptPath = Join-Path $PSScriptRoot 'Get-QualityChangeScope.ps1'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$checks = 0

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

# Documentation only: nothing runs.
Assert-Scope @('docs/ARCHITECTURE.md') $false $false $false 'documentation page'
Assert-Scope @('plans/QUALITY-AND-REFACTORING-PLAN.md', 'specs/007-x/spec.md', '.specify/memory/constitution.md') $false $false $false 'plans and specifications'
Assert-Scope @('README.md', 'AGENTS.md', 'CHANGELOG.md', 'LICENSE') $false $false $false 'root documentation'
Assert-Scope @('.github/instructions/quick-reference.md', '.github/ISSUE_TEMPLATE/bug.yml', '.github/PULL_REQUEST_TEMPLATE.md') $false $false $false 'GitHub guidance'
Assert-Scope @('docs\index.html') $false $false $false 'Windows path separators'

# Unknown or shared inputs run everything.
Assert-Scope @() $true $true $true 'empty change set'
Assert-Scope @('.github/workflows/quality.yml') $true $true $true 'workflow change'
Assert-Scope @('scripts/Test-AnalyzerBaseline.ps1') $true $true $true 'quality script change'
Assert-Scope @('scripts/Get-QualityChangeScope.ps1') $true $true $true 'change-scope rules change'
Assert-Scope @('.gitattributes') $true $true $true 'repository configuration'
Assert-Scope @('NewTopLevel/Feature.cs') $true $true $true 'new top-level folder'
Assert-Scope @('Docs/README.md') $true $true $true 'case-sensitive directory match'
Assert-Scope @('Sound/Resources/Sounds/Station/README.md') $true $false $false 'Markdown packaged by a project'

# .NET areas outside the firmware and the Domain mutation graph.
Assert-Scope @('docs/ARCHITECTURE.md', 'Backend/Service/Z21.cs') $true $false $false 'mixed documentation and backend code'
Assert-Scope @('SharedUI/ViewModel/MainWindowViewModel.cs', 'MOBAflow/View/SolutionPage.xaml') $true $false $false 'desktop code'
Assert-Scope @('MOBAdisplay/Protocol/DisplayPayloadCodec.cs') $true $false $false 'display host code'
Assert-Scope @('quality/analyzer-baseline.json') $true $false $false 'analyzer baseline change'
Assert-Scope @('Test/SharedUI/SolutionSessionTests.cs') $true $false $false 'tests outside the Domain lane'

# Firmware inputs.
Assert-Scope @('MOBAdisplay/esp32/src/main.cpp') $true $true $false 'firmware source'
Assert-Scope @('MOBAdisplay/esp32/platformio.ini') $true $true $false 'firmware build configuration'
Assert-Scope @('MOBAdisplay/esp32/lib/MobaDisplayProtocol/protocol.h', 'Backend/Z21.cs') $true $true $false 'firmware and backend code'

# Domain mutation inputs.
Assert-Scope @('Domain/Project.cs') $true $false $true 'Domain model'
Assert-Scope @('Test/Domain/ProjectTests.cs') $true $false $true 'Domain test'
Assert-Scope @('MutationTest/stryker-config.json') $true $false $true 'mutation configuration'
Assert-Scope @('Directory.Packages.props') $true $false $true '.NET build configuration'
Assert-Scope @('.config/dotnet-tools.json') $true $false $true 'Stryker tool manifest'

# Every source the mutation project compiles must select the mutation job.
[xml] $mutationProject = Get-Content -Raw -LiteralPath (Join-Path $repositoryRoot 'MutationTest/Domain.MutationTests.csproj')
$mutationSources = @($mutationProject.SelectNodes('//Compile/@Include | //ProjectReference/@Include') | ForEach-Object Value)
if ($mutationSources.Count -lt 2) {
    throw 'Expected Compile and ProjectReference items in MutationTest/Domain.MutationTests.csproj.'
}
foreach ($include in $mutationSources) {
    $samplePath = ($include -replace '^\.\.\\', '' -replace '\\', '/' -replace '\*\*/', '' -replace '\*', 'Sample')
    Assert-Scope @($samplePath) $true $false $true "mutation project input '$include'"
}

$outputFile = [IO.Path]::GetTempFileName()
$savedOutput = $env:GITHUB_OUTPUT
try {
    $env:GITHUB_OUTPUT = $outputFile
    & $scriptPath -Path @('MOBAdisplay/esp32/src/main.cpp') 6> $null | Out-Null
    $written = (Get-Content -Raw -LiteralPath $outputFile).Trim() -replace "`r`n", "`n"
    if ($written -ne "code=true`nfirmware=true`nmutation=false") {
        throw "Unexpected GITHUB_OUTPUT content '$written'."
    }
    $checks++
}
finally {
    $env:GITHUB_OUTPUT = $savedOutput
    Remove-Item -LiteralPath $outputFile -Force -ErrorAction SilentlyContinue
}

Write-Host "Quality change scope self-tests passed ($checks checks)."
