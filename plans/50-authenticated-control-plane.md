# RF-03 Authenticated MOBApi Control Plane Implementation Plan

## Document status

**GitHub Issue**: https://github.com/ahuelsmann/MOBAflow/issues/50
**Spec Kit**: Not applicable - RF-03 began before Spec Kit adoption and remains governed by this existing issue-specific sliced implementation plan.

- Status: Slices 1 through 4c merged; Slices 4d, 4e, 5, and 6 planned
- Parent programme: https://github.com/ahuelsmann/MOBAflow/issues/47
- Security design record: [MOBApi Security Design](../docs/MOBAPI-SECURITY-DESIGN.md)
- Status and acceptance criteria source: GitHub issue #50

## Purpose

This is the single implementation plan for RF-03. It sequences the work required to authenticate, authorize, validate, rate-limit, queue, and observe MOBApi control-plane traffic without combining the change into one broad server/client delivery or creating a second plan hierarchy.

Slice 1 delivered the security design record without runtime changes. Slice 2 delivered the additive authentication and credential foundation. Slice 3 migrated only the trusted MOBAflow host paths. Slice 4a added authenticated LAN discovery metadata, Slice 4b established the pinning and protected mobile credential foundation, and Slice 4c delivered the explicit fingerprint-verification and approval flow without yet enforcing authenticated reads.

## Scope boundaries

In scope:

- MOBApi REST and SignalR operations that observe or control MOBAflow runtime or hardware state;
- secure pairing and credential lifecycle for the MOBAflow host, MOBAsmart remote controls, and read-only clients;
- equivalent policy enforcement for REST and SignalR;
- boundary validation, per-client throttling, bounded command admission, security telemetry, migration, and rollback;
- negative, replay, reconnect, migration, throttling, and overflow verification.

Out of scope:

- implementation outside the explicitly named RF-03 slices;
- ESP32 provisioning credentials owned by RF-02;
- feature work from issues #30 through #36;
- broad `MauiViewModel` decomposition or unrelated MOBApi features;
- internet identity federation, multi-user accounts, or cloud control.

## Dependencies and consumers

- The security design and already-merged authentication foundation have no remaining hard delivery dependency.
- RF-09 consumes the security-critical regression and mutation surface.
- RF-12 consumes the authenticated mobile session, discovery, and credential ownership seams without reopening RF-03 policy decisions.
- RF-18 validates the configured rate limits, queue capacities, deadlines, and reconnect behavior under representative load.
- Product features that call MOBApi control paths must use the shared policies and admission module rather than add transport-specific authentication or command paths.

## Current foundation and remaining risks

- Slice 2 provides the protected server identity, device credential registry, token lifecycle,
  pairing endpoints, authentication middleware, and named capability policies.
- Slice 3 authenticates the per-launch MOBAflow host and protects host REST publication, command
  consumption, and SignalR registration.
- Slices 4a through 4c provide authenticated discovery, fingerprint pinning, protected mobile
  credential storage, and explicit user-approved pairing.
- Existing anonymous read paths remain available for the measured migration window and have not yet
  reached the Slice 4e deny-by-default end state.
- REST fallback commands and SignalR commands do not yet share one authorization, validation,
  throttling, idempotency, deadline, or admission seam.
- `RuntimeCommandQueue` remains unbounded and direct SignalR forwarding can bypass it until Slice 5.
- Locomotive validation exists in the Z21 backend (`address` 1-9999, `speed` 0-126, function F0-F31)
  but is not consistently applied before MOBApi admission.
- Compatibility telemetry, deterministic overload behavior, persistent idempotency, security-event
  retention, and migration cleanup remain open.

## Delivery sequence

### Slice 1: Security design record (complete)

Affected files:

- `plans/50-authenticated-control-plane.md` (this plan)
- `docs/MOBAPI-SECURITY-DESIGN.md` (new)

Deliverables:

- threat boundary and trust assumptions;
- pairing and authenticated transport decision;
- credential ownership, storage, issuance, rotation, revocation, expiry, and recovery rules;
- roles, capabilities, and an operation-policy matrix covering REST and SignalR;
- shared validation and command-admission boundary;
- concrete initial rate limits, bounded queue behavior, telemetry, privacy, and audit requirements;
- compatibility rollout, failure states, rollback conditions, and test matrix;
- explicit non-goals preventing feature-scope expansion.

Validation:

