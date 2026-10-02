# Research: Remove the MOBApi control-plane security

**GitHub Issue**: #165

## Findings from the current code (main at 4f159f1b)

- Every MOBApi controller action and hub method carries a capability policy; only the pairing, token
  and host-enrollment endpoints are anonymous. MOBAsmart cannot read or control without pairing.
- MOBApi listens on an HTTP port and a separate HTTPS port with a persistent server identity.
  MOBAflow starts MOBApi with a named-pipe bootstrap channel and talks to it through
  `HostControlPlaneSession` (pinned HTTPS plus host token).
- Command validation checks only required fields on REST. SignalR hub methods forward drive and
  function commands to the host without any value check. `RuntimeCommandQueue` is an unbounded
  `ConcurrentQueue`. Neither protection named in the scope decision exists yet.
- ZXing is used only for the pairing QR code (MOBAflow generation, MOBAsmart scanning). The Android
  camera permission is also used by the locomotive photo capture and therefore stays.
- LAN discovery (multicast, subnet probe, recently found addresses first) exists in MOBAsmart and
  predates pairing. MOBAsmart has no manual address entry; pairing was the only other way to supply an
  address.
- `JourneyProgressController` contains a loopback check outside the security folder.
- `ClientsController.Register/Unregister` and `RuntimeHub.RegisterRemote` derive the client identity
  from the credential claim; without authentication they must use the request `ClientId` instead.
- `Test/Integration/AuthenticatedControlPlaneProcessTests.cs` also holds the only real-process health,
  discovery-contract and server-restart/SignalR-reconnect coverage; these cases are kept without
  credentials when the fixture is replaced.
- The README, `docs/PROJECT-REFERENCE.md`, the MOBAflow user guide and the security design record
  describe the read migration; they change in the slice that removes it.

## Decisions

### D1: Remove all authentication and authorization at once

- **Decision**: Remove authentication middleware, capability policies, pairing, tokens, credential
  storage, host enrollment, host bootstrap, server identity, certificate pinning and the read migration
  in one coherent slice across MOBApi, MOBAflow and MOBAsmart.
- **Rationale**: The HTTPS listener and pinned clients depend on each other; a partial removal leaves
  a build in which clients cannot reach the server.
- **Alternatives considered**: Server first, clients later (rejected: the intermediate state cannot
  connect); keeping HTTPS without pinning (rejected: adds certificate handling without purpose).

### D2: No source-address restrictions

- **Decision**: Remove every loopback check, including the one in `JourneyProgressController`.
- **Rationale**: Clarification 1 of the spec; every device in the home network is trusted.
- **Alternatives considered**: Loopback-only host operations (rejected by the maintainer).

### D3: One command admission step for REST and SignalR

- **Decision**: Add a validator for `RuntimeCommandEnvelope` in `Common/Runtime` and one admission
  service in MOBApi that validates every remote command before forwarding it to the host or queueing
  it. Both transports build the envelope first and call the admission service.
- **Rationale**: SignalR currently bypasses all checks. One place keeps REST and SignalR equivalent
  and is easy to test. `Common/Runtime` already owns the envelope, and MOBApi must not reference
  `Backend`.
- **Limits**: Locomotive address 1-9999, speed 0-126, function index 0-31, non-empty GUIDs for signal
  and journey identifiers, defined enum values. These match the Z21 backend's argument checks. All
  fields have fixed-size types, so no payload-size limit is added; framework message limits stay.
- **Ordering**: With a connected runtime host, admitted SignalR commands are forwarded directly in
  arrival order; the bounded queue applies only to commands admitted while no host is connected.
- **Alternatives considered**: Data annotations on request models (rejected: SignalR arguments are not
  model-bound); reusing Backend limits directly (rejected: layer rule).

### D4: Bounded queue with immediate rejection

- **Decision**: Replace the unbounded queue with a bounded queue of 128 commands. A full queue
  rejects the command immediately: REST returns `429 Too Many Requests` with `queue_full`, SignalR
  throws a `HubException`.
- **Rationale**: Capacity and non-blocking behavior follow the withdrawn RF-03 design; no rate
  limiting, per-client partitioning or retry headers are added.
- **Alternatives considered**: Dropping the oldest command (rejected: silently loses accepted commands).

### D5: Delivery slices

1. Command admission (validator, admission service, bounded queue) on the current code. Additive and
   independently releasable; protects behavior before the removal.
2. Remove the read migration and the GitHub issue evidence verifier on the server, together with the
   documentation that describes them.
3. Remove the remaining control-plane security across all hosts, including UI, settings, tests,
   analyzer-baseline entries, packages and documentation.
