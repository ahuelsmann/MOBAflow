# Research: Train consist card

**Source Issue**: https://github.com/ahuelsmann/MOBAflow/issues/321

**Date**: 2026-10-17
**Source baseline**: `b9bf9d725f645df1f0c517faa5e9c4c56fb61cc7`
**Evidence type**: Static source inspection and current primary documentation. No prototype, app, build, product tests, generated pages or print job.

## 1. Edited data and ordering

**Decision**: Copy `ISolutionSession.SelectedProject.Model` and the actually selected train's `Model.Vehicles`, without I/O or runtime access.

**Evidence**: [Train](../../Domain/Train.cs) owns an ordered List of Vehicle; [Vehicle](../../Domain/Vehicle.cs) carries VehicleId, VehicleKind and IsReversed. [Project](../../Domain/Project.cs) has separate Locomotives, PassengerWagons, GoodsWagons and Trains collections. [TrainsPageViewModel](../../SharedUI/ViewModel/TrainsPageViewModel.cs) gets selection from ISolutionSession and invokes save/runtime update only through editor paths. [TrainViewModel](../../SharedUI/ViewModel/TrainViewModel.cs) rejects new same-kind/ID inserts but existing domain lists can contain repeated entries.

**Rationale**: The existing definition is the source; occurrence index, not distinct vehicle ID, defines a row. A repeat fixture can construct a domain train directly without altering editor duplicate policy.

**Alternatives rejected**: Runtime snapshots would misstate edited-data provenance; editor VehicleItems carry commands and are not an immutable card; sorting by a locomotive/wagon Pos field loses mixed list order.

## 2. Typed resolution and identity

**Decision**: Resolve only the declared kind's collection in the containing project, require one match and show full existing GUID plus type. Use name and configured locomotive DigitalAddress as display data, not identity.

**Evidence**: Project separates the three collections; Locomotive and Wagon expose Id/Name and nullable DigitalAddress. Locomotive and Wagon have manufacturer/article metadata, but these values are optional and not guaranteed unique. No physical label identity exists in the inspected model contract.

**Rationale**: A full GUID avoids false short-ID uniqueness; duplicate IDs within a typed collection remain ambiguous rather than choosing FirstOrDefault. The same GUID in another kind/project is not the target.

**Alternatives rejected**: Name/DCC identity, fixed-prefix GUIDs and global library search can fabricate matches or uniqueness. No model-field addition is needed.

## 3. Existing Messages diagnostics

**Decision**: Reuse the location convention, not expand completed #306/#309 or call runtime usage.

**Evidence**: [LocomotiveTrainReferenceAnalyzer](../../Backend/Service/Validation/LocomotiveTrainReferenceAnalyzer.cs) handles only locomotive occurrences and returns unknown coverage on invalid project/train/locomotive identities. [ILocomotiveTrainReferenceAnalyzer and location records](../../Backend/Interface/ILocomotiveTrainReferenceAnalyzer.cs) define zero-based VehicleIndex, one-based VehiclePosition and `vehicles[index].vehicleId`. ProjectDiagnosticsService maps LTU002 missing-locomotive findings to Messages.

**Rationale**: Card rows need every kind and must survive malformed positions. Use the same project/train/index/field coordinates and existing LTU002 association only for a valid unambiguous missing-locomotive location. Do not parse English diagnostic text, invent wagon LTU codes, request owner observations or mutate the global Messages filter.

**Alternatives rejected**: Using locomotive-analysis results as the complete card would drop wagons and may return no positional results on invalid identities. Refactoring or extending that service would reopen excluded scope.

## 4. Reading surface and safety

**Decision**: Dedicated SharedUI TrainCardViewModel and immutable content; WinUI ContentDialog presented from the existing Trains page with command bindings.

**Evidence**: Current TrainsPage has an existing selected-train binding and lifecycle handling. The page's editor paths save definitions and update runtime snapshots; the new readonly path must bypass those paths entirely. Current host registrations compose SharedUI ViewModels in MOBAflow/Extensions/MobaWinUiServiceCollectionExtensions.cs.

**Rationale**: Keep domain copying in a pure builder, commands in the ViewModel and focus/visual lifecycle in the host. No runtime/gateway dependency is needed. Transient card/printing state must not leak into another view or persist.

**Alternatives rejected**: Editing train controls, reusing mutation commands, adding a runtime owner, background refresh, a separate navigation domain or a persistent card history.

## 5. Printing technique

**Decision**: Native WinUI PrintDocument/PrintManagerInterop with an A5 default, actual printer page description, and an application-owned measured pagination routine.

**Repository evidence**: [LocomotivePassportHtmlRenderer](../../Backend/Service/LocomotivePassportHtmlRenderer.cs) produces encoded HTML with print CSS. LocomotiveManagementViewModel.ExportPassportAsync saves it via the existing HTML file picker; this does not establish native page counts, A5 support, keyboard reading-surface behavior or safe overheight continuation.

**Primary sources, consulted 2026-10-17**:

- [Microsoft: Print from your app](https://learn.microsoft.com/en-us/windows/apps/develop/devices-sensors/print-from-your-app): window registration, PrintDocument callbacks, actual page-description layout and unregistering on exit.
- [Microsoft: Customize the print preview UI](https://learn.microsoft.com/en-us/windows/apps/develop/devices-sensors/customize-the-print-preview-ui): displayed options/defaults are constrained by printer support.
- [Microsoft: PrintMediaSize](https://learn.microsoft.com/en-us/uwp/api/windows.graphics.printing.printmediasize): IsoA5 is an available media-size value.

**Rationale**: Uses existing Windows dependencies and preserves in-app reading/focus. Layout uses measured fragments, an 11-point floor and pagination rather than fit-to-page scaling. Source copy is unchanged across repagination. Native callbacks are not EventBus handlers.

**Alternatives rejected**: The existing HTML export alone cannot guarantee the required page association/count and overheight behavior. A browser/WebView reporting surface, external browser launch, PDF package or new general reporting framework adds dependency/surface area before evidence requires it.

**Remaining implementation verification**: Exact SDK interop and driver behavior, imageable margins, font metrics, focus restoration, callback lifetime/threading and physical A5 output. Documentation establishes an API route, not a successfully executed print flow. A5 may be unsupported by a chosen printer; disclose that instead of certifying fallback media.

## 6. Clarification outcome and hook state

The assigned PM/AD handoff already resolved source, flow, card lifetime, paper target and exclusions. No unanswered product question blocks planning. Actual print behavior remains a later verification risk, not an excuse to start a prototype. No .specify/extensions.yml exists at this baseline, so none of the specify/clarify/plan/tasks/analyze pre/post hooks apply. No research agents were started because the assignment explicitly requires this one writer and forbids new workers.
