# MOBAflow Refactoring Execution Plan

## Document status

- Status: Proposed
- Baseline: `main` at `a4b9b77ca42901d01e9b1cd01cbcd56be32b1bc2`
- Audit date: 2026-07-20
- Strategic source: [Quality Improvement Plan](QUALITY-IMPROVEMENT-PLAN.md)
- Tracking source after approval: GitHub issues and pull requests

## Purpose

This document turns the repository-wide quality findings into small, ordered, and independently verifiable work packages. It supplements the strategic quality plan with concrete delivery boundaries, dependencies, tests, acceptance criteria, and pull-request sequencing.

The plan covers security hardening, event-ordering guarantees, release-build reliability, enforceable quality gates, and incremental decomposition of the largest UI and ViewModel components. It does not authorize a big-bang rewrite or combine unrelated mechanical and functional changes.

## Verified baseline

The following statements were verified against the baseline commit:

- Five immediate P0 items remain open: ESP32 packet parsing, provisioning security, MOBApi control authentication, Z21 event ordering, and the Android Release build.
- Production code contains no known `.Result`, `.Wait()`, or `.GetAwaiter().GetResult()` calls. The completed sync-over-async cleanup must remain protected by tests and review rules.
- The Android Release build fails with `NETSDK1047` because the Windows-wide `Platform=x64` mapping also affects `net10.0-android`.
- Release builds set strict analysis properties, but `RunAnalyzersDuringBuild=false` prevents the intended analyzer gate from running.
- The current GitHub Actions workflow covers Windows desktop build, tests, coverage, dependency auditing, and Domain mutation testing. Android, ESP32, formatting, Backend mutation, and API mutation are not mandatory gates.
- The resolved `Test` project dependency graph has no currently reported vulnerable NuGet packages.
- The largest current maintainability hotspots include `TrainControlViewModel`, `TrackPlanPage`, `MauiViewModel`, and `SignalBoxPropertiesControl`.

Line counts and warning inventories are diagnostic snapshots, not permanent targets. Architectural acceptance criteria are based on responsibilities and testability rather than file size alone.

## Delivery rules

1. Secure externally reachable and hardware-control boundaries before broad architecture work.
2. Preserve behavior with characterization tests before extracting logic.
3. Keep each pull request independently buildable and reversible.
4. Do not mix formatting-only changes with behavioral changes.
5. Add a permanent CI, architecture-test, or release-checklist guard for every completed systemic fix.
6. Use Conventional Commits for all commits.
7. Run the narrowest relevant tests during development and the complete affected test graph before merge.
8. Record design decisions for authentication, queue overload behavior, and compatibility changes before implementation.

## Work package overview

| ID | Priority | Work package | Depends on | Target outcome |
| --- | --- | --- | --- | --- |
| RF-01 | P0 | Length-safe ESP32 UDP parser | None | Every packet is parsed with explicit bounds |
| RF-02 | P0 | Protected ESP32 provisioning | RF-01 recommended | Provisioning is authenticated, time-limited, and explicitly activated |
| RF-03 | P0 | Authenticated MOBApi control plane | Security design | Only paired and authorized clients can control hardware |
| RF-04 | P0 | Ordered Z21 event pipeline | None | Events are processed FIFO with defined overload behavior |
| RF-05 | P0 | Reproducible Android Release build | None | Clean restore and AAB build succeed locally and in CI |
| RF-06 | P1 | Effective Release analyzer gate | RF-05 recommended | Configured analyzers actually run and new warnings fail CI |
| RF-07 | P1 | Complete CI platform matrix | RF-01, RF-05, RF-06 | All supported delivery targets have mandatory clean-build gates |
| RF-08 | P1 | Compile MAUI XAML bindings | RF-05 | Binding errors are found at build time and runtime reflection is reduced |
| RF-09 | P1 | Coverage and mutation ratchets | RF-03, RF-04, RF-06 | Risk-critical behavior has meaningful regression protection |
| RF-10 | P2 | Formatting baseline and gate | RF-06 | Formatting drift fails CI without obscuring functional diffs |
| RF-11 | P1 | Decompose `TrainControlViewModel` | RF-04 | Independent control-state components with focused tests |
| RF-12 | P1 | Decompose `MauiViewModel` | RF-03, RF-05 | Mobile session, discovery, runtime, and upload concerns are separated |
| RF-13 | P1 | Extract track-plan editor behavior | RF-06 | Platform-neutral editor operations with thin WinUI input adapters |
| RF-14 | P1 | Refactor signal-box property editing | RF-06 | Model changes flow through commands/ViewModels instead of controls |
| RF-15 | P2 | Remove or harden latent legacy services | RF-06 | Unused synchronous and unsafe path abstractions are eliminated |
| RF-16 | P2 | Accessibility and theme completion | RF-13, RF-14 | Critical workflows pass keyboard, Narrator, contrast, and theme checks |
| RF-17 | P2 | Repository instruction cleanup | None | Agent instructions describe the current test and CI architecture |

