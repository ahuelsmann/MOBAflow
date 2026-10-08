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

# Domain mutation testing builds MutationTest/Domain.MutationTests.csproj: Domain, the linked Domain tests and
# the .NET build configuration. Get-QualityChangeScope.Tests.ps1 checks this list against the project file.
$mutationInputPatterns = @(
    '^Domain/',
    '^Test/Domain/',
    '^Test/Unit/(DomainDefaultsTests|NewSolutionTests|SolutionInstanceTests|SolutionTest)\.cs$',
    '^MutationTest/',
    '^\.config/dotnet-tools\.json$',
    '^global\.json$',
    '^version\.json$',
    '^NuGet\.Config$',
    '^\.editorconfig$',
    '^Directory\.(Build|Packages|Solution)\.(props|targets)$'
)

# .NET areas that neither the firmware build nor Domain mutation testing reads. A path outside every listed
# area (workflows, scripts, repository configuration, new top-level folders) runs every job.
$otherDotNetPatterns = @(
    '^Common/',
    '^Backend/',
    '^SharedUI/',
    '^MOBAflow/',
    '^MOBAsmart/',
    '^MOBApi/',
    '^MOBAdisplay/',
    '^Sound/',
    '^TrackLibrary\.Base/',
    '^TrackLibrary\.PikoA/',
    '^TrackPlan\.Renderer/',
    '^Test/',
    '^quality/',
    '^Moba\.slnx$',
    '^Moba\.sln\.DotSettings$'
)

function Test-MatchesAny([string] $RelativePath, [string[]] $Patterns) {
    foreach ($pattern in $Patterns) {
        if ($RelativePath -cmatch $pattern) {
            return $true
        }
    }

    return $false
}

# Returns which optional jobs a changed path needs. Order matters: firmware and mutation inputs are checked
# before the broader .NET areas that contain them.
function Get-PathScope([string] $RelativePath) {
    $normalizedPath = $RelativePath.Trim().Replace('\', '/')
    if (Test-MatchesAny $normalizedPath $documentationOnlyPatterns) {
        return [pscustomobject] @{ Code = $false; Firmware = $false; Mutation = $false }
    }

    if (Test-MatchesAny $normalizedPath $firmwareInputPatterns) {
        return [pscustomobject] @{ Code = $true; Firmware = $true; Mutation = $false }
    }

    if (Test-MatchesAny $normalizedPath $mutationInputPatterns) {
        return [pscustomobject] @{ Code = $true; Firmware = $false; Mutation = $true }
    }

    if (Test-MatchesAny $normalizedPath $otherDotNetPatterns) {
        return [pscustomobject] @{ Code = $true; Firmware = $false; Mutation = $false }
    }

    return [pscustomobject] @{ Code = $true; Firmware = $true; Mutation = $true }
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
$scope = [pscustomobject] @{
    Code = $empty -or @($pathScopes | Where-Object Code).Count -gt 0
    Firmware = $empty -or @($pathScopes | Where-Object Firmware).Count -gt 0
    Mutation = $empty -or @($pathScopes | Where-Object Mutation).Count -gt 0
}

if ($scope.Code) {
    Write-Host "Code-relevant changes found; firmware=$($scope.Firmware), mutation=$($scope.Mutation)."
    @($changedPaths | Where-Object { (Get-PathScope $_).Code }) | Select-Object -First 20 | ForEach-Object { Write-Host "  $_" }
}
else {
    Write-Host "All $($changedPaths.Count) changed paths are documentation only; skipping build, test and firmware jobs."
}

if (-not [string]::IsNullOrEmpty($env:GITHUB_OUTPUT)) {
    Add-Content -LiteralPath $env:GITHUB_OUTPUT -Encoding utf8NoBOM -Value @(
        "code=$($scope.Code.ToString().ToLowerInvariant())"
        "firmware=$($scope.Firmware.ToString().ToLowerInvariant())"
        "mutation=$($scope.Mutation.ToString().ToLowerInvariant())"
    )
}

return $scope
