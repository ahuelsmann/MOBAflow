# Localization coverage

Source issue: [#140](https://github.com/ahuelsmann/MOBAflow/issues/140).
Baseline: `7c1d92422c0cbf675fddc03a8979990957733d15` (2026-10-14).
Integration base: `877cd41eec29917ac2bba2318f7205391bd9358a` (2026-10-15).
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
- Resource-tool tests: 14 isolated cases passed, including missing/duplicate/empty keys,
  malformed/different placeholders, unknown references, stale projection and unlocalized/stale XAML defaults.
- Desktop FastDebug compilation passed with zero warnings/errors. Normal Windows Release
  test graph passed, including API dependency build/copy, XAML and PRI generation.
- Portable analyzer baseline matches 3346 diagnostics in 1156 groups; Windows baseline
  matches 2493 diagnostics in 953 groups. Existing baselines were not expanded.
- Full portable suite: 2712 passed, no failures. TRX records two NotExecuted results:
  `ResolvePhotoBaseDirectory_WhenBundledPhotosExist_ReturnsAppBaseDirectory` and
  explicitly excluded `SustainedRefresh_MeetsAcceptanceThresholds` (runner summary counts one skip).
- Full Windows suite: 2830 passed, no failures; TRX records the explicitly excluded
  `SustainedRefresh_MeetsAcceptanceThresholds` as NotExecuted. Latest focused Windows
  language/settings/DI rerun: 64 passed, including both IO-error and access-denied retry cases.
- Android FastDebug compilation passed serially (`-m:1`), 45 XC0025 warnings in existing
  controls/pages, zero errors. No new LanguageSettingsView warning. This is not an AAB or device check.
- `MOBAflow.pri` was dumped using the existing SDK MakePri tool; LanguageTitle and InfoTagline
  contain both EN-US and DE-DE candidates. The German SharedUI satellite DLL is in desktop output.
- Instruction consistency, Spec Kit governance, text endings and changed-file secrets checks passed locally.
- After integrating main's speech-cache fix, 70 portable cache/settings tests passed with no skips.

The initial local SDK workload-manifest failure was no longer present on the later Android
restore/build; this task performed no machine-wide SDK/workload repair or installation.
Parallel local build attempts had output-lock/debug-symbol collisions; serial builds passed.

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
- Android Release AAB was explicitly skipped because the PR remains draft. Local Android
  FastDebug compilation above does not replace the Release/AAB or device checks.
- No review threads existed at this snapshot. Earlier superseded Windows/Common runs were
  cancelled by subsequent pushes and are not counted as passing evidence.

This snapshot completes publication evidence T024/T027. Subsequent commits, including evidence
updates, require fresh current-head CI/Sonar verification before handoff; retain the exact snapshot
above rather than attributing its results to a later commit.

A passing local build or quality gate alone does not establish PR readiness. Require green
current-head checks and zero OPEN/CONFIRMED PR issues. Keep the PR draft; no merge is authorized.
