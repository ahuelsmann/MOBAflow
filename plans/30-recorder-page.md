# RecorderPage Event Journal and Safe Replay Implementation Plan

## Document status

- Status: In progress
- Completed delivery slices: WP1 journal model/format/filtering, WP2 recording state machine/bounded ingestion, WP3 producer capture, WP4 explicit command and Workflow 2.0 lifecycle capture, WP5 RecorderPage/file operations, and WP6 isolated replay
- Next unblocked delivery slice: WP7 interactive WinUI accessibility/theme acceptance evidence
- Resolved dependency: issue #43 merged through PR #45 and is present in the branch baseline
- Primary issue: https://github.com/ahuelsmann/MOBAflow/issues/30
- Status and acceptance criteria source: GitHub issue #30
- Recommended priority: P1 after the P0 boundary work
- Required label before implementation: `plan-required`
- Plan ownership: one-to-one with issue #30
- Resolved ordering dependency: https://github.com/ahuelsmann/MOBAflow/issues/43
- Resolved workflow dependency: issue #32 merged through PR #74 and is present in the branch baseline
- Related isolation consumer: https://github.com/ahuelsmann/MOBAflow/issues/35
- Lifecycle: delete this plan after issue #30 is completed; the closed issue, pull requests, and Git history remain the permanent record

## Purpose

This plan defines the technical design and delivery sequence for the RecorderPage described by GitHub issue #30. The feature records an operating session as a deterministic, immutable event journal and replays imported or captured events only inside an explicitly isolated runtime.

GitHub owns committed scope, priority, status, and acceptance criteria. This plan owns architecture, file impact, dependency gates, sequencing, risks, validation, and rollback. It must not absorb Workflow 2.0, AutomationTestbenchPage, timetable, interlocking, or unrelated RF work.

## Constitution check

| Principle | Design response | Gate |
| --- | --- | --- |
| Architecture and threading boundaries | Recording models, serialization, filtering, capture, and replay remain platform-neutral. The RecorderPage ViewModel marshals the non-EventBus recording status callback through `IUiDispatcher`; EventBus handlers retain the decorator-owned UI boundary. | Pass |
| Async and UI correctness | Channel processing, import/export, and replay are asynchronous and cancellation-aware. Page code-behind remains an input adapter. | Pass |
| Tests are required | Every state transition, ordering rule, serializer branch, mapper, and safety boundary has automated coverage; UI-only behavior has focused manual checks. | Pass |
| Testable and compatible specifications | The recording artifact is separate from `solution.json`; the current format is introduced directly and has explicit compatibility rules. | Pass |
| Simple and traceable changes | Existing EventBus, `TimeProvider`, DI, runtime snapshot, navigation, and file-picker patterns are extended through decorators and narrow interfaces rather than duplicated. | Pass |

No constitutional exception or complexity waiver is required. Re-run this check after the design-contract slice and before implementation begins.

## Current verified foundation

- `IEventBus` remains typed and has no global reflective observer hook. `RecordingEventBusDecorator` now captures an exact mapper allow-list before `UiThreadEventBusDecorator` marshals the original event to UI subscribers.
- Issue #43 is merged; its ordered bounded Z21 FIFO is the producer-order foundation used by recording capture.
- EventBus events inherit `EventBase`, whose `CreatedUtc` value uses `DateTime.UtcNow`; events do not carry a sequence number, correlation ID, source, severity, or stable serialized type key.
- Z21 exposes typed events for connection, power, status, locomotive state, feedback, signal, switch, and health changes. Raw traffic packets are available separately and are not safe recording payloads by default.
- `JourneyManager` now publishes immutable journey feedback, station, completion, restart/activation, stopped, and reset EventBus transitions before its legacy mutable callbacks.
- `WorkflowService` publishes correlated Workflow 2.0 lifecycle events with deterministic source sequence, execution/workflow/step identifiers, mode, result, timing, and sanitized detail.
- Explicit user commands use both `IMobaRuntime` and `IRuntimeCommandGateway`. Runtime snapshots describe resulting state but cannot reliably reconstruct operator intent, correlation, or command failure.
- `MobaRuntimeService.ActivateProjectAsync` already executes against a deep-cloned project. This is useful isolation groundwork but is not a replay boundary because the same runtime still owns live `IZ21` services.
- `TimeProvider.System` is registered in backend DI and can be replaced in tests. The bounded session service exposes ordered immutable journal pages for batched UI projection.
- `NavigationRegistration` now registers RecorderPage after MonitorPage. `IFilePickerService` has recording-specific open/save operations and `IRecordingFileService` owns validated, atomic artifact import/export.
- `RecorderPageViewModel` owns lifecycle commands, annotations, status, filtering, selection, and batched timeline loading; page code-behind only initializes the view.
- `RecordingReplayService` schedules validated entries against a dependency-free `IsolatedReplayRuntime`; a read-only production snapshot gate blocks play, step, and seek while Z21 is connected.

