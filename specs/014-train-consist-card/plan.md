# Implementation Plan: Train consist card

**Branch**: `codex/issue-321-train-card-planning` | **Date**: 2026-10-17 | **Spec**: [spec.md](spec.md)

**GitHub Issue**: https://github.com/ahuelsmann/MOBAflow/issues/321

**Spec Kit**: Required

**Status**: Draft

**Input**: Feature specification in `specs/014-train-consist-card/spec.md`

## Summary

Create one immutable read-only card from the selected edited project/train, preserve each occurrence and positional diagnostic, and use that same card for a native Windows reading dialog and A5 print preparation. This PR delivers planning documents only. Every implementation task remains unchecked.

## Technical Context

**Language/Version**: .NET 10 / C#; `global.json` pins SDK 10.0.402 with latestPatch roll-forward.

**Primary Dependencies**: Existing Domain/SharedUI, CommunityToolkit.Mvvm, Microsoft.WindowsAppSDK 2.5.1 and Windows printing APIs. No new packages, tools, services or paid dependencies.

**Storage/Protocols**: In-memory copied presentation data; native user-initiated print job. No new storage/protocol.

**Testing**: NUnit/Moq, strict session/print fakes, pure reference/pagination checks, Windows compile, separately authorized actual UI and print acceptance.

**Target Platforms**: MOBAflow WinUI desktop, `net10.0-windows10.0.22621.0`; SharedUI stays `net10.0`.

**Affected Layers**: SharedUI and MOBAflow only, plus Test and documentation in a later implementation. Domain/Backend/contracts/runtime owners are read-only dependencies.

**Performance Goals**: Linear occurrence traversal after indexing each typed library; no network or disk I/O during generation. Pagination must always consume a fragment or return a concrete layout failure. No invented timing SLA; measure responsiveness during actual enlarged-text/multipage acceptance.

**Data and API Effects**: No JSON/schema/default/layout, REST/SignalR, Z21 or runtime ownership changes. No compatibility or migration layer; no domain model is superseded.

**Scale/Scope**: Selected existing train, arbitrary ordered occurrences and multipage output; synthetic 7-position, 60-position and overheight cases. No editor duplicate-policy change, optional photos, export feature or new Messages category.

**Evidence baseline**: Static source inspection at `b9bf9d725f645df1f0c517faa5e9c4c56fb61cc7`; official printing documentation consulted on 2026-10-17. No app, prototype, build, product tests or print job executed in planning.

## Constitution Check

The design was checked before research and again after design. Checked items below describe planning compliance, not completed product validation.

- [x] Layer/platform boundaries are preserved; card components need no runtime interface or transport client.
- [x] EventBus marshalling remains unchanged. Native print callbacks are separate host boundaries and marshal only host visuals where necessary.
- [x] Printing uses awaited asynchronous APIs; no sync-over-async.
- [x] Feature actions/state live in a dedicated ViewModel; code-behind coordinates dialog, focus and print visuals only.
- [x] UI strings are English; user names are preserved. Screen uses ThemeResource and later Light/Dark checks.
- [x] JSON, defaults, public APIs, Z21 and persisted layouts have no changes.
- [x] Meaningful future regression tests and actual UI/print evidence are explicit; this document-only package uses structural checks.
- [x] Validation commands match the affected targets and do not authorize app/hardware launches.
- [x] Existing session, constructor injection and host registrations are reused; no broad shared-host requirement is added.
- [x] Specification and plan reference #321; artifacts remain in one specs feature directory.
- [x] Every file read is preceded by a successful secrets scan under the stricter current assignment; credentials and MOBAflow/solution.json are excluded.
- [x] Sonar code analysis is CI-only; publication is Draft and Ready belongs to the Chief Project Manager after current checks and zero OPEN/CONFIRMED issues.

The constitution's older `IMobaRuntime.CurrentSnapshot` and broad shared-ViewModel examples are stale against current AGENTS/architecture instructions. No conflict is introduced: this feature uses edited data and no runtime dependency at all. No constitution edit or exception is needed.

## Project Structure

### Documentation (this feature)

```text
specs/014-train-consist-card/
  spec.md
  plan.md
  research.md
  data-model.md
  quickstart.md
  contracts/presentation.md
  checklists/requirements.md
  tasks.md
```

### Planned source ownership

All paths in this section are future implementation targets, not files delivered by this planning PR.

