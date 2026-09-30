[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptPath = Join-Path $PSScriptRoot 'Get-QualityChangeScope.ps1'
$checks = 0

function Assert-Scope([string[]] $Paths, [bool] $ExpectedCode, [string] $Scenario) {
    $savedOutput = $env:GITHUB_OUTPUT
    $env:GITHUB_OUTPUT = $null
    try {
        $actual = & $scriptPath -Path $Paths 6> $null
    }
    finally {
        $env:GITHUB_OUTPUT = $savedOutput
    }

    if ($actual -ne $ExpectedCode) {
        throw "Expected code=$ExpectedCode for '$Scenario', but got code=$actual."
    }

    $script:checks++
}

Assert-Scope @('docs/ARCHITECTURE.md') $false 'documentation page'
Assert-Scope @('plans/QUALITY-AND-REFACTORING-PLAN.md', 'specs/007-x/spec.md', '.specify/memory/constitution.md') $false 'plans and specifications'
Assert-Scope @('README.md', 'AGENTS.md', 'CHANGELOG.md', 'LICENSE') $false 'root documentation'
Assert-Scope @('.github/instructions/quick-reference.md', '.github/ISSUE_TEMPLATE/bug.yml', '.github/PULL_REQUEST_TEMPLATE.md') $false 'GitHub guidance'
Assert-Scope @('docs\index.html') $false 'Windows path separators'

Assert-Scope @() $true 'empty change set'
Assert-Scope @('docs/ARCHITECTURE.md', 'Backend/Service/Z21.cs') $true 'mixed documentation and code'
Assert-Scope @('Sound/Resources/Sounds/Station/README.md') $true 'Markdown packaged by a project'
Assert-Scope @('.github/workflows/quality.yml') $true 'workflow change'
Assert-Scope @('scripts/Test-AnalyzerBaseline.ps1') $true 'quality script change'
Assert-Scope @('quality/analyzer-baseline.json') $true 'analyzer baseline change'
Assert-Scope @('Docs/README.md') $true 'case-sensitive directory match'

$outputFile = [IO.Path]::GetTempFileName()
$savedOutput = $env:GITHUB_OUTPUT
try {
    $env:GITHUB_OUTPUT = $outputFile
    & $scriptPath -Path @('docs/ARCHITECTURE.md') 6> $null | Out-Null
    $written = (Get-Content -Raw -LiteralPath $outputFile).Trim()
    if ($written -ne 'code=false') {
        throw "Expected GITHUB_OUTPUT to contain 'code=false', but found '$written'."
    }
    $checks++
}
finally {
    $env:GITHUB_OUTPUT = $savedOutput
    Remove-Item -LiteralPath $outputFile -Force -ErrorAction SilentlyContinue
}

Write-Host "Quality change scope self-tests passed ($checks checks)."