- every requirement in issue #50 maps to a design section and future verification;
- every existing REST controller and SignalR hub method is classified as public bootstrap, read-only, remote-control, or host-only;
- REST/SignalR equivalents use the same named capability and admission path;
- no production code, configuration, package, schema, or client behavior changes are present;
- Markdown links and repository scope are valid.

### Slice 2: Authentication and credential foundation (complete; PR #62)

Current boundary:

- protected server identity and credential registry;
- short-lived access tokens plus rotating device refresh credentials;
- pairing, refresh, rotation, and revocation endpoints;
- authentication middleware and named capability policies ready for deny-by-default migration;
- focused unit and integration tests, with no control-path migration yet.

Implementation decisions:

- use an ASP.NET Core authentication scheme backed by purpose-isolated Data Protection rather than introducing an external token package;
- issue five-minute opaque bearer access tokens containing credential identity, role, capability version, and capabilities;
- validate every access token against live credential state so revocation and role changes invalidate already-issued tokens;
- persist only protected registry and server-identity documents, and store refresh credentials as keyed hashes;
- rotate refresh credentials on every successful exchange and revoke the credential family when a consumed refresh credential is replayed;
- keep one in-memory, two-minute pairing window with explicit approval, single-use claims, bounded failed attempts, and cooldown;
- require HTTPS for every endpoint that accepts or returns a pairing or refresh secret;
- register named capability policies without applying them to existing runtime controllers or hubs in this slice.

Explicit exclusions:

- no fallback policy and no authorization attributes on existing runtime controllers or hubs;
- no Kestrel transport migration, host bootstrap, MOBAsmart secure-storage work, or client changes;
- no command validation, rate limiting, bounded command admission, telemetry expansion, or feature work from issues #30 through #36.

Validation evidence:

- thirteen focused security tests cover capability templates, named policies, query-token path restrictions, token expiry, live role/revocation invalidation, refresh rotation/replay/inactivity, protected storage, pairing approval/single claim/cooldown, and server-identity persistence;
- `dotnet build MOBApi/MOBApi.csproj -c Release --no-restore` passes with zero warnings and zero errors;
- `dotnet test Test/Test.csproj --no-restore` passes on both test targets with 2,282 passed, 4 skipped, and 0 failed tests;
- scoped `dotnet format ... whitespace --verify-no-changes` checks and `git diff --check` pass for all changed C# files.

Storage and rollback notes:

- the registry and ECDSA P-256 server identity are written as purpose-protected documents below `%LOCALAPPDATA%/MOBAflow/security` by default; `ControlPlaneSecurity:StorageDirectory` provides an environment-specific override;
- this slice adds no committed secret, database schema, client credential, or Kestrel binding change;
- rollback removes the additive endpoints, services, and middleware registration. Existing runtime controllers and hubs continue to use their prior behavior because no control-path authorization attribute or fallback policy is enabled yet;
- protected local files may be retained for re-upgrade or removed by an operator after rollback because no existing runtime path reads them.

### Slice 3: Host enrollment and protected publication paths (complete; PR #65)

Current boundary:

- per-launch MOBAflow host bootstrap over loopback using an injected one-time secret;
- host capability enforcement for solution, runtime settings, snapshots, command consumption, and host hub registration;
- removal of loopback as the sole authorization decision;
- host reconnect, token refresh, restart, and rollback tests.

Implementation decisions:

- create a fresh 256-bit bootstrap secret for each MOBApi child launch and transfer it through an inherited anonymous-pipe handle; command-line arguments, committed configuration, and ordinary environment variables never carry the secret;
- bind a dedicated HTTPS loopback endpoint alongside the unchanged HTTP LAN endpoint, using the protected server identity introduced in Slice 2;
- return the expected server-certificate fingerprint through the bootstrap channel so MOBAflow can pin the HTTPS endpoint before sending the secret;
- keep the host access token and rotating renewal credential in memory in both processes; they are not added to the persistent device registry and become invalid when the MOBApi process or host session ends;
- require `host.publish` for solution, runtime-settings, and snapshot writes, and `host.consume` for command consumption and host hub registration;
- retain loopback checks as an additional transport constraint, never as an authorization substitute;
- supply the same bearer token to host REST requests and SignalR through a refresh-aware in-memory session service;
- do not reuse a separately started MOBApi process as an implicitly trusted host: without the inherited bootstrap channel, host publication remains disabled.

Validation evidence:

