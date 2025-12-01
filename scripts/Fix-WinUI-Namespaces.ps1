# WinUI Namespace Migration Script
# Run this after closing Visual Studio

Write-Host "Starting WinUI namespace migration..." -ForegroundColor Cyan

$files = @(
    "C:\Repos\ahuelsmann\MOBAflow\WinUI\View\MainWindow.xaml.cs",
    "C:\Repos\ahuelsmann\MOBAflow\WinUI\View\EditorPage.xaml.cs",
    "C:\Repos\ahuelsmann\MOBAflow\WinUI\App.xaml.cs"
)

foreach ($file in $files) {
    if (Test-Path $file) {
        Write-Host "`nProcessing: $($file.Split('\')[-1])" -ForegroundColor Yellow
        
        $content = [System.IO.File]::ReadAllText($file)
        $originalLength = $content.Length
        
        # Replace Backend.Model namespace with Domain
        $content = $content -replace 'Backend\.Model\.Journey','Journey'
        $content = $content -replace 'Backend\.Model\.Workflow','Workflow'
        $content = $content -replace 'Backend\.Model\.Station','Station'
        $content = $content -replace 'Backend\.Model\.Train','Train'
        $content = $content -replace 'Backend\.Model\.Project','Project'
        $content = $content -replace 'Backend\.Model\.Solution','Solution'
        
        # Handle Action.Base (old hierarchy) -> WorkflowAction
        $content = $content -replace 'Backend\.Model\.Action\.Base','WorkflowAction'
        
        if ($content.Length -ne $originalLength) {
            [System.IO.File]::WriteAllText($file, $content)
            Write-Host "  ✅ Updated" -ForegroundColor Green
        } else {
            Write-Host "  ⏭️  No changes needed" -ForegroundColor Gray
        }
    } else {
        Write-Host "  ❌ File not found: $file" -ForegroundColor Red
    }
}

Write-Host "`n✅ Migration complete!" -ForegroundColor Green
Write-Host "Run 'dotnet build WinUI/WinUI.csproj' to verify" -ForegroundColor Cyan
