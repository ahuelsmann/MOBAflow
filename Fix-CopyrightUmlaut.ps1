# Fix-CopyrightUmlaut.ps1
# Fixes the umlaut encoding in copyright headers
# Changes "Hülsmann" or "HÃ¼lsmann" to "Huelsmann"

param(
    [string]$RootPath = "."
)

$projectDirs = @("Backend", "SharedUI", "Test", "WinUI", "WebApp", "MAUI", "Sound", "Common")
$excludeDirs = @("bin", "obj", "TestResults", ".vs", ".git", "packages", "node_modules")

function Fix-CopyrightHeader {
    param([string]$filePath)
    
    try {
        $content = Get-Content $filePath -Raw -Encoding UTF8
        
        # Check if file has copyright with umlaut variants
        if ($content -match "Copyright.*Andreas (Hülsmann|HÃ¼lsmann)") {
            # Replace both umlaut variants with "Huelsmann"
            $newContent = $content -replace "(Copyright \(c\) \d{4}(?:-\d{4})? Andreas )(Hülsmann|HÃ¼lsmann)", '${1}Huelsmann'
            
            [System.IO.File]::WriteAllText($filePath, $newContent, [System.Text.Encoding]::UTF8)
            
            Write-Host "FIXED: $filePath" -ForegroundColor Green
            return $true
        }
        return $false
    }
    catch {
        Write-Host "ERROR: $filePath - $_" -ForegroundColor Red
        return $false
    }
}

Write-Host "Fixing Copyright Headers..." -ForegroundColor Cyan
$fixedCount = 0

foreach ($projectDir in $projectDirs) {
    $fullPath = Join-Path $RootPath $projectDir
    if (-not (Test-Path $fullPath)) { continue }
    
    $csFiles = Get-ChildItem -Path $fullPath -Filter "*.cs" -Recurse | 
        Where-Object { 
            $exclude = $false
            foreach ($excludeDir in $excludeDirs) {
                if ($_.FullName -like "*\$excludeDir\*") { $exclude = $true; break }
            }
            -not $exclude
        }
    
    foreach ($file in $csFiles) {
        if (Fix-CopyrightHeader -filePath $file.FullName) {
            $fixedCount++
        }
    }
}

Write-Host ""
Write-Host "Summary: $fixedCount files fixed" -ForegroundColor Green
Write-Host ""
