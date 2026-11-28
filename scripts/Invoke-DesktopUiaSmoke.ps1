<#
.SYNOPSIS
    Offline WinUI 3 desktop UI Automation smoke and accessibility check for MOBAflow (#569).

.DESCRIPTION
    Builds MOBAflow, stages an isolated copy of the output with a sanitized offline configuration and sample
    solution, launches it, and verifies the main window, the navigation items, the Signal Box, Track Plan and
    Train Control pages and the Dark/Light theme toggle through Windows UI Automation patterns only.

    Safety: the staged configuration disables the web app and points to a solution without a Z21 address, so the
    app never opens a network connection. The script never toggles Track Power and never sends Z21/ESP32 commands.
    It refuses to start while another MOBAflow process runs (except with -DryRun).

.PARAMETER BuildConfiguration
    Debug or Release build of MOBAflow/MOBAflow.csproj. Default: Debug.

.PARAMETER NoBuild
    Reuse the existing build output instead of building.

.PARAMETER DryRun
    Validate guards, staging inputs and configuration only; build nothing, launch nothing.

.PARAMETER DumpTree
    Print the UIA control tree (type, class, name, AutomationId) of the main window for diagnosis.

.PARAMETER OutputJson
    Optional path for the structured JSON report.

.PARAMETER TimeoutSeconds
    Maximum wait for the window and for each page (default 60).
#>
[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$BuildConfiguration = 'Debug',
    [switch]$NoBuild,
    [switch]$DryRun,
    [switch]$DumpTree,
    [string]$OutputJson,
    [ValidateRange(5, 600)]
    [int]$TimeoutSeconds = 60
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
$projectPath = Join-Path $repoRoot 'MOBAflow/MOBAflow.csproj'
$processName = 'MOBAflow'
$results = [System.Collections.Generic.List[object]]::new()
$knownGapReport = [System.Collections.Generic.List[object]]::new()
# Tolerated accessibility gaps (key: ControlType|ClassName|AutomationId). Currently none. They are reported as
# warnings; any other unnamed focusable control fails the run. Remove an entry when the product is fixed.
$knownAccessibilityGaps = @{}
$stagingRoot = $null
$process = $null
$exitCode = 0

function Add-Result {
    param([string]$Name, [bool]$Passed, [string]$Detail = '')
    $results.Add([pscustomobject]@{ Name = $Name; Passed = $Passed; Detail = $Detail })
    $mark = if ($Passed) { 'PASS' } else { 'FAIL' }
    Write-Host ("[{0}] {1}{2}" -f $mark, $Name, $(if ($Detail) { " - $Detail" } else { '' }))
}

function Wait-Until {
    param([scriptblock]$Condition, [int]$Seconds, [int]$PollMs = 250)
    $deadline = [DateTime]::UtcNow.AddSeconds($Seconds)
    while ([DateTime]::UtcNow -lt $deadline) {
        $value = & $Condition
        if ($value) { return $value }
        Start-Sleep -Milliseconds $PollMs
    }
    return $null
}

function Write-OfflineSettings {
    param([string]$SourcePath, [string]$TargetPath)
    $settings = Get-Content -LiteralPath $SourcePath -Raw | ConvertFrom-Json
    $settings.Application.AutoStartWebApp = $false
    $settings.Application.AutoLoadLastSolution = $true
    $settings.Application.ResetWindowLayoutOnStart = $true
    # The app has no Z21 "AutoConnect" switch: it connects only to the Z21 address stored in the project, so the
    # sanitized solution below (empty address) is what keeps the run offline. Retry timer is slowed down as well.
    $settings.Z21.AutoConnectRetryIntervalSeconds = 3600
    $settings.Display.Esp32IpAddress = ''
    $settings.RestApi.CurrentIpAddress = ''
    $settings.HealthCheck.Enabled = $false
    $settings | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $TargetPath -Encoding utf8
    return $settings
}

function Write-OfflineSolution {
    param([string]$SourcePath, [string]$TargetPath)
    $solution = Get-Content -LiteralPath $SourcePath -Raw | ConvertFrom-Json
    foreach ($project in $solution.projects) {
        $z21 = [pscustomobject]@{ ipAddress = ''; port = 21105; serialNumber = 0 }
        if ($project.PSObject.Properties['z21']) { $project.z21 = $z21 }
        else { $project | Add-Member -NotePropertyName z21 -NotePropertyValue $z21 }
    }
    $solution | ConvertTo-Json -Depth 50 | Set-Content -LiteralPath $TargetPath -Encoding utf8
    return $solution
}

$running = @(Get-Process -Name $processName -ErrorAction SilentlyContinue)
if ($running.Count -gt 0 -and -not $DryRun) {
    [Console]::Error.WriteLine("Refusing to start: $($running.Count) '$processName' process(es) already running (PID $($running.Id -join ', ')). Close them first.")
    exit 2
}

try {
    Add-Result 'No running MOBAflow process' ($running.Count -eq 0 -or $DryRun) "running=$($running.Count)"

    foreach ($required in 'MOBAflow/appsettings.json', 'MOBAflow/solution.json') {
        if (-not (Test-Path -LiteralPath (Join-Path $repoRoot $required))) { throw "Missing required file: $required" }
    }

    # Validate the offline configuration in a temp folder (also used by -DryRun).
    $stagingRoot = Join-Path ([IO.Path]::GetTempPath()) ("mobaflow-uia-" + [Guid]::NewGuid().ToString('N'))
    $appDir = Join-Path $stagingRoot 'app'
    New-Item -ItemType Directory -Path $appDir -Force | Out-Null
    $settings = Write-OfflineSettings (Join-Path $repoRoot 'MOBAflow/appsettings.json') (Join-Path $appDir 'appsettings.json')
    $solutionPath = Join-Path $stagingRoot 'solution/offline-solution.json'
    New-Item -ItemType Directory -Path (Split-Path $solutionPath) -Force | Out-Null
    $solution = Write-OfflineSolution (Join-Path $repoRoot 'MOBAflow/solution.json') $solutionPath
    $settings.Application.LastSolutionPath = $solutionPath
    $settings | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath (Join-Path $appDir 'appsettings.json') -Encoding utf8

    $addresses = @($solution.projects | ForEach-Object { $_.z21.ipAddress })
    Add-Result 'Offline config: AutoStartWebApp=false' ($settings.Application.AutoStartWebApp -eq $false)
    Add-Result 'Offline solution: all Z21 addresses empty' (@($addresses | Where-Object { $_ }).Count -eq 0) "projects=$($addresses.Count)"

    if ($DryRun) {
        Add-Result 'Dry run' $true 'build, launch and UIA skipped'
    }
    else {

        if (-not $NoBuild) {
            Write-Host "Building $projectPath ($BuildConfiguration)..."
            & dotnet build $projectPath -c $BuildConfiguration -p:BuildMOBApiDependency=false -p:CopyMOBApiToOutput=false -nologo -v:minimal
            if ($LASTEXITCODE -ne 0) { throw "dotnet build failed with exit code $LASTEXITCODE" }
        }
        $props = & dotnet msbuild $projectPath -getProperty:TargetPath -p:Configuration=$BuildConfiguration -nologo
        $outputDir = Split-Path (($props | Out-String).Trim())
        $exe = Join-Path $outputDir 'MOBAflow.exe'
        if (-not (Test-Path -LiteralPath $exe)) { throw "Built executable not found: $exe (build first or omit -NoBuild)" }
        Add-Result 'Build output present' $true $outputDir

        # Stage an isolated copy; the original output and its appsettings stay untouched.
        $stagedConfig = Join-Path $appDir 'appsettings.json'
        $stagedConfigText = Get-Content -LiteralPath $stagedConfig -Raw
        Copy-Item -Path (Join-Path $outputDir '*') -Destination $appDir -Recurse -Force
        Set-Content -LiteralPath $stagedConfig -Value $stagedConfigText -Encoding utf8
        Get-ChildItem -LiteralPath $appDir -Filter 'appsettings.Development.json' -ErrorAction SilentlyContinue | Remove-Item -Force

        Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes
        $navItemClass = 'Microsoft.UI.Xaml.Controls.NavigationViewItem'
    $A = [System.Windows.Automation.AutomationElement]
        $CT = [System.Windows.Automation.ControlType]
        $PF = [System.Windows.Automation.PropertyCondition]
        $scope = [System.Windows.Automation.TreeScope]

        $process = Start-Process -FilePath (Join-Path $appDir 'MOBAflow.exe') -WorkingDirectory $appDir -PassThru
        $window = Wait-Until -Seconds $TimeoutSeconds -Condition {
            $process.Refresh()
            if ($process.HasExited) { throw "MOBAflow exited early with code $($process.ExitCode)" }
            $candidates = $A::RootElement.FindAll($scope::Children, ($PF::new($A::ProcessIdProperty, $process.Id)))
            foreach ($candidate in $candidates) { if ($candidate.Current.Name -like '*MOBAflow*') { $candidate; break } }
        }
        if (-not $window) { throw "Main window did not appear within $TimeoutSeconds s" }
        $windowName = $window.Current.Name
        Add-Result 'Main window appears' ($windowName -like '*MOBAflow*') "name='$windowName'"

            if ($DumpTree) {
            Start-Sleep -Seconds 5
            $walker = [System.Windows.Automation.TreeWalker]::RawViewWalker
            $dump = {
                param($element, $depth)
                if ($depth -gt 12) { return }
                $c = $element.Current
                Write-Host ("{0}{1} class='{2}' name='{3}' id='{4}'" -f ('  ' * $depth), $c.ControlType.ProgrammaticName, $c.ClassName, $c.Name, $c.AutomationId)
                for ($child = $walker.GetFirstChild($element); $child; $child = $walker.GetNextSibling($child)) { & $dump $child ($depth + 1) }
            }
            & $dump $window 0
        }
        $getNavItems = {
            $window.FindAll($scope::Descendants,
                [System.Windows.Automation.AndCondition]::new(
                    $PF::new($A::ClassNameProperty, $navItemClass),
                    $PF::new($A::IsOffscreenProperty, $false)))
        }
        $items = Wait-Until -Seconds $TimeoutSeconds -Condition {
            $found = @(& $getNavItems)
            if ($found.Count -gt 0) { , $found }
        }
        if (-not $items) { throw 'No navigation items found' }
        $items = @($items)
        $names = @(foreach ($i in $items) { $i.Current.Name })
        $unnamed = @($names | Where-Object { [string]::IsNullOrWhiteSpace($_) })
        Add-Result 'Navigation items exposed' ($items.Count -gt 0) "count=$($items.Count)"
        Add-Result '0 unnamed navigation items' ($unnamed.Count -eq 0) "unnamed=$($unnamed.Count)"

        function Find-ByName {
            param([string]$Name, $Root = $window)
            $Root.FindFirst($scope::Descendants, ($PF::new($A::NameProperty, $Name)))
        }

        function Select-NavItem {
            param([string]$Name)
            # Items may be hidden in a collapsed pane; the pane toggle is a semantic Button in the same tree.
            $item = $window.FindFirst($scope::Descendants,
                [System.Windows.Automation.AndCondition]::new(
                    $PF::new($A::ClassNameProperty, $navItemClass),
                    $PF::new($A::NameProperty, $Name)))
            if (-not $item) { return $null }
            $pattern = $null
            if ($item.TryGetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern, [ref]$pattern)) {
                $pattern.Select()
            }
            elseif ($item.TryGetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern, [ref]$pattern)) {
                $pattern.Invoke()
            }
            else { return $false }
            return $true
        }

        $pages = @(
            @{ Nav = 'Signal Box'; Marker = 'Delete' }
            @{ Nav = 'Track Plan'; Marker = 'Track plan toolbar' }
            @{ Nav = 'Train Control'; Marker = 'Emergency Stop' }
        )
        foreach ($page in $pages) {
            $selected = Select-NavItem $page.Nav
            if ($selected -ne $true) {
                Add-Result "Navigate to $($page.Nav)" $false 'navigation item not found or has no Select/Invoke pattern'
                continue
            }
            $marker = Wait-Until -Seconds $TimeoutSeconds -Condition { Find-ByName $page.Marker }
            Add-Result "Navigate to $($page.Nav)" ($null -ne $marker) "marker='$($page.Marker)'"
            if ($marker) {
                $controls = $window.FindAll($scope::Descendants, ($PF::new($A::IsKeyboardFocusableProperty, $true)))
                $unnamedControls = @(foreach ($c in $controls) {
                        if ([string]::IsNullOrWhiteSpace($c.Current.Name) -and -not $c.Current.IsOffscreen -and
                            $c.Current.ControlType -in @($CT::Button, $CT::CheckBox, $CT::ComboBox, $CT::Slider)) { $c }
                    })
                $unnamedKeys = @($unnamedControls | ForEach-Object { "$($_.Current.ControlType.ProgrammaticName -replace 'ControlType\.','')|$($_.Current.ClassName)|$($_.Current.AutomationId)" })
                $newGaps = @($unnamedKeys | Where-Object { $_ -notin $knownAccessibilityGaps.Keys })
                $knownHits = @($unnamedKeys | Where-Object { $_ -in $knownAccessibilityGaps.Keys })
                foreach ($key in ($knownHits | Sort-Object -Unique)) {
                    $count = @($knownHits | Where-Object { $_ -eq $key }).Count
                    $knownGapReport.Add([pscustomobject]@{ Page = $page.Nav; Control = $key; Count = $count; Reason = $knownAccessibilityGaps[$key] })
                    Write-Host "[WARN] $($page.Nav): known accessibility gap x$count $key - $($knownAccessibilityGaps[$key])"
                }
                Add-Result "$($page.Nav): focusable buttons/inputs have names" ($newGaps.Count -eq 0) "new unnamed=$($newGaps.Count), known gaps=$($knownHits.Count), focusable=$($controls.Count) $(($newGaps | Sort-Object -Unique) -join '; ')"
            }
        }

        # Theme toggle: ToggleSwitch named "Dark theme" exposes TogglePattern; toggle twice and verify the app survives.
        $toggle = Find-ByName 'Dark theme'
        if ($toggle) {
            $pattern = $null
            if ($toggle.TryGetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern, [ref]$pattern)) {
                $before = $pattern.Current.ToggleState
                $pattern.Toggle(); Start-Sleep -Milliseconds 500
                $mid = $pattern.Current.ToggleState
                $pattern.Toggle(); Start-Sleep -Milliseconds 500
                $after = $pattern.Current.ToggleState
                $process.Refresh()
                Add-Result 'Theme toggle Dark <-> Light stable' (($mid -ne $before) -and ($after -eq $before) -and -not $process.HasExited) "before=$before mid=$mid after=$after"
            }
            else { Add-Result 'Theme toggle Dark <-> Light stable' $false 'no TogglePattern' }
        }
        else { Add-Result 'Theme toggle Dark <-> Light stable' $false "'Dark theme' toggle not found in UIA tree" }

        $process.Refresh()
        Add-Result 'App still running after checks' (-not $process.HasExited)
    }
}
catch {
    Add-Result 'Unexpected error' $false $_.Exception.Message
    $exitCode = 1
}
finally {
    $closedCleanly = $true
    if ($process -and -not $process.HasExited) {
        try {
            $closeWindow = $null
            foreach ($candidate in [System.Windows.Automation.AutomationElement]::RootElement.FindAll(
                    [System.Windows.Automation.TreeScope]::Children,
                    [System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $process.Id))) {
                if ($candidate.Current.Name -like '*MOBAflow*') { $closeWindow = $candidate; break }
            }
            $wp = $null
            if ($closeWindow -and $closeWindow.TryGetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern, [ref]$wp)) { $wp.Close() }
        }
        catch { Write-Warning "WindowPattern.Close failed: $($_.Exception.Message)" }
        if (-not $process.WaitForExit(15000)) {
            $closedCleanly = $false
            Write-Warning 'MOBAflow did not close within 15 s; stopping the process.'
            Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
            $process.WaitForExit(5000) | Out-Null
        }
    }
    if ($process) {
        $leftover = @(Get-Process -Name $processName -ErrorAction SilentlyContinue).Count
        Add-Result 'Closed cleanly via WindowPattern.Close' $closedCleanly
        Add-Result '0 leftover MOBAflow processes' ($leftover -eq 0) "leftover=$leftover"
    }
    if ($stagingRoot -and (Test-Path -LiteralPath $stagingRoot)) {
        Remove-Item -LiteralPath $stagingRoot -Recurse -Force -ErrorAction SilentlyContinue
    }

    $failed = @($results | Where-Object { -not $_.Passed })
    Write-Host ("Summary: {0} checks, {1} passed, {2} failed" -f $results.Count, ($results.Count - $failed.Count), $failed.Count)
    if ($OutputJson) {
        [pscustomobject]@{
            Timestamp = [DateTime]::UtcNow.ToString('o')
            Configuration = $BuildConfiguration
            DryRun = [bool]$DryRun
            Passed = ($failed.Count -eq 0)
            KnownAccessibilityGaps = $knownGapReport
            Checks = $results
        } | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $OutputJson -Encoding utf8
    }
    if ($failed.Count -gt 0) { $exitCode = 1 }
}
exit $exitCode
