[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [string] $RepositoryPath = ".",

    [ValidateRange(1, 100)]
    [int] $MaxSamplesPerRule = 20,

    [string] $OutputPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root = (Resolve-Path -LiteralPath $RepositoryPath).Path
$excludedDirectoryPattern = "(^|/)(\.git|\.nuget|bin|obj|artifacts|TestResults|StrykerOutput|node_modules|packages|dist|build|coverage)(/|$)|(^|/)\.agents/skills(/|$)"
$sourceExtensions = @(
    ".c", ".cc", ".cpp", ".cs", ".cshtml", ".cxx", ".fs", ".go", ".h", ".hpp", ".ino",
    ".java", ".js", ".jsx", ".kt", ".kts", ".m", ".mm", ".php", ".ps1", ".py", ".razor",
    ".rs", ".sh", ".sql", ".swift", ".ts", ".tsx", ".vb", ".xaml"
)

function Get-RepositoryRelativeFiles {
    $git = Get-Command git -ErrorAction SilentlyContinue
    if ($null -ne $git) {
        $insideWorkTree = & git -C $root rev-parse --is-inside-work-tree 2>$null
        if ($LASTEXITCODE -eq 0 -and $insideWorkTree -eq "true") {
            return @(& git -C $root ls-files --cached --others --exclude-standard 2>$null) |
                Where-Object { $_ -and ($_ -replace "\\", "/") -notmatch $excludedDirectoryPattern } |
                ForEach-Object { $_ -replace "\\", "/" } |
                Sort-Object -Unique
        }
    }

    return @(Get-ChildItem -LiteralPath $root -File -Recurse -Force | ForEach-Object {
        [IO.Path]::GetRelativePath($root, $_.FullName) -replace "\\", "/"
    } | Where-Object { $_ -notmatch $excludedDirectoryPattern } | Sort-Object -Unique)
}

function Get-GitValue {
    param([string[]] $Arguments)

    if ($null -eq (Get-Command git -ErrorAction SilentlyContinue)) {
        return $null
    }

    $value = & git -C $root @Arguments 2>$null
    if ($LASTEXITCODE -ne 0) {
        return $null
    }

    return ($value -join "`n").Trim()
}

function Get-PatternSamples {
    param(
        [object[]] $Files,
        [string] $Pattern,
        [string[]] $Extensions
    )

    $samples = [Collections.Generic.List[object]]::new()
    $total = 0

    foreach ($file in $Files) {
        if ($Extensions.Count -gt 0 -and $file.Extension -notin $Extensions) {
            continue
        }

        try {
            $lines = @(Get-Content -LiteralPath $file.FullPath -ErrorAction Stop)
        }
        catch {
            continue
        }

        for ($index = 0; $index -lt $lines.Count; $index++) {
            if ($lines[$index] -notmatch $Pattern) {
                continue
            }

            $total++
            if ($samples.Count -lt $MaxSamplesPerRule) {
                $text = $lines[$index].Trim()
                if ($text.Length -gt 240) {
                    $text = $text.Substring(0, 240)
                }

                $samples.Add([ordered]@{
                    path = $file.Path
                    line = $index + 1
                    text = $text
                })
            }
        }
    }

    return [ordered]@{
        count = $total
        samples = @($samples)
        truncated = $total -gt $samples.Count
    }
}

$relativeFiles = @(Get-RepositoryRelativeFiles)
$fileRecords = foreach ($relativePath in $relativeFiles) {
    $fullPath = Join-Path $root ($relativePath -replace "/", [IO.Path]::DirectorySeparatorChar)
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        continue
    }

    $extension = [IO.Path]::GetExtension($relativePath).ToLowerInvariant()
    $leafName = [IO.Path]::GetFileName($relativePath)
    if (-not $extension -and $leafName.StartsWith(".")) {
        $extension = $leafName.ToLowerInvariant()
    }
    [pscustomobject]@{
        Path = $relativePath
        FullPath = $fullPath
        Extension = $extension
        IsSource = $extension -in $sourceExtensions
        IsGenerated = $relativePath -match "(?i)(\.g\.cs|\.g\.i\.cs|\.generated\.[^/]+|\.designer\.cs)$"
        IsTest = $relativePath -match "(?i)(^|/)(test|tests|spec|specs)(/|$)|(^|/)[^/]*(test|tests|spec|specs)\.[^/]+$"
    }
}

$sourceFiles = @($fileRecords | Where-Object IsSource)
$productionSource = @($sourceFiles | Where-Object { -not $_.IsTest -and -not $_.IsGenerated })
$testSource = @($sourceFiles | Where-Object { $_.IsTest -and -not $_.IsGenerated })
$generatedSource = @($sourceFiles | Where-Object IsGenerated)

$languageInventory = @($sourceFiles |
    Group-Object Extension |
    Sort-Object Count -Descending |
    ForEach-Object {
        [ordered]@{
            extension = if ($_.Name) { $_.Name } else { "(none)" }
            files = $_.Count
        }
    })