## Scope boundaries

### Included

- named recording sessions with start, pause, resume, stop, markers, and operator notes;
- immutable sequence-bearing event envelopes;
- explicit allow-listed mappers for feedback, journey progress/stop transitions, Workflow 2.0 lifecycle, relevant Z21 state, runtime state, and operator commands;
- deterministic gap records for overload or dropped events;
- live filtered timeline without mutating journal order;
- versioned human-readable import/export independent of solution data;
- isolated replay with play, pause, single-step, seek, speed changes, cancellation, and position reporting;
- a hard safety gate that prevents replay while the production Z21 runtime is connected;
- deterministic tests for ordering, filtering, serialization, import validation, replay timing, cancellation, and side-effect isolation;
- RecorderPage navigation, English UI text, accessibility, and Light/Dark/High Contrast validation.

### Excluded

- recording raw UDP packets, credentials, network endpoints, file contents, audio, scripts, photos, or exception stack traces;
- replaying against `IZ21`, MOBApi, speakers, displays, scripts, the production filesystem, or any other live side-effect implementation;
- replacing the application log or MonitorPage;
- storing recordings inside `solution.json` or changing the solution schema;
- Workflow 2.0 implementation owned by #32;
- the full scenario editor and assertion engine owned by #35;
- comparing recordings, extracting minimal reproductions, or attaching artifacts to GitHub issues;
- legacy recording-format migration or silent compatibility fallback;
- implementing RF-04 or changing its queue policy inside #30.

## Dependency and sequencing gates

| Dependency | Type | Required handling |
| --- | --- | --- |
| Issue #43 / RF-04 ordered Z21 pipeline | Hard for live capture | Core models and serializer may proceed, but producer-order capture and the live RecorderPage must not be accepted until #43 is merged and its FIFO/overload behavior is characterized. |
| Issue #32 Workflow 2.0 lifecycle events | Resolved hard dependency | PR #74 supplies the structured lifecycle contract consumed by the RecorderPage mapper. #30 does not change Workflow 2.0 execution semantics. |
| Issue #35 isolated testbench contracts | Soft coordination | Agree on isolation, virtual-time, and captured-effect interface names before making them public. #30 implements only the minimum replay target; #35 may reuse or extend it later. |
| RF-06/07 analyzer and CI gates | Quality coordination | New code must pass the active analyzer baseline and expose test commands that can become permanent CI lanes. |
| RF-09 coverage/mutation ratchets | Soft consumer | Recorder ordering and safety tests should be suitable for later Backend/Common mutation ratchets. |
| Issues #31, #33, and #34 | Future event producers | Keep stable type keys and mapper registration extensible so later timetable, usage, route, block, and interlocking events can be added without changing format major version. |

### Earliest implementation point

- Plan and format-contract work: immediately after this plan is linked from issue #30 and the issue has `plan-required`.
- Platform-neutral journal, filtering, serialization, and import validation: may start while #43 is in progress.
- Live capture integration: after #43 is merged.
- Workflow coverage: after #32 delivers deterministic lifecycle events.
- Replay public contracts: after a short coordination review with #35; #35 implementation is not a blocker.
- Full issue acceptance: only after all hard gates and hardware-safety tests pass.

## Technical decisions

### 1. Capture before UI marshalling

Register the EventBus as an explicit decorator chain:

```text
producer -> RecordingEventBusDecorator -> UiThreadEventBusDecorator -> EventBus -> ViewModel handlers
```

`RecordingEventBusDecorator` observes only events that have an explicitly registered allow-list mapper, submits their safe projection to the platform-neutral recording service, and then forwards the original event unchanged. It does not expose a global reflective serializer and does not change `IEventBus` public methods.

The recording service must not depend on `IEventBus`; this avoids a DI cycle. RecorderPage state updates are published through a separate narrow `IRecordingStatusSource` callback/observable contract, which the ViewModel receives on the existing UI boundary.

### 2. Explicit command capture

