# RF-23 automated acceptance

**GitHub Issue**: [#191](https://github.com/ahuelsmann/MOBAflow/issues/191)
**Specification**: [spec.md](spec.md)
**Reviewed implementation**: `39312ca93413eef13fa6d7bb8360d5bf5fdda4ae` on `main`
**Evidence date**: 2026-10-11

## Delivery and ownership

| Slice | Integrated pull requests |
| --- | --- |
| Solution session and auto-save | [#192](https://github.com/ahuelsmann/MOBAflow/pull/192), [#193](https://github.com/ahuelsmann/MOBAflow/pull/193) |
| Definitions refreshed after saved edits | [#194](https://github.com/ahuelsmann/MOBAflow/pull/194) |
| Project Z21 assignment and independent runtimes | [#196](https://github.com/ahuelsmann/MOBAflow/pull/196), [#207](https://github.com/ahuelsmann/MOBAflow/pull/207) |
| Safe stop before disposal | [#211](https://github.com/ahuelsmann/MOBAflow/pull/211) |
| Project-addressed MOBApi and MOBAsmart | [#217](https://github.com/ahuelsmann/MOBAflow/pull/217), [#228](https://github.com/ahuelsmann/MOBAflow/pull/228) |
| Shell-dependency acceptance aligned | [#236](https://github.com/ahuelsmann/MOBAflow/pull/236) |

The `main` tree at `39312ca9` equals the integrated #236 tree at `28f928b6`;
#207 adds the already-integrated slice history without a remaining file diff.
There is one editable project model. Runtime threads read a definitions snapshot
refreshed after saved changes, as agreed in the 2026-10-08 clarification. This
snapshot is not a second editable solution.

## Requirement evidence

| Criteria | Evidence |
| --- | --- |
| FR-001 / SC-004: session ownership and named shell exceptions | [SolutionSessionArchitectureTests](../../Test/Architecture/SolutionSessionArchitectureTests.cs), [SolutionSessionTests](../../Test/SharedUI/SolutionSessionTests.cs); #236 aligns the issue, specification, plan and tasks without adding an exception. |
| FR-002, FR-004, FR-005 / SC-001: independent runtimes, conflicts and connection takeover | [ProjectRuntimeHostTests](../../Test/Backend/ProjectRuntimeHostTests.cs): two Z21 fakes, feedback isolation, duplicate/missing endpoint, reload, removal and ownership changes. |
| FR-003: per-project assignment and user-led discovery | [Z21AssignmentViewModelTests](../../Test/SharedUI/Z21AssignmentViewModelTests.cs), [Z21AssignmentDiagnosticsTests](../../Test/Backend/Z21AssignmentDiagnosticsTests.cs). |
| FR-006 / SC-002: stop before disposal and cancellation | [ProjectRuntimeHostTests](../../Test/Backend/ProjectRuntimeHostTests.cs), [MainWindowViewModelShutdownTests](../../Test/SharedUI/MainWindowViewModelShutdownTests.cs), [WinUiShutdownCoordinatorTests](../../Test/MOBAflow/WinUiShutdownCoordinatorTests.cs). |
| FR-007, FR-008 / SC-003: definition updates preserve runtime progress and workflows | [MobaRuntimeServiceProjectIsolationTests](../../Test/Backend/MobaRuntimeServiceProjectIsolationTests.cs). |
| FR-009: selected-project state and commands | [ProjectRuntimeHostTests](../../Test/Backend/ProjectRuntimeHostTests.cs): selected event forwarding and selected-project command routing. |
| FR-010: project-addressed snapshots and validated remote commands | [ProjectRuntimeCommandRouterTests](../../Test/MOBAflow/ProjectRuntimeCommandRouterTests.cs), [RuntimeCommandValidatorTests](../../Test/Common/RuntimeCommandValidatorTests.cs), [RuntimeHubLiveE2ETests](../../Test/Integration/RuntimeHubLiveE2ETests.cs). |
| FR-011: phone selection and its Z21 connection | [MauiViewModelMobaflowOfflineTests](../../Test/SharedUI/MauiViewModelMobaflowOfflineTests.cs), [MobileSolutionContextTests](../../Test/SharedUI/MobileSolutionContextTests.cs), [MobaRuntimeServiceProjectZ21Tests](../../Test/Backend/MobaRuntimeServiceProjectZ21Tests.cs). |
| FR-012: separate feedback stores | Project runtime isolation tests and the per-project `CounterStoreFactory` in [Windows composition](../../MOBAflow/Extensions/MobaWinUiServiceCollectionExtensions.cs); mobile store switching and restart tests in [PersistentInPortCounterTests](../../Test/Backend/PersistentInPortCounterTests.cs) and the per-project store factory in [Android composition](../../MOBAsmart/Extensions/MobaMauiServiceCollectionExtensions.cs). Files are keyed by project id. |
| FR-013 / SC-005: current user documentation | [Desktop guide](../../docs/wiki/MOBAFLOW-USER-GUIDE.md), [installation](../../docs/wiki/INSTALLATION.md), [mobile guide](../../docs/wiki/MOBASMART-USER-GUIDE.md), [architecture](../../docs/ARCHITECTURE.md), [changelog](../../CHANGELOG.md). |
| FR-014: no hardware required by automated acceptance | Tests use fake Z21 connections. MOBApi transport tests start isolated API processes on local ports, without launching MOBAflow or MOBAsmart. |

## Automated validation

- Portable Release full suite: 2,198 passed, 1 skipped, 0 failed (2,199 total).
  The bundled-photo-directory test is skipped because the test output has no
  bundled photos; the explicit sustained-refresh test is outside the ordinary
  suite. Command: `dotnet test Test/Test.csproj -c Release -f net10.0 --no-build --no-restore -p:IncludeMobaSmartTests=false`.
  The full Release rebuild and portable analyzer baseline also pass.
- [Windows Release CI on #236](https://github.com/ahuelsmann/MOBAflow/actions/runs/38153718915):
  2,255 passed, 1 skipped, 0 failed (2,256 total). The log reports the SAPI/audio
  `OutputSpeech_MinimalTest` skip; the explicit sustained-refresh test is outside
  this ordinary suite. Build, coverage ratchet and analyzer gates pass.
- Portable analyzer baseline: 3,575 diagnostics in 1,200 groups.
- Windows analyzer baseline: 2,533 diagnostics in 964 groups.
- Local Android Release full rebuild:
  `dotnet build MOBAsmart/MOBAsmart.csproj -f net10.0-android -c Release --no-restore -m:1 -t:Rebuild -p:MobaAnalyzerGate=true -p:UseSharedCompilation=false`.
  Zero errors; existing analyzer/XAML warnings remain. Its analyzer baseline
  matches 1,804 diagnostics in 614 groups. One stale CA1031 count is reduced
  from four to three; no new/increased diagnostic is accepted.
- The GitHub Android Release AAB job is disabled while MOBAsmart is not distributed.
  Its planned skip is not Android build evidence; the local Release rebuild above
  supplies compile/analyzer evidence. No signed distribution or device deployment
  is claimed.
- #236 CI and SonarCloud pass on `acef5f16760d8201be942025a3462a17ec4fc92a`.
  Fresh OPEN/CONFIRMED issue queries for #236 and #207 return `total: 0`.
- Changed-document link checks, instruction consistency, Spec Kit governance,
  secrets scans, line endings and diff checks are required for the closing PR.
  It must also pass its own current-head CI/Sonar gate before integration.

## Manual and hardware checks

MOBAflow/MOBAsmart were not launched for this acceptance. Light/dark visual
inspection, Android device behavior, two real layouts, real Z21 feedback and
locomotive stop observation are not performed evidence. The specification
requires separate authorization for app starts and hardware actions; these
remain a separately authorized manual follow-up. FR-014 and issue #191 accept
the feature's automated behavior with fakes.

RF-24/#219 remains planning-only until RF-23 documentation, automated acceptance,
review and integration are complete. Named page, layout and shell-state
dependencies remain owned by RF-24; this acceptance starts no RF-24 implementation.
