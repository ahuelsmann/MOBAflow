[CmdletBinding()]
param(
    # Repository root to scan. Defaults to the repository that contains this script.
    [string] $Root = (Split-Path -Parent $PSScriptRoot),

    # Also request every http(s) link. External servers fail independently of a change, so pull requests check
    # internal links only and a scheduled workflow checks external links.
    [switch] $IncludeExternal,

    # Lists the unique external links instead of checking anything.
    [switch] $ListExternalLinks,

    [ValidateRange(1, 120)]
    [int] $TimeoutSeconds = 20
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Vendored tool templates contain placeholder links by design.
$excludedPrefixes = @('.specify/', '.agents/', 'skills/')

# Markdown links and images: [text](target) and ![alt](target "title"); reference definitions: [id]: target.
$markdownLinkPattern = '!?\[[^\]]*\]\(\s*<?(?<target>[^)\s>]+)>?(?:\s+"[^"]*")?\s*\)'
$referencePattern = '(?m)^\s{0,3}\[[^\]]+\]:\s*<?(?<target>[^\s>]+)>?'
# HTML attributes in Markdown and in the website.
$htmlAttributePattern = '(?i)\b(?:href|src)\s*=\s*"(?<target>[^"]+)"'
# Markdown autolinks: <https://example.com>.
$autolinkPattern = '<(?<target>https?://[^>\s]+)>'

function Get-DocumentationFiles {
    Push-Location -LiteralPath $Root
    try {
        $files = @(git ls-files -- '*.md' '*.html')
        if ($LASTEXITCODE -ne 0) {
            throw 'git ls-files failed; run the check inside the repository.'
        }
    }
    finally {
        Pop-Location
    }

    return @($files | Where-Object {
        $path = $_
        -not @($excludedPrefixes | Where-Object { $path.StartsWith($_, [StringComparison]::Ordinal) }).Count
    })
}

function Get-LinkTargets([string] $Content) {
    # Code spans and fenced blocks show link syntax as text; they are not links.
    $withoutCode = [regex]::Replace($Content, '(?ms)^\s*(```|~~~).*?^\s*\1', '')
    $withoutCode = [regex]::Replace($withoutCode, '`[^`\r\n]*`', '')
    foreach ($pattern in @($markdownLinkPattern, $referencePattern, $htmlAttributePattern, $autolinkPattern)) {
        foreach ($match in [regex]::Matches($withoutCode, $pattern)) {
            $match.Groups['target'].Value
        }
    }
}

function Test-IsExternal([string] $Target) {
    return $Target -match '^(?i)https?://'
}

function Test-IsIgnored([string] $Target) {
    # Fragments within the page, other schemes and template placeholders are not files.
    return $Target.StartsWith('#') -or
        $Target -match '^(?i)(mailto|tel|data|javascript|vscode|ms-[a-z-]+):' -or
        $Target -match '^\{\{|\$\{|^<'
}

function Resolve-InternalTarget([string] $File, [string] $Target) {
    $pathPart = ($Target -split '[?#]', 2)[0]
    if ([string]::IsNullOrEmpty($pathPart)) {
        return $null
    }

    $pathPart = [Uri]::UnescapeDataString($pathPart)
    $baseDirectory = if ($pathPart.StartsWith('/')) { $Root } else { Split-Path -Parent (Join-Path $Root $File) }
    return [IO.Path]::GetFullPath((Join-Path $baseDirectory $pathPart.TrimStart('/')))
}

function Get-ExternalLinkError([string] $Url) {
    # Retry temporary server/network failures; missing pages and denied requests still fail immediately.
    $maximumAttempts = 3
    for ($attempt = 1; $attempt -le $maximumAttempts; $attempt++) {
        try {
            # Some servers reject HEAD; GET is the portable check.
            $response = Invoke-WebRequest -Uri $Url -Method Get -TimeoutSec $TimeoutSeconds `
                -MaximumRedirection 10 -SkipHttpErrorCheck -UserAgent 'MOBAflow-link-check'
            $status = [int] $response.StatusCode
            if ($status -lt 400 -or $status -eq 429) {
                return $null
            }

            $detail = "returned $status"
            if ($status -notin @(408, 500, 502, 503, 504)) {
                return $detail
            }
        }
        catch {
            $detail = "failed: $($_.Exception.Message)"
        }

        if ($attempt -eq $maximumAttempts) {
            return $detail
        }

        Write-Verbose "External link '$Url' $detail; retrying after $attempt second(s)."
        Start-Sleep -Seconds $attempt
    }
}

$failures = [Collections.Generic.List[string]]::new()
$externalTargets = [Collections.Generic.Dictionary[string, string]]::new([StringComparer]::Ordinal)
$checkedLinks = 0

foreach ($file in Get-DocumentationFiles) {
    $content = [IO.File]::ReadAllText((Join-Path $Root $file))
    foreach ($target in Get-LinkTargets $content) {
        if (Test-IsIgnored $target) {
            continue
        }

        if (Test-IsExternal $target) {
            if (-not $externalTargets.ContainsKey($target)) {
                $externalTargets[$target] = $file
            }

            continue
        }

        $checkedLinks++
        $resolved = Resolve-InternalTarget $file $target
        if ($null -ne $resolved -and -not (Test-Path -LiteralPath $resolved)) {
            $failures.Add("${file}: missing link target '$target'")
        }
    }
}

if ($ListExternalLinks) {
    return @($externalTargets.Keys | Sort-Object)
}

$checkedExternal = 0
if ($IncludeExternal) {
    foreach ($entry in $externalTargets.GetEnumerator()) {
        $checkedExternal++
        $errorDetail = Get-ExternalLinkError $entry.Key
        if ($null -ne $errorDetail) {
            $failures.Add("$($entry.Value): external link '$($entry.Key)' $errorDetail")
        }
    }
}

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Host $_ }
    throw "$($failures.Count) documentation link(s) are broken."
}

$summary = "Documentation links passed: $checkedLinks internal link(s)"
if ($IncludeExternal) {
    $summary += " and $checkedExternal external link(s)"
}

Write-Host "$summary checked."
