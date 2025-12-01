$file = "SharedUI\ViewModel\MainWindowViewModel.cs"
$backup = "$file.bak"

# Create backup
Copy-Item $file $backup -Force

$lines = Get-Content $file

for ($i = 0; $i -lt $lines.Count; $i++) {
    $line = $lines[$i]
    
    # Add AppSettings import after Backend.Interface
    if ($line -match "^using Moba.Backend.Interface;") {
        $lines[$i] = $line + "`nusing Moba.Common.Configuration;"
    }
    
    # Add AppSettings field after _uiDispatcher
    if ($line -match "private readonly IUiDispatcher _uiDispatcher;") {
        $lines[$i] = $line + "`n    private readonly AppSettings _settings;"
    }
    
    # Update constructor - add AppSettings parameter
    if ($line -match "IUiDispatcher uiDispatcher,") {
        $lines[$i] = $line -replace "IUiDispatcher uiDispatcher,", "IUiDispatcher uiDispatcher,`n        AppSettings settings,"
    }
    
    # Add _settings assignment
    if ($line -match "^\s+_uiDispatcher = uiDispatcher;") {
        $lines[$i] = $line + "`n        _settings = settings;"
    }
    
    # Fix Settings references
    $lines[$i] = $lines[$i] -replace "Solution\.Settings = loadedSolution\.Settings \?\? new Settings\(\);", "// Settings are now in AppSettings"
    $lines[$i] = $lines[$i] -replace "Solution\.Settings\.CurrentIpAddress", "_settings.Z21.CurrentIpAddress"
    $lines[$i] = $lines[$i] -replace "Solution\.Settings\.DefaultPort", "_settings.Z21.DefaultPort"
    $lines[$i] = $lines[$i] -replace "Solution\?\.Settings != null && ", ""
    $lines[$i] = $lines[$i] -replace "Solution\.Settings = new Settings\(\);", "// Settings remain in AppSettings"
    $lines[$i] = $lines[$i] -replace "Solution\.Settings == setting", "false // TODO: Settings refactoring"
}

$lines | Out-File $file -Encoding UTF8 -Force
Write-Host "✅ File updated"
