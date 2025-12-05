$ErrorActionPreference='Stop'
Set-Location "C:\Repos\ahuelsmann\MOBAflow"

Write-Host "=== MOBAflow Project Cleanup ===" -ForegroundColor Cyan

# 1. Remove old backup files
Write-Host "[1/3] Removing backup files..." -ForegroundColor Yellow
Remove-Item "WinUI\View\*.backup*","WinUI\View\*.old*","WinUI\View\*.tmp","WinUI\View\*.new","WinUI\View\*.temp" -ErrorAction SilentlyContinue

# 2. Remove placeholder/unused editor pages
Write-Host "[2/3] Removing unused editor pages..." -ForegroundColor Yellow
Remove-Item "WinUI\View\PlaceholderEditorPage.*","WinUI\View\*Editor1Page.*" -ErrorAction SilentlyContinue

# 3. Clean bin/obj folders
Write-Host "[3/3] Cleaning bin/obj folders..." -ForegroundColor Yellow
Get-ChildItem -Path . -Include bin,obj -Recurse -Directory | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue

Write-Host "Cleanup complete!" -ForegroundColor Green
Write-Host "Remaining files in WinUI/View:" -ForegroundColor Cyan
Get-ChildItem "WinUI\View" -File | Select-Object Name | Format-Table -AutoSize
