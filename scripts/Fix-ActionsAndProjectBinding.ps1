$ErrorActionPreference='Stop'
Set-Location "C:\Repos\ahuelsmann\MOBAflow"

Write-Host "=== Fixing Actions DataTemplate & Project PropertyGrid ===" -ForegroundColor Cyan

$content = Get-Content "WinUI\View\EditorPage.xaml" -Raw

# Fix 1: Actions ListView - use {Binding} instead of {x:Bind}
Write-Host "[1/2] Fixing Actions DataTemplate (polymorphic ViewModels)..." -ForegroundColor Yellow

# Change from x:Bind to Binding for ToString()
$content = $content -replace '\{x:Bind ToString\(\), Mode=OneWay\}', '{Binding}'

# The ListView ItemsSource should also work better with DataContext
# But x:Bind to SelectedWorkflow.Actions should be fine

Set-Content "WinUI\View\EditorPage.xaml" -Value $content -Encoding utf8
Write-Host "[1/2] Actions: Changed to classic Binding for polymorphic display" -ForegroundColor Green

# Fix 2: Solution PropertyGrid - check if SelectedProject binding is correct
Write-Host "[2/2] Checking Solution PropertyGrid binding..." -ForegroundColor Yellow

$content = Get-Content "WinUI\View\EditorPage.xaml" -Raw

# PropertyGrid in Solution should bind to SelectedProject
if ($content -match 'SolutionContent.*?SimplePropertyGrid.*?SelectedObject="\{x:Bind ViewModel\.SelectedProject') {
    Write-Host "[2/2] Solution PropertyGrid binding looks correct (ViewModel.SelectedProject)" -ForegroundColor Green
} else {
    Write-Host "[2/2] WARNING: Solution PropertyGrid binding might be wrong!" -ForegroundColor Red
    # Extract the actual binding
    if ($content -match 'SolutionContent[\s\S]{0,1000}SimplePropertyGrid[^>]*SelectedObject="([^"]*)"') {
        Write-Host "     Current binding: $($matches[1])" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "Checking if ViewModel.SelectedProject property exists..." -ForegroundColor Cyan
$vmContent = Get-Content "SharedUI\ViewModel\MainWindowViewModel.cs" -Raw

if ($vmContent -match 'public.*SelectedProject') {
    Write-Host "SelectedProject property: FOUND" -ForegroundColor Green
    Select-String -Path "SharedUI\ViewModel\MainWindowViewModel.cs" -Pattern "SelectedProject" | Select-Object -First 3
} else {
    Write-Host "SelectedProject property: NOT FOUND!" -ForegroundColor Red
    Write-Host "Available Project properties:" -ForegroundColor Yellow
    Select-String -Path "SharedUI\ViewModel\MainWindowViewModel.cs" -Pattern "public.*Project" | Select-Object -First 5
}

Write-Host ""
Write-Host "Done! Please rebuild and test." -ForegroundColor Green