EventBus state events cannot prove operator intent. Decorate `IRuntimeCommandGateway` and inventory direct `IMobaRuntime` command consumers. Each explicit user command records:

- a command-request entry before execution;
- a command-result entry after success;
- a sanitized command-failure entry after failure;
- one correlation ID shared by the request and outcome.

Do not persist exception messages, paths, stack traces, endpoint details, or payload bytes. Where a command path bypasses `IRuntimeCommandGateway`, either route it through a narrow command gateway or add an equivalent platform-neutral decorator; do not put recording behavior in page code-behind.

### 3. Stable allow-listed event mapping

Use `IRecordingEventMapper<TEvent>` implementations registered explicitly in DI. Every mapper declares:

- a stable lowercase dotted type key such as `z21.feedback.received`;
- category, source, and default severity;
- the related stable entity IDs;
- a compact typed payload DTO containing only approved fields;
- whether the event is replay-applicable or display-only.

Mapper registration is the only path into persisted payloads. Reflection-based serialization of arbitrary events, runtime snapshots, logs, or exceptions is prohibited.

Initial mapper inventory:

- Z21 connection, track power, emergency/fault state, and feedback activation;
- journey feedback progress, station/stop transition, reset, completion, and stop;
- Workflow 2.0 start, step/result, failure, cancellation, and completion from #32;
- explicit operator locomotive, function, signal, turnout, track-power, journey-reset, and simulated-feedback commands;
- recording control entries, markers, notes, pause intervals, and gap records.

Raw Z21 traffic and full `MobaRuntimeSnapshot` objects remain excluded. Add narrow mappers for relevant state fields instead.

### 4. Sequence and overload semantics

- Assign a monotonic 64-bit ingest sequence with `Interlocked.Increment` at the recording boundary.
- Preserve producer order from #43. For truly concurrent non-Z21 producers, the atomic ingest sequence defines journal order.
- Use UTC timestamps from injected `TimeProvider`; identical timestamps are ordered by sequence only.
- Feed accepted projections into one bounded channel with a single consumer.
- Default pending capacity: 10,000 entries. Make it configurable through `RecorderOptions`, bounded to a validated safe range.
- Default session limits: 250,000 entries and 64 MiB estimated serialized payload. Stop accepting normal events when either limit is reached and add a terminal limit record.
- Default stop/drain timeout: 5 seconds. A timeout produces a terminal gap/fault record and completes without synchronous blocking.
- Channel saturation never blocks the hardware receive path. Track the first and last dropped ingest sequence plus count, then emit one deterministic gap entry as soon as capacity is available or during stop/drain.
- Pause is intentional omission, not overload. Persist pause and resume control entries with an interval; do not classify events during pause as dropped.
- Stop drains accepted entries with a bounded timeout and emits a final gap/limit entry when required.

Capacity and size defaults must be verified with stress tests before they become release defaults; changing validated defaults is configuration, not a format change.

### 5. Recording artifact v1

Use the suggested extension `.mobarecording.json` and format identifier `mobaflow-recording` with semantic `formatVersion` `1.0`. The first importer accepts exactly `1.0`; a later compatible minor version requires an explicit contract update and tests rather than permissive guessing.

The root artifact contains fixed-order fields:

1. format identifier and version;
2. declared session metadata;
3. source application version and optional project identity (stable ID and display name only);
4. recording options relevant to interpretation;
5. ordered entries;
6. summary counts and declared integrity value.

Each entry contains fixed-order fields for sequence, timestamp, elapsed offset, category, source, type key, severity, correlation/session ID, sorted entity references, and mapper-owned payload.

Serialization rules:

- write JSON with a dedicated canonical writer rather than `Domain.JsonOptions`;
- write properties in a fixed order and sort entity references by kind then ID;
- use invariant culture, UTC ISO-8601 timestamps, and explicit enum strings;
- reject duplicate root fields, invalid sequences, decreasing elapsed offsets, invalid IDs, oversize strings/payloads, and unsupported format major versions;
- enforce JSON depth 32, 128 characters for session names and type keys, 64 characters for source/category/severity keys, 4 KiB for a marker/note, 16 KiB canonical JSON per mapped payload, and the overall entry/byte limits before materializing the complete artifact;
- preserve unknown event types as display-only imported entries if their envelope is valid;
- reject malformed payloads for known types with a precise validation result;
- never execute or dynamically bind a type name from imported JSON;
- store `entriesSha256` as lowercase hexadecimal SHA-256 over canonical entry bytes while excluding declared nondeterministic session metadata.