- `dotnet build MOBApi/MOBApi.csproj -c Release --no-restore` passes with zero warnings and zero errors;
- `dotnet test Test/Test.csproj --no-build --disable-build-servers -m:1` passes with 2,292 tests and 4 expected skips across both targets;
- five focused host credential tests cover single-use bootstrap, bounded failed attempts, renewal replay revocation, process-restart invalidation, and shared REST/SignalR capability attributes;
- `git diff --cached --check` passes; scoped formatter normalization was applied to changed files. The unscoped MOBApi formatter still reports pre-existing final-newline issues in untouched files.

Explicit exclusions:

- no remote/MOBAsmart pairing or secure-storage migration and no anonymous-read enforcement;
- no unified command validation, rate limiting, bounded admission queue, or telemetry expansion;
- no feature work from issues #30 through #36.

Validation targets:

- wrong, expired, replayed, non-loopback, and non-HTTPS bootstrap attempts fail closed;
- renewal rotates on every use and replay or host-session termination invalidates the host credential family;
- protected REST actions and SignalR host registration reject missing or insufficient capabilities with matching policies;
- SignalR reconnect and access-token renewal preserve host operation without persisting secrets;
- MOBApi restart generates a new bootstrap secret and invalidates prior host credentials;
- rollback removes the dedicated host endpoint and host authorization attributes while leaving the Slice 2 protected identity and device registry compatible.

### Slice 4: Remote pairing and read-only migration

Overall boundary:

- explicit pairing UI flow and server-certificate fingerprint verification;
- secure MOBAsmart credential storage and recovery from missing or unreadable secure storage;
- authenticated read-only REST and SignalR paths;
- compatibility telemetry before anonymous reads are disabled.

This slice does not include unrelated MOBAsmart decomposition.

#### Slice 4a: Authenticated LAN discovery contract (complete; PR #81)

This independently reviewable prerequisite exposes the persistent MOBApi server instance,
LAN HTTPS port, and SHA-256 public-key fingerprint through discovery. The version 2 response
retains the legacy IP and HTTP port fields so existing clients continue to work during the
compatibility window. MOBApi serves the persistent certificate on the LAN HTTPS endpoint;
discovery remains untrusted candidate metadata until a client verifies the pinned fingerprint.

Pairing UI, MOBAsmart secure credential storage, authenticated client requests, and anonymous-read
enforcement remain in the following Slice 4 deliveries. No credential is transmitted by this slice.

Validation evidence:

- version 2 discovery parsing, legacy fallback, invalid identity metadata, persistent instance ID,
  and host bootstrap metadata are covered by 40 focused tests;
- the full test suite passes for both targets with 2,869 tests passed and four expected skips;
- `dotnet build MOBApi/MOBApi.csproj -c Release --no-restore` and the documented MOBAflow
  FastDebug compile check pass with zero warnings and zero errors;
- changed-file secret scanning reports zero issues;
- PR #81 is merged; all required checks, including SonarCloud, passed and the PR had zero open or
  confirmed Sonar findings.

#### Slice 4b: MOBAsmart pinned pairing and credential foundation (complete; PR #84)

This additive client slice consumes only version 2 discovery metadata for authenticated setup,
pins MOBApi HTTPS to the advertised SHA-256 subject-public-key fingerprint, and provides the
pairing/claim/refresh state machine required by a later explicit UI flow. Refresh credentials are
stored through MAUI `ISecureStorage`; access tokens and pending claim secrets remain process-only.
Refresh rotation persists the successor before the in-memory access session changes, and a rejected
stored credential is cleared so the client returns to an unpaired state.

Android backup and device-transfer rules keep the MAUI SecureStorage shared-preference file outside
the file-only backup allowlist. Legacy discovery and anonymous read paths remain available during
this slice; they cannot be used to create a pinned session, and no existing runtime client starts
sending bearer credentials yet.

Validation evidence:

- certificate-pin matching, legacy-discovery rejection, nonce generation, pending approval,
  protected refresh storage, atomic rotation failure, revocation recovery, and diagnostic redaction
  are covered by 11 focused platform-neutral tests;
- the full test suite passes for both targets with 2,891 tests passed and four expected skips;
- the documented MOBAsmart FastDebug Android compile check passes with zero errors without starting
  the app; its 45 existing XAML compiled-binding warnings remain unchanged;
- `dotnet format --verify-no-changes` passes for all changed C# files, and changed-file secret
  scanning reports zero issues;
- PR #84 is merged; all required checks, including Windows coverage, Android AAB, mutation, CodeQL,
  and SonarCloud, passed and the PR had zero open or confirmed Sonar findings.

