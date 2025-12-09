$ErrorActionPreference='Stop'
[Console]::OutputEncoding=[Text.Encoding]::UTF8
[Console]::InputEncoding=[Text.Encoding]::UTF8
$ProgressPreference='SilentlyContinue'
if ($PSStyle) { $PSStyle.OutputRendering='Ansi' }

cd C:\Repos\ahuelsmann\MOBAflow

Write-Host "=========================================="
Write-Host "VIEWMODEL COMPLETENESS AUDIT"
Write-Host "Checking: Domain properties exist 1:1 in ViewModels"
Write-Host "=========================================="
Write-Host ""

function Get-PublicProperties($filePath) {
    Get-Content $filePath | 
        Select-String -Pattern '^\s+public\s+\w+\??\s+(\w+)\s*\{' |
        ForEach-Object { 
            if ($_.Line -match 'public\s+\w+\??\s+(\w+)\s*\{') {
                $matches[1]
            }
        }
}

# Station
Write-Host "=== STATION ===" -ForegroundColor Cyan
$domainStation = Get-PublicProperties "Domain\Station.cs"
$vmStation = Get-PublicProperties "SharedUI\ViewModel\StationViewModel.cs"

Write-Host "Domain.Station properties:" -ForegroundColor Yellow
$domainStation | ForEach-Object { Write-Host "  - $_" }

Write-Host "`nStationViewModel properties:" -ForegroundColor Yellow
$vmStation | ForEach-Object { Write-Host "  - $_" }

Write-Host "`nMISSING in ViewModel:" -ForegroundColor Red
$domainStation | Where-Object { $vmStation -notcontains $_ } | ForEach-Object { Write-Host "  ❌ $_" -ForegroundColor Red }

Write-Host ""
Write-Host "=== JOURNEY ===" -ForegroundColor Cyan
$domainJourney = Get-PublicProperties "Domain\Journey.cs"
$vmJourney = Get-PublicProperties "SharedUI\ViewModel\JourneyViewModel.cs"

Write-Host "Domain.Journey properties:" -ForegroundColor Yellow
$domainJourney | ForEach-Object { Write-Host "  - $_" }

Write-Host "`nJourneyViewModel properties:" -ForegroundColor Yellow
$vmJourney | ForEach-Object { Write-Host "  - $_" }

Write-Host "`nMISSING in ViewModel:" -ForegroundColor Red
$domainJourney | Where-Object { $vmJourney -notcontains $_ } | ForEach-Object { Write-Host "  ❌ $_" -ForegroundColor Red }

Write-Host ""
Write-Host "=== WORKFLOW ===" -ForegroundColor Cyan
$domainWorkflow = Get-PublicProperties "Domain\Workflow.cs"
$vmWorkflow = Get-PublicProperties "SharedUI\ViewModel\WorkflowViewModel.cs"

Write-Host "Domain.Workflow properties:" -ForegroundColor Yellow
$domainWorkflow | ForEach-Object { Write-Host "  - $_" }

Write-Host "`nWorkflowViewModel properties:" -ForegroundColor Yellow
$vmWorkflow | ForEach-Object { Write-Host "  - $_" }

Write-Host "`nMISSING in ViewModel:" -ForegroundColor Red
$domainWorkflow | Where-Object { $vmWorkflow -notcontains $_ } | ForEach-Object { Write-Host "  ❌ $_" -ForegroundColor Red }