The artifact is independent of `solution.json`; no solution-schema migration or compatibility adapter is added.

### 6. Recording state machine

The platform-neutral session service uses these states:

```text
Idle -> Recording -> Paused -> Recording -> Stopping -> Completed
  |         |          |                         |
  +---------+----------+-------------------------+-> Faulted
```

- Start requires a trimmed non-empty session name and creates a new immutable session ID.
- Pause and resume are idempotent only when already in their target state; invalid transitions return structured failures.
- Markers and notes are allowed while Recording or Paused and become ordinary ordered entries.
- Stop is single-shot, cancellation-aware, and produces an immutable completed artifact snapshot.
- Import creates a read-only completed session and never changes the current production project.
- Starting a second session while one is active is rejected.

### 7. Structured journey events

Add platform-neutral EventBus events at authoritative `JourneyManager` transitions rather than inferring them from broad runtime snapshots. Events carry stable journey/station IDs, progress counters, run/session identity, and transition kind, but not mutable `JourneySessionState` references.

The runtime publishes these events through the same EventBus chain. Existing .NET events may remain for current internal consumers until their migration is separately justified, but RecorderPage consumes only immutable structured events.

### 8. Isolated replay boundary

Replay never calls `IMobaRuntime`, `IZ21`, `IRuntimeCommandGateway`, speakers, displays, scripts, network clients, or production file services.

Build an explicitly constructed isolated replay graph with:

- a deep-cloned project snapshot where one is available;
- `IReplayRuntime` as the only event-application target;
- an `IReplayEventApplier<TPayload>` allow-list registry;
- a replaceable virtual delay/clock abstraction backed by `TimeProvider`;
- an in-memory captured-effect sink for intended commands and external effects;
- immutable replay state snapshots for RecorderPage display.

The isolated graph is created by a factory that does not receive the production service provider and has no registration for live-effect interfaces. An architecture test verifies this negative dependency boundary.

The first version also hard-blocks replay while `IMobaRuntime.Current` reports an active Z21 connection. The user must disconnect before entering explicit Isolated Replay mode. This provides defense in depth even though the replay graph cannot resolve hardware services.

### 9. Replay timing and seek

- The replay cursor identifies the next unapplied entry by sequence.
- Play schedules future entries from elapsed offsets and the selected speed.
- Supported initial speeds: 0.25x, 0.5x, 1x, 2x, 4x, and 8x.
- A speed change affects only future waits and never changes event order.
- Pause cancels the current virtual wait without applying the next entry.
- Single-step applies exactly one replay-applicable entry; display-only and unknown entries advance the cursor with a visible skipped status.
- Seek resets the isolated runtime and deterministically reapplies entries from the beginning to the target with delays suppressed. Checkpoint optimization is explicitly deferred.
- Cancellation discards the isolated runtime and captured effects, resets position/status, and leaves the production runtime and UI in defined idle state.
- A malformed known replay payload stops replay with a structured error; an unknown display-only type is skipped.

### 10. Filtering and live UI projection

Filtering operates over immutable entries and returns sequence references or view projections; it never sorts or mutates the underlying journal.

Filters combine category, source, entity, severity, free text, and replay applicability. Text search uses invariant case-insensitive matching over precomputed safe display text, not raw JSON.

RecorderPageViewModel owns commands and state. RecorderPage code-behind only forwards lifecycle/input events that cannot be expressed in XAML. The WinUI view uses virtualized/batched rows so the complete session is not copied into multiple `ObservableCollection` instances.

Register RecorderPage under the Monitoring navigation category after MonitorPage. UI labels, empty states, validation, and errors are English and use `ThemeResource` values with non-color-only status indicators.

## Candidate file impact

Exact file names must be revalidated immediately before implementation, but the intended ownership is:

### New platform-neutral recording area

- `Common/Recording/RecordingArtifact.cs`
- `Common/Recording/RecordingEntry.cs`
- `Common/Recording/RecordingSessionMetadata.cs`
- `Common/Recording/RecordingFilter.cs`
- `Common/Recording/RecordingFormat.cs`
- `Common/Recording/RecordingReplay.cs`
- `Common/Recording/RecordingValidationResult.cs`
- `Common/Events/JourneyRuntimeEvents.cs`
- `Backend/Interface/IRecordingReplayService.cs`
- `Backend/Interface/IRecordingSessionService.cs`
- `Backend/Service/Recording/RecordingSessionService.cs`
- `Backend/Service/Recording/RecordingEventBusDecorator.cs`
- `Backend/Service/Recording/RecordingEventMapperRegistry.cs`
- `Backend/Service/Recording/RecordingArtifactSerializer.cs`
- `Backend/Service/Recording/RecordingReplayService.cs`
- `Backend/Service/Recording/IsolatedReplayRuntime.cs`