#### Slice 4c: Explicit MOBAsmart pairing and approval UI (complete; PR #86)

This independently reviewable UI slice adds a dedicated MOBAsmart Pairing tab backed by a
platform-neutral `RemotePairingViewModel`. Discovery exposes the candidate HTTPS endpoint, persistent
server instance, and formatted SHA-256 public-key fingerprint. The user must explicitly confirm that
the fingerprint matches MOBAflow before the 43-character pairing secret can be submitted.

The client requests either remote-control or read-only capability, clears the submitted secret from
the bound UI state, displays the server-generated confirmation code, and polls the protected claim
endpoint until MOBAflow approves, rejects, or the bounded wait expires. Successful pairing exposes
only the granted role; access and refresh tokens never enter the ViewModel or XAML. The same surface
can remove the locally protected credential to recover to an unpaired state.

Authenticated REST and SignalR reads, compatibility telemetry, and anonymous-read enforcement remain
following Slice 4 deliveries. Existing runtime communication is deliberately unchanged.

Validation evidence:

- six focused ViewModel tests cover explicit fingerprint confirmation, pending-to-approved polling,
  remote-control and read-only roles, rejected claims, protected credential restoration, local
  credential removal, and diagnostic redaction;
- the full test suite passes for both targets with 2,907 tests passed and four expected skips;
- the MOBAsmart FastDebug Android compile passes with zero errors without starting the app; the 45
  existing XAML compiled-binding warnings remain unchanged and no warning points to the pairing UI;
- scoped `dotnet format --verify-no-changes`, `git diff --check`, and changed-file secret scanning
  pass;
- PR #86 is merged; all required checks, including SonarCloud, passed and the PR had zero open or
  confirmed Sonar findings.

#### Slice 4d: Authenticated REST and SignalR reads

Planned boundary:

- migrate every non-bootstrap REST read and SignalR read/subscription path to an authenticated
  session;
- apply the same named read capability and live credential-state checks to transport-equivalent
  operations;
- send and refresh the authenticated session from MOBAsmart without exposing access or refresh
  credentials to ViewModels, XAML, URLs outside the SignalR token transport requirement, or logs;
- close active SignalR connections immediately when a credential is revoked, downgraded, or
  expired, while retaining a live credential and capability check for every hub invocation;
- preserve the existing anonymous read compatibility path only until Slice 4e completes its
  readiness gate;
- keep connection loss side-effect free: a REST timeout or SignalR disconnect never creates an
  implicit drive, stop, or emergency-stop command.

Validation:

- authenticated REST reads and SignalR subscriptions return equivalent data under the same
  capability;
- anonymous, expired, rotated, revoked, and downgraded credentials produce the documented
  transport-neutral outcomes;
- refresh, reconnect, server restart, secure-storage loss, and active-session revocation tests pass;
- read-only credentials cannot invoke any control, host publication, pairing-administration, or
  credential-administration operation;
- detailed diagnostics require at least read-only capability and administrative security details
  remain host-only;
- rollback restores the compatibility read path without weakening any control or administration
  policy.

Implementation checkpoint (2026-07-31; local Slice 4d pre-publication gates complete):

- MOBAsmart now obtains, refreshes, and sends an endpoint-bound access session through the pinned
  HTTPS transport for runtime settings, solution/runtime snapshots, photos, presence registration,
  photo uploads, and REST command fallback; SignalR negotiation and reconnect use the same session
  without exposing credentials to ViewModels, XAML, ordinary URLs, or logs;
- read routes and subscriptions use the shared read policy while the Slice 4d anonymous compatibility
  path remains available; any presented token is live-validated and malformed, expired, revoked, or
  downgraded credentials cannot fall through as anonymous;
- control, photo-write, presence, host-publication, host-consumption, and security-management routes
  use the designed named capabilities; presence ownership is derived from the authenticated
  credential rather than caller-selected client IDs;
- active authenticated hubs register for immediate abort on revocation, capability change, refresh
  replay, or access-token expiry, and every protected invocation re-evaluates live credential state;
- the new process-level test pairs a real read-only client with a child MOBApi process and proves
  equivalent authenticated REST and SignalR snapshots before and after the same SignalR connection
  automatically reconnects across a complete server restart with persisted identity, credential
  refresh, and no hardware access;
- the client recovery test proves that a missing secure-storage credential cannot restore or
  refresh a session and that an explicit replacement pairing stores a new credential;
