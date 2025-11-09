@echo off
REM Start FeedbackApi on port 5001
echo Starting FeedbackApi on http://192.168.0.22:5001...
cd /d "%~dp0FeedbackApi"
dotnet run --launch-profile FeedbackApi
pause
