$ErrorActionPreference='Stop'
[Console]::OutputEncoding=[Text.Encoding]::UTF8
[Console]::InputEncoding=[Text.Encoding]::UTF8
$ProgressPreference='SilentlyContinue'
if ($PSStyle) { $PSStyle.OutputRendering='Ansi' }

cd C:\Repos\ahuelsmann\MOBAflow

Write-Host "=========================================="
Write-Host "BINDING ANALYSIS: Domain → ViewModel → XAML"
Write-Host "=========================================="
Write-Host ""

# StationViewModel
Write-Host "=== StationViewModel ===" -ForegroundColor Cyan
Get-Content "SharedUI\ViewModel\StationViewModel.cs" | 
    Select-String -Pattern '^\s+public\s+\w+\s+\w+\s*(\{|$)' | 
    ForEach-Object { $_.Line.Trim() } |
    Select-Object -First 15

Write-Host ""

# JourneyViewModel  
Write-Host "=== JourneyViewModel ===" -ForegroundColor Cyan
Get-Content "SharedUI\ViewModel\JourneyViewModel.cs" |
    Select-String -Pattern '^\s+public\s+\w+\s+\w+\s*(\{|$)' |
    ForEach-Object { $_.Line.Trim() } |
    Select-Object -First 15

Write-Host ""

# WorkflowViewModel
Write-Host "=== WorkflowViewModel ===" -ForegroundColor Cyan
Get-Content "SharedUI\ViewModel\WorkflowViewModel.cs" |
    Select-String -Pattern '^\s+public\s+\w+\s+\w+\s*(\{|$)' |
    ForEach-Object { $_.Line.Trim() } |
    Select-Object -First 15

Write-Host ""

# TrainViewModel
Write-Host "=== TrainViewModel ===" -ForegroundColor Cyan
if (Test-Path "SharedUI\ViewModel\TrainViewModel.cs") {
    Get-Content "SharedUI\ViewModel\TrainViewModel.cs" |
        Select-String -Pattern '^\s+public\s+\w+\s+\w+\s*(\{|$)' |
        ForEach-Object { $_.Line.Trim() } |
        Select-Object -First 15
} else {
    Write-Host "TrainViewModel.cs not found!" -ForegroundColor Red
}