- the 20-test focused client/process gate set passes; the complete `net10.0` target passes with
  1,520 tests and four expected skips, and the Windows target passes with 1,573 tests; MOBApi Release and MOBAsmart
  FastDebug Android builds pass with zero errors, with the 45 existing XAML binding warnings
  unchanged;
- scoped format checks, `git diff --check`, Spec Kit governance, and the 38-file Sonar secrets scan
  pass; the worktree is fast-forwarded to `github/main` at `f9c141a1`;
- the authenticated local Sonar branch analysis was attempted against `github/main` and reported
  zero secret or code findings, but its Vortex analysis failed for all 38 files because that feature
  is unavailable to the SonarCloud organization (`403 Forbidden`); this is recorded as unavailable,
  not green, and the remote SonarCloud check remains mandatory;
- draft PR #110 was published; its first two remote Sonar analyses reported six open findings in total,
  including one insecure legacy HTTP URL builder, and each finding was corrected locally without suppression;
- the remaining Slice 4d publication gate is the updated remote analysis with green required checks
  and zero open or confirmed Sonar findings. Optional real-device and hardware evidence remains
  non-blocking under the issue-wide acceptance rule.

#### Slice 4e: Compatibility telemetry and anonymous-read enforcement

Planned boundary:

- record pseudonymous compatibility and authenticated-read outcomes without credentials, tokens,
  pairing secrets, full payloads, or hardware state in metric labels;
- require at least one stable authenticated client release followed by fourteen consecutive days
  without a critical authentication, refresh, reconnect, or REST/SignalR read-parity defect;
- restart the fourteen-day readiness window after the fix for any critical defect;
- disable anonymous reads only after the readiness evidence is recorded in issue #50;
- return one structured, machine-readable upgrade-required outcome to legacy REST and SignalR
  clients instead of silently falling back to anonymous access;
- retain only a manually activated anonymous read-only rollback switch that expires automatically
  after at most seven days;
- emit a startup warning, audit event, and operational metric while the rollback switch is active;
- never allow the rollback switch to enable anonymous control, host publication, pairing
  administration, or credential administration.

Validation:

- the compatibility window cannot complete from elapsed time alone when authenticated traffic is
  absent or critical defects remain unresolved;
- rollback activation, expiry, restart behavior, warnings, audit events, and metrics are tested;
- legacy clients receive the same upgrade-required reason across REST and SignalR;
- the anonymous health response exposes only minimal reachability and no version, server identity,
  runtime, hardware, client, or security state;
- disabling anonymous reads leaves pairing bootstrap and the minimal health surface available;
- rollback and downgrade evidence identifies every persisted or migrated value and proves the
  expected fail-closed behavior.

Implementation checkpoint (2026-08-01):

- Slice 4e code records bounded, process-local pseudonymous outcome counters and low-cardinality
  metrics. Metric labels contain only transport and outcome; client pseudonyms are limited to 64
  current-process buckets plus one overflow bucket and are never persisted.
- `read-migration.dat` is a data-protected document under the existing control-plane security
  storage directory. It persists only the stable client release, observation start, one REST and
  one SignalR traffic marker, open critical-defect codes, the issue-comment evidence reference,
  enforcement time, rollback expiry, and the last fixed-defect audit fields. Normal reads use a
  cached decision and do not write the file; only the first matching REST and SignalR observations
  advance persisted readiness evidence.
- MOBAsmart sends `X-MOBAflow-Client-Release` on authenticated REST and SignalR traffic. Readiness
  advances only when that value exactly matches the manually selected stable release and both
  transports have been observed. Open critical defects block enforcement and cannot be cleared by
  starting a new window; fixing one restarts the full fourteen-day window.
- The rollback is read-only, manually activated, persisted, capped at seven days, and restored on
  restart. Activation emits an audit warning immediately; startup emits another warning while the
  rollback is active; the gauge returns to zero automatically at expiry.
- A missing, unreadable, or cryptographically invalid migration document fails closed in the new
  server because authorization cannot establish a compatibility decision. The document is retained
  across source rollback. Binary downgrade to a pre-Slice-4e MOBApi is prohibited after enforcement:
  that older binary does not understand the document and would restore its historical anonymous-read
  behavior. Reverting source alone is therefore not an approved rollback procedure.
- This checkpoint does not satisfy the operational gate. Issue #50 currently has no Slice 4e
  fourteen-day evidence comment, no stable-release observation window has been completed, and
  authenticated-only reads must remain disabled until those external gates are recorded.

