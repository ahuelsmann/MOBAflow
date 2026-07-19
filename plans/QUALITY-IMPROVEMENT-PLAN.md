# MOBAflow Quality Improvement Plan

## Purpose

This plan translates the repository-wide code-quality audit from July 2026 into a prioritized improvement program. The implementation order is risk-based: physical control and security risks come first, followed by reproducible builds and enforceable quality gates, architectural improvements, and broader product-quality investments.

The initial target is to reduce the six high-severity audit findings to zero. After that, all supported release builds and the analyzer, coverage, mutation, formatting, accessibility, and security gates should remain green.

## Guiding principles

1. Secure system boundaries before performing cosmetic refactoring.
2. Express quality requirements as executable CI gates wherever possible.
3. Refactor UI architecture incrementally instead of performing a big-bang rewrite.
4. Expand tests according to operational risk rather than pursuing a universal coverage percentage.
5. Deliver small, independently verifiable pull requests.
6. Do not mix mechanical formatting changes with functional changes.

## Phase 1: Eliminate immediate risks

Recommended timing: first implementation cycle, before larger feature work.

| Priority | Action | Implementation | Acceptance criteria |
| --- | --- | --- | --- |
| P0 | Make the ESP32 UDP parser length-safe | Process received data with explicit lengths and remove reliance on implicit null termination | Boundary tests and fuzz tests pass; PlatformIO build introduces no new warnings |
| P0 | Protect the provisioning access point | Use a device-specific WPA2 credential, require physical activation, limit the provisioning window, and disable the AP after pairing | An unpaired client cannot read or change the configuration |
| P0 | Authenticate MOBApi control operations | Add device pairing, a rotatable credential, RuntimeHub authorization, input bounds, rate limiting, and security logging | Anonymous control requests receive 401/403; invalid values are rejected |
| P0 | Guarantee Z21 event ordering | Replace one `Task.Run` per event with a bounded FIFO channel and a single consumer | Stress tests prove ordered processing and defined overload behavior |
| P0 | Repair the Android release build | Restrict the global `Platform=x64` mapping to Windows target frameworks and retain `AnyCPU` for Android | A clean restore and Release build produce a valid AAB |

MOBApi authentication requires coordinated changes to MOBAflow and MOBAsmart. Define the pairing protocol, credential rotation, migration behavior, and failure states before implementing the clients.

## Phase 2: Establish enforceable quality gates

### 2.1 Enable analyzers in Release and CI

- Set `RunAnalyzersDuringBuild=true` for Release and CI builds.
- Capture and classify the initial Roslyn diagnostic inventory.
- Correct defects before suppressing diagnostics.
- Keep necessary suppressions local and document their justification.
- Reject new unapproved CA and IDE diagnostics in CI.

Acceptance criteria:

- Release builds execute the configured Roslyn analyzers.
- New analyzer warnings fail CI.
- Every retained suppression has a narrow scope and a documented reason.

### 2.2 Complete the CI platform matrix

Add mandatory jobs for:

- cross-platform .NET projects;
- Windows and WinUI;
- Android and MAUI Release;
- ESP32 PlatformIO;
- resolved transitive package vulnerability auditing;
- tests and coverage enforcement;
- Domain, Backend, and API mutation testing.

Acceptance criterion: every supported delivery target builds from a clean checkout using a documented command.

### 2.3 Restore the coverage ratchet

Add focused tests for:

- SharedUI until it meets the existing 44 percent line threshold;
- Sound until it meets the existing 64.5 percent line threshold;
- Z21 event ordering and error handling;
- API authentication, authorization, input validation, and rate limiting;
- Android pairing and connection behavior.

Raise thresholds only after a stable baseline exists. Coverage is a regression indicator and must be considered together with assertion quality and mutation results.

### 2.4 Normalize repository formatting

- Create a dedicated mechanical formatting commit.
- Normalize line endings and final-newline behavior according to `.editorconfig`.
- Add `dotnet format --verify-no-changes` to CI after the baseline commit.
- Keep formatting changes separate from functional modifications.

## Phase 3: Improve architecture and maintainability

### 3.1 Extract the track-plan editor behavior

Use the following incremental extraction order:

1. document snapshots and undo/redo;
2. selection and grouping;
3. movement, snapping, and placement;
4. connect and disconnect operations;
5. command state and validation.

Target architecture:

