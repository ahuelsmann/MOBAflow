# Desktop UIA smoke and accessibility check

`scripts/Invoke-DesktopUiaSmoke.ps1` starts MOBAflow offline and checks it through Windows UI Automation (UIA),
the same interface that Narrator and other screen readers use. It never clicks pixel coordinates; it only uses
UIA patterns (`SelectionItemPattern`, `InvokePattern`, `TogglePattern`, `WindowPattern`).

## Run it

```text
pwsh -NoProfile -File ./scripts/Invoke-DesktopUiaSmoke.ps1
```

| Parameter | Effect |
| --- | --- |
| `-BuildConfiguration Debug\|Release` | Configuration to build (default `Debug`). |
| `-NoBuild` | Reuse the existing build output. |
| `-DryRun` | Check the guards and the offline configuration only; build and launch nothing. |
| `-OutputJson <path>` | Write the structured report (checks and known accessibility gaps). |
| `-DumpTree` | Print the UIA tree of the main window to diagnose a failing lookup. |
| `-TimeoutSeconds <n>` | Wait limit for the window and each page (default 60). |

The script exits `0` when every check passes, `1` when a check fails and `2` when it refuses to start.
It launches the desktop app, so run it only on a Windows machine where that is allowed. It needs an interactive
desktop session.

## Safety and isolation

- It refuses to start while any `MOBAflow` process runs (`-DryRun` is exempt).
- It builds with `-p:BuildMOBApiDependency=false -p:CopyMOBApiToOutput=false` and copies the output into a
  temporary folder. Your build output, `appsettings.json`, `solution.json` and user data stay untouched.
- The staged `appsettings.json` sets `Application.AutoStartWebApp=false`, clears the Display ESP32 and REST IP
  addresses, turns the health check off and sets `Application.LastSolutionPath` to a sanitized copy of
  `MOBAflow/solution.json`.
- MOBAflow has no `Z21.AutoConnect` setting. It connects only to the Z21 stored in the project, so the sanitized
  solution sets every project's `z21` to `ipAddress = ""`, `serialNumber = 0`. The runtime then reports
  "No Z21 assigned" and opens no socket.
- It never toggles Track Power and sends no Z21 or ESP32 command.
- A `try/finally` always closes the window with `WindowPattern.Close()`, falls back to `Stop-Process` after 15 s
  and deletes the temporary folder.

## What it checks

1. Main window `MOBAflow` appears.
2. Navigation items (`Microsoft.UI.Xaml.Controls.NavigationViewItem`) exist and **none is unnamed**.
3. Selecting `Signal Box`, `Track Plan` and `Train Control` shows a page marker
   (`Delete`, `Track plan toolbar`, `Emergency Stop` automation names).
4. On each page, focusable buttons, check boxes, combo boxes and sliders have an accessible name.
5. The `Dark theme` toggle switches Dark -> Light -> Dark and the app keeps running.
6. The app closes cleanly and leaves `0` MOBAflow processes.

## Known accessibility gaps

Unnamed controls that exist in the product today are listed in `$knownAccessibilityGaps` at the top of the script.
They print as `[WARN]` and appear under `KnownAccessibilityGaps` in the JSON report, and they do not fail the run.
Any other unnamed control fails it, so new gaps cannot slip in. Remove an entry when the product fix lands.

Current entries: the diagnostics `Expander` in `MainWindow.xaml`, the speed steps `ComboBox` on Train Control and
the `FunctionToggleControl` buttons without a label.

## Troubleshooting

- `Main window did not appear`: a locked screen or a non-interactive session blocks UIA. Run it from a
  normal logged-in desktop session.
- A lookup fails after a UI change: run with `-DumpTree` and adjust the marker names in the script.
- Names such as the back button follow the Windows display language; the script only relies on app-defined names.