### Slice 5: Unified control admission

Planned boundary:

- one deep, platform-neutral `CommandAdmission` module is the sole seam for every REST and SignalR
  hardware command;
- controller actions and hub methods adapt authenticated transport inputs only; capability checks,
  validation, rate limiting, idempotency, deadlines, ordering, and bounded queue admission remain
  behind the module's small interface;
- remove direct SignalR-to-host forwarding and every REST fallback path that bypasses admission;
- require a client-generated command ID for every control command and deduplicate it per credential;
- persist a protected, bounded idempotency history before hardware dispatch so a restart cannot
  cause automatic duplicate execution;
- report a command that was in flight during an indeterminate process failure as `OutcomeUnknown`;
  clients must refresh state and issue a conscious new command with a new ID rather than retry it
  automatically;
- return one transport-neutral result taxonomy: `Accepted`, `Unauthenticated`, `Forbidden`,
  `Invalid`, `Duplicate`, `Throttled`, `QueueFull`, `Expired`, `OutcomeUnknown`, `Unavailable`, and
  `Failed`;
- reject new normal commands immediately when the queue is full; never wait at ingress or silently
  discard a command that was accepted;
- assign a monotonic sequence when a normal command is accepted and execute normal commands in one
  deterministic client-independent FIFO order;
- treat acceptance as a guarantee of a recorded terminal outcome, not unconditional execution:
  stale queued commands finish as `Expired` and never reach hardware;
- reserve a separate bounded priority lane for authenticated and authorized emergency stop
  commands without bypassing validation or audit;
- coalesce semantically identical pending emergency stops into one hardware action while retaining
  one audit record and observable outcome per caller;
- enforce both per-credential and global token buckets and per-credential outstanding-command
  limits;
- preserve local MOBAflow-to-Z21 control if remote security configuration is invalid, while remote
  admission remains fail-closed.

Validation:

- a mandatory operation matrix declares capability, validator, rate-limit class, deadline class,
  idempotency behavior, and result mapping for every REST action and SignalR hub method;
- unclassified control operations cannot be published;
- the same contract tests exercise transport-equivalent REST and SignalR operations through the
  admission interface;
- negative, replay, parity, throttling, ordering, reconnect, saturation, expiry, emergency-stop,
  process-restart, and overflow tests pass;
- no invalid command consumes queue capacity or reaches `IMobaRuntime`;
- no accepted command disappears without a terminal outcome;
- RF-18 load verification confirms or tightens the defaults before completion.

### Slice 6: Enforcement, operations, and cleanup

Planned boundary:

- anonymous-deny enforcement for all non-bootstrap operations;
- structured metrics, audit events, redaction tests, dashboards/runbook documentation, and
  operational thresholds;
- local host-only management for security settings, credential revocation, and a strongly
  confirmed complete control-plane security reset;
- capability increases, including read-only to remote-control promotion, require a new explicit
  local approval; capability reductions and revocation take effect immediately;
- device credentials cannot be exported, restored, or transferred; secure-storage loss,
  reinstallation, or device replacement requires a new pairing;
- a full security reset revokes every session, discards the server identity and all device
  credentials, generates a new fingerprint, and requires complete re-pairing;
- remove migration code only after Slice 4e evidence and rollback-expiry gates pass;
- load verification consumed by RF-18 and security coverage consumed by RF-09.

Completion evidence:

- every acceptance criterion from issue #50 maps to an automated, process-level, or build
  verification result;
- optional real Android, MOBAflow, MOBApi, Z21, and layout-hardware checks are documented as
  best-effort evidence only and do not block a slice, release, or issue closure;
- architecture and user guidance describe pairing, roles, recovery, upgrade-required behavior,
  configuration, audit retention, and reset consequences;
- every temporary compatibility path and switch has been removed or has an explicitly recorded
  expiry and owner.

## Operational defaults and hard limits

The Settings page is a local host adapter for these values. Remote-control and read-only clients
cannot modify them. All user-visible labels, validation messages, and restart notices are English.
Options validation enforces safe lower bounds and the non-configurable hard limits below.