### Existing platform-neutral integration points

- `Common/Events/IEventBus.cs` only if documentation is needed; do not expand it to untyped reflective subscription
- `Backend/Manager/JourneyManager.cs`
- `Backend/Service/MobaRuntimeService.cs`
- `Backend/Service/MobaRuntimeService.RuntimeApi.cs`
- `Backend/Extensions/MobaBackendServiceCollectionExtensions.cs`
- Workflow lifecycle event files delivered by issue #32

### SharedUI and WinUI

- `SharedUI/Interface/IRuntimeCommandGateway.cs` or narrower command gateway interfaces after command-path inventory
- `SharedUI/Interface/IIoService.cs`
- `SharedUI/Interface/IRecordingFileService.cs`
- `SharedUI/Service/LocalRuntimeCommandGateway.cs`
- `SharedUI/Service/RecordingRuntimeCommandGatewayDecorator.cs`
- `SharedUI/ViewModel/RecorderPageViewModel.cs`
- `MOBAflow/Service/IoService.cs`
- `MOBAflow/Service/NavigationRegistration.cs`
- `MOBAflow/Extensions/MobaWinUiServiceCollectionExtensions.cs`
- `MOBAflow/View/RecorderPage.xaml`
- `MOBAflow/View/RecorderPage.xaml.cs`

### Tests and fixtures

- `Test/Common/RecordingArtifactTests.cs`
- `Test/Common/RecordingFilterTests.cs`
- `Test/Backend/RecordingSessionServiceTests.cs`
- `Test/Backend/RecordingArtifactSerializerTests.cs`
- `Test/Backend/RecordingEventMapperTests.cs`
- `Test/Backend/ReplayEngineTests.cs`
- `Test/Backend/ReplayIsolationArchitectureTests.cs`
- `Test/SharedUI/RecorderPageViewModelTests.cs`
- `Test/MOBAflow/RecorderPageRegistrationTests.cs`
- `Test/TestData/Recording/recording-v1-golden.json`

## Delivery work packages

### WP0: Traceability and dependency readiness

- Add `plan-required` to issue #30 and link this plan from the issue.
- Confirm #43 ownership of source ordering and overload behavior.
- Agree the minimal isolation/virtual-time interface boundary with #35.
- Confirm #32 lifecycle type keys and correlation fields before #30 adds workflow mappers.
- Inventory every user command and event producer in the then-current main branch.
- Re-run the constitution check and record any deviation before code begins.

Exit: the plan is `Ready`, hard dependency contracts are named, and no unresolved architecture question remains.

### WP1: Journal model, canonical format, and filtering

- implement immutable artifact, metadata, entry, entity-reference, and summary models;
- implement canonical v1 writer and defensive importer;
- implement combined filters without journal mutation;
- add golden, round-trip, malformed-input, size-limit, unknown-type, and determinism tests;
- document the payload allow-list and format limits alongside code-level XML documentation.

Exit: the same in-memory artifact serializes byte-for-byte identically, reopens with the same order, and invalid files cannot allocate unbounded memory or create executable types.

### WP2: Recording state machine and bounded ingestion

- implement start, pause, resume, marker, note, stop, drain, fault, and import transitions;
- implement sequence assignment, bounded channel, gap records, session limits, and summaries;
- use injected `TimeProvider` and cancellation throughout;
- add concurrency, identical-timestamp, saturation, stop/drain, limit, cancellation, and invalid-transition tests.

Exit: the platform-neutral service produces a complete deterministic artifact without EventBus or UI integration.

Completed on 2026-07-21. Evidence: the DI-registered platform-neutral session service covers lifecycle and import transitions, atomic sequence assignment, a single-consumer bounded channel, deterministic overload gaps, count/payload limits, bounded stop/drain with terminal cancellation/timeout faults, and immutable artifact completion. Eleven focused tests pass on both `net10.0` and the Windows target; all 30 recording tests and the full `net10.0` suite pass.

### WP3: Producer capture after issue #43

Prerequisite: #43 merged.

- register `RecordingEventBusDecorator` before UI marshalling;
- add initial Z21 and runtime allow-list mappers;
- add immutable structured journey lifecycle events and mappers;
- preserve existing EventBus handler order and exception isolation;
- stress-test end-to-end producer order, capture overload, reconnect, duplicates, and UI dispatch behavior.

