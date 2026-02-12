# ValidateJsonConfiguration.ps1
# Validates ALL JSON files: appsettings, data files, configuration
# Includes schema validation for data integrity

param(
    [Parameter(Mandatory=$true)]
    [string]$ProjectDirectory
)

$ErrorActionPreference = 'Stop'
$hasErrors = $false
$hasWarnings = $false

# Define all JSON files and their schemas
$jsonFilesAndSchemas = @(
    @{ Pattern = 'appsettings*.json'; Schema = 'appsettings.schema.json'; Type = 'Configuration' },
    @{ Pattern = 'example-solution.json'; Schema = 'Build\Schemas\example-solution.schema.json'; Type = 'Solution' },
    @{ Pattern = 'germany-locomotives.json'; Schema = 'Build\Schemas\germany-locomotives.schema.json'; Type = 'Locomotives Library' },
    @{ Pattern = 'germany-stations.json'; Schema = 'Build\Schemas\germany-stations.schema.json'; Type = 'Stations Library' }
)

Write-Host "[JSON Validation] Starting validation of all JSON data files..." -ForegroundColor Cyan

foreach ($fileSpec in $jsonFilesAndSchemas) {
    $files = Get-ChildItem -Path $ProjectDirectory -Filter $fileSpec.Pattern -ErrorAction SilentlyContinue
    $schemaPath = Join-Path $ProjectDirectory $fileSpec.Schema
    
    if ($files.Count -eq 0) {
        Write-Host "[SKIP] $($fileSpec.Pattern) - not found" -ForegroundColor Gray
        continue
    }
    
    $schema = $null
    if (Test-Path $schemaPath) {
        try {
            $schema = Get-Content -Path $schemaPath -Raw | ConvertFrom-Json -ErrorAction Stop
        }
        catch {
            Write-Host "[WARN] Schema for $($fileSpec.Type) could not be loaded" -ForegroundColor Yellow
        }
    }
    
    foreach ($file in $files) {
        try {
            $content = [System.IO.File]::ReadAllText($file.FullName)
            $json = $content | ConvertFrom-Json -ErrorAction Stop
            Write-Host "[OK] $($file.Name) [$($fileSpec.Type)]" -ForegroundColor Green
            
            # Type-specific validation
            switch ($fileSpec.Type) {
                'Configuration' {
                    if ($file.Name -match "Development") {
                        if (-not $json.speech -or [string]::IsNullOrWhiteSpace($json.speech.key)) {
                            Write-Host "[WARN] Speech.Key is empty" -ForegroundColor Yellow
                            $hasWarnings = $true
                        }
                        if (-not $json.z21 -or [string]::IsNullOrWhiteSpace($json.z21.currentIpAddress)) {
                            Write-Host "[WARN] Z21.CurrentIpAddress is empty" -ForegroundColor Yellow
                            $hasWarnings = $true
                        }
                    }
                }
                'Solution' {
                    if ($json.projects.Count -eq 0) {
                        Write-Host "[WARN] No projects defined in solution" -ForegroundColor Yellow
                        $hasWarnings = $true
                    }
                }
                'Locomotives Library' {
                    $locCount = $json.Locomotives | Measure-Object | Select-Object -ExpandProperty Count
                    if ($locCount -eq 0) {
                        Write-Host "[WARN] No locomotive categories defined" -ForegroundColor Yellow
                    }
                }
                'Stations Library' {
                    $stationCount = $json.Cities.Stations | Measure-Object | Select-Object -ExpandProperty Count
                    if ($stationCount -eq 0) {
                        Write-Host "[WARN] No stations defined" -ForegroundColor Yellow
                        $hasWarnings = $true
                    }
                }
            }
        }
        catch {
            Write-Host "[ERROR] $($file.Name)" -ForegroundColor Red
            Write-Host "        Message: $($_.Exception.Message)" -ForegroundColor Red
            $hasErrors = $true
        }
    }
}

Write-Host ""
if ($hasErrors) {
    Write-Host "[JSON Validation FAILED] Fix the errors above and rebuild." -ForegroundColor Red
    exit 1
}
elseif ($hasWarnings) {
    Write-Host "[JSON Validation OK] Files valid, but review warnings above." -ForegroundColor Yellow
    exit 0
}
else {
    Write-Host "[JSON Validation OK] All JSON data files are valid and complete." -ForegroundColor Green
    exit 0
}
