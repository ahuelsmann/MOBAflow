# Removes redundant blank lines from C# files while preserving intentional spacing.
param(
    [Parameter(Mandatory = $true)]
    [string[]]$Paths,

    [switch]$WhatIf
)

function Test-ShouldKeepBlankLine {
    param(
        [string]$PreviousLine,
        [string]$NextLine
    )

    if ([string]::IsNullOrWhiteSpace($PreviousLine) -or [string]::IsNullOrWhiteSpace($NextLine)) {
        return $false
    }

    $previous = $PreviousLine.Trim()
    $next = $NextLine.Trim()

    if ($next.StartsWith('///')) {
        if ($previous.StartsWith('///')) {
            return $false
        }

        return $true
    }

    if ($previous.StartsWith('namespace ') -and $next.StartsWith('using ')) {
        return $true
    }

    if ($previous.StartsWith('using ') -and $next.StartsWith('///')) {
        return $true
    }

    if ($next.StartsWith('#region') -or $next.StartsWith('#endregion')) {
        return $true
    }

    if ($previous.StartsWith('#region')) {
        return $true
    }

    if ($previous -eq '}') {
        return $true
    }

    if ($previous.StartsWith('using ') -and $next.StartsWith('using ')) {
        return $false
    }

    if ($next.StartsWith('[')) {
        return $false
    }

    if ($previous -eq '{' -or $next -eq '{') {
        return $false
    }

    if ($next -eq '}') {
        return $false
    }

    return $false
}

function Get-NormalizedLines {
    param([string[]]$Lines)

    $collapsed = New-Object System.Collections.Generic.List[string]
    foreach ($line in $Lines) {
        if ([string]::IsNullOrWhiteSpace($line)) {
            if ($collapsed.Count -gt 0 -and [string]::IsNullOrWhiteSpace($collapsed[$collapsed.Count - 1])) {
                continue
            }
        }

        $collapsed.Add($line)
    }

    $normalized = New-Object System.Collections.Generic.List[string]
    for ($index = 0; $index -lt $collapsed.Count; $index++) {
        $line = $collapsed[$index]
        if (-not [string]::IsNullOrWhiteSpace($line)) {
            $normalized.Add($line)
            continue
        }

        $previousLine = $null
        for ($previousIndex = $normalized.Count - 1; $previousIndex -ge 0; $previousIndex--) {
            if (-not [string]::IsNullOrWhiteSpace($normalized[$previousIndex])) {
                $previousLine = $normalized[$previousIndex]
                break
            }
        }

        $nextLine = $null
        for ($nextIndex = $index + 1; $nextIndex -lt $collapsed.Count; $nextIndex++) {
            if (-not [string]::IsNullOrWhiteSpace($collapsed[$nextIndex])) {
                $nextLine = $collapsed[$nextIndex]
                break
            }
        }

        if (Test-ShouldKeepBlankLine -PreviousLine $previousLine -NextLine $nextLine) {
            $normalized.Add($line)
        }
    }

    while ($normalized.Count -gt 0 -and [string]::IsNullOrWhiteSpace($normalized[$normalized.Count - 1])) {
        $normalized.RemoveAt($normalized.Count - 1)
    }

    return ,$normalized.ToArray()
}

foreach ($path in $Paths) {
    $resolvedPaths = if (Test-Path $path -PathType Leaf) {
        ,$path
    }
    else {
        Get-ChildItem -Path $path -Recurse -Filter '*.cs' |
            Where-Object { -not $_.PSIsContainer -and $_.FullName -notmatch '\\(obj|bin|\.vs|\.nuget)\\' } |
            ForEach-Object { $_.FullName }
    }

    foreach ($filePath in $resolvedPaths) {
        $originalLines = Get-Content -Path $filePath
        $normalizedLines = Get-NormalizedLines -Lines $originalLines
        if ($normalizedLines.Count -eq $originalLines.Count -and
            ($normalizedLines -join "`n") -eq ($originalLines -join "`n")) {
            continue
        }

        $relativePath = Resolve-Path -Relative $filePath
        if ($WhatIf) {
            Write-Host "Would normalize $relativePath ($($originalLines.Count) -> $($normalizedLines.Count) lines)"
            continue
        }

        $content = ($normalizedLines -join "`r`n")
        [System.IO.File]::WriteAllText($filePath, $content, [System.Text.UTF8Encoding]::new($false))
        Write-Host "Normalized $relativePath ($($originalLines.Count) -> $($normalizedLines.Count) lines)"
    }
}