## Milestone 1: Eliminate immediate boundary risks

### RF-01: Length-safe ESP32 UDP parser

Scope:

- Move packet classification and metadata parsing into functions that accept a pointer/span and an explicit length.
- Remove implicit null-termination assumptions when constructing the host-version string.
- Define behavior for empty, truncated, oversized, malformed, and unknown packets.
- Keep packet handling allocation-conscious for the ESP32 target.

Required verification:

- Unit or host-side parser tests cover lengths around every protocol prefix and buffer boundary.
- Fuzz or property-based tests demonstrate that arbitrary byte sequences cannot read outside the supplied buffer.
- PlatformIO firmware build completes without new warnings.
- Existing frame start, frame data, metadata, and frame completion behavior remains compatible.

### RF-02: Protected ESP32 provisioning

Scope:

- Require explicit physical activation or a narrowly defined first-boot state.
- Generate or derive a device-specific WPA2 provisioning credential.
- Limit the provisioning window and disable the AP after successful pairing or timeout.
- Protect credential read/write endpoints and avoid returning secrets in diagnostics.
- Document credential recovery and factory-reset behavior.

Required verification:

- An unpaired network client cannot read or change configuration.
- Provisioning automatically closes after the configured timeout.
- Reboot, failed connection, factory reset, and credential rotation paths are tested on hardware.
- Logs contain useful security events without credentials or tokens.

### RF-03: Authenticated MOBApi control plane

This package must begin with a short design record covering pairing, credential storage, rotation, revocation, migration, and failure states. Implementation should then be split into server and client pull requests.

Scope:

- Add authentication and authorization to REST and SignalR control operations.
- Distinguish host, remote-control, and read-only capabilities.
- Validate locomotive addresses, speed values, function indices, signal identifiers, enum values, and payload sizes at the boundary.
- Add per-client rate limits and bounded command queues.
- Record authentication failures, rejected values, throttling, and queue overflow as structured security events.
- Update MOBAflow and MOBAsmart clients without embedding credentials in source or logs.

Required verification:

- Anonymous control requests return `401` or `403`.
- A read-only client cannot invoke control methods.
- Expired, rotated, or revoked credentials are rejected.
- Invalid and out-of-range commands never enter the runtime command queue.
- REST and SignalR paths enforce equivalent policies.
- Negative, replay, throttling, and reconnect tests pass.

### RF-04: Ordered Z21 event pipeline

Scope:

- Replace one `Task.Run` per event with a bounded FIFO channel and a single consumer.
- Define capacity, backpressure/drop policy, cancellation, shutdown, and exception behavior.
- Preserve the EventBus UI-thread boundary; ViewModels must not add manual dispatch calls.
- Add metrics for queue depth, rejected/dropped events, handler failures, and shutdown timeouts.

Required verification:

- Stress tests prove FIFO publication for dependent event sequences.
- Queue saturation has a documented, deterministic outcome.
- One failing subscriber does not silently terminate the consumer.
- Shutdown drains or cancels according to the documented policy without deadlock.
- Existing Z21 protocol and EventBus tests remain green.

### RF-05: Reproducible Android Release build

Scope:

- Restrict the global `Platform=x64` mapping to Windows target frameworks.
- Preserve `AnyCPU` for Android and verify runtime identifiers used for AAB packaging.
- Correct the documented restore command; `dotnet restore -f` means `--force`, not framework selection.
- Add a clean Android restore and Release AAB build to CI.

Required verification:

- Delete only project build outputs, then restore from a clean dependency state.
- `dotnet build MOBAsmart/MOBAsmart.csproj -f net10.0-android -c Release --no-restore` succeeds.
- A valid AAB is produced for the intended Android architectures.
- WinUI x64 and cross-platform project outputs remain unchanged.

Milestone exit criterion: RF-01 through RF-05 are merged, their regression tests are mandatory, and no P0 audit finding remains open.

## Milestone 2: Make quality requirements enforceable

### RF-06: Effective Release analyzer gate

Implementation sequence:

1. Run analyzers explicitly and capture the complete diagnostic inventory.
2. Classify findings as defect, refactoring, generated-code issue, or justified suppression.
3. Fix correctness and reliability findings before style findings.
4. Enable `RunAnalyzersDuringBuild=true` for Release and CI.
5. Keep suppressions narrow, documented, and reviewable.

Acceptance criteria:

- Release and CI logs prove analyzer execution.
- New unapproved diagnostics fail the build.
- No repository-wide suppression is added solely to make the gate green.
- MOBAsmart exceptions are removed or reduced to explicitly tracked diagnostics.

### RF-07: Complete CI platform matrix