| Setting | Default | Hard limit |
| --- | --- | --- |
| Normal commands per credential | 20/second, burst 40 | 50/second, burst 100 |
| Normal commands globally | 100/second, burst 200 | 200/second, burst 400 |
| Normal command queue | 64 | 256 |
| Outstanding normal commands per credential | 16 | 64 |
| Emergency-stop deadline | 1 second | 2 seconds |
| Drive, direction, and function deadline | 2 seconds | 5 seconds |
| Accessory, signal, and other layout-command deadline | 5 seconds | 30 seconds |
| REST reads per credential | 10/second, burst 20 | 50/second, burst 100 |
| Pending SignalR state messages per client | 32 | 256 |
| Active device credentials | 16 | 64 |
| Device-credential inactivity | 90 days | 365 days |
| Device-credential family lifetime | 365 days | 365 days |
| Idempotency history | 24 hours or 10,000 IDs | 7 days or 100,000 IDs |
| Local security-event retention | 30 days or 100 MiB | 365 days or 1 GiB |
| Anonymous read-only rollback | Disabled | 7 days per manual activation |

- Rate limits, logical queue capacity, per-client outstanding limits, deadlines, read limits, and
  SignalR buffer limits update atomically through a versioned configuration snapshot and apply to
  new admissions without restarting MOBApi.
- Shrinking a limit never discards accepted work. New admissions are rejected until usage falls
  below the new limit.
- TLS identity, protected-store location, cryptographic purpose changes, and other structural
  security settings require a controlled MOBApi restart.
- An invalid or unreadable remote-security configuration disables remote control fail-closed and
  surfaces an actionable local error without disabling local MOBAflow-to-Z21 operation.
- Active device credentials expire after ninety days without a successful refresh and after one
  year regardless of activity.
- The idempotency store never evicts a non-terminal or `OutcomeUnknown` entry to make room. If safe
  persistence is unavailable or full, admission fails closed.
- Local security events contain only a pseudonymous credential identifier, operation, outcome,
  reason, and relevant limit category. Invalid input is recorded by field and error class, never as
  a raw secret, complete payload, or metric label.
- When the SignalR state buffer is full, replaceable updates coalesce by state key so the latest
  state wins. Audit and security events never coalesce; a persistently slow client is disconnected.

## Required implementation invariants

1. Authentication is deny-by-default. Only the minimal health and time-bounded pairing bootstrap surface may be anonymous.
2. Authorization uses named capabilities, not controller-specific role guesses or source IP addresses.
3. REST actions and SignalR hub methods representing the same operation use the same capability, validator, limiter partition, and command-admission service.
4. Revocation and capability downgrades affect active SignalR sessions, not only new HTTP requests.
5. Credentials and tokens never appear in source, configuration committed to Git, URLs outside the SignalR framework requirement, exception details, structured log properties, or metrics labels.
6. Invalid commands are rejected before they can consume queue capacity or reach `IMobaRuntime`.
7. Command queues are bounded, preserve a documented order, never silently discard an accepted command, and expose deterministic overload results.
8. Migration is fail-closed for control operations and reversible through a time-bounded compatibility switch for reads only.
9. Security-sensitive settings are validated at startup; an invalid protected store or missing server identity prevents remote control from starting.
10. Each slice is independently testable, reviewable, and rollback-capable.
11. An accepted command always reaches one observable terminal outcome; acceptance never means that
    an expired or indeterminate command must be sent to hardware.
12. A client disconnect never creates an implicit hardware command.
13. Operational setting changes apply atomically to new admissions and never mutate the rules used
    for a command already admitted.
14. Device credentials are installation-bound, non-exportable, immediately revocable, inactive
    after ninety days without refresh, and absolutely expired after one year.
15. Capability elevation always requires explicit local approval; no remote caller can grant or
    widen its own capabilities.

## Test strategy

Future implementation slices must add:

- unit tests for token validation, capability policies, rotation families, revocation, pairing expiry
  and single use, validation ranges, limiter partitions, queue admission, deadline expiry,
  configuration snapshots, hard limits, idempotency retention, and telemetry redaction;
- operation-matrix contract tests that apply the same authentication, authorization, validation,
  limiter, deadline, idempotency, and result expectations to REST and SignalR equivalents;
- integration tests covering `401` versus `403`, active-hub revocation, token expiry, refresh replay,
  reconnect, throttling, global and per-client saturation, queue overflow, deterministic FIFO,
  emergency-stop priority and coalescing, process restart, `OutcomeUnknown`, and migration modes;
- process-level tests for MOBAflow host bootstrap, MOBAsmart secure-storage recovery, legacy-client
  upgrade responses, anonymous-read rollback expiry, and complete control-plane security reset;
