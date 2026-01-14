# Terminal (PowerShell) – Hard Rules

## Command Chaining
- NEVER use `&&` (Bash/CMD).
- ALWAYS chain with `;` in PowerShell.
- After native tools (e.g., `dotnet`, `git`, `npm`), prefer:
  `; if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }`.

## Built-ins & Aliases (PowerShell equivalents)
- Prefer PowerShell cmdlets: `Get-ChildItem`, `Select-String`, `Resolve-Path`.
- Translate common Unix tools to PowerShell:
  - `tail -N` → pipeline: `... | Select-Object -Last N`; file: `Get-Content PATH -Tail N`
  - `head -N` → pipeline: `... | Select-Object -First N`; file: `Get-Content PATH -TotalCount N`
  - `grep PATTERN` → `Select-String PATTERN` (use `-SimpleMatch` for literal)
  - `sed 's/a/b/'` → `... | ForEach-Object { $_ -replace 'a','b' }`
  - `awk '{print $1}'` → `... | ForEach-Object { ($_ -split '\s+')[0] }`
  - `wc -l` → `(... | Measure-Object -Line).Lines`

## Paths & Quoting
- Use Windows paths (`\`). Quote spaces: `"C:\Program Files\..."`.
- Avoid backtick escapes in favor of quoting (single `'` or double `"`).

## Exit / Errors
- Scripts **must** return non-zero exit codes on failure: `throw` or `exit 1`.
- Do not swallow errors; use `-ErrorAction Stop` in setup.
- For native tools, rely on `$LASTEXITCODE`. In PS7 set:
  `$PSNativeCommandUseErrorActionPreference = $true` (recommended).

## Output
- Use UTF-8; avoid ANSI art unless explicitly requested.
- Keep output concise for agent parsing; prefer `-v minimal/quiet` switches in CLIs.

## Environment
- Assume PowerShell 7 (`pwsh`) inside Visual Studio Terminal.

**Updated:** 2026-01-14
