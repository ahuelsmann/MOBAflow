# Terminal (PowerShell) – Hard Rules

⚠️ **COPILOT LIMITATION WARNING:**
The terminal execution through Copilot tools currently has significant reliability issues when creating or modifying files, especially XAML files. The tool frequently produces encoding errors, incomplete writes, or syntax failures that require manual recovery.

**DO NOT use terminal for:**
- Creating XAML files (use create_file via Git + manual verification)
- Modifying project files or creating infrastructure
- Complex file operations
- Anything that can be done with standard file tools instead

**DO use terminal for:**
- Checking Git status and running simple Git commands
- Running `dotnet build` or `dotnet test`
- Verifying tool installations

---

- Avoid `... | Select-Object -Last N` with long-running native commands (buffers entire output → appears to hang).
- Prefer:
  - live + tail: `... | Tee-Object file; Get-Content file -Tail N`
  - streaming subset: `... | Select-Object -First N`
- When checking success of native tools (`dotnet`, `git`, `npm`), capture `$LASTEXITCODE` immediately after the native command.

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

## Redirection & Output Streams
- For native commands, redirect error to stdout when piping: `2>&1`.
- Avoid `Out-Host` in automation; prefer returning objects or plain text.

## Paths & Quoting
- Use Windows paths (`\`). Quote spaces: `"C:\Program Files\..."`.
- Prefer quoting (single `'` or double `"`); avoid backtick escapes where possible.

## Exit / Errors
- Scripts **must** return non-zero exit codes on failure: `throw` or `exit 1`.
- Do not swallow errors; use `-ErrorAction Stop` in setup.
- For native tools, rely on `$LASTEXITCODE`. In PS7 set:
  ```powershell
  $PSNativeCommandUseErrorActionPreference = $true
