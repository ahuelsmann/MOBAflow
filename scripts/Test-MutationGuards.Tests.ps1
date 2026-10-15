[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$fixture = Join-Path ([IO.Path]::GetTempPath()) ('mobaflow-mutation-guards-' + [guid]::NewGuid().ToString('N'))
[void] [IO.Directory]::CreateDirectory($fixture)
$checks = 0
$laneGuard = Join-Path $PSScriptRoot 'Test-MutationLaneCoverage.ps1'
$runGuard = Join-Path $PSScriptRoot 'Test-MutationRunResults.ps1'

function Write-Fixture([string] $RelativePath, [string] $Content) {
    $path = Join-Path $fixture $RelativePath
    [void] [IO.Directory]::CreateDirectory((Split-Path -Parent $path))
    [IO.File]::WriteAllText($path, $Content)
    return $path
}
function Assert-Succeeds([string] $Script, [hashtable] $Parameters, [string] $Scenario) {
    try { & $Script @Parameters | Out-Null }
    catch { throw "Expected success for '$Scenario': $($_.Exception.Message)" }
    $script:checks++
}
function Assert-Fails([string] $Script, [hashtable] $Parameters, [string] $Scenario, [string] $ExpectedMessage) {
    $message = $null
    try { & $Script @Parameters | Out-Null }
    catch { $message = $_.Exception.Message }
    if ($null -eq $message -or $message -notlike "*$ExpectedMessage*") {
        throw "Expected '$ExpectedMessage' for '$Scenario', got '$message'."
    }
    $script:checks++
}
function Write-HostProject([string] $CompileItems) {
    [void] (Write-Fixture 'MutationTest/Host.csproj' @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><TargetFramework>net10.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup>
  <ItemGroup>$CompileItems<ProjectReference Include="../Common/Common.csproj" /></ItemGroup>
</Project>
"@)
}
function Write-Report([string] $Status = 'Killed', [string] $TestId = 'test-1', [string] $FilePath = $script:testFile) {
    $report = @{
        projectRoot = Join-Path $fixture 'Common'
        testFiles = @{ $FilePath = @{ tests = @(@{ id = $TestId; name = 'SettingsTests.SettingsTest' }) } }
        files = @{ 'Settings.cs' = @{ mutants = @(@{ id = '0'; status = $Status; killedBy = @($TestId); coveredBy = @($TestId) }) } }
    }
    return Write-Fixture 'mutation-report.json' ($report | ConvertTo-Json -Depth 12)
}

try {
    $script:testFile = Write-Fixture 'Test/Speech/SpeechSettingsTests.cs' '[TestFixture] class SettingsTests { [Test] void SettingsTest() {} }'
    [void] (Write-Fixture 'Common/Common.csproj' '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>')
    $registry = @{ lanes = @(
        @{ name = 'common'; status = 'active'; 'test-directories' = @(); 'test-files' = @('Test/Speech/SpeechSettingsTests.cs');
            'production-projects' = @('Common/Common.csproj'); 'stryker-config' = 'MutationTest/stryker-config.json' },
        @{ name = 'speech'; status = 'planned'; 'test-directories' = @('Test/Speech'); 'production-projects' = @() }
    ) }
    [void] (Write-Fixture 'MutationTest/mutation-lanes.json' ($registry | ConvertTo-Json -Depth 8))
    $config = @{ 'stryker-config' = @{ project = 'Common.csproj'; 'test-projects' = @('Host.csproj');
        'target-framework' = 'net10.0'; configuration = 'Debug' } }
    [void] (Write-Fixture 'MutationTest/stryker-config.json' ($config | ConvertTo-Json -Depth 8))
    $laneParameters = @{ RepositoryRoot = $fixture }
    Write-HostProject ''
    Assert-Fails $laneGuard $laneParameters 'registered Speech tests omitted from host' 'Registered tests missing'
    Write-HostProject '<Compile Include="../Test/Speech/*.cs" />'
    $projectPath = Join-Path $fixture 'MutationTest/Host.csproj'
    $validProject = Get-Content -Raw -LiteralPath $projectPath
    [void] (Write-Fixture 'MutationTest/Host.csproj' ($validProject.Replace('<ProjectReference Include="../Common/Common.csproj" />', '')))
    Assert-Fails $laneGuard $laneParameters 'host does not reference mutation target' 'does not reference'
    [void] (Write-Fixture 'MutationTest/Host.csproj' $validProject)
    Assert-Succeeds $laneGuard $laneParameters 'wildcard includes Speech tests'
    Assert-Succeeds $laneGuard @{ RepositoryRoot = $fixture; RequireActive = $true } 'active and planned overlap is active'
    Write-HostProject '<Compile Include="../Test/Speech/*.cs" /><Compile Remove="../Test/Speech/SpeechSettingsTests.cs" Condition="''$(Configuration)'' == ''Debug''" />'
    Assert-Fails $laneGuard $laneParameters 'effective conditional Compile Remove' 'Registered tests missing'
    Write-HostProject '<Compile Include="../Test/Speech/*.cs" />'
    $config.'stryker-config'['test-case-filter'] = 'FullyQualifiedName~OtherTests'
    [void] (Write-Fixture 'MutationTest/stryker-config.json' ($config | ConvertTo-Json -Depth 8))
    Assert-Fails $laneGuard $laneParameters 'filtered test cases' 'must not filter'
    $config.'stryker-config'.Remove('test-case-filter')
    $config.'stryker-config'.project = 'Wrong.csproj'
    [void] (Write-Fixture 'MutationTest/stryker-config.json' ($config | ConvertTo-Json -Depth 8))
    Assert-Fails $laneGuard $laneParameters 'wrong mutation target' 'production project does not match'
    Assert-Fails $laneGuard @{ RepositoryRoot = $fixture; LaneName = 'speech' } 'planned lane cannot be executed' 'Expected one active'
    [void] (Write-Fixture 'Test/Unmapped.cs' '[Test] class OtherTests {}')
    Assert-Fails $laneGuard $laneParameters 'unregistered new test fixture' 'without a mutation lane'
    Remove-Item -LiteralPath (Join-Path $fixture 'Test/Unmapped.cs')
    $registry.lanes[0].status = 'planned'
    [void] (Write-Fixture 'MutationTest/mutation-lanes.json' ($registry | ConvertTo-Json -Depth 8))
    Assert-Fails $laneGuard @{ RepositoryRoot = $fixture; RequireActive = $true } 'only planned is not active' 'only to planned'

    # Activating Speech.Base must include its cache fixture and reference the actual production assembly.
    [void] (Write-Fixture 'Test/Speech/AudioCacheTests.cs' '[TestFixture] class AudioCacheTests { [Test] void CacheTest() {} }')
    [void] (Write-Fixture 'Speech.Base/Speech.Base.csproj' '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>')
    $registry.lanes += @{
        name = 'speech-base'; status = 'active'; 'test-directories' = @(); 'test-files' = @('Test/Speech/AudioCacheTests.cs');
        'production-projects' = @('Speech.Base/Speech.Base.csproj'); 'stryker-config' = 'MutationTest/stryker-speech-base-config.json'
    }
    [void] (Write-Fixture 'MutationTest/mutation-lanes.json' ($registry | ConvertTo-Json -Depth 8))
    [void] (Write-Fixture 'MutationTest/stryker-speech-base-config.json' (@{
        'stryker-config' = @{ project = 'Speech.Base.csproj'; 'test-projects' = @('Speech.csproj');
            'target-framework' = 'net10.0'; configuration = 'Debug' }
    } | ConvertTo-Json -Depth 8))
    $speechProject = '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup><ProjectReference Include="../Speech.Base/Speech.Base.csproj" />{0}</ItemGroup></Project>'
    [void] (Write-Fixture 'MutationTest/Speech.csproj' ($speechProject -f ''))
    $speechParameters = @{ RepositoryRoot = $fixture; LaneName = 'speech-base' }
    Assert-Fails $laneGuard $speechParameters 'active Speech.Base host omits AudioCacheTests' 'Registered tests missing'
    $speechCompile = '<Compile Include="../Test/Speech/AudioCacheTests.cs" />'
    [void] (Write-Fixture 'MutationTest/Speech.csproj' ($speechProject -f $speechCompile))
    Assert-Succeeds $laneGuard $speechParameters 'active Speech.Base host includes its cache fixture'
    [void] (Write-Fixture 'MutationTest/Speech.csproj' (($speechProject -f $speechCompile).Replace('../Speech.Base/Speech.Base.csproj', '../Common/Common.csproj')))
    Assert-Fails $laneGuard $speechParameters 'Speech.Base host references only Common' 'does not reference'

    $started = [datetime]::UtcNow.AddSeconds(-2)
    $passingBaseline = '<TestRun><TestDefinitions><UnitTest><TestMethod className="SettingsTests" name="SettingsTest" /></UnitTest></TestDefinitions><ResultSummary><Counters total="1" executed="1" passed="1" /></ResultSummary></TestRun>'
    $baseline = Write-Fixture 'baseline.trx' $passingBaseline
    $parameters = @{ ReportPath = (Write-Report); BaselinePath = @($baseline); TestFilePath = @($testFile);
        ProductionRoot = (Join-Path $fixture 'Common'); StartedAt = $started }
    Assert-Succeeds $runGuard $parameters 'completed mutation run'
    [void] (Write-Fixture 'baseline.trx' ($passingBaseline.Replace('SettingsTest"', 'OtherTest"')))
    Assert-Fails $runGuard $parameters 'same count but different test cases' 'test cases do not match'
    [void] (Write-Fixture 'baseline.trx' $passingBaseline)
    $unicodeBaseline = $passingBaseline.Replace('SettingsTest"', 'SettingsTest(\ud83d\udd0d)"')
    [void] (Write-Fixture 'baseline.trx' $unicodeBaseline)
    $unicodeReport = Get-Content -Raw -LiteralPath $parameters.ReportPath | ConvertFrom-Json -AsHashtable
    $unicodeReport.testFiles[$testFile].tests[0].name = 'SettingsTests.SettingsTest(' + [char]::ConvertFromUtf32(0x1F50D) + ')'
    [void] (Write-Fixture 'mutation-report.json' ($unicodeReport | ConvertTo-Json -Depth 12))
    Assert-Succeeds $runGuard $parameters 'TRX escaped surrogate pairs match Stryker Unicode names'
    [void] (Write-Fixture 'baseline.trx' $passingBaseline)
    [void] (Write-Report)
    $invalidReport = Get-Content -Raw -LiteralPath $parameters.ReportPath | ConvertFrom-Json -AsHashtable
    $invalidReport.files['Settings.cs'].mutants[0].killedBy = @('absent-test')
    [void] (Write-Fixture 'mutation-report.json' ($invalidReport | ConvertTo-Json -Depth 12))
    Assert-Fails $runGuard $parameters 'unknown test reference' 'test absent from the report'
    $invalidReport.files['Settings.cs'].mutants[0].killedBy = @()
    [void] (Write-Fixture 'mutation-report.json' ($invalidReport | ConvertTo-Json -Depth 12))
    Assert-Fails $runGuard $parameters 'killed mutant without executed test' 'no test execution evidence'
    [void] (Write-Report)
    $invalidReport = Get-Content -Raw -LiteralPath $parameters.ReportPath | ConvertFrom-Json -AsHashtable
    $invalidReport.testFiles[$testFile].tests += $invalidReport.testFiles[$testFile].tests[0]
    [void] (Write-Fixture 'mutation-report.json' ($invalidReport | ConvertTo-Json -Depth 12))
    Assert-Fails $runGuard $parameters 'duplicate test IDs' 'empty or duplicate'
    [void] (Write-Report)
    $invalidReport = Get-Content -Raw -LiteralPath $parameters.ReportPath | ConvertFrom-Json -AsHashtable
    $invalidReport.files['Settings.cs'].mutants = @()
    [void] (Write-Fixture 'mutation-report.json' ($invalidReport | ConvertTo-Json -Depth 12))
    Assert-Fails $runGuard $parameters 'zero mutants' 'No mutants were executed'
    [void] (Write-Report)
    [void] (Write-Report 'Survived')
    Assert-Succeeds $runGuard $parameters 'surviving mutant is measured, not unfinished'
    foreach ($status in @('Pending', 'NotRun', 'Unexpected')) {
        [void] (Write-Report $status)
        Assert-Fails $runGuard $parameters "unfinished status $status" 'unfinished or unknown'
    }
    foreach ($status in @('Ignored', 'CompileError', 'NoCoverage')) {
        [void] (Write-Report $status)
        Assert-Fails $runGuard $parameters "no executed mutants ($status)" 'No mutants were executed'
    }
    [void] (Write-Report 'Killed' 'test-1' (Join-Path $fixture 'Test/Other.cs'))
    Assert-Fails $runGuard $parameters 'missing registered test in report' 'missing from mutation report'
    [void] (Write-Report)
    [IO.File]::SetLastWriteTimeUtc($parameters.ReportPath, $started.AddMinutes(-1))
    Assert-Fails $runGuard $parameters 'stale successful report' 'older than this run'
    [void] (Write-Report)
    [IO.File]::SetLastWriteTimeUtc($baseline, $started.AddMinutes(-1))
    Assert-Fails $runGuard $parameters 'stale ordinary baseline' 'baseline is older'
    [void] (Write-Fixture 'baseline.trx' $passingBaseline)
    $parameters.ProductionRoot = Join-Path $fixture 'Wrong'
    Assert-Fails $runGuard $parameters 'report for wrong production assembly' 'different production project'
    $parameters.ProductionRoot = Join-Path $fixture 'Common'
    [void] (Write-Fixture 'baseline.trx' ($passingBaseline.Replace('total="1" executed="1" passed="1"', 'total="2" executed="2" passed="2"')))
    Assert-Fails $runGuard $parameters 'report omits a discovered case' 'ordinary baseline passed 2'
    [void] (Write-Fixture 'baseline.trx' '<TestRun><ResultSummary><Counters total="2" executed="1" passed="1" /></ResultSummary></TestRun>')
    Assert-Fails $runGuard $parameters 'ordinary tests skip themselves' 'did not run'
    [void] (Write-Fixture 'baseline.trx' '<TestRun><ResultSummary><Counters total="1" executed="1" passed="0" /></ResultSummary></TestRun>')
    Assert-Fails $runGuard $parameters 'ordinary tests fail' 'did not pass'
    [void] (Write-Fixture 'baseline.trx' '<TestRun><ResultSummary><Counters total="0" executed="0" passed="0" /></ResultSummary></TestRun>')
    Assert-Fails $runGuard $parameters 'zero tests is not validation' 'Only 0 tests passed'
    Remove-Item -LiteralPath $parameters.ReportPath
    Assert-Fails $runGuard $parameters 'missing mutation report' 'does not exist'
    foreach ($lane in @('common', 'domain', 'speech-base')) {
        Assert-Fails (Join-Path $PSScriptRoot 'Invoke-MutationLane.ps1') @{ LaneName = $lane; OutputPath = $fixture } `
            "runner accepts $lane but rejects pre-existing outputs" 'already exists'
    }
    Write-Host "Mutation guard self-tests passed ($checks checks)."
}
finally {
    Remove-Item -LiteralPath $fixture -Recurse -Force -ErrorAction SilentlyContinue
}