Add mandatory lanes for:

- cross-platform .NET restore, build, and tests;
- Windows/WinUI build and desktop tests;
- Android/MAUI Release AAB build;
- ESP32 PlatformIO build and parser tests;
- resolved transitive dependency auditing;
- coverage threshold enforcement;
- Domain, Backend, and API mutation testing;
- formatting verification after RF-10.

Acceptance criterion: every supported deliverable can be reproduced from a clean checkout using the same documented command locally and in CI.

### RF-08: Compile MAUI XAML bindings

Scope:

- Enable Source-binding compilation where compatible.
- Add correct `x:DataType` values at each binding-context boundary.
- Correct all resulting binding errors instead of suppressing `XC0025` globally.
- Consider making relevant XAML compilation warnings errors in CI after the baseline is clean.

Acceptance criteria:

- Android Release builds without `XC0025` warnings in project-owned XAML.
- Binding behavior is covered for locomotive selection, signal aspects, function toggles, and control-page state.
- Startup and interaction performance do not regress.

### RF-09: Coverage and mutation ratchets

Order:

1. Z21 ordering and overload behavior;
2. MOBApi authentication, authorization, validation, and throttling;
3. Backend mutation lane;
4. API mutation lane;
5. Common and SharedUI lanes;
6. remaining projects where the tooling is technically suitable.

Acceptance criteria:

- Existing coverage thresholds never decrease without an approved rationale.
- New security and concurrency paths include negative and failure-path assertions.
- Each activated mutation lane records a baseline and adopts a non-decreasing threshold.

### RF-10: Formatting baseline and gate

- Apply formatting in one dedicated mechanical pull request after analyzer fixes stabilize.
- Exclude generated, vendored, and firmware-library code as appropriate.
- Add `dotnet format --verify-no-changes` to CI.
- Do not combine formatting with architecture extraction.

Milestone exit criterion: Release analyzers, the supported-platform build matrix, dependency auditing, coverage, and the first three mutation lanes are mandatory and green.

## Milestone 3: Reduce architectural concentration

Every extraction starts with characterization tests and ends with deletion of the superseded path. Partial classes may be used as a temporary mechanical step, but merely moving methods between partial files does not complete an architectural extraction.

### RF-11: Decompose `TrainControlViewModel`

Extract in this order:

1. locomotive selection and fleet projection;
2. speed conversion, ramping, and command debounce;
3. function-state and appearance management;
4. brake and door state machine;
5. journey/station projection;
6. telemetry aggregation and peaks;
7. host-specific settings persistence.

Target:

- `TrainControlViewModel` coordinates focused platform-neutral collaborators.
- State transitions are independently unit-testable with `TimeProvider` and cancellation.
- WinUI and MAUI host differences remain explicit through options or interfaces.

### RF-12: Decompose `MauiViewModel`

Extract:

- discovery and endpoint selection;
- SignalR session/reconnect lifecycle;
- remote snapshot projection;
- solution synchronization;
- photo capture/upload orchestration;
- application/network lifecycle handling.

Target: the root mobile ViewModel becomes a composition boundary rather than the owner of networking, storage, runtime projection, and UI state transitions.

### RF-13: Extract track-plan editor behavior

Extraction order:

1. snapshots and undo/redo;
2. selection and grouping;
3. movement and coordinate conversion;
4. snapping and placement;
5. connect/disconnect operations;
6. validation and command state;
7. rendering-state preparation.

Keep WinUI code-behind limited to pointer/keyboard adaptation, capture management, and drawing API calls. Domain mutations must flow through testable editor commands.

### RF-14: Refactor signal-box property editing

- Move signal configuration and validation into ViewModels/services.
- Replace direct model mutation in controls with commands.
- Isolate article selection, supported-aspect calculation, multiplexer configuration, and speed-indicator state.
- Add an architecture test that rejects new direct model mutations from XAML code-behind.

### RF-15: Remove or harden latent legacy services

- Remove the unreferenced synchronous `FrameSender`, or migrate all behavior to `IFrameSender.SendFrameAsync` before deleting it.
- Remove the unused abstract photo-storage implementation, or route every path through `PhotoPathHelper` with canonical-root containment checks.
- Add traversal tests for `..`, rooted paths, alternate separators, and encoded edge cases if the abstraction remains.
- Remove stale duplicate utilities only after reference and behavior checks.

Milestone exit criterion: the four main hotspots delegate domain behavior to focused, platform-neutral, tested components; new feature logic and direct model mutation in code-behind are prevented by review or architecture tests.

## Milestone 4: Complete product-quality work

### RF-16: Accessibility and themes