$projectFiles = @($fileRecords | Where-Object { $_.Extension -in @(".csproj", ".fsproj", ".vbproj") })
$projects = foreach ($project in $projectFiles) {
    $content = Get-Content -Raw -LiteralPath $project.FullPath
    $targetFrameworks = @([regex]::Matches($content, "<TargetFrameworks?>([^<]+)</TargetFrameworks?>") |
        ForEach-Object { $_.Groups[1].Value -split ";" } |
        ForEach-Object { $_.Trim() } |
        Where-Object { $_ } |
        Sort-Object -Unique)

    [ordered]@{
        path = $project.Path
        targetFrameworks = $targetFrameworks
        nullableConfiguredLocally = $content -match "<Nullable>"
        warningsAsErrorsConfiguredLocally = $content -match "<(TreatWarningsAsErrors|WarningsAsErrors)>"
        analyzerPackagesReferencedLocally = $content -match "(?i)(analyzers|stylecop|roslynator|sonar|meziantou)"
    }
}

$qualityPathPattern = "(?i)(^|/)(\.editorconfig|global\.json|Directory\.(Build|Packages|Solution)\.(props|targets)|AGENTS\.md)$|(^|/)\.github/(copilot-instructions\.md|instructions/.*\.md|workflows/.*)$|(^|/)\.azure-pipelines/.*$|(^|/)scripts/[^/]*(quality|test|coverage|mutation|analy|security|format|build)[^/]*$|(^|/)(test|tests|mutationtest)/(.*runsettings|.*thresholds.*\.json|mutation-lanes\.json|stryker-config\.json|.*\.(cs|fs|vb)proj)$"
$qualityConfiguration = @($fileRecords |
    Where-Object { $_.Path -match $qualityPathPattern } |
    Select-Object -ExpandProperty Path |
    Sort-Object -Unique)

$patterns = [ordered]@{
    blocking_task_wait = Get-PatternSamples -Files $sourceFiles -Pattern "(\.Result\b|\.Wait\s*\(|GetAwaiter\s*\(\s*\)\s*\.GetResult\s*\()" -Extensions @(".cs", ".fs", ".vb")
    async_void = Get-PatternSamples -Files $sourceFiles -Pattern "\basync\s+void\b" -Extensions @(".cs")
    diagnostic_suppression = Get-PatternSamples -Files $fileRecords -Pattern "(?i)(#pragma\s+warning\s+disable|SuppressMessage\s*\(|<NoWarn>|dotnet_diagnostic\.[^.]+\.severity\s*=\s*(none|silent))" -Extensions @(".cs", ".csproj", ".props", ".targets", ".editorconfig")
    work_marker = Get-PatternSamples -Files $sourceFiles -Pattern "(?i)(//|/\*|\*|#|<!--)\s*(TODO|FIXME|HACK|XXX)(\b|:)" -Extensions @()
    binary_formatter = Get-PatternSamples -Files $sourceFiles -Pattern "\bBinaryFormatter\b" -Extensions @(".cs", ".fs", ".vb")
    thread_sleep = Get-PatternSamples -Files $sourceFiles -Pattern "\bThread\.Sleep\s*\(" -Extensions @(".cs", ".fs", ".vb")
}

$lineCounts = [ordered]@{
    production = 0
    test = 0
    generated = 0
}

foreach ($file in $sourceFiles) {
    try {
        $count = @(Get-Content -LiteralPath $file.FullPath).Count
    }
    catch {
        continue
    }

    if ($file.IsGenerated) {
        $lineCounts.generated += $count
    }
    elseif ($file.IsTest) {
        $lineCounts.test += $count
    }
    else {
        $lineCounts.production += $count
    }
}

$gitStatus = Get-GitValue -Arguments @("status", "--porcelain")
$result = [ordered]@{
    schemaVersion = 1
    generatedAtUtc = [DateTime]::UtcNow.ToString("O")
    repository = [ordered]@{
        root = $root
        commit = Get-GitValue -Arguments @("rev-parse", "HEAD")
        branch = Get-GitValue -Arguments @("branch", "--show-current")
        dirty = -not [string]::IsNullOrWhiteSpace($gitStatus)
    }
    scope = [ordered]@{
        repositoryFiles = $fileRecords.Count
        sourceFiles = $sourceFiles.Count
        productionSourceFiles = $productionSource.Count
        testSourceFiles = $testSource.Count
        generatedSourceFiles = $generatedSource.Count
        lines = $lineCounts
        languages = $languageInventory
    }
    projects = @($projects)
    qualityConfigurationPaths = $qualityConfiguration
    candidates = $patterns
    caveats = @(
        "Pattern matches are candidates and require contextual validation before becoming findings.",
        "Generated, ignored, vendored, and build-output files may be absent from git-backed inventory.",
        "Line counts are physical text lines and are not a maintainability or complexity score."
    )
}

$json = $result | ConvertTo-Json -Depth 10
if ($OutputPath) {
    $resolvedOutput = if ([IO.Path]::IsPathRooted($OutputPath)) {
        $OutputPath
    }
    else {
        Join-Path $root $OutputPath
    }
    Set-Content -LiteralPath $resolvedOutput -Value $json -Encoding utf8NoBOM
}
else {
    $json
}
