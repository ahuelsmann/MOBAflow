# PowerShell script to start FeedbackApi
Write-Host "Starting FeedbackApi on http://192.168.0.22:5001..." -ForegroundColor Green
Set-Location -Path "$PSScriptRoot\FeedbackApi"
dotnet run --launch-profile FeedbackApi