Exit: live producer events reach the journal in defined order while ViewModels still receive them on the UI thread.

Completed on 2026-07-21. Evidence: the recording decorator captures an exact mapper allow-list outside the UI-thread decorator while forwarding original events unchanged. The #43 pipeline order, overload gaps, duplicates, reconnect events, mapper/subscriber isolation, UI dispatch order, runtime-field exclusion, and known-schema round trips have focused coverage. `JourneyManager` publishes immutable feedback, station, completion, restart/activation, stopped, and reset transitions before legacy mutable callbacks. All focused recording/journey/DI tests, the full `net10.0` suite, and Windows-target integration tests pass.

### WP4: Explicit user-command and Workflow 2.0 capture

Prerequisite for workflow portion: issue #32 lifecycle events available.

WP4 completed on 2026-07-22. `RecordingRuntimeCommandGateway` wraps the concrete local and mobile command routes and is used by WinUI, MOBAsmart, RuntimeHub, and the REST fallback consumer. Track power, simulated feedback, journey reset, signal aspect, locomotive drive/function, and turnout commands emit correlated allow-listed request/result/failure entries. The completed WP4b mapper consumes the stable `WorkflowLifecycleEvent` contract from #32 without changing execution semantics. It persists only fixed lifecycle enums, correlation/execution/workflow/step identifiers, source sequence, attempt, mode, elapsed ticks, and normalized result values; free-form lifecycle detail is deliberately excluded. `workflow.lifecycle` is validated on import and may project only into the isolated in-memory replay runtime. Focused capture/replay tests pass (19), as do the full platform-neutral suite (1,404 passed plus four environment-dependent skips), Windows suite (1,457 passed), and WinUI FastDebug build (zero warnings/errors). Scoped formatting and changed-file secret scans pass. Local Sonar analysis against refreshed `github/main` found zero secrets; Vortex agentic analysis remains unavailable for the organization with `403 Forbidden`, so publication must remain a draft until remote SonarCloud is green with zero unresolved PR findings.

- route or decorate explicit runtime command paths;
- capture correlated request/result/failure entries with sanitized payloads;
- register Workflow 2.0 lifecycle mappers without changing #32 execution semantics;
- verify that commands from WinUI and supported remote gateways use the same stable command type keys where applicable;
- add negative tests proving secrets, raw payloads, exception details, paths, and network endpoints never enter artifacts.

Exit: the complete event coverage required by issue #30 is represented by stable allow-listed entries.

### WP5: RecorderPage live timeline and file operations

- add recording-specific open/save picker methods and `.mobarecording.json` filters;
- implement RecorderPageViewModel commands, status, filtering, selection, and import/export;
- register the page and DI services;
- implement virtualized timeline, active filters, replay position area, marker/note interaction, empty/error states, and accessible semantics;
- keep all user-visible text English and validate Light, Dark, and High Contrast themes.

Exit: a user can record, annotate, stop, export, import, and inspect the same ordered timeline without replay enabled.

Completed on 2026-07-21. Evidence: RecorderPage is registered under Monitoring after MonitorPage and resolves through production DI. The shared ViewModel owns lifecycle commands, annotations, selection, active filters, errors, and 512-entry journal paging; the virtualized WinUI timeline uses theme resources and accessible command names. Recording-specific pickers produce the `.mobarecording.json` convention, and `RecordingFileService` validates imports and exports atomically. Focused tests cover ViewModel lifecycle/filtering, bounded journal pages, import failures, file round trips, DI, and navigation. The full suite passes on both targets (1,213 cross-platform tests and 1,161 Windows tests, with four environment-dependent skips), the FastDebug WinUI build passes with zero warnings, and scoped format verification passes.

### WP6: Isolated replay engine

- implement the no-live-dependency replay graph and architecture guard;
- implement allow-listed event appliers and captured effects;
- implement play, pause, single-step, speed changes, seek-by-reset/reapply, cancellation, and position reporting;
- enforce the live-Z21 block and explicit Isolated Replay mode;
- verify production runtime snapshots, commands, and external effects remain unchanged/zero.

Exit: replay behavior is deterministic at every supported speed and cannot reach live hardware or external-effect services.

