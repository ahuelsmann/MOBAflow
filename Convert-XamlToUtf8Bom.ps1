# Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

<#
.SYNOPSIS
    Converts all XAML files in the workspace to UTF-8 with BOM encoding.

.DESCRIPTION
    This script recursively searches for all .xaml files in the current directory
    and its subdirectories, and converts them to UTF-8 with BOM (Byte Order Mark)
    encoding to prevent encoding dialogs in Visual Studio.

.PARAMETER Path
    The root path to search for XAML files. Defaults to current directory.

.PARAMETER WhatIf
    Shows what would be converted without actually converting files.

.EXAMPLE
    .\Convert-XamlToUtf8Bom.ps1
    Converts all XAML files in current directory and subdirectories.

.EXAMPLE
    .\Convert-XamlToUtf8Bom.ps1 -WhatIf
    Shows which files would be converted without making changes.

.EXAMPLE
    .\Convert-XamlToUtf8Bom.ps1 -Path "C:\Repo\ahuelsmann\MOBAflow"
    Converts all XAML files in the specified directory.
#>

[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory = $false)]
    [string]$Path = (Get-Location).Path
)

# UTF-8 with BOM encoding
$encoding = New-Object System.Text.UTF8Encoding $true

# Find all XAML files (excluding .nuget, bin, obj folders)
Write-Host "Searching for XAML files in: $Path" -ForegroundColor Cyan
$xamlFiles = Get-ChildItem -Path $Path -Filter "*.xaml" -Recurse -File | 
    Where-Object { 
        $_.FullName -notlike "*\.nuget\*" -and 
        $_.FullName -notlike "*\bin\*" -and 
        $_.FullName -notlike "*\obj\*" -and
        $_.FullName -notlike "*\packages\*"
    }

if ($xamlFiles.Count -eq 0) {
    Write-Host "No XAML files found." -ForegroundColor Yellow
    exit 0
}

Write-Host "Found $($xamlFiles.Count) XAML file(s)" -ForegroundColor Green
Write-Host ""

$converted = 0
$skipped = 0
$errors = 0

foreach ($file in $xamlFiles) {
    try {
        # Read the file to check current encoding
        $content = Get-Content -Path $file.FullName -Raw -Encoding UTF8
        
        # Check if file already has UTF-8 BOM
        $bytes = [System.IO.File]::ReadAllBytes($file.FullName)
        $hasBom = $bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF
        
        if ($hasBom) {
            Write-Host "[SKIP] $($file.FullName) (already UTF-8 with BOM)" -ForegroundColor Gray
            $skipped++
        }
        else {
            if ($PSCmdlet.ShouldProcess($file.FullName, "Convert to UTF-8 with BOM")) {
                # Convert to UTF-8 with BOM
                [System.IO.File]::WriteAllText($file.FullName, $content, $encoding)
                Write-Host "[OK] $($file.FullName)" -ForegroundColor Green
                $converted++
            }
            else {
                Write-Host "[WHATIF] Would convert: $($file.FullName)" -ForegroundColor Yellow
                $converted++
            }
        }
    }
    catch {
        Write-Host "[ERROR] $($file.FullName): $($_.Exception.Message)" -ForegroundColor Red
        $errors++
    }
}

Write-Host ""
Write-Host "Summary:" -ForegroundColor Cyan
Write-Host "  Converted: $converted" -ForegroundColor Green
Write-Host "  Skipped:   $skipped" -ForegroundColor Gray
Write-Host "  Errors:    $errors" -ForegroundColor $(if ($errors -gt 0) { "Red" } else { "Gray" })

if ($WhatIfPreference) {
    Write-Host ""
    Write-Host "This was a dry run. No files were modified." -ForegroundColor Yellow
    Write-Host "Run without -WhatIf to actually convert the files." -ForegroundColor Yellow
}
