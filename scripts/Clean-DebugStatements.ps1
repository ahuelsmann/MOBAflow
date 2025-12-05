$ErrorActionPreference='Stop'
Set-Location "C:\Repos\ahuelsmann\MOBAflow"

# Simply remove all non-ASCII characters from Debug.WriteLine statements
$files = Get-ChildItem -Path "WinUI" -Filter "*.cs" -Recurse

foreach ($file in $files) {
    $content = [System.IO.File]::ReadAllText($file.FullName, [System.Text.Encoding]::UTF8)
    
    # Replace any non-ASCII in Debug.WriteLine
    if ($content -match 'Debug\.WriteLine') {
        $lines = $content -split "`n"
        $newLines = @()
        
        foreach ($line in $lines) {
            if ($line -match 'Debug\.WriteLine') {
                # Remove non-ASCII characters (anything > 127)
                $cleanLine = -join ($line.ToCharArray() | ForEach-Object {
                    if ([int]$_ -le 127) { $_ } else { '' }
                })
                $newLines += $cleanLine
            } else {
                $newLines += $line
            }
        }
        
        [System.IO.File]::WriteAllText($file.FullName, ($newLines -join "`n"), [System.Text.Encoding]::UTF8)
        Write-Host "Cleaned: $($file.Name)"
    }
}

Write-Host "Done cleaning Debug statements" -ForegroundColor Green