- load tests establishing that the defaults and hard limits protect the runtime without impairing
  normal driving input; RF-18 may tighten, but must not silently raise, the agreed limits;
- optional manual checks with real Android, Z21, and layout hardware when available. Missing manual
  hardware evidence is recorded but is not a merge, release, or issue-closing blocker.

Each implementation slice:

1. runs its focused tests, `dotnet build MOBApi/MOBApi.csproj`, and
   `dotnet test Test/Test.csproj`;
2. runs the documented Windows FastDebug or Android build lanes when it changes those clients,
   without launching MOBAflow unless separately approved;
3. scans every changed file with `sonar analyze secrets`;
4. attempts local Sonar analysis against the actual pull-request base;
5. is published only as a draft pull request;
6. requires a green remote SonarCloud result and zero `OPEN` or `CONFIRMED` pull-request findings
   before review readiness;
7. documents exact persistence, configuration, rollback, and downgrade behavior.

A missing, unauthenticated, or failed secrets or Sonar check is blocked evidence, never a clean
result. No gate may be weakened, suppressed, or bypassed to advance a slice.

## Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| A copied bearer token controls hardware | TLS with paired server identity, short access-token lifetime, rotating refresh credentials, immediate registry checks, and revocation |
| SignalR keeps stale claims after revocation | Close connections at token expiry and re-check live credential state/capabilities for every hub invocation |
| Pairing secret is guessed or replayed | Cryptographic single-use secret, short expiry, attempt throttling, explicit local approval, and replay telemetry |
| SignalR and REST drift | Shared named policies, validators, limiter keys, admission service, and contract tests |
| Queue overload delays stale hardware commands | Bounded capacity, zero wait at ingress, command deadlines, per-client outstanding limits, and explicit rejection |
| Migration locks out existing installations | Measured read-only compatibility window, explicit pairing readiness UI, staged enforcement, and rollback switch |
| Compatibility switch becomes a permanent bypass | No anonymous control mode, startup warning and metric, expiry date, and removal gate in Slice 6 |
| Secrets leak through logs or backups | Redaction tests, token fingerprints only, protected stores, Android backup exclusion, and documented recovery |
| Several paired clients bypass per-client throttling together | Global token bucket, global queue capacity, and RF-18 load verification |
| A process fails after hardware dispatch but before result persistence | Protected pre-dispatch idempotency record, `OutcomeUnknown`, state refresh, and no automatic retry |
| A local setting effectively disables protection | Validated lower bounds, immutable hard limits, atomic snapshots, and auditable changes |
| A slow SignalR client causes unbounded memory growth | Bounded per-client buffer, latest-state coalescing, and controlled disconnect |
| A security reset is triggered accidentally | Local-only access, strong confirmation, explicit consequence text, and an audit event |

## Rollback strategy

- Slice 1 is documentation-only and can be reverted without runtime effect.
- Later slices keep authentication components additive until protected host and remote paths pass parity tests.
- Slice 4d retains the compatibility read path until Slice 4e records its readiness evidence.
- Slice 4e rollback may restore anonymous read-only access only through the documented manual switch
  for at most seven days; anonymous control, host publication, pairing administration, and
  credential administration never have a rollback bypass.
- Slice 5 rollback disables remote admission fail-closed, preserves the protected idempotency
  history for diagnosis and duplicate prevention, and leaves local MOBAflow-to-Z21 operation
  available. It never re-enables a direct REST or SignalR hardware bypass.
- If the credential store, TLS identity, revocation checks, policy mapping, or bounded admission service fails, remote control remains disabled while local MOBAflow-to-Z21 operation continues.
- Each implementation slice must document the exact configuration and data changes it introduces and prove downgrade behavior before merge.
- A downgrade must define how newer protected records are retained, ignored, migrated, or removed;
  reverting the source change alone is not sufficient evidence.

## Documentation and completion

- Keep delivery status and acceptance evidence in issue #50.
- Keep the operation matrix, defaults, hard limits, migration window, rollback activation, and
  expiry evidence linked from issue #50.
- Update `docs/MOBAPI-SECURITY-DESIGN.md` only through reviewed design changes; update current
  architecture and user guidance when implementation becomes available.
- Document the English Settings page fields, safe ranges, live-apply behavior, restart-required
  structural settings, credential expiry, non-exportability, upgrade-required recovery, and full
  reset consequences.
- Delete this standalone plan after issue #50 is completed and merged. The closed issue and Git history retain the implementation record.