- Inventory all icon-only and custom-drawn controls.
- Add accessible names, help text, roles/patterns, and automation peers where native semantics are insufficient.
- Verify complete keyboard operation, focus order, visible focus, Narrator output, text scaling, and High Contrast.
- Replace general-purpose literal colors with theme resources; retain fixed railway signal colors only where semantically required.
- Add an Accessibility Insights and Light/Dark/High Contrast checklist to release validation.

### RF-17: Repository instruction cleanup

- Replace stale xUnit guidance with the repository's NUnit conventions.
- Remove guidance that permits sync-over-async workarounds.
- Replace stale Azure DevOps and "GitHub Actions planned" statements with the current GitHub workflow model.
- Add a lightweight consistency check or scheduled review for agent instructions and build documentation.

Milestone exit criterion: critical desktop and mobile workflows pass accessibility/theme validation, and repository instructions no longer contradict enforced engineering rules.

## Recommended pull-request sequence

| PR | Contents | Notes |
| --- | --- | --- |
| 1 | RF-01 parser extraction, boundary tests, firmware build | Smallest high-risk fix |
| 2 | RF-02 provisioning threat model and protected activation | Hardware verification required |
| 3 | RF-03 authentication design record | No production behavior change |
| 4 | RF-03 server authentication, authorization, validation, and limits | Include negative tests |
| 5 | RF-03 MOBAflow/MOBAsmart pairing clients and migration | Coordinate compatibility window |
| 6 | RF-04 bounded ordered Z21 event pipeline | Include stress and shutdown tests |
| 7 | RF-05 Android platform mapping and clean-build CI | Keep separate from XAML cleanup |
| 8 | RF-06 analyzer inventory and correctness fixes | Split if diagnostic set is large |
| 9 | RF-06 analyzer enforcement and RF-07 base CI matrix | Gate only after baseline is green |
| 10 | RF-08 compiled MAUI bindings | UI-focused regression tests |
| 11 | RF-09 Backend and API mutation lanes | Establish baseline before threshold |
| 12 | RF-10 mechanical formatting baseline and CI check | No functional changes |
| 13+ | RF-11 through RF-14 incremental architecture slices | One responsibility per PR |
| Final | RF-15 through RF-17 cleanup and product-quality work | May proceed independently when safe |

## Validation matrix

| Change area | Minimum local validation | Mandatory pre-merge validation |
| --- | --- | --- |
| Common, Domain, Backend | affected NUnit fixtures | full `Test/Test.csproj` target-framework matrix |
| Z21 pipeline | ordering, overload, cancellation, shutdown tests | stress tests plus complete Backend tests |
| MOBApi | controller/hub integration and negative security tests | API tests, dependency audit, load/rate-limit checks |
| WinUI | focused ViewModel/service tests and FastDebug build | Windows Release build and desktop test framework |
| MAUI/Android | focused shared/mobile tests | clean restore and Release AAB build |
| ESP32 | host-side parser tests | PlatformIO build and hardware smoke test |
| XAML/accessibility | binding build plus keyboard inspection | Narrator, High Contrast, focus order, and theme checklist |
| Formatting/docs | link and command review | formatting gate and clean Git diff |

## Cross-cutting definition of done

A work package is complete only when:

- its acceptance criteria are demonstrated in the pull request;
- all affected Release builds succeed from a clean restore;
- compiler and analyzer warnings are zero or narrowly justified;
- new and changed behavior has automated tests, including failure paths;
- security-sensitive changes include negative tests and avoid secret-bearing logs;
- cancellation, shutdown, ordering, and overload behavior are documented where relevant;
- existing coverage and mutation thresholds do not regress;
- architecture and operational documentation is updated;
- the superseded implementation is removed rather than left as a parallel path;
- a permanent regression guard is added where practical.

## Tracking and review cadence

1. Create one GitHub issue per RF work package, with sub-issues for RF-03 and architecture slices.
2. Copy the acceptance criteria from this document into the relevant issue.
3. Link every pull request to exactly one primary RF package; list secondary packages explicitly.
4. Review priorities after every milestone or whenever a new security or hardware-safety finding appears.
5. Update this document when scope, dependencies, or acceptance criteria change.
6. Mark a package complete only after its permanent CI/test guard is merged.

Suggested status values are `Proposed`, `Ready`, `In progress`, `Blocked`, and `Complete`. GitHub remains authoritative for day-to-day state; this document remains authoritative for execution order, dependencies, and completion criteria.

## Completion target

The refactoring program is complete when:

- all P0 findings are closed;
- every supported delivery target builds from a clean checkout in CI;
- Release analyzers, dependency auditing, formatting, coverage, and the agreed mutation lanes are enforced;
- hardware-control commands require authenticated and authorized clients;
- Z21 event ordering and overload behavior are deterministic;
- the largest UI components no longer own domain behavior that can be platform-neutral and unit-tested;
- critical workflows pass accessibility and theme validation;
- repository guidance and automated gates describe the same engineering rules.