- platform-neutral editor and interaction services;
- ViewModels exposing commands and observable state;
- code-behind limited to pointer and keyboard event adaptation and visual coordinate transformations;
- unit tests that do not require a WinUI runtime.

Each extraction must preserve behavior and be independently testable. Avoid a complete editor rewrite.

### 3.2 Refactor the signal box using the same pattern

- Move property changes into ViewModels or services.
- Translate pointer and drag events into commands.
- Remove direct model mutation from controls.
- Add an architecture test that detects new model mutation in XAML code-behind.

### 3.3 Remove synchronous waiting on asynchronous operations

- Implement `IAsyncDisposable` where shutdown requires asynchronous cleanup.
- Add an asynchronous UI-dispatch API.
- Model shutdown with cancellation and bounded timeouts.
- Remove `.Result`, `.Wait()`, and `.GetAwaiter().GetResult()` from production code.

Acceptance criterion: shutdown and UI-dispatch tests complete without deadlocks, indefinite waits, or unobserved exceptions.

### 3.4 Harden latent path operations

Remove the unused photo-storage abstraction or harden it by:

- normalizing input through `PhotoPathHelper`;
- resolving a canonical full path;
- verifying that the result remains inside the configured storage root;
- adding traversal tests for `..`, rooted paths, and mixed path separators.

## Phase 4: Increase product maturity

### 4.1 Accessibility

- Provide complete keyboard operation for the signal box and track-plan editor.
- Add accessible names and appropriate automation peers.
- Preserve visible focus indicators.
- Validate Narrator behavior, High Contrast, focus order, and text scaling.
- Add Accessibility Insights to the release checklist.

### 4.2 Theming

- Replace general-purpose hardcoded colors with theme resources.
- Retain fixed colors only where required by railway signaling semantics.
- Validate Light, Dark, and High Contrast themes.

### 4.3 Mutation testing

Activate mutation lanes in this order:

1. Backend;
2. API;
3. Common;
4. SharedUI;
5. Sound and display;
6. WinUI and MAUI where technically feasible.

Measure the baseline for each lane before defining a non-decreasing ratchet. Do not copy the Domain threshold blindly to unrelated components.

### 4.4 Performance and operational verification

- Benchmark EventBus throughput and runtime-snapshot serialization.
- Load-test the SignalR hub and its rate limits.
- Measure MAUI startup and binding costs.
- Run firmware endurance tests with packet loss and Wi-Fi reconnection.
- Record structured telemetry for dropped events, authentication failures, queue overload, and failed shutdown operations.

## Cross-cutting definition of done

A change is complete only when:

- all affected Release builds succeed;
- compiler and analyzer warnings are zero or narrowly justified;
- new and changed behavior has automated tests;
- repository coverage thresholds remain satisfied;
- security-sensitive changes include negative tests;
- threading and event-ordering guarantees are documented and tested;
- UI changes are verified with keyboard input and Light, Dark, and High Contrast themes;
- no new feature commands or model mutations are introduced in code-behind;
- interface, threat-model, and operational documentation is updated where applicable.

## Recommended pull-request sequence

1. Make the ESP32 packet parser length-safe and add parser tests.
2. Protect and time-limit Wi-Fi provisioning.
3. Define and implement the MOBApi authentication and validation contract.
4. Update the desktop and mobile clients for pairing.
5. Introduce the ordered Z21 event pipeline.
6. Correct the Android platform mapping and add Android CI.
7. Enable Release analyzers and resolve the initial diagnostic inventory.
8. Close coverage gaps and activate the Backend and API mutation lanes.
9. Extract track-plan behavior in several small pull requests.
10. Refactor the signal box and complete accessibility and theming work.
11. Normalize formatting in a dedicated mechanical pull request.
12. Add performance, hardware, and endurance tests.

## Progress indicators

Track the following indicators on every quality-program review:

| Indicator | Initial objective |
| --- | --- |
| Open high-severity audit findings | Reduce from 6 to 0 |
| Supported Release builds | All green from a clean checkout |
| Analyzer gate | Enabled and green for Release and CI |
| Coverage ratchet | All configured project thresholds satisfied |
| Mutation testing | Domain retained; Backend and API activated first |
| Known vulnerable packages | 0 in resolved transitive graphs |
| Production sync-over-async calls | 0 |
| New feature logic in code-behind | 0 |
| Accessibility release checks | Completed for every affected UI workflow |

Review this plan after every phase. Completed actions should be converted into permanent CI checks, architecture tests, or release-checklist items so the improvement cannot silently regress.
