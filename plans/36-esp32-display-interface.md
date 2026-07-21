# Issue 36: ESP32 Display Interface Implementation Plan

## Document status

- Status: In progress
- Primary work item: [GitHub issue #36 - Define a common ESP32 SPI display firmware interface](https://github.com/ahuelsmann/MOBAflow/issues/36)
- Recommended priority: P1
- Required label: `plan-required`
- Plan ownership: one-to-one with issue #36
- Implementation baseline: `codex/issue-36-esp32-display-interface` at `68449c60` in `C:\Repo\ahuelsmann\MOBAflow-issue-36`
- Lifecycle: delete this plan after issue #36 is complete; the closed issue, pull requests, and Git history remain the permanent record

## Implementation progress

Status on 2026-07-21:

- WP0 local readiness is complete: the assigned branch and worktree were
  synchronized non-destructively with current `main`; the single protocol-doc
  conflict retained both the normative v1 contract and RF-01 parser guidance.
  All files read or changed for this slice passed the deterministic per-file
  secret scan.
- RF-01 / issue #49 is merged and the authoritative native-test and ESP32-S3
  PlatformIO commands are now documented. RF-02 / issue #48 remains open, so
  firmware decomposition and v1 dispatcher integration remain gated.
- WP1 is complete: the normative v1.0 specification now has exact valid and
  invalid golden payloads for all twelve message types, complete envelope
  fixtures, and a deterministic 5 by 4 conformance-pattern RGB565 vector with
  CRC32 and SHA-256 hashes.
- WP2 now has immutable envelope, version, capability, health, frame, region,
  command, and result models plus strict network-byte-order envelope and payload
  codecs. Validation covers lengths, reserved fields, enums, flags, UTF-8,
  metadata consistency, CRC32, immutable region bytes, and deterministic
  arbitrary-byte input for every message type.
- WP2 remains open for identifier generation, response correlation, fake
  endpoint behavior, bounded retry, cancellation, and transport anomaly tests.
- No firmware, provisioning, UI, configuration, legacy-sender, or RF-owned code
  has been changed in this slice.
- Validation: `dotnet build MOBAdisplay/MOBAdisplay.csproj` completed with zero
  warnings and errors; the full net10.0 suite passed 1,179 tests with 4 existing
  skips and no failures. The local SDK required
  `MSBuildEnableWorkloadResolver=false` for the test process because its
  installed workload set is missing manifests. Optional Sonar agentic analysis
  could not start because no Sonar project is configured for this worktree; the
  mandatory per-file Sonar secrets scans completed successfully.

## Purpose

Define and deliver a reusable, versioned interface between MOBAflow hosts and ESP32 firmware projects that drive SPI displays. The implementation must preserve board-specific configuration inside each firmware project while giving the host enough verified capability information to send only valid commands and frames.

This plan owns only issue #36. It consumes the results of related RF work packages but must not implement or silently absorb their unrelated scope.

## Outcome

When this plan is complete:

- different ESP32 display projects can implement one portable display contract without sharing board pins, controller configuration, or graphics-library internals;
- MOBAflow can connect to an explicitly configured endpoint, negotiate a supported protocol version, read capabilities and health, and send compatible content;
- frame transfer is atomic from the user's perspective: incomplete or invalid frames are rejected and never presented;
- retries are bounded and idempotent, so packet loss or duplicate packets cannot present the same frame twice or corrupt the active frame;
- the current ESP32-S3/ST7789/TFT_eSPI implementation remains functional through a reference adapter;
- a second fake or alternate adapter proves that the firmware contract is not tied to the current board and display;
- protocol version, firmware version, device identity, negotiated capabilities, health, and the last transfer result are visible in diagnostics;
- host and firmware conformance tests cover normal operation, invalid input, cancellation, timeout, packet anomalies, unsupported features, and reboot behavior;
- the legacy synchronous chunk sender is removed after the new contract is proven.

## Current verified foundation

The GitHub baseline establishes the following starting point:

- `MOBAdisplay/Transport/IFrameSender.cs` exposes only `SendFrameAsync` for a complete RGB565 frame.
- `MOBAdisplay/Transport/UdpLineFrameSender.cs` sends `HOST_VER`, `FRAME_START`, indexed rows, and `FRAME_DONE` over UDP.
- `MOBAdisplay/Runtime/FrameLoopOptions.cs` contains endpoint, refresh rate, dimensions, and an unused `SendDisplayMetadata` option.
- `MOBAdisplay/docs/protocol.md` says `DISPLAY_META` is sent before each frame, but the sender does not currently send it.
- `MOBAdisplay/esp32/src/main.cpp` combines Wi-Fi provisioning, HTTP configuration, UDP parsing, frame assembly, diagnostics, and TFT_eSPI rendering.
- The firmware currently presents a captured buffer even when not all rows were received, then records the frame as incomplete.
- The current firmware constructs host-version text from the UDP buffer without passing the received length to the string constructor. RF-01 owns correction of that parser safety defect.
- `MOBAdisplay/FrameSender.cs` is a synchronous, unindexed legacy chunk sender.
- `Common/Configuration/DisplaySettings` stores only one ESP32 IP address.
- `SharedUI/ViewModel/DisplayViewModel.cs` and the DisplayPage currently model fixed display choices and resolutions but not endpoint health, capabilities, protocol state, or test-pattern execution.
- Existing display tests cover rendering and scheduler behavior, not protocol negotiation, UDP responses, frame assembly, or firmware conformance.
- `MOBAdisplay/esp32/platformio.ini` defines the current ESP32-S3 target, but the repository does not yet document the exact agent-safe PlatformIO validation command.

The first implementation turn must revalidate these statements against the local worktree after every affected local file passes the required secret scan.

## Scope

### Included

- a portable C++ display-backend interface;
- one TFT_eSPI reference adapter for the current ESP32-S3/ST7789 project;
- one fake or alternate display adapter;
- a versioned host/firmware protocol;
- explicit endpoint configuration;
- version and capability negotiation;
- device health and structured result reporting;
- RGB565 full-frame and region transfer within device-reported limits;
- atomic frame validation and presentation;
- bounded timeout, cancellation, acknowledgement, retry, and duplicate handling;
- optional brightness and built-in test-pattern commands;
- host-side capability and response models;
- a fake ESP32 endpoint and host conformance suite;
- a deterministic firmware parser/frame-assembly harness;
- MOBAflow configuration, diagnostics, and configured-device test-pattern support;
- retirement of superseded display transport paths after validation;
- protocol, architecture, operating, and hardware-validation documentation.

### Excluded

- RF-01 parser-safety implementation;
- RF-02 Wi-Fi provisioning security implementation;
- firmware flashing and update distribution;
- Wi-Fi onboarding UX;
- automatic device discovery;
- automatic display-controller or pin detection;
- moving board pins, credentials, controller type, DMA settings, or graphics-library configuration into shared host models;
- changing unrelated Z21, MOBApi, workflow, timetable, recorder, or interlocking behavior;
- introducing a permanent compatibility layer for undocumented legacy frame formats;
- editing vendored TFT_eSPI sources unless a separately justified upstream compatibility change is unavoidable.

## Dependency and sequencing gates

| Dependency | Type | Required handling |
| --- | --- | --- |
| RF-01: Length-safe ESP32 UDP parser | Hard | Must be merged before issue #36 changes packet parsing or builds the v2 firmware dispatcher on that parser boundary. Issue #36 must not duplicate RF-01. |
| RF-02: Protected ESP32 provisioning | Strong sequencing | Protocol specification and host work may proceed, but the large `main.cpp` extraction should start only after RF-02 stabilizes the provisioning boundary. |
| RF-06: Effective Release analyzer gate | Indirect | Do not block contract design; ensure new .NET code is clean under the active analyzer baseline. |
| RF-07: Complete CI platform matrix | Coordination | Align host tests, firmware parser tests, and PlatformIO build commands so RF-07 can make them mandatory without inventing a second test lane. |
| RF-15: Remove or harden latent legacy services | Overlap/ownership | Issue #36 owns the display-sender cutover and deletion of `MOBAdisplay/FrameSender.cs`; RF-15 should consume that result rather than perform a parallel edit. |
| Issue #35: AutomationTestbenchPage | Soft consumer | Expose testable effects and fakes that #35 can reuse later, but do not add testbench UI or workflow scope here. |
| Issue #30: RecorderPage | Soft consumer | Make diagnostic events/data structurally consumable later, but do not implement recording here. |

### Earliest start

- Planning and protocol decisions: immediately.
- Host contract models and fake endpoint: after the decision gates below are approved; they may proceed while unrelated P0 packages continue.
- Firmware protocol integration: after RF-01.
- Firmware decomposition and TFT_eSPI reference adapter: after RF-02.
- Final cutover and legacy removal: after host conformance, firmware conformance, and current-hardware smoke tests pass.

For a single sequential delivery lane, begin implementation at the start of the P1 phase after RF-01 through RF-05. For parallel delivery, begin the host-only slices after RF-01 while unrelated P0 packages continue.

## Design decision gates

The following decisions must be approved in issue #36 or its linked pull request before implementation establishes public types or on-wire constants.

| ID | Decision | Recommended default | Exit evidence |
| --- | --- | --- | --- |
| D1 | Protocol envelope | Binary fixed header with magic, protocol major/minor, message type, request ID, frame ID, payload length, flags, and integrity field | Published byte layout and golden test vectors |
| D2 | Version negotiation | Explicit host `Hello` request and device capability response; reject incompatible major versions and negotiate supported minor features | Compatibility matrix for supported/unsupported versions |
| D3 | Datagram size | Device reports maximum payload/region size; host defaults to a conservative non-fragmenting payload and splits wide rows into regions | Tests include 240, 172, and 800 pixel widths without assuming one row per datagram |
| D4 | Acknowledgement model | Acknowledge control operations and frame completion; use missing-range or negative acknowledgement rather than acknowledging every data packet | Sequence diagrams for success, loss, retry, duplicate, busy, and timeout paths |
| D5 | Idempotency | Unique request and frame IDs; duplicate begin/data/end commands are recognized and do not present twice | Duplicate-packet conformance tests |
| D6 | Presentation rule | Only a fully validated frame/region transaction may update the visible display; incomplete buffers are discarded on timeout, replacement, cancellation, or reboot | Firmware tests prove no partial `present` call |
| D7 | Capability freshness | Query on connection and after reboot/version change; persisted capabilities are diagnostic cache only until reconfirmed | Stale-capability and reboot tests |
| D8 | Configuration ownership | Keep network endpoint in environment/user settings; keep logical device assignment in project data only if multiple project-bound displays are explicitly required | Persistence decision recorded with schema impact |
| D9 | Legacy cutover | Temporary, explicit v1 validation window only; no silent fallback after v2 is selected; delete the synchronous chunk sender before issue closure | Cutover checklist and reference-hardware evidence |
| D10 | Second adapter | Prefer a deterministic in-memory/host-native adapter plus a distinct capability profile; use a second hardware driver only if it is available for repeatable CI | Adapter builds and passes the same contract suite |

### Protocol invariants

Regardless of the final encoding decision, the protocol must preserve these invariants:

- every received packet is parsed using its explicit byte length;
- no parser reads beyond the received payload or assumes null termination;
- every transaction is associated with a negotiated protocol version and device session;
- frame dimensions, region bounds, pixel format, byte count, rotation, and packet sequence are validated before data is accepted;
- a new frame cannot silently merge with a previous or pre-reboot frame;
- repeated rows or regions do not inflate completion counts;
- out-of-order data is either supported deterministically or rejected with a structured result;
- incomplete data never reaches the visible display;
- optional operations report `unsupported` rather than appearing to succeed;
- `busy`, `invalid`, `unsupported`, `timeout`, and `hardware failure` are distinguishable;
- cancellation stops host retries and leaves firmware in a defined state;
- credentials and provisioning secrets never appear in protocol diagnostics.

## Target architecture

### Host side

Keep `IFrameSender` as the high-level scheduler-facing abstraction unless local analysis proves that a compatible evolution is simpler. Introduce a lower-level capability-aware device client rather than forcing discovery, health, and command responses into a single frame-only method.

Proposed responsibilities:

- protocol primitives: message kinds, identifiers, versions, capabilities, health, pixel formats, rotations, error/result codes, frame metadata, and region descriptors;
- codec: deterministic serialization/deserialization with length and bounds checks;
- device client: endpoint connection, hello/capability query, health query, command execution, timeout, cancellation, retry, and response correlation;
- frame transport: validates rendered output against negotiated capabilities and translates a complete frame into bounded regions;
- scheduler adapter: continues to accept rendered frames and reports a useful transmission result without coupling rendering to UDP details;
- configuration service: stores the explicit endpoint and last selected device, never credentials;
- diagnostics projection: exposes negotiated and observed state without requiring UI types in `Common`, `Domain`, `Backend`, or `MOBAdisplay`.

### Firmware side

Extract a small portable interface that owns display behavior, not network or board configuration. Equivalent operations should include:

- initialize;
- read capabilities and health;
- begin frame or region transaction;
- write validated RGB565 data;
- complete and present;
- abort/discard;
- clear;
- set brightness when supported;
- render a built-in test pattern when supported.

The network protocol dispatcher must depend on this interface. The current TFT_eSPI implementation becomes one adapter. Pin mappings, TFT controller selection, resolution, color order, backlight pin, PSRAM/DMA choices, Wi-Fi credentials, and PlatformIO environment remain in the concrete firmware project.

### Atomic frame state

The firmware frame assembler should explicitly track:

- current session and frame ID;
- declared dimensions, format, rotation, and expected byte/region coverage;
- received region coverage without double counting;
- transaction start and last-activity time;
- validation/error state;
- whether presentation has already occurred;
- abort reason and last structured result.

The display adapter must receive `present` only after the assembler reports complete and valid. A new frame, timeout, reboot, cancellation, or unrecoverable validation error discards the staging state.

## Candidate file impact

These are planning targets, not authorization to create guessed APIs. Reconfirm all paths and surrounding patterns after the local secret-scan gate is available.

### Existing files likely to change

- `MOBAdisplay/Transport/IFrameSender.cs`
- `MOBAdisplay/Transport/UdpLineFrameSender.cs`
- `MOBAdisplay/Runtime/FrameLoopOptions.cs`
- `MOBAdisplay/Runtime/FrameLoopScheduler.cs`
- `MOBAdisplay/docs/protocol.md`
- `MOBAdisplay/esp32/src/main.cpp`
- `MOBAdisplay/esp32/platformio.ini`
- `Common/Configuration/AppSettings.Sections.cs`
- `SharedUI/ViewModel/DisplayViewModel.cs`
- `MOBAflow/View/DisplayPage.xaml`
- `MOBAflow/Extensions/MobaWinUiServiceCollectionExtensions.cs`
- relevant project files only if new source/test directories require explicit inclusion

### Existing file expected to be removed

- `MOBAdisplay/FrameSender.cs`, only after the v2 reference path is proven and repository-wide reference checks show no remaining consumers

### Candidate new areas

- `MOBAdisplay/Protocol/` for platform-neutral protocol types and codec logic;
- `MOBAdisplay/Transport/` additions for capability-aware device communication;
- `MOBAdisplay/esp32/include/` or a focused project-local library for the portable display and frame-assembler contracts;
- `MOBAdisplay/esp32/src/` adapters for TFT_eSPI and the deterministic fake/alternate backend;
- `Test/MOBAdisplay/` protocol, transport, configuration, and conformance fixtures;
- a firmware host-native test/harness location selected according to the established PlatformIO test layout.

Do not create a second README in a subdirectory. Update the existing root/reference documentation and `MOBAdisplay/docs/protocol.md` instead.

## Delivery work packages

### WP0: Readiness, local baseline, and decisions

Tasks:

1. Restore the required local secret-scanning capability without initiating an interactive login from this task.
2. Scan every local file before reading it.
3. Compare the local worktree with the GitHub baseline used by this plan.
4. Confirm RF-01 and RF-02 ownership and merge status; do not implement their scope in issue #36.
5. Search all local references to `IFrameSender`, `UdpLineFrameSender`, `FrameSender`, `FrameLoopOptions`, `DISPLAY_META`, and the ESP32 frame constants.
6. Inspect the applicable architecture, backend, testing, naming, and planning instructions.
7. Resolve D1 through D10 and record the accepted decisions in issue #36 or the first design pull request.
8. Confirm the authoritative PlatformIO build and host-native test commands.

Exit criteria:

- the local baseline is known and secrets-safe;
- no overlapping user changes are at risk;
- RF dependency status is explicit;
- on-wire layouts and sequence diagrams are approved;
- test and hardware commands are reproducible;
- candidate file paths are replaced with verified paths.

### WP1: Protocol specification and golden conformance vectors

Tasks:

1. Replace the current descriptive row protocol with a normative versioned specification.
2. Define byte order, header fields, length rules, identifiers, limits, and integrity checks.
3. Define hello/version negotiation and capability response fields:
   - device identity;
   - firmware version;
   - protocol versions;
   - width and height;
   - supported pixel formats;
   - rotation support;
   - optional commands;
   - maximum datagram and region size;
   - acknowledgement behavior;
   - staging-buffer and atomic-presentation support;
   - health/error information that is safe to expose.
4. Define frame/region messages and structured responses.
5. Define timeout, cancellation, retry, duplicate, out-of-order, replacement-frame, and reboot behavior.
6. Publish golden byte vectors for every message kind and representative error.
7. Define the standard deterministic test pattern and its expected RGB565 output/hash.
8. Mark the current `DISPLAY_META` documentation mismatch explicitly and remove ambiguity.

Exit criteria:

- another implementation can build a compatible client or firmware from the specification alone;
- all normative messages have golden valid and invalid vectors;
- protocol behavior does not depend on current display dimensions;
- the specification contains no board pins, credentials, or TFT_eSPI-specific types.

### WP2: Host protocol models, codec, and fake endpoint

Tasks:

1. Add immutable protocol/version/capability/result models in a platform-neutral project.
2. Add strict codec functions that consume explicit spans/lengths and reject trailing, truncated, oversized, or inconsistent data.
3. Add request/frame ID generation and response correlation.
4. Add a fake UDP endpoint that can:
   - negotiate capabilities;
   - acknowledge commands;
   - capture complete frames and regions;
   - delay, drop, duplicate, reorder, or reject packets;
   - simulate busy, unsupported, invalid, timeout, hardware failure, and reboot;
   - assert that no invalid or incomplete frame was presented.
5. Add conformance tests from the golden vectors.
6. Add cancellation and bounded-retry tests using controllable time rather than long real delays.
7. Keep all transport exceptions translated into structured results at the appropriate boundary while preserving actionable diagnostics.

Exit criteria:

- codec tests cover every length boundary and message type;
- arbitrary invalid input cannot escape bounds validation;
- the fake endpoint deterministically exercises all required anomaly cases;
- host cancellation and retries are bounded and idempotent;
- no WinUI, MAUI, or hardware-driver reference enters the protocol layer.

### WP3: Portable firmware interface and deterministic assembler

Entrance gates: RF-01 merged; RF-02 boundary stable before restructuring shared portions of `main.cpp`.

Tasks:

1. Extract network-independent display capabilities and result types.
2. Define the portable display-backend interface.
3. Extract deterministic frame/region assembly and coverage tracking from the Arduino loop.
4. Parse every packet with explicit length and delegate only validated operations.
5. Ensure incomplete, invalid, timed-out, replaced, or reboot-interrupted frames are discarded.
6. Add a deterministic fake/alternate display adapter that records calls and exposes a capability profile different from the current ST7789 adapter.
7. Add host-native firmware tests for metadata, bounds, duplicates, ordering, completion, abort, timeout, optional commands, and responses.
8. Add fuzz or property-based parser coverage where supported by the selected firmware test lane.
9. Keep Wi-Fi, HTTP provisioning, NVS, board pins, and concrete driver configuration outside the portable component.

Exit criteria:

- the assembler can be tested without Arduino, Wi-Fi, TFT_eSPI, or physical hardware;
- two adapters pass the same backend contract tests;
- no incomplete test case calls `present`;
- frame replacement and reboot behavior are deterministic;
- PlatformIO builds without new warnings.

### WP4: TFT_eSPI reference adapter and v2 transport integration

Tasks:

1. Implement the current ESP32-S3/ST7789 behavior through the portable adapter.
2. Report actual compiled/runtime capabilities rather than accepting host assumptions.
3. Add the v2 UDP dispatcher and structured responses.
4. Add the host device client and capability-aware frame transport.
5. Split rendered rows into bounded regions according to the negotiated payload limit.
6. Reject incompatible dimensions, pixel formats, rotations, and optional commands before frame transmission.
7. Preserve `CancellationToken` propagation through all host operations.
8. Update the scheduler adapter to surface negotiated and transfer failures without stopping local preview.
9. Keep any temporary v1 validation path explicit, isolated, and scheduled for deletion in WP6.
10. Perform current-hardware smoke tests for negotiation, standard test pattern, repeated frames, loss, reconnect, and reboot.

Exit criteria:

- the current display receives and atomically presents the standard pattern;
- host and device report matching protocol/firmware/capability diagnostics;
- an incompatible fake profile is rejected before data transmission;
- timeout and retry do not duplicate presentation;
- the reference firmware remains usable after reboot and reconnect.

### WP5: MOBAflow configuration, diagnostics, and test-pattern UX

Tasks:

1. Extend display configuration according to D8 without storing credentials.
2. Model endpoint validation, connection state, negotiation state, capability freshness, health, and last result in platform-neutral/ViewModel code.
3. Replace fixed-resolution assumptions with negotiated read-only device information where a configured device is selected.
4. Allow a test pattern only after endpoint validation and successful capability negotiation.
5. Disable or explain unsupported brightness, rotation, format, or built-in pattern controls.
6. Add English status, empty, invalid, busy, offline, unsupported, and stale-capability messages.
7. Keep commands in the ViewModel; code-behind remains an input adapter only.
8. Use `ThemeResource` values and validate Light, Dark, High Contrast, keyboard, focus, and screen-reader behavior.
9. Persist only the approved endpoint/selection fields and a clearly stale diagnostic capability cache if D8 permits it.
10. Add ViewModel and configuration-default regression tests.

Exit criteria:

- no test image can be sent to an unconfigured or unnegotiated endpoint;
- all unsupported operations are visibly disabled or return an actionable message;
- app restart preserves the approved configuration without treating cached capabilities as live;
- UI behavior is testable without an ESP32;
- user-visible strings are English and theme/accessibility checks pass.

### WP6: Cutover, legacy removal, and operational hardening

Tasks:

1. Run repository-wide reference checks for the synchronous `FrameSender` and undocumented legacy packet paths.
2. Remove `MOBAdisplay/FrameSender.cs` after all consumers use the validated async path.
3. Remove firmware support for undocumented unindexed legacy rows after the agreed validation window.
4. Remove unused flags such as `SendDisplayMetadata` or implement their approved replacement; do not leave misleading options.
5. Remove temporary v1 fallback branches rather than retaining a permanent dual protocol.
6. Verify that RF-15 tracking recognizes the completed display cleanup and does not repeat it.
7. Add permanent CI hooks for host conformance, firmware parser tests, and PlatformIO build in coordination with RF-07.
8. Review logging and diagnostics for credentials, SSIDs, tokens, packet payload leakage, and excessive frame-level noise.

Exit criteria:

- one supported production transport path remains;
- no synchronous sleep/send path remains;
- no undocumented legacy packet format remains enabled;
- CI reproduces all software validation from a clean checkout;
- diagnostics are useful without exposing secrets.

### WP7: Final validation and documentation

Tasks:

1. Run the complete validation matrix below.
2. Update `MOBAdisplay/docs/protocol.md` to match the final implementation exactly.
3. Update root/reference architecture and user documentation where endpoint configuration, diagnostics, or test-pattern behavior is user-facing.
4. Document hardware-specific configuration only in the existing appropriate hardware notes.
5. Record the final protocol and firmware versions used for acceptance.
6. Link every pull request to issue #36 and list RF packages only as secondary dependencies.
7. Close issue #36 only after automated and maintainer-led hardware evidence satisfies every acceptance criterion.
8. Delete this completed plan in the final cleanup pull request.

## Test strategy

### Host unit tests

- protocol version ordering and compatibility;
- message encoding/decoding golden vectors;
- empty, truncated, oversized, malformed, and unknown packets;
- invalid lengths, enums, dimensions, region bounds, rotations, formats, and result codes;
- request and frame ID correlation;
- endpoint validation;
- capability compatibility checks;
- payload/region splitting across reported limits;
- cancellation before send, during transfer, and while waiting for response;
- timeout and bounded retry;
- duplicate response and late response handling;
- stale capability invalidation after reboot/version change;
- configuration defaults and persistence;
- ViewModel command eligibility and status projection.

### Host integration/conformance tests

- successful hello, capability query, health query, clear, optional commands, frame transfer, and present;
- unsupported protocol major/minor behavior;
- packet loss at begin, data, end, and response stages;
- duplicate begin, region, end, acknowledgement, and response;
- out-of-order regions;
- incomplete frame timeout;
- wrong dimensions, byte count, format, and rotation;
- busy response and retry exhaustion;
- hardware failure response;
- device reboot during negotiation and frame transfer;
- endpoint change during a scheduler lifetime;
- assertion that a frame ID is presented at most once.

### Firmware host-native tests

- parser length safety around every prefix/header boundary;
- metadata and capability serialization;
- frame assembler coverage and duplicate tracking;
- region overlap and bounds validation;
- incomplete, replacement, abort, timeout, and reboot cleanup;
- optional feature dispatch;
- fake adapter call ordering;
- no `present` call for invalid or incomplete frames;
- deterministic standard test-pattern output.

### Firmware build and hardware validation

- authoritative PlatformIO clean build for the current ESP32-S3 environment;
- no new compiler warnings;
- serial diagnostics show safe version and health information;
- negotiate and display the standard pattern on the current ST7789 hardware;
- validate clear and supported optional commands;
- simulate dropped, duplicated, and reordered UDP packets;
- interrupt transfer, reboot the device, and reconnect;
- confirm the previously visible frame remains intact after incomplete transfer;
- run sustained refresh testing and record dropped/rejected frame counters;
- verify configuration/provisioning behavior from RF-02 remains unchanged.

### .NET validation commands

Use the repository-authoritative commands after the local baseline is scanned and restored:

```text
dotnet build MOBAdisplay/MOBAdisplay.csproj
dotnet test Test/Test.csproj
dotnet build MOBAflow/MOBAflow.csproj -c FastDebug --no-restore /p:BuildMOBApiDependency=false /p:CopyMOBApiToOutput=false
```

Do not guess the PlatformIO command. WP0 must establish and document the exact repository-supported command before firmware validation is automated.

## Acceptance traceability

| Issue #36 acceptance criterion | Owning work packages | Required evidence |
| --- | --- | --- |
| Two different ESP32 display projects implement the same interface without shared board configuration | WP3, WP4 | Two adapter builds and shared contract tests with distinct capability profiles |
| MOBAflow receives capabilities and sends only compatible content | WP1, WP2, WP4, WP5 | Negotiation, compatibility rejection, and ViewModel tests |
| Configured device displays the standard test pattern | WP1, WP4, WP5 | Golden pattern vector plus current-hardware evidence |
| Invalid or incomplete frames are rejected without corrupted partial display | WP2, WP3, WP4 | Fake endpoint, firmware assembler, packet anomaly, and hardware interruption tests |
| Protocol and firmware versions are visible in diagnostics | WP1, WP4, WP5 | Diagnostic projection and UI/manual evidence |
| Current MOBAdisplay firmware remains functional through the reference adapter | WP3, WP4 | PlatformIO build and ST7789 smoke test |
| Automated tests cover negotiation, validation, assembly, optional commands, timeouts, anomalies, and unsupported hardware features | WP2 through WP6 | Green host and firmware conformance suites in CI |

## Risk register

| Risk | Probability | Impact | Mitigation |
| --- | --- | --- | --- |
| RF-01 or RF-02 changes the same firmware areas after #36 begins | High | High | Enforce entrance gates; keep host-only work separate; rebase after each dependency merge |
| UDP/IP fragmentation makes wide-display rows unreliable | High | High | Negotiate payload limits and split into bounded regions; test 800-pixel width |
| Retry behavior presents a frame twice | Medium | High | Frame IDs, idempotent end/present, duplicate tests, at-most-once assertion |
| Incomplete staging buffer becomes visible | Current/High | High | Separate staging from active display; present only after complete validation |
| Large displays exceed internal RAM/PSRAM assumptions | Medium | High | Report memory/region capabilities; do not require every adapter to hold a full framebuffer |
| Documentation and implementation drift again | Medium | Medium | Golden vectors, conformance tests, and protocol-doc review in the same PR |
| Capability cache is mistaken for live device state | Medium | Medium | Mark cached values stale and require negotiation before sending |
| Permanent v1/v2 compatibility code increases complexity | Medium | Medium | Time-box explicit validation path and delete it in WP6 |
| Hardware is unavailable in CI | High | Medium | Host-native firmware harness and fake adapter as mandatory gates; maintainer hardware sign-off remains separate |
| UI starts owning transport behavior | Medium | Medium | Capability-aware service/client boundary; ViewModel commands; thin code-behind |
| Diagnostics expose Wi-Fi credentials or payload data | Low | High | Structured allow-listed diagnostics and explicit secret review |
| Local worktree differs from inspected GitHub baseline | Unknown | High | Mandatory secret-scanned local comparison in WP0 before edits |

## Pull request sequence

Keep every pull request independently buildable, reviewable, and reversible. Every pull request has issue #36 as its single primary issue.

1. `docs(display): specify versioned ESP32 display protocol`
   - decisions D1-D10, normative protocol, sequence diagrams, golden vectors;
   - no production behavior change.
2. `feat(display): add protocol models and conformance endpoint`
   - host models, codec, fake endpoint, conformance tests.
3. `refactor(display): extract portable firmware display contract`
   - deterministic assembler, portable interface, fake/alternate adapter, firmware tests;
   - only after RF-01 and RF-02 gates.
4. `feat(display): add capability-aware ESP32 transport`
   - v2 device client, TFT_eSPI reference adapter, region transfer, structured responses.
5. `feat(display): add configured-device diagnostics and test pattern`
   - settings, ViewModel, DisplayPage, diagnostics, accessibility, tests.
6. `refactor(display): remove legacy frame transport`
   - delete synchronous sender, legacy packet handling, misleading options, and temporary fallback.
7. `docs(display): finalize ESP32 display operation and validation`
   - final docs, CI references, hardware evidence, plan deletion.

Split a pull request further if it mixes protocol behavior, mechanical movement, UI changes, or hardware-only changes. Do not combine repository-wide formatting with any issue #36 pull request.

## Definition of done

Issue #36 is complete only when:

- RF-01 and RF-02 entrance conditions have been respected without implementing their unrelated scope here;
- all approved design decisions are reflected consistently in code, tests, and documentation;
- protocol parsing is explicit-length and bounds-safe on host and firmware;
- two adapters pass the same portable display contract;
- host and firmware negotiate versions and capabilities successfully;
- incompatible content is rejected before transmission or presentation;
- incomplete, invalid, duplicate, reordered, timed-out, replaced, and reboot-interrupted frames never corrupt the visible display;
- cancellation and retry behavior is deterministic and bounded;
- current ST7789 hardware passes the standard-pattern and reconnect smoke tests;
- endpoint, capability, health, version, and last-result diagnostics work without exposing credentials;
- the MOBAflow test-pattern action is unavailable until configuration and capability negotiation succeed;
- all user-visible strings are English;
- Light, Dark, High Contrast, keyboard, focus, and screen-reader checks pass for changed UI;
- relevant .NET tests, complete `Test/Test.csproj`, WinUI FastDebug build, firmware host tests, and PlatformIO build pass;
- no new compiler or analyzer warnings remain without a narrow documented justification;
- `MOBAdisplay/FrameSender.cs`, undocumented legacy rows, unused metadata flags, and temporary compatibility paths are removed;
- protocol and operational documentation matches the shipped behavior;
- each pull request references issue #36 and uses Conventional Commits;
- issue #36 contains or links the automated and hardware acceptance evidence;
- this completed plan is deleted.