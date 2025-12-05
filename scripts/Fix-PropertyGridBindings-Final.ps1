$ErrorActionPreference='Stop'
Set-Location "C:\Repos\ahuelsmann\MOBAflow"

Write-Host "=== Fixing ALL PropertyGrid bindings to use CurrentSelectedObject ===" -ForegroundColor Cyan

$content = Get-Content "WinUI\View\EditorPage.xaml" -Raw

# Fix all PropertyGrid bindings to use CurrentSelectedObject
# This is the unified property that automatically returns the most specific selected item

Write-Host "[1/4] Solution Tab PropertyGrid..." -ForegroundColor Yellow
$content = $content -replace '(<Grid x:Name="SolutionContent"[\s\S]*?<local:SimplePropertyGrid[^>]*SelectedObject="\{x:Bind )ViewModel\.CurrentProjectViewModel', '$1ViewModel.CurrentSelectedObject'

Write-Host "[2/4] Journeys Tab PropertyGrid..." -ForegroundColor Yellow
$content = $content -replace '(<Grid x:Name="JourneysContent"[\s\S]*?<local:SimplePropertyGrid[^>]*SelectedObject="\{x:Bind )ViewModel\.SelectedJourney', '$1ViewModel.CurrentSelectedObject'

Write-Host "[3/4] Workflows Tab PropertyGrid..." -ForegroundColor Yellow
$content = $content -replace '(<Grid x:Name="WorkflowsContent"[\s\S]*?<local:SimplePropertyGrid[^>]*SelectedObject="\{x:Bind )ViewModel\.SelectedWorkflow', '$1ViewModel.CurrentSelectedObject'

Write-Host "[4/4] Trains Tab PropertyGrid..." -ForegroundColor Yellow
$content = $content -replace '(<Grid x:Name="TrainsContent"[\s\S]*?<local:SimplePropertyGrid[^>]*SelectedObject="\{x:Bind )ViewModel\.SelectedTrain', '$1ViewModel.CurrentSelectedObject'

Set-Content "WinUI\View\EditorPage.xaml" -Value $content -Encoding utf8

Write-Host ""
Write-Host "All PropertyGrid bindings now use CurrentSelectedObject!" -ForegroundColor Green
Write-Host ""
Write-Host "How it works:" -ForegroundColor Cyan
Write-Host "  - Select Project -> CurrentSelectedObject returns SelectedProject" -ForegroundColor White
Write-Host "  - Select Journey -> CurrentSelectedObject returns SelectedJourney" -ForegroundColor White
Write-Host "  - Select Workflow -> CurrentSelectedObject returns SelectedWorkflow" -ForegroundColor White
Write-Host "  - Select Train -> CurrentSelectedObject returns SelectedTrain" -ForegroundColor White
Write-Host ""
Write-Host "Please rebuild and test!" -ForegroundColor Green
