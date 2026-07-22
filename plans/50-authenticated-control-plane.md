# RF-03 Authenticated MOBApi Control Plane Implementation Plan

## Document status

- Status: Slices 1 through 4a merged; Slice 4b MOBAsmart security foundation ready for draft review
- Primary issue: https://github.com/ahuelsmann/MOBAflow/issues/50
- Parent programme: https://github.com/ahuelsmann/MOBAflow/issues/47
- Security design record: [MOBApi Security Design](../docs/MOBAPI-SECURITY-DESIGN.md)
- Status and acceptance criteria source: GitHub issue #50

## Purpose

This is the single implementation plan for RF-03. It sequences the work required to authenticate, authorize, validate, rate-limit, queue, and observe MOBApi control-plane traffic without combining the change into one broad server/client delivery.

Slice 1 delivered the security design record without runtime changes. Slice 2 delivered the additive authentication and credential foundation. Slice 3 migrated only the trusted MOBAflow host paths. Slice 4a added authenticated LAN discovery metadata, and Slice 4b now establishes the pinning and protected mobile credential foundation without yet enforcing authenticated reads.

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

## Current foundation and risks

- `MOBApi/Program.cs` maps all controllers and both hubs without an authentication or authorization default.
- Several host-only writes rely on source-address loopback checks, which identify a network path rather than a principal.
- `RuntimeHub` distinguishes host and remote connections through caller-selected registration methods and connection IDs, not credentials.
- REST fallback commands and SignalR commands do not share one authorization, validation, throttling, or admission boundary.
- `RuntimeCommandQueue` is an unbounded `ConcurrentQueue`; direct SignalR forwarding bypasses it.
- Locomotive validation exists in the Z21 backend (`address` 1-9999, `speed` 0-126, function F0-F31) but is not consistently applied before MOBApi admission.
- MOBAsmart persists a non-secret client ID in preferences and currently connects over HTTP without credentials.

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

### Slice 2: Authentication and credential foundation

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

### Slice 3: Host enrollment and protected publication paths

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

Planned boundary:

- explicit pairing UI flow and server-certificate fingerprint verification;
- secure MOBAsmart credential storage and recovery from missing or unreadable secure storage;
- authenticated read-only REST and SignalR paths;
- compatibility telemetry before anonymous reads are disabled.

This slice does not include unrelated MOBAsmart decomposition.

#### Slice 4a: Authenticated LAN discovery contract

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
- local agentic Sonar analysis was attempted against `github/main` and returned the organization
  capability error `Vortex agentic analysis is not available for this organization (403 Forbidden)`;
  the draft PR therefore remains gated on the remote SonarCloud result.

#### Slice 4b: MOBAsmart pinned pairing and credential foundation

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
- local agentic Sonar analysis against `github/main` was attempted but not authorized to export the
  private changed-file contents; the draft PR remains gated on a green remote SonarCloud check and
  zero open or confirmed PR findings.

### Slice 5: Unified control admission

Planned boundary:

- one platform-neutral command validator and one bounded admission service used by REST and SignalR;
- remote-control capability checks, per-credential token buckets, idempotency/replay handling, command deadlines, and explicit overflow responses;
- removal of direct SignalR-to-host command forwarding that bypasses admission;
- negative, replay, parity, throttling, ordering, reconnect, and overflow tests.

### Slice 6: Enforcement, operations, and cleanup

Planned boundary:

- anonymous-deny enforcement for all non-bootstrap operations;
- structured metrics, audit events, redaction tests, dashboards/runbook documentation, and operational thresholds;
- migration-code removal only after the compatibility window and evidence gates pass;
- load verification consumed by RF-18 and security coverage consumed by RF-09.

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

## Test strategy

Future implementation slices must add:

- unit tests for token validation, capability policies, rotation families, revocation, pairing expiry/single use, validation ranges, limiter partitions, queue admission, expiry, and telemetry redaction;
- integration tests covering `401` versus `403`, REST/SignalR parity, active-hub revocation, token expiry, refresh replay, reconnect, queue overflow, and migration modes;
- live or process-level tests for MOBAflow host bootstrap and MOBAsmart secure-storage recovery;
- load tests establishing that the proposed limits and queue capacity protect the runtime without impairing normal driving input.

Each implementation slice runs its focused tests, `dotnet build MOBApi/MOBApi.csproj`, and `dotnet test Test/Test.csproj`. Platform-client slices additionally run the documented Windows FastDebug and Android build lanes.

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

## Rollback strategy

- Slice 1 is documentation-only and can be reverted without runtime effect.
- Later slices keep authentication components additive until protected host and remote paths pass parity tests.
- Rollback may temporarily restore anonymous read-only access when the documented compatibility switch is enabled; anonymous control, host publication, pairing administration, and credential administration never have a rollback bypass.
- If the credential store, TLS identity, revocation checks, policy mapping, or bounded admission service fails, remote control remains disabled while local MOBAflow-to-Z21 operation continues.
- Each implementation slice must document the exact configuration and data changes it introduces and prove downgrade behavior before merge.

## Documentation and completion

- Keep delivery status and acceptance evidence in issue #50.
- Update `docs/MOBAPI-SECURITY-DESIGN.md` only through reviewed design changes; update current architecture and user guidance when implementation becomes available.
- Delete this standalone plan after issue #50 is completed and merged. The closed issue and Git history retain the implementation record.
