# RF-03 Authenticated MOBApi Control Plane Implementation Plan

## Document status

- Status: Slice 1 design complete - implementation pending
- Primary issue: https://github.com/ahuelsmann/MOBAflow/issues/50
- Parent programme: https://github.com/ahuelsmann/MOBAflow/issues/47
- Security design record: [MOBApi Security Design](../docs/MOBAPI-SECURITY-DESIGN.md)
- Status and acceptance criteria source: GitHub issue #50

## Purpose

This is the single implementation plan for RF-03. It sequences the work required to authenticate, authorize, validate, rate-limit, queue, and observe MOBApi control-plane traffic without combining the change into one broad server/client delivery.

The current delivery is Slice 1 only: a security design record. It changes no REST, SignalR, MOBAflow, or MOBAsmart runtime behavior. Slices 2 through 6 have not started.

## Scope boundaries

In scope:

- MOBApi REST and SignalR operations that observe or control MOBAflow runtime or hardware state;
- secure pairing and credential lifecycle for the MOBAflow host, MOBAsmart remote controls, and read-only clients;
- equivalent policy enforcement for REST and SignalR;
- boundary validation, per-client throttling, bounded command admission, security telemetry, migration, and rollback;
- negative, replay, reconnect, migration, throttling, and overflow verification.

Out of scope:

- implementation in this design-only slice;
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

### Slice 1: Security design record (current delivery)

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

Planned boundary:

- protected server identity and credential registry;
- short-lived access tokens plus rotating device refresh credentials;
- pairing, refresh, rotation, and revocation endpoints;
- deny-by-default authentication middleware and named capability policies;
- focused unit and integration tests, with no control-path migration yet.

### Slice 3: Host enrollment and protected publication paths

Planned boundary:

- per-launch MOBAflow host bootstrap over loopback using an injected one-time secret;
- host capability enforcement for solution, runtime settings, snapshots, command consumption, and host hub registration;
- removal of loopback as the sole authorization decision;
- host reconnect, token refresh, restart, and rollback tests.

### Slice 4: Remote pairing and read-only migration

Planned boundary:

- explicit pairing UI flow and server-certificate fingerprint verification;
- secure MOBAsmart credential storage and recovery from missing or unreadable secure storage;
- authenticated read-only REST and SignalR paths;
- compatibility telemetry before anonymous reads are disabled.

This slice does not include unrelated MOBAsmart decomposition.

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
