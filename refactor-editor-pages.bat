@echo off
echo === MOBAflow Refactoring Script ===
echo.
echo Bitte Visual Studio KOMPLETT schliessen vor Ausfuehrung!
echo.
pause

cd /d "%~dp0"
cd ..

echo.
echo [1/10] Rename EditorPage to EditorPage1...
move /Y "WinUI\View\EditorPage.xaml" "WinUI\View\EditorPage1.xaml"
move /Y "WinUI\View\EditorPage.xaml.cs" "WinUI\View\EditorPage1.xaml.cs"

echo [2/10] Rename ProjectConfigurationPage to EditorPage2...
move /Y "WinUI\View\ProjectConfigurationPage.xaml" "WinUI\View\EditorPage2.xaml"
move /Y "WinUI\View\ProjectConfigurationPage.xaml.cs" "WinUI\View\EditorPage2.xaml.cs"

echo [3/10] Update EditorPage1.xaml x:Class...
powershell -Command "(Get-Content 'WinUI\View\EditorPage1.xaml' -Raw) -replace 'x:Class=\"Moba.WinUI.View.EditorPage\"', 'x:Class=\"Moba.WinUI.View.EditorPage1\"' | Set-Content 'WinUI\View\EditorPage1.xaml' -NoNewline"

echo [4/10] Update EditorPage1.xaml.cs class name...
powershell -Command "$c = (Get-Content 'WinUI\View\EditorPage1.xaml.cs' -Raw); $c = $c -replace 'public sealed partial class EditorPage ', 'public sealed partial class EditorPage1 '; $c = $c -replace 'public EditorPage\(', 'public EditorPage1('; $c = $c -replace 'EditorPage_Loaded', 'EditorPage1_Loaded'; $c = $c -replace 'Loaded \+= EditorPage_Loaded', 'Loaded += EditorPage1_Loaded'; Set-Content 'WinUI\View\EditorPage1.xaml.cs' -Value $c -NoNewline"

echo [5/10] Update EditorPage2.xaml x:Class...
powershell -Command "(Get-Content 'WinUI\View\EditorPage2.xaml' -Raw) -replace 'x:Class=\"Moba.WinUI.View.ProjectConfigurationPage\"', 'x:Class=\"Moba.WinUI.View.EditorPage2\"' | Set-Content 'WinUI\View\EditorPage2.xaml' -NoNewline"

echo [6/10] Update EditorPage2.xaml.cs class name...
powershell -Command "$c = (Get-Content 'WinUI\View\EditorPage2.xaml.cs' -Raw); $c = $c -replace 'public sealed partial class ProjectConfigurationPage ', 'public sealed partial class EditorPage2 '; $c = $c -replace 'public ProjectConfigurationPage\(\)', 'public EditorPage2()'; Set-Content 'WinUI\View\EditorPage2.xaml.cs' -Value $c -NoNewline"

echo [7/10] Update MainWindow.xaml.cs navigation...
powershell -Command "$c = (Get-Content 'WinUI\View\MainWindow.xaml.cs' -Raw); $c = $c -replace 'typeof\(EditorPage\)', 'typeof(EditorPage1)'; $c = $c -replace 'typeof\(ProjectConfigurationPage\)', 'typeof(EditorPage2)'; $c = $c -replace 'new SharedUI\.ViewModel\.EditorPageViewModel\(ViewModel\)', 'ViewModel'; $c = $c -replace 'new SharedUI\.ViewModel\.ProjectConfigurationPageViewModel\(ViewModel\)', 'ViewModel'; Set-Content 'WinUI\View\MainWindow.xaml.cs' -Value $c -NoNewline"

echo [8/10] Delete EditorPageViewModel.cs...
del /F "SharedUI\ViewModel\EditorPageViewModel.cs"

echo [9/10] Delete ProjectConfigurationPageViewModel.cs...
del /F "SharedUI\ViewModel\ProjectConfigurationPageViewModel.cs"

echo [10/10] Update EditorPage1.xaml bindings...
powershell -Command "$c = (Get-Content 'WinUI\View\EditorPage1.xaml' -Raw); $c = $c -replace '\{Binding ViewModel\.MainWindowViewModel\.', '{Binding ViewModel.'; Set-Content 'WinUI\View\EditorPage1.xaml' -Value $c -NoNewline"

echo.
echo === Refactoring Complete! ===
echo.
echo Naechste Schritte:
echo 1. Oeffne Visual Studio
echo 2. Build Solution (Ctrl+Shift+B)
echo 3. Sollte erfolgreich sein!
echo.
pause
