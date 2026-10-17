# Localization coverage

Source issue: [#140](https://github.com/ahuelsmann/MOBAflow/issues/140).
Baseline: `7c1d92422c0cbf675fddc03a8979990957733d15` (2026-10-14).
Integration base at local review-fix validation: `6888b0d9239bee3b623fbca0f6ae9d71fe7ec185` (2026-10-15).
Rows distinguish implemented scope from still-pending manual acceptance.

| Platform/surface | Files | Scope | Delivery/acceptance |
| --- | --- | --- | --- |
| Desktop navigation | `MOBAflow/Service/NavigationItemFactory.cs`, `NavigationRegistration.cs` | Page titles/tooltips; preserve tags and custom badge suffixes | Slice 1 implemented; manual acceptance pending |
| Desktop UI language | `MOBAflow/View/SettingsPage.xaml`, new language area | Language heading, choices, save/restart/error text and accessible names only | Slice 1 implemented; manual acceptance pending |
| Desktop Info | `MOBAflow/View/InfoPage.xaml`, `.xaml.cs` | All prose, headings, links/accessibility names; version/product/URLs invariant | Slice 1 implemented; manual acceptance pending |
| Desktop Help | `MOBAflow/View/HelpPage.xaml`, `.xaml.cs` | View chrome and contents navigation; embedded/remote article bodies excluded | Slice 1 implemented; manual acceptance pending |
| Android tabs | `MOBAsmart/Controls/AppBottomTabBar.xaml` | Four labels and accessible names; tab IDs/routing invariant | Slice 1 implemented; manual acceptance pending |
| Android UI language | new `MOBAsmart/Controls/LanguageSettingsView.xaml`, hosted by `MOBAsmart/MainPage.xaml` | New self-contained settings area, not Counter behavior | Slice 1 implemented; manual acceptance pending |
| Shared dynamic text | new `SharedUI/Localization/`, new language ViewModel | Language save/restart/error templates; numeric/date formatting contract | Slice 1 implemented; manual acceptance pending |
| Desktop shell body | `MOBAflow/View/MainWindow.xaml`, related status/command partials | Menus, operational statuses, command dialogs | Follow-up in #140 |
| Other desktop Settings | `MOBAflow/View/SettingsPage.xaml`, `.xaml.cs`, `.Layout.cs`, `SharedUI/ViewModel/SettingsPageViewModel*` | Existing connection, display, speech, feature toggles/layout and reset sections | Follow-up in #140; speech coordination with #136 |
| Desktop pages | `MOBAflow/View/OverviewPage.xaml`, `SolutionPage.xaml`, `StationsPage.xaml`, `LocomotivesPage.xaml`, `GoodsWagonPage.xaml`, `PassengerWagonPage.xaml`, `TrainsPage.xaml` | Page bodies, dialogs, templates and corresponding shared ViewModels | Follow-up in #140 |
| Journey/Workflow/Counter | `MOBAflow/View/JourneysPage.xaml`, `WorkflowsPage.xaml`, `TimetablePage.xaml`, `EventManagerPage.xaml`, `JourneyMapPage.xaml`, `SharedUI/ViewModel/MainWindowViewModel.Journey.cs`, `.Workflow.cs`, `.Counter.cs`, `MOBAsmart/MainPage.xaml` | Existing operational texts only; no competing extraction | Follow-up after checking RF-24/#219 file ownership |
| Other desktop tools | `MOBAflow/View/TrainControlPage.xaml`, `TrackPlanPage.xaml`, `SignalBoxPage.xaml`, `MonitorPage.xaml`, `RecorderPage.xaml`, `MatrixPage.xaml`, `DisplayPage.xaml` | Bodies, controls, converters, dialogs and shared status text | Follow-up in #140 |
| Android operational UI | `MOBAsmart/MainPage.xaml`, `View/EnginePage.xaml`, `ControlPage.xaml`, `SignalBoxPage.xaml`, `SplashPage.xaml`, connection/control components and `SharedUI/Mobile/` | Existing body/status/dialog/empty-state text | Follow-up in #140 |

Each included surface must be checked for static text, dynamic text, full templates,
tooltips, accessible names, error/empty/status states, formatting and user-data boundaries.
Only update a row to implemented when resource and build checks exist; record manual acceptance separately.

## Local evidence (2026-10-15)

- SharedUI build passed; 109 source keys, 2 languages and 52 WinUI property targets validated.
- Resource-tool tests: 18 isolated cases passed, including missing/duplicate/empty keys,
  missing context, invalid context argument references, escaped/valid context arguments,
  malformed/different placeholders, unknown references, stale projection and unlocalized/stale XAML defaults.
- Desktop FastDebug compilation passed with zero warnings/errors. Normal Windows Release
  test graph passed, including API dependency build/copy, XAML and PRI generation.
- Portable analyzer baseline matches 3346 diagnostics in 1156 groups; Windows baseline
  matches 2493 diagnostics in 953 groups. Existing baselines were not expanded.
- Full portable suite: 2712 passed, no failures. TRX records two NotExecuted results:
  `ResolvePhotoBaseDirectory_WhenBundledPhotosExist_ReturnsAppBaseDirectory` and
  explicitly excluded `SustainedRefresh_MeetsAcceptanceThresholds` (runner summary counts one skip).
- Full Windows suite before review corrections: 2830 passed, no failures; TRX records the explicitly excluded
  `SustainedRefresh_MeetsAcceptanceThresholds` as NotExecuted. Latest focused Windows
  language/settings/DI rerun before review corrections: 64 passed. Its IO-error/access-denied
  retry cases used a mocked settings service; they did not cover the actual Windows writer.
- Android FastDebug compilation passed serially (`-m:1`), 45 XC0025 warnings in existing
  controls/pages, zero errors. No new LanguageSettingsView warning. This is not an AAB or device check.
- `MOBAflow.pri` was dumped using the existing SDK MakePri tool; LanguageTitle and InfoTagline
  contain both EN-US and DE-DE candidates. The German SharedUI satellite DLL is in desktop output.
- Instruction consistency, Spec Kit governance, text endings and changed-file secrets checks passed locally.
- After integrating main's speech-cache fix, 70 portable cache/settings tests passed with no skips.

The initial local SDK workload-manifest failure was no longer present on the later Android
restore/build; this task performed no machine-wide SDK/workload repair or installation.
Parallel local build attempts had output-lock/debug-symbol collisions; serial builds passed.

## Claude review corrections (2026-10-15)

- P1: The Windows settings writer now propagates write failures to its callers. The existing language
  ViewModel can therefore restore the previous in-memory preference and show its localized retry
  message instead of reporting success. Existing automatic-save callers already observe failures.
- Two new Windows integration cases use the actual SettingsService with an exclusively write-blocked
  or read-only temporary file. Both failed before the fix because the writer swallowed the exception.
  They now verify the original exception, unchanged disk/memory preference, localized error, a later
  unrelated save retaining the previous language, lock release and a successful explicit retry.
- The affected Windows settings/language/DI run passed all 66 tests with zero failures/skips.
  The normal Release test graph also compiled the actual desktop host and its API dependency.
- Full Windows rerun after the fix: 2913 passed, no failures. The explicit performance case
  `SustainedRefresh_MeetsAcceptanceThresholds` remained excluded.
- P3: All 109 entries in each catalogue now describe the actual surface/meaning. Only the Version
  template documents its version argument. Resource keys and English/German values are unchanged.
  The resource guard rejects comment references to absent arguments; four added isolated cases cover
  missing context, copied/unmatched argument notes and valid/escaped context references (18 total).

Consult the PR description for fresh PR-head CI/Sonar results and the central review handover.
The earlier publication snapshot below remains historical evidence, not the result of these fixes.

## Resumed validation (2026-10-17)

The previous three uncommitted fixes were preserved unchanged while fast-forwarding to the published
task branch. The full local rebuild/test input was `81009534c43ac201d0d7fb67df7a78744d6ffe8b`
plus these fixes; the subsequent remote synchronization `edc866467407a8853c0577e8ba81fa9a8ac86667`
adds only the RF-25 plan and does not change the validated source.

- Normal Windows Release test-graph rebuild, including the desktop host and API dependency, passed.
  The complete suite passed 2995 tests with no failures. Both actual file-lock/read-only regression
  cases passed; the explicit `SustainedRefresh_MeetsAcceptanceThresholds` case was not executed.
- The redundant log-and-rethrow catch is removed (S2139). The test's file lock has a deterministic
  `using` lifetime (CA2000); the original IO/access-denied exceptions and retry behavior are preserved.
- Complete, worktree-source-verified Windows SARIF matches 2453 diagnostics in 945 groups. The existing
  baseline changes only SettingsService CA1031 from 3 to 2 and CA1848 from 10 to 9. No diagnostic is
  added, suppressed or excluded; the earlier incomplete incremental report was not used.
- Resource validation still passes 109 keys, two languages and 52 WinUI targets; all 18 isolated guard
  cases pass. English/German values and generated projections are unchanged.

Delivery remains a draft with no Ready/auto-merge/merge or app/hardware launch. The assigned central
owner coordinates the later review handover; this resumed correction dispatches no additional Claude
review. Manual restart, themes, scaling, keyboard/screenreader and Android/device acceptance remain
open. Fresh exact-head CI/Sonar evidence is kept in the PR description rather than attributed to an
earlier commit.

## Reset and concurrent-save corrections (2026-10-17)

The correction input is `7af769d3d8d238cbbda7e218143c459eff0d9a43` plus the changes described here.
Four new regression cases failed before the correction: reset left an explicit/unsaved language
selected, and an in-flight failed language save exposed its candidate to another settings save.

- The settings service now owns the language commit. It serializes a candidate under the host's
  settings-file write lock and updates the shared preference only after successful persistence.
  Both Windows and Android implement this narrow operation; other settings-save callers keep
  their existing interface and observe the last committed UI language.
- A successful reset notifies the language ViewModel even when System was already persisted.
  The picker discards its unsaved choice and clears previous status through IUiDispatcher.
  Disposal removes the subscription; the running UI language remains unchanged.
- The Windows boundary tests pause the actual writer deterministically, queue an unrelated
  automatic save, then inject IO/access-denied failures. They verify the queued file keeps the
  previous language and the unrelated edit, followed by a successful explicit language retry.
  Real reset/reload tests cover both an explicit saved language and an unsaved draft over System.
- Fresh normal Windows Release rebuild, including host/API dependencies, passed. The complete
  Windows suite passed 3047 tests with zero failures; the explicit SustainedRefresh performance
  case was not executed. Complete Windows SARIF matches the unchanged 2454/946 baseline.
- The fresh portable Release rebuild passed; its unchanged analyzer baseline matches 3214/1127.
  The final portable settings fixture passed all 49 cases. Catalogue/tool checks still pass
  109 keys, two languages, 52 WinUI targets and 18 isolated cases.
- Android FastDebug and normal Release rebuilds passed. The signed Release AAB contains the
  required manifest/resources/DEX and exactly arm64-v8a/x86_64 native ABIs. Complete isolated
  Android/dependency SARIF matches 1505 diagnostics in 552 groups after removing only the
  SettingsService CA1869 entry (reused serializer options). No new warning is accepted.
  These are compile/package checks, not emulator/device or manual UI acceptance.

Exact publication-head CI/Sonar results are recorded in the PR description.
No app, debugger, device or hardware is started. Delivery remains Draft without
Ready/auto-merge/merge; the central owner arranges the subsequent independent review.

## Acceptance and publication evidence

No desktop or Android app was launched. Full process restart, system-language startup,
long-text/font scaling, keyboard/focus, screenreader and Light/Dark acceptance remain open
under T022/T026. No hardware or paid service was used. #140 remains open for these checks
and the explicitly listed follow-up page slices.

[Draft PR #294](https://github.com/ahuelsmann/MOBAflow/pull/294) is published. Its
[current checks](https://github.com/ahuelsmann/MOBAflow/pull/294/checks) and
[Sonar PR analysis](https://sonarcloud.io/dashboard?id=ahuelsmann_MOBAflow2&pullRequest=294)
are the authoritative publication evidence; verify the current head SHA before relying on them.

Verified publication snapshot: `2e9035d28920d664b468f74c33b279237d81bc4f`, based on
`877cd41eec29917ac2bba2318f7205391bd9358a` (2026-10-15).

- [Quality run 38733685767](https://github.com/ahuelsmann/MOBAflow/actions/runs/38733685767)
  passed Windows desktop build/tests/coverage (2834 tests passed, zero failures/skips), unchanged
  portable/Windows analyzer baselines, Common and Domain mutation tests, Display protocol/parser/
  firmware and repository instruction/resource checks.
- [Spec Kit run 38733685750](https://github.com/ahuelsmann/MOBAflow/actions/runs/38733685750)
  passed artifact validation. The issue-event-only traceability job was skipped.
- [CodeQL run 38733681290](https://github.com/ahuelsmann/MOBAflow/actions/runs/38733681290)
  passed all five language analyses and its aggregate check.
- SonarCloud check on this exact SHA passed; the quality-gate API returned OK and the PR issue
  query returned zero OPEN/CONFIRMED issues. The initial HelpPage S1192 finding is corrected.
- Android Release AAB was explicitly skipped in that PR run. Local Android
  FastDebug compilation above does not replace the Release/AAB or device checks.
- No review threads existed at this snapshot. Earlier superseded Windows/Common runs were
  cancelled by subsequent pushes and are not counted as passing evidence.

This snapshot completes publication evidence T024/T027. Subsequent commits, including evidence
updates, require fresh current-head CI/Sonar verification before handoff; retain the exact snapshot
above rather than attributing its results to a later commit.

A passing local build or quality gate alone does not establish PR readiness. Require green
current-head checks and zero OPEN/CONFIRMED PR issues. Keep the PR draft; no merge is authorized.