| Path | Responsibility |
| --- | --- |
| `SharedUI/Service/TrainCardBuilder.cs` | Pure edited-model copying, typed lookup and positional diagnostics; includes immutable card/position/location result types |
| `SharedUI/Service/TrainCardPaginator.cs` | Pure pagination of measured text fragments; no WinUI types |
| `SharedUI/Interface/ITrainCardPrintService.cs` | Narrow print request/result for an already generated card |
| `SharedUI/ViewModel/TrainCardViewModel.cs` | Open/print/close commands, immutable current card, honest print status |
| `MOBAflow/View/TrainCardDialog.xaml` and `.xaml.cs` | Read-only wrapped/scrollable screen and focus lifecycle |
| `MOBAflow/Service/TrainCardPrintService.cs` | Window-bound native printing, text measurement, page visuals and callback disposal |
| `MOBAflow/View/TrainsPage.xaml` and `.xaml.cs` | Train-card entry bound to the current selection; show/close dialog through card ViewModel state |
| `MOBAflow/Extensions/MobaWinUiServiceCollectionExtensions.cs` | Windows-only registration of stateless builder, per-view card ViewModel and print adapter |
| `Test/SharedUI/TrainCardBuilderTests.cs`, `TrainCardViewModelTests.cs`, `TrainCardPaginatorTests.cs` | Synthetic regression and boundary checks |

**Structure Decision**: Keep immutable presentation records with the builder, matching the small colocated-query style already used by locomotive usage. Avoid a new shared project, persisted model or universal reporting framework. The existing `TrainsPageViewModel` and its editor/runtime synchronization path need no change: the page binds a separate injected `TrainCardViewModel.OpenCommand` with the selected train as parameter. Windows code-behind only translates the resulting card state into dialog lifetime.

## Phase 0: Research

See [research.md](research.md). Key verified constraints: Train.Vehicles is the order authority; the existing editor rejects new duplicates but existing model occurrences can repeat; lookup must be project-local and typed; locomotive Messages locations are reusable conventions only. The locomotive passport renderer exports HTML and does not prove native A5 pagination.

## Phase 1: Design

### Generation boundary

On Open, the card ViewModel reads `ISolutionSession.SelectedProject` and the supplied selected TrainViewModel, checks actual model membership, captures one `DateTimeOffset` via injected `TimeProvider`, then synchronously copies names, identities, each occurrence and diagnostics on the UI thread. No awaits occur while reading mutable model data. It never rebuilds from `VehicleItems`, since that editor projection is not the occurrence contract.

The stateless builder receives the owning Solution for project membership/identity validation, the selected project, the selected train and creation time. Other projects are inspected only for project-ID ambiguity; their vehicle collections never participate in lookup or card contents. It retains no domain objects, mutable collections, session, runtime or commands in the result. Build all lookups by declared kind and project; zero matches are missing, more than one is ambiguous. No cross-kind/project fallback. Empty/duplicate project or train identities become source diagnostics, not confident Messages links. A stale membership produces unavailable data, never a successful empty result.

### Presentation and DI

Use a dedicated command ViewModel so card actions never call the editor's `SaveChanges`, `SynchronizeSelectedVehicles` or `RefreshProjectRuntimeSnapshot`. Bind "Train card" to the current selected train. It is unavailable without a selection; validate membership again on execution.

A transient card ViewModel and print adapter belong to the Windows view; a stateless builder can be singleton. Reuse or register `TimeProvider.System` in the Windows composition root, with a controllable provider in tests. Do not require a Windows service from Backend or the general SharedUI registration chain. Inspect actual page construction during implementation and adapt only this page's factory/constructor if needed. No service locator.

The dialog shows full names, full record identities, kind, textual "Reversed" orientation, configured locomotive DCC/unconfigured text, provenance, timestamp and diagnostics. No repeat badge is necessary. Close releases the card, returns focus, and retains no history. While open, its copied content remains valid as an observation even if source edits occur elsewhere; no auto-refresh or deferred apply. See [presentation contract](contracts/presentation.md).

### Printing and pagination

Use `Microsoft.UI.Xaml.Printing.PrintDocument`, `PrintManagerInterop.GetForWindow` and awaited `ShowPrintUIForWindowAsync`. Register only the active view/window and unregister on exit; print completion callbacks update host UI through the appropriate dispatcher. Prevent overlapping jobs in this card with a disabled print action while busy.

Request portrait `PrintMediaSize.IsoA5` and expose media size/orientation. A printer may not support the request: use the actual `GetPageDescription` size/imageable area, disclose unsupported A5 and never label another media size as verified A5. No new PDF library, browser launch or HTML export is required.

