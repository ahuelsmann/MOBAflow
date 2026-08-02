# Issue 36: ESP32 Display Interface Implementation Plan

## Document status

**GitHub Issue**: #36
**Spec Kit**: Required

- Status: In progress
- Primary work item: [GitHub issue #36 - Define a common ESP32 SPI display firmware interface](https://github.com/ahuelsmann/MOBAflow/issues/36)
- Recommended priority: P1
- Required label: `plan-required`
- Plan ownership: one-to-one with issue #36
- Implementation baseline: `github/main` at `7f4edea0`, including merged PR #89; WP4.1 is implemented from that baseline
- Lifecycle: delete this plan after issue #36 is complete; the closed issue, pull requests, and Git history remain the permanent record

## Implementation progress

Status on 2026-08-02:

- PR #109 is merged in `github/main` at `0975315715f2f107b0c68e058dfdc9d4fdb1a685`.
  WP5 through the software and documentation portions of WP7 are implemented
  on `codex/issue-36-wp5` from that baseline. MOBAflow has not been launched.
- WP5 now persists only one configured ESP32 IP address and UDP port. The
  display client negotiates a live session, projects protocol, firmware,
  identity, capability, health, and last-result diagnostics, and invalidates
  capabilities after endpoint changes, failed renegotiation, timeout,
  transport failure, disposal, or `WrongSession`. Standard and optional
  commands remain fail-closed until the live capabilities authorize them.
- The Display page uses English, capability-driven WinUI controls and
  `ThemeResource` styling. Fixed Waveshare model selectors and their obsolete
  configuration controls are removed. ViewModel and endpoint/client tests
  cover unconfigured, invalid, offline, unsupported, stale, reboot, and
  successful operation paths without an ESP32.
- WP6 adds a permanent `display-protocol` GitHub Actions job for the legacy
  cutover invariant, .NET conformance tests, native PlatformIO tests, the
  ESP32-S3 build, firmware SHA-256 recording, and firmware artifact upload.
  `scripts/Test-DisplayProtocolCutover.ps1` passes locally and prevents the
  removed legacy sender, parser, metadata, and synchronous frame paths from
  returning.
- The final full .NET runs pass 1,591 net10.0 tests with four existing skips
  (1,595 total) and all 1,644 Windows tests. The focused WP5 suite passes all
  53 tests. The MOBAflow FastDebug build passes with zero warnings and errors.
  The complete native matrix passes all 47 tests, and the ESP32-S3 release
  build succeeds.
- The cross-platform analyzer baseline matches 4,156 diagnostics in 1,354
  groups, and the Windows baseline matches 2,848 diagnostics in 1,025 groups.
  The reviewed baseline refresh removes resolved warnings and records only two
  new `CA1812` false positives where NUnit constructs internal test fixtures by
  reflection; no source suppression was added.
- All 30 changed or new existing files pass the deterministic Sonar secrets
  scan. A direct server-side agentic upload to SonarQube Cloud was requested
  after general Sonar authorization but was blocked by the execution security
  reviewer because the external destination and complete source payload were
  not separately authorized. It was not bypassed. The draft PR's established
  SonarCloud gate and zero `OPEN`/`CONFIRMED` issue requirement remain
  mandatory publication evidence.
- WP7 protocol, architecture, reference, user, and hardware-acceptance notes
  are updated. The current firmware image SHA-256 is
  `98BC90D9541FE60F6C0AF84742C06A180951F45A528E7AB855F34934AE703245`.
  Flash remains at 1,047,357 of 1,048,576 bytes (99.9 percent), leaving only
  1,219 bytes; this is a critical release and hardware-validation risk.
- Final issue closure is still blocked by manual Light, Dark, High Contrast,
  keyboard, focus, and screen-reader checks; approved sustained-refresh
  duration and dropped/rejected-frame thresholds; and current
  ESP32-S3/ST7789 negotiation, standard-pattern, optional-command,
  packet-anomaly, interruption, reconnect, reboot, and sustained-refresh
  acceptance. The plan remains authoritative until those gates pass.

Status on 2026-07-31:

- WP4.1 is published from `codex/issue-36-wp4-1` in ready-for-review PR #109.
  Its implementation baseline remains `github/main` `7f4edea0`; the current
  PR base head is `f9c141a1`. MOBAflow has not been launched.
- The host production sender now negotiates v1.0 capabilities and transfers
  frames atomically through `BeginFrame`, bounded `FrameRegion` packets, and
  `CompleteFrame`. Cancellation preserves `OperationCanceledException`, makes
  a best-effort `AbortFrame` attempt after an uncertain begin, and the UDP
  receive loop survives transient socket failures.
- Firmware replay protection now tracks exact observed request fingerprints:
  16 active cached results plus 64 evicted tombstones. Unseen out-of-order and
  wrapped request IDs remain valid; a reboot clears both histories. The host
  line sender, firmware line parser, and legacy framebuffer presentation path
  are removed, leaving one production v1.0 path.
- The post-review implementation sends regions without per-packet positive
  acknowledgement, repairs reported gaps through bounded `CompleteFrame`
  retries, splits wide rows for 172-, 240-, and 800-pixel displays, invalidates
  stale negotiated capabilities after `WrongSession`, and reports conflicting
  request-ID reuse as `Conflict`. Direct sender lifecycle tests and an
  end-to-end native conformance-pattern presenter test cover the remaining
  review gaps.
- The follow-up GitHub review found two reconnect/retry races. A fresh
  successful `Hello` now resets the firmware frame-ID epoch, so a restarted
  host may begin with a lower randomized frame ID without rebooting the ESP32.
  Acknowledged frame operations now retry after 250 milliseconds, safely below
  the firmware's one-second staging timeout. Regression tests first failed on
  both old behaviors and now pass for a lower post-reconnect frame ID and a
  dropped `BeginFrame` response.
- The complete native PlatformIO matrix passes 47 tests: 14 display-core,
  20 dispatcher/adapter, 8 provisioning, and 5 length-safe parser cases. The
  ESP32-S3 release build creates `firmware.bin`; RAM usage is 43,960 of 327,680
  bytes (13.4 percent), while the post-Sonar-remediation flash usage is
  1,047,357 of 1,048,576 bytes (99.9 percent), leaving only 1,219 bytes and
  therefore a release risk.
- The complete .NET matrices pass 1,502 net10.0 tests with four existing skips
  (1,506 total) and all 1,556 Windows tests. `MOBAdisplay`, the Windows test
  graph, and the MOBAflow FastDebug graph build with zero warnings and errors.
  `git diff --check` passes. The cross-platform analyzer baseline matches 4,051
  diagnostics in 1,343 groups, and the Windows baseline matches 2,801
  diagnostics in 1,025 groups without adding suppressions or baseline entries.
- All 28 existing changed or new files pass the deterministic Sonar secrets
  scan with no findings. After explicit source-upload authorization, local
  analysis ran against current `github/main` with project
  `ahuelsmann_MOBAflow2`, but Vortex failed for all enumerated files with
  `Vortex agentic analysis is not available
  for this organization (403 Forbidden)`. This organization capability cannot
  be repaired in WP4.1. A direct PR query after the review-fix push exposed nine
  `OPEN` Sonar issues despite a green quality gate. All nine are addressed in
  code without acceptance, suppression, or baseline changes: duplicated host
  diagnostics use constants, the datagram helper has seven parameters, and the
  native conformance test uses encapsulated state and C++11-compatible clear
  comparisons/copying/flag values. The remote quality gate and zero
  `OPEN`/`CONFIRMED` issue count must be reconfirmed after the Sonar-fix push.
- Current ESP32-S3/ST7789 hardware negotiation, conformance-pattern, packet
  loss, reconnect, and reboot acceptance remain mandatory before issue closure
  but are not WP4.1 merge gates. WP5 through WP7, permanent CI coverage, final
  documentation, and plan deletion remain open.

Historical status on 2026-07-28:

- PR #89 is merged. Its ESP32-S3 release build, 46 native firmware tests,
  complete .NET test matrices, changed-file secrets scan, and SonarCloud gate
  passed at publication time.
- Two later review findings against the merged implementation remain
  actionable: the firmware must not reject an unseen request ID solely from its
  numeric distance to the latest ID, and the legacy native-word RGB565 path is
  incompatible with the v1.0 big-endian presenter.
- WP4.1 is the next blocking slice before WP5. It corrects request replay
  handling and removes the host and firmware legacy transports rather than
  preserving a temporary dual-protocol window.
- WP4.1 may merge after the complete automated repository gates pass. Current
  ESP32-S3/ST7789 hardware validation remains mandatory before issue #36 can
  close, but it is not a WP4.1 merge gate.
- WP5 uses one explicitly configured endpoint from user/environment settings.
  Capabilities are negotiated live and are not persisted as authoritative
  project state.
- WP5 through WP7, permanent CI coverage, hardware acceptance, final
  documentation, and plan deletion remain open.

Historical status on 2026-07-25:

- The actionable review findings for draft PR #89 are fixed locally in the
  isolated `codex/89-review-fixes` worktree. Default request IDs now use one
  process-wide randomized sequence, while explicitly seeded test sequences
  remain deterministic.
- The TFT_eSPI adapter presents network-order RGB565 bytes without a second
  byte swap and restores the driver's previous swap setting. Native tests cover
  the byte sequence and driver-state restoration.
- Retryable, busy, and incomplete results are no longer cached as terminal
  responses. The dispatcher rejects request IDs that fall outside its bounded
  replay window while retaining valid out-of-order requests inside that window.
  A fresh Hello starts a new request epoch.
- The UDP classifier treats a `MOBA` prefix as versioned only when the complete
  fixed envelope and declared payload length are valid. A 480-byte legacy line
  that happens to start with those bytes remains on the explicit legacy path.
- A checked-in `sdkconfig.defaults` now supplies the 1 kHz FreeRTOS tick,
  8 MiB flash selection, and Arduino component autostart. PlatformIO explicitly
  embeds the public ESP Insights and RainMaker CA files required by the managed
  components, and only the vendored TFT_eSPI translation unit downgrades its
  GCC 14 misleading-indentation diagnostics from errors to warnings.
- The authoritative ESP32-S3 release build completes and creates the firmware
  image. The complete native PlatformIO matrix passes all 46 tests: 14
  display-core cases, 15 dispatcher/adapter cases, 8 provisioning regressions,
  and 9 parser cases.
- The complete .NET matrix passes 1,422 net10.0 tests with four existing skips
  and 1,475 Windows tests without failures or skips. `MOBAdisplay` builds with
  zero warnings and errors, `git diff --check` passes, and scoped formatting
  verification passes for both changed C# files. Project-wide formatting still
  reports pre-existing repository findings and generated managed-component
  source outside this PR scope.
- Publication is blocked at the local deterministic secrets gate: the Sonar CLI
  is not available and the configured Sonar MCP cannot start while the Docker
  daemon is stopped. No commit or push may occur until all changed files pass
  that scan.
- Current-hardware negotiation, conformance-pattern, loss, reconnect, and reboot
  smoke tests remain pending. WP4 and PR #89 therefore stay in draft even after
  the automated remediation is published.

Historical status on 2026-07-23:

- WP3 and PR #82 are merged. RF-01 and RF-02 are closed, so the versioned
  firmware-dispatcher entrance gates are satisfied.
- The WP4 firmware slice is implemented in draft PR #89: an allocation-free v1.0
  dispatcher validates the fixed envelope, CRC32, request/session/frame
  correlation, retry fingerprints, typed payloads, packet sequences, optional
  commands, and structured responses before it calls `FrameAssembler` or the
  display backend.
- The length-safe RF-01 classifier now recognizes the `MOBA` binary magic and
  routes versioned datagrams without weakening the exact legacy v0 parsing
  rules. The existing v0 path remains explicit and isolated for later WP6
  deletion.
- The current ESP32-S3/ST7789 implementation uses a project-local TFT_eSPI
  adapter. It advertises only RGB565 big-endian, rotation zero, clear, the
  deterministic conformance pattern, bounded regions, full-frame staging, and
  atomic presentation; board pins and driver configuration remain outside the
  portable contract.
- The PR review remediation is pushed in commit `56fe74d7`. A successful v1
  negotiation now locks out the shared-buffer legacy path, duplicate operations
  return cached results without executing display commands again, exact
  duplicated Hello datagrams preserve the retry cache, and a fresh Hello starts
  a new request epoch with a newly calculated datagram limit.
- Default host request-ID sequences now start in process-randomized
  ranges, while explicitly seeded sequences retain deterministic wrap behavior
  for tests. Firmware health reports the last frame or command result, including
  background frame timeouts, and reboot clears negotiation state.
- All 51 versioned ESP32 files passed deterministic per-file secret scanning
  before local inspection. All 17 changed or new files passed the same scan
  after implementation.
- The native PlatformIO matrix passes 41 tests: 14 display-core cases, 10
  versioned-dispatcher cases, 8 RF-02 provisioning regressions, and 9 RF-01
  parser cases including deterministic arbitrary input.
- The focused net10.0 request-ID and display-client suite passes 22 tests after
  the host reconnect hardening. The complete .NET matrix passes 1,422 net10.0
  tests with four existing skips and 1,475 Windows tests without failures or
  skips.
- The authoritative ESP32-S3 build still cannot be claimed. Commit `442d2c39`
  from RF-02 introduced an explicit reference to an absent
  `MOBAdisplay/esp32/sdkconfig.defaults`. A scanned temporary local file moved
  validation past that error and exposed the required Arduino
  `CONFIG_FREERTOS_HZ=1000` setting, but the subsequent ESP-IDF configuration
  did not reach a captured C++ compiler result before the bounded build was
  terminated. Both generated `sdkconfig` files were removed, and issue #36 does
  not invent or commit RF-02 security/build defaults.
- Local Sonar code analysis against `github/main` was requested but blocked by
  execution policy because it would transmit changed repository code to an
  external service; it is not counted as validation. Remote SonarCloud analysis
  for PR #89 passes. Five C++11-compatible findings were fixed in commit
  `332d05a7`; the remaining 93 findings were individually accepted with narrow
  rationales covering newer-language-only recommendations and intentional
  allocation-free or process-lifetime embedded design. The final direct query
  reports zero `OPEN` or `CONFIRMED` PR issues.
- Current-hardware negotiation, conformance-pattern, loss, reconnect, and reboot
  smoke tests remain pending. WP4 is therefore not complete and must stay in a
  draft pull request until the ESP32-S3 build baseline and hardware evidence are
  resolved.

Historical status on 2026-07-22:

- RF-02's protected-provisioning boundary and UDP-loop cleanup are merged in
  commits `442d2c` and `677ba975`. WP3 therefore started from current `main`
  without changing provisioning state, credentials, NVS, Wi-Fi, board pins, or
  Security 2 transport.
- The portable WP3 core slice is implemented in `MobaDisplayCore`: shared
  capabilities and structured result types, a display-backend interface, an
  allocation-free deterministic frame assembler, matching IEEE CRC32, and a
  recording backend with a configurable capability profile.
- The assembler validates frame metadata, geometry, byte offsets, packet
  metadata, the complete packet-index set, region limits, duplicate bytes,
  coverage, CRC, timeout, abort, explicit replacement, reboot reset, and
  session-wide at-most-once completion before it calls `Present`. Conflicting
  overlap and every incomplete or invalid terminal path leave the visible
  backend untouched.
- The PR review follow-up preserves rotation in the backend presentation call,
  retains staged data for structured retryable backend failures, rejects frame
  ID zero before duplicate handling, prevents completed-frame resurrection and
  older-frame replay, and normalizes a zero inactivity timeout to a bounded
  minimum. Packet receipt uses a second allocation-free bit field alongside
  pixel coverage.
- Two distinct native adapters exercise the same backend contract with different
  capability profiles. The complete native PlatformIO matrix passes 30 tests:
  14 display-core cases, 8 RF-01 parser cases including deterministic arbitrary
  input, and 8 RF-02 provisioning regressions.
- `MOBAdisplay` builds with zero warnings and errors. The complete .NET test
  matrix passes 1,408 net10.0 tests with four existing skips and 1,461 Windows
  tests without failures or skips.
- The versioned v1.0 UDP dispatcher and TFT_eSPI reference adapter remain WP4
  work; the
  legacy `main.cpp` frame path is intentionally unchanged in this slice.
- The ESP32-S3 build is blocked before compiling WP3 code because current
  `main` references an absent `MOBAdisplay/esp32/sdkconfig.defaults`. This
  pre-existing build-baseline defect is not repaired in issue #36 because the
  missing configuration belongs to the RF-02/firmware-build boundary.
- Local Sonar branch analysis was attempted against `github/main` after explicit
  authorization. Its secret phase reported zero findings; its agentic phase
  still fails for all nine changed files with `Vortex agentic analysis is not
  available for this organization (403 Forbidden)`. Remote SonarCloud passed on
  corrected commit `a453a056`: all applicable findings were fixed and the 38
  remaining C++17/20-only suggestions were accepted individually with their
  C++11 compatibility rationale. PR #82 reports zero open or confirmed Sonar
  findings, and all required GitHub checks are green.

Historical status on 2026-07-21:

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
- PR #69 merged the immutable envelope, version, capability, health, frame,
  region, command, and result models plus strict network-byte-order codecs.
  Validation covers lengths, reserved fields, enums, flags, UTF-8, metadata
  consistency, CRC32, immutable region bytes, and deterministic arbitrary-byte
  input for every message type.
- WP2 is complete: non-zero thread-safe identifier sequences, an independent
  datagram transport boundary, parallel response correlation, structured host
  outcomes, bounded timeout/retry/cancellation behavior, safe anomaly events,
  and transport-exception translation are implemented. The deterministic fake
  endpoint negotiates capabilities, reports health, stages and validates frame
  regions, presents complete frames at most once, and simulates delayed,
  dropped, duplicated, reordered/held, rejected, corrupted, wrong-correlation,
  transport-failure, conflict, and reboot paths.
- WP2 tests use an injected `TimeProvider` and manual clock, so timeout and
  retry-delay coverage contains no long real waits. Focused MOBAdisplay tests
  pass 117 cases with no failures. The complete net10.0 suite passes 1,291
  tests with one existing skip and no failures; the Windows/WinUI suite passes
  1,338 tests without skips or failures. Production and test-source format
  verification also passes.
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

## Original verified foundation

The following list records the original starting point. The dated implementation
progress above and merged pull requests supersede it where behavior has already
changed:

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
- `MOBAdisplay/esp32/platformio.ini` defines the current ESP32-S3 target. The
  authoritative PlatformIO build and native-test commands are now documented in
  `MOBAdisplay/docs/protocol.md` and repeated in the validation section below.

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
| RF-01: Length-safe ESP32 UDP parser | Hard | Must be merged before issue #36 changes packet parsing or builds the versioned v1.0 firmware dispatcher on that parser boundary. Issue #36 must not duplicate RF-01. |
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
- Corrective cutover and legacy removal: WP4.1, after host and firmware
  conformance tests pass and before WP5. Hardware evidence remains a final
  issue-closure gate rather than a WP4.1 merge gate.

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
| D8 | Configuration ownership | Approved 2026-07-28: keep one endpoint in user/environment settings; negotiate capabilities live; do not add project-bound display assignment or authoritative persisted capabilities in issue #36 | Configuration-default and restart tests prove no project schema migration and no live use of stale capabilities |
| D9 | Legacy cutover | Approved 2026-07-28: remove host and firmware legacy transports in WP4.1 before WP5; do not retain a dual-protocol release path | Repository-wide reference checks and automated v1.0 regression gates before merge; current-hardware evidence before issue closure |
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
- an unseen request ID is never rejected solely from its numeric distance to a
  previously observed ID;
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

- `MOBAdisplay/FrameSender.cs`, in WP4.1 after repository-wide reference checks
  show that the versioned v1.0 path owns every remaining consumer

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

1. Confirm local secret-scanning capability without initiating an interactive
   login from this task.
2. Scan files likely to contain secrets before reading them, and scan every
   changed file before commit or pull-request publication.
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

### WP4: TFT_eSPI reference adapter and v1.0 transport integration

Tasks:

1. Implement the current ESP32-S3/ST7789 behavior through the portable adapter.
2. Report actual compiled/runtime capabilities rather than accepting host assumptions.
3. Add the versioned v1.0 UDP dispatcher and structured responses.
4. Add the host device client and capability-aware frame transport.
5. Split rendered rows into bounded regions according to the negotiated payload limit.
6. Reject incompatible dimensions, pixel formats, rotations, and optional commands before frame transmission.
7. Preserve `CancellationToken` propagation through all host operations.
8. Update the scheduler adapter to surface negotiated and transfer failures without stopping local preview.
9. Keep the legacy v0 path explicit and isolated until the corrective WP4.1
   cutover.
10. Record the automated build, test, secrets, and Sonar evidence for the
    delivered transport slice.

Exit criteria:

- the v1.0 dispatcher and reference adapter compile and pass their native
  contract and conformance suites;
- host and device diagnostic payloads agree in automated conformance tests;
- an incompatible fake profile is rejected before data transmission;
- timeout and retry do not duplicate presentation;
- the ESP32-S3 release firmware image builds without new warnings.

### WP4.1: Protocol correctness and legacy cutover

This corrective slice blocks WP5. It remains narrowly scoped to the two
post-merge review findings, regression coverage, and removal of superseded
display transports.

Tasks:

1. Replace numeric-age replay rejection with session-scoped handling based on
   actually observed request fingerprints.
2. Add firmware tests for valid unseen out-of-order request IDs separated by
   more than the retained history length, delayed duplicates, conflicting
   retries, wraparound, fresh Hello epochs, and reboot.
3. Remove the firmware legacy v0 classifier/capture/presentation path and any
   temporary fallback after a v1.0 negotiation.
4. Remove `MOBAdisplay/FrameSender.cs`, migrate or remove every remaining
   consumer, and remove misleading options such as `SendDisplayMetadata`.
5. Keep the v1.0 RGB565 golden-color presenter test and add an integration-level
   conformance assertion that the standard pattern reaches the display adapter
   in network byte order.
6. Normalize plan, protocol, diagnostics, and code terminology to legacy v0 and
   versioned protocol v1.0.
7. Run repository-wide reference checks, native firmware tests, the ESP32-S3
   release build, relevant .NET tests, the complete test matrix, formatting,
   changed-file secrets scanning, local Sonar against the actual base, and the
   remote SonarCloud PR gate.

Exit criteria:

- valid unseen out-of-order requests are not rejected by numeric age;
- delayed duplicate operations remain at-most-once within the documented
  session/retry contract;
- one production protocol path remains and every legacy display transport,
  parser branch, sender, and misleading metadata option is absent;
- the v1.0 standard-pattern bytes remain correct through the adapter boundary;
- all automated quality gates pass with zero unresolved `OPEN` or `CONFIRMED`
  Sonar findings and no unresolved actionable review findings;
- the pull request may merge without current-hardware evidence; hardware
  acceptance remains mandatory in WP7 before issue closure.

### WP5: MOBAflow configuration, diagnostics, and test-pattern UX

Tasks:

1. Store one explicit endpoint in user/environment settings without storing
   credentials or adding project-bound display assignments.
2. Model endpoint validation, connection state, negotiation state, capability freshness, health, and last result in platform-neutral/ViewModel code.
3. Replace fixed-resolution assumptions with negotiated read-only device information where a configured device is selected.
4. Allow a test pattern only after endpoint validation and successful capability negotiation.
5. Disable or explain unsupported brightness, rotation, format, or built-in pattern controls.
6. Add English status, empty, invalid, busy, offline, unsupported, and stale-capability messages.
7. Keep commands in the ViewModel; code-behind remains an input adapter only.
8. Use `ThemeResource` values and validate Light, Dark, High Contrast, keyboard, focus, and screen-reader behavior.
9. Persist only the endpoint fields. Treat capabilities as live negotiated
   state; any diagnostic cache must be visibly stale and must never authorize a
   send.
10. Add ViewModel and configuration-default regression tests.

Exit criteria:

- no test image can be sent to an unconfigured or unnegotiated endpoint;
- all unsupported operations are visibly disabled or return an actionable message;
- app restart preserves the approved configuration without treating cached capabilities as live;
- UI behavior is testable without an ESP32;
- user-visible strings are English and theme/accessibility checks pass.

### WP6: CI and operational hardening

Tasks:

1. Confirm that WP4.1 left no synchronous sender, undocumented packet path,
   fallback branch, or misleading metadata option.
2. Verify that RF-15 tracking recognizes the completed display cleanup and does
   not repeat it.
3. Add permanent CI hooks for host conformance, firmware parser tests, and
   PlatformIO build in coordination with RF-07.
4. Review logging and diagnostics for credentials, SSIDs, tokens, packet
   payload leakage, and excessive frame-level noise.

Exit criteria:

- WP4.1 legacy-removal invariants remain enforced by repository checks;
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
7. Run current-hardware negotiation, standard-pattern, optional-command,
   packet-anomaly, incomplete-transfer, reconnect, reboot, and sustained-refresh
   acceptance on the ESP32-S3/ST7789 reference device.
8. Record the tested commit and firmware-image hash, board/display identity,
   protocol and firmware versions, exact commands, results, and safe diagnostic
   evidence in issue #36.
9. Before the hardware run, approve the sustained-refresh duration and allowed
   dropped/rejected-frame thresholds. These values are an open acceptance
   detail but do not block WP4.1 or WP5.
10. Close issue #36 only after automated and maintainer-led hardware evidence
    satisfies every acceptance criterion.
11. Delete this completed plan in the final cleanup pull request.

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
python -m platformio test -d MOBAdisplay/esp32 -e native
python -m platformio run -d MOBAdisplay/esp32 -e esp32s3
```

## Acceptance traceability

| Issue #36 acceptance criterion | Owning work packages | Required evidence |
| --- | --- | --- |
| Two different ESP32 display projects implement the same interface without shared board configuration | WP3, WP4 | Two adapter builds and shared contract tests with distinct capability profiles |
| MOBAflow receives capabilities and sends only compatible content | WP1, WP2, WP4, WP4.1, WP5 | Negotiation, compatibility rejection, replay regression, and ViewModel tests |
| Configured device displays the standard test pattern | WP1, WP4, WP4.1, WP5, WP7 | Golden pattern vector, adapter byte-order assertion, and current-hardware evidence |
| Invalid or incomplete frames are rejected without corrupted partial display | WP2, WP3, WP4, WP4.1, WP7 | Fake endpoint, firmware assembler, replay/anomaly tests, and hardware interruption evidence |
| Protocol and firmware versions are visible in diagnostics | WP1, WP4, WP5, WP7 | Diagnostic projection and UI/hardware evidence |
| Current MOBAdisplay firmware remains functional through the reference adapter | WP3, WP4, WP4.1, WP7 | PlatformIO build and ST7789 smoke test |
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
| Permanent legacy v0/v1.0 compatibility code increases complexity | Current | High | Remove the legacy path in WP4.1 and enforce repository-wide absence checks |
| WP4.1 reaches `main` before current-hardware acceptance | Medium | High | Keep WP4.1 isolated and reversible, require complete automated gates, and retain hardware acceptance as a hard issue-closure gate |
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
   - v1.0 device client, TFT_eSPI reference adapter, region transfer, structured responses.
5. `fix(display): correct replay handling and remove legacy transport`
   - reject only observed conflicting/replayed requests, remove host and firmware
     legacy paths, retain v1.0 golden-byte coverage;
   - automated quality gates are required before merge; hardware remains a
     final issue gate.
6. `feat(display): add configured-device diagnostics and test pattern`
   - settings, ViewModel, DisplayPage, diagnostics, accessibility, tests.
7. `ci(display): enforce display conformance gates`
   - permanent host, native firmware, and ESP32-S3 validation lanes plus
     diagnostic hardening.
8. `docs(display): finalize ESP32 display operation and validation`
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
