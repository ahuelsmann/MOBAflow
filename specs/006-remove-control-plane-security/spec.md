# Feature Specification: Remove the MOBApi control-plane security

**Feature Directory**: `006-remove-control-plane-security`

**Source Issue**: https://github.com/ahuelsmann/MOBAflow/issues/165

**Created**: 2026-09-26

**Status**: Draft

**Spec Kit**: Required

**Input**: User description: "Remove the complete control-plane security (RF-19). MOBAflow is only meant for a private household; anyone who needs security, user management or login should fork the repository. Keep command validation and the bounded command queue."

## Governance and Traceability *(mandatory)*

**GitHub Issue**: #165

**Parent Programme**: https://github.com/ahuelsmann/MOBAflow/issues/47 (package RF-19; supersedes the withdrawn #50)

**Affected Platforms**: MOBApi, MOBAflow (Windows), MOBAsmart (Android), shared libraries (`Common`, `SharedUI`).

**Out of Scope**: ESP32 provisioning protection (RF-02/#48); direct MOBAsmart-to-Z21 UDP control;
broad `MauiViewModel` decomposition (RF-12); new connection, discovery or remote-control features;
internet exposure of MOBApi.

**Sensitive Data**: None required. Regression tests use synthetic addresses and solutions.

**Data and API Effects**: Pairing, token, credential, host-enrollment, security-administration and
read-migration endpoints are removed. All remaining REST and SignalR operations are available without
credentials over plain HTTP on the configured port; the separate HTTPS port is removed. Discovery
responses no longer carry a server fingerprint or instance identity. Credential stores left on disk or
on a phone by earlier builds are ignored; no migration, cleanup or recovery feature is added. Pairing
and security settings are removed from configuration; removed JSON fields are ignored on load.
Solution JSON, Z21 behavior, safe locomotive startup and persisted layouts are unchanged.

## Clarifications

### Session 2026-09-26

- Q: Should host-only operations (publishing solution, runtime snapshot and settings, consuming the
  remote command queue) stay limited to the same PC? → A: No. Every device in the home network may call
  every operation; no source-address check remains.
- Q: Should MOBAflow keep an address-only QR code? → A: No. The QR code is removed completely;
  MOBAsmart connects through discovery, manual entry or a recent address.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Connect MOBAsmart without setup steps (Priority: P1)

An operator starts MOBAflow on the home PC and opens MOBAsmart on a phone in the same home network.
MOBAsmart finds MOBAflow automatically, or the operator enters the PC address once, and the phone shows
the current solution and runtime state. No pairing, QR scan, approval or certificate step exists.

**Why this priority**: This is the everyday connection path and the reason for the scope decision.

**Independent Test**: With MOBApi running and no stored pairing data, start a MOBAsmart client against it
by discovery and by a manually entered address; both reach the solution, runtime snapshot and settings.

**Acceptance Scenarios**:

1. **Given** MOBAflow runs with the web API enabled, **When** MOBAsmart searches the home network,
   **Then** it connects and shows solution and runtime state without any pairing prompt.
2. **Given** discovery finds nothing, **When** the operator enters the PC address, **Then** MOBAsmart
   connects with the same result and remembers the address in its recent addresses.
3. **Given** a phone that stored pairing data from an earlier build, **When** it connects, **Then** the
   stored data is ignored and the connection works like on a fresh phone.

---

### User Story 2 - Control trains remotely with validated commands (Priority: P1)

An operator drives a locomotive, switches functions and sets signal aspects from MOBAsmart. MOBApi
accepts these commands without credentials, rejects invalid values before they reach the runtime and
keeps the remote command queue bounded.

**Why this priority**: Remote control is the main use of the connection; validation protects the
layout from invalid commands, independent of authentication.

**Independent Test**: Send valid and invalid drive, function and signal commands through REST and
SignalR without credentials; valid commands reach the desktop runtime, invalid ones are rejected with a
client error, and a full queue rejects further commands instead of growing.

**Acceptance Scenarios**:

1. **Given** a connected MOBAsmart, **When** the operator sends a valid drive or function command,
   **Then** MOBAflow executes it as before.
2. **Given** a command with an out-of-range address, speed, function index or unknown signal aspect,
   **When** it is sent through REST or SignalR, **Then** MOBApi rejects it and the runtime never sees it.
3. **Given** the remote command queue is full, **When** another command arrives, **Then** it is rejected
   and the queue does not grow beyond its bound.

---

### User Story 3 - Understand the operating scope (Priority: P2)

A developer or user reads the README, SECURITY.md or the user guides and learns that MOBAflow is meant
for a trusted home network, has no login or user management, must not be exposed to the internet, and
that installations needing these capabilities fork the repository.

**Why this priority**: The scope decision must be visible so nobody relies on security that no longer
exists.

**Independent Test**: Review current documentation; no page describes pairing, credentials, certificate
pinning or the RF-03 rollout, and the scope statement is present in README and SECURITY.md.

**Acceptance Scenarios**:

1. **Given** the current documentation, **When** a reader looks for how to connect MOBAsmart,
   **Then** the steps contain no pairing, QR or approval.
2. **Given** SECURITY.md, **When** a reader checks the security model, **Then** it states the
   home-network scope and the fork recommendation.

### Edge Cases

- MOBAflow runs with the web API disabled (`AutoStartWebApp` off): MOBAsmart cannot connect, exactly as
  today without pairing; no new switch is introduced.
- Several phones connect at the same time: each can read and control; commands still pass through the
  one bounded queue in arrival order.
- MOBAflow restarts MOBApi: MOBAsmart reconnects through its existing reconnect logic without any
  credential renewal.
- A request carries an old bearer token or pairing header from an earlier build: the header is ignored
  and the request is handled like any other.
- Leftover security files from earlier builds exist on the PC or phone: they are neither read nor
  deleted.
- A device other than the MOBAflow PC calls a host operation such as publishing the solution: MOBApi
  accepts it like any other request, because every device in the home network is trusted.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST remove the anonymous-read migration, including its observation window,
  readiness evidence and the runtime lookup of GitHub issue comments.
- **FR-002**: The system MUST remove pairing, device credentials, access and refresh tokens,
  capability-based authorization, credential administration, protected credential storage, host
  enrollment and the host bootstrap channel between MOBAflow and MOBApi.
- **FR-003**: MOBApi MUST serve all remaining REST and SignalR operations over plain HTTP on its
  configured port without credentials; the HTTPS listener, server identity and certificate pinning MUST
  be removed from MOBApi, MOBAflow and MOBAsmart.
- **FR-004**: MOBAsmart MUST connect through the existing discovery, a manually entered address or a
  recent address, with no pairing screen, QR scanning, camera permission request for pairing or
  credential store.
- **FR-005**: MOBAflow and MOBAsmart MUST remove the pairing and credential management UI, the QR code
  display and scanning, and the related settings and permissions.
- **FR-006**: MOBApi MUST validate locomotive addresses, speeds, function indices, signal identifiers
  and aspects, identifiers and payload sizes for every remote command on REST and SignalR before the
  command reaches the runtime, and MUST reject invalid values with a client error.
- **FR-007**: The remote command queue MUST be bounded and MUST reject commands when full instead
  of growing or silently dropping accepted commands.
- **FR-008**: Removed security code MUST leave no disabled, hidden or compatibility path, and no
  migration of earlier credential data. Superseded tests, analyzer-baseline entries, the RF-03 plan and
  the security design record MUST be removed in the same change.
- **FR-009**: Documentation MUST state the home-network operating scope, the absence of login and user
  management, the warning against internet exposure and the fork recommendation.
- **FR-010**: The system MUST preserve configuration defaults, safe locomotive startup (speed zero, no
  restored movement), solution synchronization, photo upload and the `AutoStartWebApp` setting.
- **FR-011**: MOBApi MUST NOT restrict any operation by the caller's network address; host
  operations are available to every device in the home network.

### Key Entities *(include if feature involves data)*

- **Remote command**: a drive, function, signal or journey-reset request from a remote client; validated
  at the API boundary and queued in bounded order.
- **Discovery response**: the announcement that lets MOBAsmart find MOBAflow; carries address and port,
  without identity or fingerprint.
- **Connection settings**: the address, port, recent addresses and connection toggle on the phone; no
  credential or pairing data.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Connecting a fresh MOBAsmart to a running MOBAflow needs zero pairing or approval steps.
- **SC-002**: The solution contains zero authentication, authorization, pairing, token, credential or
  certificate-pinning types, endpoints, settings and UI elements after the change.
- **SC-003**: 100% of invalid remote command values in the regression suite are rejected on both REST
  and SignalR, and a full queue rejects further commands.
- **SC-004**: MOBAflow and MOBAsmart ship no QR-code library and request no camera permission for
  connecting.
- **SC-005**: Current documentation contains no instructions for pairing or credentials and states the
  home-network scope in README and SECURITY.md.

## Assumptions

- The home network is trusted; any device in it may read and control the layout. The maintainer accepted
  this on 2026-09-25 (#50 closed as not planned).
- The existing discovery, manual address entry and recent addresses remain the connection mechanisms.
- `AutoStartWebApp` remains the way to disable network access to MOBAflow.
- Validation limits follow the Z21 backend: locomotive address 1-9999, speed 0-126, functions F0-F31.
- Research on 2026-09-26 showed that today only required fields are checked on REST, SignalR commands
  are not validated and the queue is unbounded; FR-006 and FR-007 therefore add these protections.
- No app start, device deployment or hardware action is authorized by this specification.