Completed on 2026-07-22. Evidence: `IRecordingReplayService` provides non-blocking play, pause, single-step, absolute seek through reset/reapply, reset/cancel, position reporting, and 0.25x through 8x timing over an injected scheduler. Display-only entries advance as skips, supported entries project only into a fresh in-memory runtime, speed changes affect future waits, and pause/cancel cannot apply the waiting entry. The isolated runtime and factory have no live dependencies and reject non-allow-listed replay types. A read-only safety gate blocks play, step, and seek while Z21 is connected and rechecks after every delay. RecorderPage exposes position, elapsed time, current entry, speed, seek, and accessible controls. The full suite passes on both targets (1,172 platform-neutral tests with four environment-dependent skips and 1,224 Windows tests), the FastDebug WinUI build passes with zero warnings, and scoped format verification passes. Authenticated local Sonar analysis was attempted against the freshly fetched `github/main` base: secret analysis reported zero findings, while Vortex agentic analysis was unavailable for the organization with `403 Forbidden`; this capability limitation must remain in any later draft PR validation section.

### WP7: Integration, resilience, and acceptance evidence

Automated WP7 resilience coverage completed on 2026-07-22. The importer now has a deterministic malformed-mutation corpus covering empty/whitespace input, invalid roots, truncation, invalid UTF-8, and trailing data without unhandled exceptions. RecorderPage drains and filters a 1,200-entry imported timeline across multiple bounded 512-entry UI batches while preserving total order. Architecture and recording-format documentation describe the shipped capture and isolation boundaries. `winapp` 0.3.1 can navigate to RecorderPage and exposes accessible names for session input, recording controls, replay status, progress, and replay commands. Full keyboard, Narrator, text-scaling, and Light/Dark/High Contrast checks remain pending: loose-package registration fails because the FastDebug layout lacks the manifest splash-screen path, while direct launch uses the configured production runtime and auto-connects to Z21. No further interactive or replay action is permitted until an explicitly offline UI-test configuration is available, and no full visual acceptance claim is made without that evidence.

- run full malformed/fuzz-style importer coverage within bounded resource limits;
- run ordering and UI-throughput stress scenarios at configured limits;
- complete WinUI build, navigation, keyboard, Narrator, and theme validation;
- update current architecture and user documentation for the shipped feature and recording format;
- attach validation evidence to issue #30 and remove any superseded temporary path.

Exit: every issue acceptance criterion has automated or explicitly documented manual evidence.

## Test strategy

### Unit tests

- state-machine transitions and idempotency;
- sequence allocation and identical timestamps;
- pause intervals, markers, notes, terminal states, and summaries;
- filter combinations and preservation of underlying order;
- canonical serialization and byte-for-byte determinism;
- format version, duplicate fields, invalid IDs, invalid sequence, malformed known payloads, and unknown event types;
- payload allow-list and sensitive-field exclusion;
- virtual timing, speed changes, pause, step, seek, skip, cancellation, and reset;
- size, count, string, and nesting limits.

### Concurrency and integration tests

- multiple concurrent producers receive one total sequence;
- #43 FIFO event order is preserved through the recording decorator;
- channel saturation yields a deterministic gap record without blocking producer threads;
- handler failures do not terminate capture or later EventBus delivery;
- stop drains or cancels according to policy without deadlock;
- live-Z21 state blocks replay;
- replay DI graph cannot resolve or invoke `IZ21`, runtime command gateways, speakers, displays, scripts, network clients, or production file services;
- import/export through a fake recording file service preserves the complete artifact;
- RecorderPage registration and ViewModel DI validation succeed.

### Validation commands

During platform-neutral slices:

```powershell
dotnet test Test/Test.csproj --filter "FullyQualifiedName~Recording|FullyQualifiedName~Replay"
dotnet test Test/Test.csproj --filter "FullyQualifiedName~EventBus|FullyQualifiedName~JourneyManager|FullyQualifiedName~WorkflowService"
dotnet build Backend/Backend.csproj
dotnet build SharedUI/SharedUI.csproj
dotnet test Test/Test.csproj
```

During WinUI slices:

```powershell
dotnet restore MOBAflow/MOBAflow.csproj
dotnet build MOBAflow/MOBAflow.csproj -c FastDebug --no-restore /p:BuildMOBApiDependency=false /p:CopyMOBApiToOutput=false
```

Final manual validation:

- record, pause, resume, annotate, stop, export, import, filter, replay, seek, step, and cancel;
- verify Light, Dark, and High Contrast states;
- verify keyboard navigation, focus order, accessible names, Narrator output, and text scaling;
- verify replay is rejected while Z21 is connected and succeeds only after entering isolated mode;
- verify no hardware command, sound, display action, script, network call, or production file write occurs during replay.

