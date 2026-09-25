$ErrorActionPreference = 'Stop'
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../../..'))
if (-not (Test-Path -LiteralPath (Join-Path $repositoryRoot 'AGENTS.md'))) {
    [Console]::Error.WriteLine('Secrets hook: repository root could not be verified.')
    exit 2
}
Set-Location -LiteralPath $repositoryRoot
if (-not (Get-Command sonar -ErrorAction SilentlyContinue)) {
    @{ systemMessage = 'Secrets scan unavailable: install/authenticate Sonar CLI; avoid sensitive files and record the limitation before publication.' } | ConvertTo-Json -Compress
    exit 0
}
$stdinData = [Console]::In.ReadToEnd()
$stdinData | & sonar hook codex-prompt-submit
$scanExitCode = $LASTEXITCODE
if ($scanExitCode -ne 0) {
    [Console]::Error.WriteLine('Secrets hook did not pass. Resolve the scanner finding or failure before proceeding.')
}
exit $scanExitCode