Use paper-specific black-on-white visuals and at least 11-point body text, scaled upward by the Windows text-size setting (`UISettings.TextScaleFactor`) as the single source of "larger requested text" (no new print size control), without shrinking to fit. Apply the factor exactly once: disable automatic scaling on print visuals (`IsTextScaleFactorEnabled=false`), set the explicit scaled font size, and measure with that same configuration. Measure wrapped text, including long unbroken names, within the imageable width. Generate ordered line fragments carrying occurrence/field/continuation identity, then feed the pure paginator their measured extents and reserved header/footer area. Prefer a whole position when it fits; split an oversized position at line boundaries. A fragment larger than the printable area or nonpositive usable area returns a clear layout failure instead of a blank-page loop. Every successful iteration must consume content. Rebuild page layouts on paper/scale changes from the same immutable card, never by recapturing edited data.

The first page holds the complete train/project/provenance identity as continuable body fragments. Each page repeats only a bounded identifier ("Train card", original timestamp, continuation reference to page 1) and the position association, and pages show x of y after pagination completes. Preview and AddPages must consume the same finalized page collection for that option set; clear obsolete visual pages when repaginating. Header/footer content must wrap too; because the repeated header is bounded, user-authored names never cause the failure path, which remains only for an imageable area too small for that bounded header and one body line. No fixed-height clipping or ellipsis.

## Validation Strategy

### This planning package

Review the full document diff, references/links and command arguments; run Spec Kit prerequisite/template resolution, governance and changed/staged line-ending checks; secrets-scan changed files before every publication. No instruction files or executable tooling change, so no local instruction self-tests, .NET build or product tests are needed. CI remains authoritative for applicable repository checks and Sonar. Conditional build/test skips are skips, not executed feature tests.

### Later implementation, not executed here

- **Automated tests**: Builder fixtures cover provenance/time, order/repeats, all kinds, typed/project boundaries, null/invalid/missing/ambiguous entries, empty versus unavailable data, actual identity and deep-copy behavior. ViewModel tests use strict session/print fakes to verify selection and no save/runtime/command effects. Pagination tests prove full ordered fragment conservation, page counts, breaks, overheight progress and layout failures.
- **Builds**: Run `dotnet build SharedUI/SharedUI.csproj`; Windows `dotnet restore MOBAflow/MOBAflow.csproj` then `dotnet build MOBAflow/MOBAflow.csproj -c FastDebug --no-restore -p:BuildMOBApiDependency=false -p:CopyMOBApiToOutput=false`. FastDebug is compile evidence only.
- **Tests**: `dotnet test Test/Test.csproj -p:TargetFrameworks=net10.0 -f net10.0 --filter "FullyQualifiedName~TrainCard"` after those fixtures exist; require discovered/executed tests. Run existing `TrainsPageViewModelTests` and `LocomotiveTrainReferenceAnalyzerTests` if integration affects those boundaries; CI's relevant full suite and analyzer gates still apply.
- **Manual checks**: Follow [quickstart.md](quickstart.md), only after explicit app-launch/print acceptance assignment. Keyboard/focus, Light/Dark, 200% text, actual Windows callbacks, digital A5 output and physical print are separate evidence.
- **Regression checks**: Strict mocks/model copies prove no train edits, Solution writes, owner start/update or operator commands; card components do not depend on Z21. No Android or layout/hardware certification.
- **Secrets/Sonar**: Changed-file scans; Draft PR with exact head/base, current checks, green Sonar and zero OPEN/CONFIRMED PR issues. Chief Project Manager alone verifies Ready; existing Claude plan then owns independent review/fix/re-review/merge.
- **Traceability**: [tasks.md](tasks.md) maps all FR/SC requirements. No extra task issues or agents are created by this package.

## Documentation and wiki impact

This planning PR adds only feature plans, so existing user-facing availability statements remain accurate and unchanged. The future implementation must update `docs/wiki/MOBAFLOW-USER-GUIDE.md` ("Rolling stock and trains"), `docs/PROJECT-REFERENCE.md` (desktop surfaces/edited data) and `docs/ARCHITECTURE.md` (presentation/runtime and Windows service boundaries). State source, timestamp, reference diagnostics, keyboard flow and print limitations without runtime/physical certification. Reassess after behavior-changing review fixes and verify against actual integrated main. Do not publish an external wiki or announce availability during planning.

## Risks and remaining technical verification

The design is source-backed and uses documented Windows APIs, but no native printing integration was compiled or run. Implementation must verify XamlRoot/HWND ownership, dialog focus behavior, text measurement at 200%, callback threading/disposal, actual driver imageable area and A5 support. Synthetic pure paginator tests cannot prove physical clipping. If those constraints prevent the approved reading/printing behavior, report concrete evidence to the Chief Project Manager/Art Director before changing scope or adding a different rendering dependency.

## Complexity Tracking

No principle violation or approved exception. The two delivery representations require a shared immutable content model and a Windows print adapter; the small pure paginator exists specifically to test mandatory overheight progress and content conservation, not to create a general reporting engine.