## Acceptance traceability

| Issue capability | Technical evidence |
| --- | --- |
| Record, annotate, stop, reopen | WP1, WP2, WP5 round-trip and ViewModel tests |
| Stable ordering and non-mutating filters | WP1/WP2 sequence, canonical serializer, and filter tests |
| Adjustable replay, pause, and step | WP6 virtual-time and cursor tests |
| Live-hardware safety boundary | WP6 negative dependency and connected-runtime gate tests |
| Defined cancellation/idle state | WP2 and WP6 state-machine cancellation tests |
| Deterministic export | WP1 golden and byte-for-byte tests |
| Malformed input and unknown types | WP1 defensive import tests |
| Complete required event coverage | WP3/WP4 mapper inventory and correlation tests |

## Risk register

| Risk | Mitigation |
| --- | --- |
| Recorder captures already reordered Z21 events | Block live capture on #43 and run end-to-end FIFO stress tests |
| Recorder work changes the UI-thread boundary | Capture in an outer decorator before `UiThreadEventBusDecorator`; keep ViewModel handlers unchanged |
| Broad reflection leaks sensitive fields | Require explicit typed mappers and negative allow-list tests |
| Snapshots duplicate or obscure causal events | Record narrow authoritative lifecycle events; exclude full snapshots and raw traffic by default |
| User commands bypass the recorded gateway | Complete command-path inventory in WP0 and add architecture/DI tests for decorated paths |
| Replay reaches hardware through an indirect service | Construct an isolated graph without production provider access and enforce negative dependency tests plus a live-connection block |
| Unbounded sessions exhaust memory or freeze UI | Enforce pending, entry-count, byte-size, string, and payload limits; virtualize/batch UI rows |
| Seek is too slow for large journals | Use reset-and-reapply for deterministic v1 behavior; defer checkpoint optimization until evidence justifies it |
| Format becomes coupled to CLR names | Persist stable explicit type keys and mapper DTOs only |
| Workflow contracts change during #32 | Wait for #32 lifecycle contract before workflow mappers; do not create a temporary duplicate |
| #30 and #35 create competing isolation engines | Review public isolation contracts together before WP6 and keep #30 target minimal |
| Dirty or unrelated work is mixed into implementation | Start implementation on a dedicated `codex/issue-30-recorder-page` branch from the then-current approved baseline |

## Rollback strategy

- The recording format is additive and separate from solution data; disabling or removing RecorderPage does not require solution migration.
- Keep RecorderPage navigation and DI registration in the final UI slice so platform-neutral foundations can be reverted independently.
- Decorators forward original events/commands unchanged; if capture is disabled, the underlying EventBus and runtime gateway remain authoritative.
- The isolated replay graph owns no production state and can be discarded on cancellation or rollback.
- Do not delete or replace existing MonitorPage, logs, runtime events, or internal journey events solely for #30.
- Each work package must be independently buildable and reversible; do not combine #43 or #32 implementation into #30 pull requests.

## Pull request sequence

1. Journal models, canonical v1 serializer/importer, filters, and golden tests.
2. Recording session state machine, bounded ingestion, limits, and concurrency tests.
3. EventBus capture decorator, journey lifecycle events, and Z21/runtime mappers after #43.
4. Command capture and Workflow 2.0 mappers after #32.
5. RecorderPage, file picker integration, navigation, and ViewModel tests.
6. Isolated replay graph, virtual timing, safety gates, and negative dependency tests.
7. Resilience, accessibility, documentation, and final acceptance evidence.

## Definition of done

- Issue #30 has `plan-required` and links exactly this plan.
- Every hard dependency gate is satisfied or explicitly demonstrated as no longer applicable.
- The constitution check passes after final design and implementation.
- Recording, serialization, filtering, and replay remain platform-neutral and unit-testable.
- All persisted fields are allow-listed and sensitive-field negative tests pass.
- Replay cannot resolve or invoke live hardware or external-effect services.
- Relevant focused tests, `dotnet test Test/Test.csproj`, Backend/SharedUI builds, and WinUI FastDebug build pass.
- Light, Dark, High Contrast, keyboard, Narrator, and text-scaling checks are documented.
- Current architecture and user documentation describe the shipped behavior and artifact format.
- Acceptance evidence is recorded in GitHub issue #30.
- After the issue is completed and merged, this plan is deleted from `plans/`.
