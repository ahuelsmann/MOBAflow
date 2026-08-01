# MOBApi Authenticated Control Plane Security Design

## Record status

- Status: Proposed for implementation
- Decision owner: RF-03, [GitHub issue #50](https://github.com/ahuelsmann/MOBAflow/issues/50)
- Parent programme: [GitHub issue #47](https://github.com/ahuelsmann/MOBAflow/issues/47)
- Implementation plan: [RF-03 Authenticated MOBApi Control Plane](../plans/50-authenticated-control-plane.md)
- Scope of this delivery: Security design only
- Last reviewed: 2026-07-20

## Decision summary

MOBApi will become a deny-by-default, capability-authorized control plane. Remote clients will pair through an explicit, short-lived local approval flow over HTTPS while pinning a persistent MOBApi server identity. Successful pairing issues a rotating, device-specific refresh credential. Clients exchange that credential for five-minute access tokens and use those tokens for both REST and SignalR.

MOBAflow is a distinct `host` principal. When it launches MOBApi, the two processes establish a per-launch host credential through a user-restricted local bootstrap channel. Loopback remains a transport constraint for host enrollment but is not an identity or authorization decision.

REST actions and SignalR hub methods will use the same named capabilities, live credential-state checks, validators, rate-limit partitions, and command-admission service. Every hardware command enters one bounded queue after authorization and validation. No accepted command is silently discarded, and no invalid command consumes queue capacity.

This record does not implement the design. Server, MOBAflow, and MOBAsmart changes remain separate reviewable slices in the issue plan.

## Context

MOBApi is a standalone ASP.NET Core process used as a cache and bridge between MOBAflow and MOBAsmart. It currently maps controllers and the `PhotoHub` and `RuntimeHub` without authentication middleware or authorization policies.

The current trust checks are insufficient for a hardware control plane:

- REST reads, client registration, photo access, photo upload, and remote runtime commands are anonymous.
- Host publication and queue-consumer actions generally trust a loopback source address.
- `RuntimeHub.RegisterHost` trusts loopback plus the caller-selected hub method; `RegisterRemote` trusts a caller-supplied client ID.
- SignalR runtime commands can be forwarded directly to the host and bypass the REST fallback queue.
- The REST fallback `RuntimeCommandQueue` is an unbounded `ConcurrentQueue`.
- Boundary validation differs by transport. The Z21 backend rejects locomotive addresses outside 1-9999, speed outside 0-126, and function indices outside F0-F31, but MOBApi does not consistently reject those values before admission.
- MOBAsmart stores a random client ID in ordinary preferences. That identifier is useful as display metadata but is not a credential.
- LAN communication currently uses HTTP, so adding bearer credentials without authenticated encryption would expose them to interception.

## Scope and non-goals

This decision covers authentication and authorization for MOBApi REST and SignalR operations that observe or mutate solution, runtime, client, photo, or hardware-control state. It also covers the local MOBAflow host identity, MOBAsmart credential handling, throttling, bounded command admission, security telemetry, compatibility migration, and rollback.

This decision does not cover:

- ESP32 provisioning credentials, which belong to RF-02;
- product features from issues #30 through #36;
- internet exposure, cloud relay, social login, enterprise identity federation, or multi-user account administration;
- general MOBAsmart ViewModel decomposition;
- authorization of direct MOBAsmart-to-Z21 UDP traffic;
- new runtime or hardware commands.

MOBApi remains a LAN control plane. Port forwarding or direct internet exposure is unsupported even after this design is implemented.

## Security objectives

1. A network peer cannot observe control-plane state or control hardware without a valid credential and the required capability.
2. Possession of a read-only credential cannot be upgraded into control, host, or security-administration authority.
3. Expired, rotated, replayed, or revoked credentials cannot submit or execute hardware commands.
4. REST and SignalR provide equivalent authorization and admission decisions for equivalent operations.
5. Invalid values, oversized payloads, throttled requests, and queue overflow are rejected before runtime execution.
6. Compromise of logs, source, normal configuration, or the server-side credential registry does not directly disclose usable client secrets.
7. Revocation and role changes apply to active SignalR connections as well as future HTTP requests.
8. A security subsystem failure disables remote control while preserving local MOBAflow-to-Z21 operation.

## Threat model and trust boundaries

### Protected against

- an unauthenticated device on the same wired or wireless LAN;
- passive LAN capture and active man-in-the-middle attempts during pairing or normal use;
- guessing or replaying a pairing secret;
- replay of a rotated refresh credential;
- reuse of an expired access token;
- a paired read-only client attempting control or host operations;
- a compromised or lost paired device after operator revocation;
- request flooding by one client, accidental reconnect storms, and command-queue exhaustion;
- malformed or oversized JSON, SignalR payloads, identifiers, enums, addresses, speeds, and function indices;
- credential disclosure through normal request logging, exception details, telemetry labels, or Android backup.

### Assumptions

- The person using the local MOBAflow desktop UI is the control-plane owner.
- The Windows account running MOBAflow and its child MOBApi process is trusted. Malware executing as that user is outside this boundary.
- The owner can visually compare or scan pairing information displayed by MOBAflow.
- MOBApi and its clients use cryptographically secure random-number generation and operating-system protected storage.
- The device clock may be inaccurate. Token validation allows at most 30 seconds of clock skew, while credential inactivity and queue deadlines use server time.

### Explicit residual risks

- A fully compromised paired remote-control device can use its granted capabilities until it is revoked.
- Certificate pinning authenticates the paired MOBApi instance, not the human owner.
- LAN denial of service cannot be eliminated inside the application; rate limits and bounded queues limit application-level impact.
- Local code running as the same OS user may access process memory or protected files and is not prevented by this design.

## Server identity and transport

### Final transport state

- MOBApi exposes the LAN API and hubs over Kestrel HTTPS only.
- The persistent server identity is an ECDSA P-256 self-signed certificate created specifically for MOBApi. It is not the ASP.NET development certificate.
- Clients authenticate the server by a SHA-256 SubjectPublicKeyInfo fingerprint obtained during pairing. Dynamic LAN addresses therefore do not require a public DNS name or public certificate authority.
- The certificate private key and token-signing keys are stored for the current OS user in the protected MOBApi security store. On Windows, persisted ASP.NET Core Data Protection keys are protected with DPAPI and the containing directory is restricted to that user.
- Discovery is only a candidate-location mechanism. An unauthenticated UDP discovery response never establishes trust and never carries a pairing or device credential.
- The discovery response will advertise protocol version, HTTPS port, server instance ID, and server fingerprint. A previously paired client rejects a mismatched instance or fingerprint instead of silently trusting rediscovery.
- Plain HTTP is removed from the LAN endpoint at enforcement. Clients must construct HTTPS URLs directly; they must not send a credential to HTTP and rely on a redirect.

### Server identity rotation

Routine rotation uses an authenticated overlap:

1. MOBApi creates a new certificate and retains the old certificate and fingerprint during a bounded transition.
2. An authenticated client receives the pending fingerprint from the old pinned connection and stores both fingerprints.
3. After all active devices have acknowledged the new identity, the owner activates it and retires the old identity.
4. A client that missed the overlap must pair again.

Emergency rotation after suspected private-key compromise invalidates all refresh families and requires every remote client to pair again. The old identity is never accepted as a fallback after emergency rotation.

## Principal and credential model

### Principal types

| Principal | Credential lifetime | Purpose |
| --- | --- | --- |
| `host` | One MOBApi process lifetime | MOBAflow publication, command consumption, security administration |
| `remote-control` | Device credential with inactivity and absolute expiry | MOBAsmart or another approved controller |
| `read-only` | Device credential with inactivity and absolute expiry | Monitoring without hardware control |

The server assigns an immutable credential ID and principal type. A client-supplied `ClientId` or device name is display metadata only and never becomes `sub`, a role, a limiter key, or an authorization input.

### Remote device credential

- A device refresh credential contains 256 random bits and an opaque server-assigned credential ID.
- The clear credential is returned only once over the pinned TLS pairing response and is never stored by MOBApi.
- MOBApi stores a keyed SHA-256 verifier, credential ID, refresh-family ID, principal type, capability version, creation time, last-use time, inactivity expiry, absolute expiry, and revocation metadata.
- The initial defaults are 30 days of inactivity and 365 days absolute lifetime. Both are server-controlled and covered by expiry tests.
- Every successful refresh rotates the credential. Reuse of any prior family member revokes the entire refresh family and emits a replay event.
- The refresh credential is accepted only by the token endpoint. It is never sent to controllers, hubs, query strings, or application logs.

### Access token

- MOBApi issues a signed access token with a five-minute lifetime and at most 30 seconds clock skew.
- Required claims are issuer, audience, credential ID as subject, principal type, capability version, token ID, issued-at, not-before, and expiry.
- The token carries capabilities for efficient policy evaluation, but a live credential-registry check also verifies that the credential exists, is not expired or revoked, and still has the claimed capability version.
- Access-token signing keys rotate independently of device credentials. The previous signing key remains verification-only for no longer than the maximum access-token lifetime plus clock skew.
- REST sends the token in the `Authorization: Bearer` header.
- SignalR .NET clients use `AccessTokenProvider`. Query-string access tokens are accepted only on the two known hub paths because some SignalR transports require that form.
- `CloseOnAuthenticationExpiration` closes connections when their access token expires. A hub invocation filter additionally rechecks live credential status and the named capability on every invocation. Role change or revocation actively aborts all connections mapped to that credential ID.

### Host bootstrap credential

- MOBAflow generates a fresh 256-bit bootstrap secret for every MOBApi child-process launch.
- The secret is delivered through a current-user-only inherited handle or named-pipe bootstrap channel. It is not placed in command-line arguments, committed configuration, normal environment diagnostics, or logs.
- MOBApi retains only an in-memory verifier. The bootstrap endpoint is HTTPS, loopback-only, single-use, and rate-limited.
- A successful exchange issues a short-lived host access token and process-bound renewal credential. Both become invalid when MOBApi exits or when the owning MOBAflow process disconnects permanently.
- A standalone MOBApi instance has no host authority until an owner completes an explicit local enrollment flow. Loopback alone never grants the `host` principal.

## Pairing protocol

Pairing is disabled by default and can be opened only by an authenticated `host` principal with `security.manage`.

1. MOBAflow asks MOBApi to open one pairing window for 120 seconds and one requested principal type (`read-only` by default). Only the owner can approve `remote-control`.
2. MOBApi creates a single-use 256-bit pairing secret in memory. It displays a QR payload containing protocol version, HTTPS endpoint, server instance ID, server fingerprint, pairing-window ID, and the secret.
3. MOBAsmart creates its local credential-storage entry, scans the QR payload, and connects only when the TLS public-key fingerprint matches the scanned fingerprint.
4. MOBAsmart submits the pairing-window ID, secret, requested device display metadata, and a client-generated request nonce. The server compares secrets in constant time.
5. MOBAflow displays the candidate device, requested role, and a short confirmation code derived from both sides' nonces. The owner explicitly approves or rejects it.
6. On approval, the server assigns the credential ID and capabilities, burns the pairing secret, and returns the first refresh credential. On rejection, expiry, or cancellation, no credential is issued.
7. MOBAsmart stores the credential and server fingerprint in secure storage, discards the pairing secret, obtains an access token, and reconnects through authenticated REST and SignalR paths.

Pairing failure rules:

- A pairing secret is valid for one successful exchange only and is never persisted.
- Five failed submissions from one IP or for one window close the window and impose a ten-minute pairing cooldown.
- At most one pairing window is active per MOBApi instance.
- Restart, owner cancellation, fingerprint mismatch, request-nonce reuse, or timeout closes the window.
- Pairing errors do not reveal whether the window ID, secret, or requested device already exists.
- Discovery never opens a pairing window and never advertises a pairing secret.

## Credential ownership and storage

| Material | Owner | Storage | Backup and recovery |
| --- | --- | --- | --- |
| MOBApi server private key | MOBApi instance / OS user | Protected current-user security store | No plaintext export; emergency reset creates a new identity and forces re-pairing |
| Access-token signing keys | MOBApi instance / OS user | ASP.NET Core Data Protection-backed protected store | Retain only the active and bounded verification-only predecessor |
| Device credential verifiers and metadata | MOBApi instance / OS user | Atomic protected registry with restrictive ACL | Corruption fails closed; owner reset revokes all devices |
| Host bootstrap and renewal secrets | Current MOBAflow/MOBApi process pair | Memory and user-restricted local channel only | Restart creates new values |
| MOBAsmart refresh credential and pinned fingerprint | Paired mobile app installation | `.NET MAUI` `ISecureStorage` | Exclude Android SecureStorage preferences from Auto Backup; unreadable storage removes the entry and requires re-pairing |
| Access token | Current client session | Memory only | Reissue from the rotating refresh credential |
| Existing MOBAsmart client ID | MOBAsmart | Ordinary preferences, metadata only | May be retained for display continuity; never migrated into a credential |

No credential, verifier, private key, or token is stored in `appsettings.json`, source, shipped data, a solution file, or normal logs.

## Roles and capabilities

Authorization uses named capabilities. Roles are only owner-facing templates that grant capabilities; endpoints depend on capabilities rather than role-name comparisons.

| Capability | Host | Remote control | Read only | Meaning |
| --- | :---: | :---: | :---: | --- |
| `controlplane.read` | Yes | Yes | Yes | Read solution, runtime, status, settings, progress, and photos |
| `client.presence` | Yes | Yes | Yes | Register and remove only the caller's own presence record |
| `runtime.control` | Yes | Yes | No | Submit validated hardware and journey commands |
| `photo.write` | Yes | Yes | No | Upload a validated photo |
| `host.publish` | Yes | No | No | Publish solution, runtime settings, snapshots, and feedback configuration |
| `host.consume` | Yes | No | No | Consume admitted runtime commands and register the active host connection |
| `security.manage` | Yes | No | No | Open pairing, approve roles, rotate, revoke, and reset security state |

The initial templates are fixed. Future fine-grained capabilities require a design update and cannot be inferred from product feature issues.

## REST policy matrix

The final policy applies to all routes even when a reverse proxy or loopback connection is used.

| REST operation | Capability or state | Notes |
| --- | --- | --- |
| `GET /api/photos/health` | Anonymous, minimal, rate-limited | Reports process readiness and security protocol version only; no client, runtime, address, or credential data |
| Pairing exchange endpoint | Active pairing window | Anonymous bootstrap over fingerprint-pinned TLS; separately throttled |
| Token refresh endpoint | Valid rotating refresh credential | Never accepts an access token in place of the refresh credential |
| `GET /api/status` | `controlplane.read` | Connected-client details are protected state |
| `POST /api/clients/register` | `client.presence` | Registers subject credential ID; ignores caller-selected identity |
| `POST /api/clients/unregister` | `client.presence` | Removes only the caller's presence entry |
| `GET /api/photos/file` | `controlplane.read` | Existing path containment and extension checks remain required |
| `POST /api/photos/upload` | `photo.write` | Existing 10 MiB file cap remains; content signature must match an allowed image type |
| `GET /api/runtime-settings` | `controlplane.read` | Z21 address is protected control-plane information |
| `PUT /api/runtime-settings` | `host.publish` | Loopback is an additional transport restriction, not authorization |
| `GET /api/runtime/meta` | `controlplane.read` | No anonymous compatibility after enforcement |
| `GET /api/runtime/snapshot` | `controlplane.read` | Response remains remotely filtered where required |
| `PUT /api/runtime/snapshot` | `host.publish` | Loopback plus capability; size and schema validated |
| `GET /api/runtime/journeys/{id}/feedback-progress` | `controlplane.read` | Journey ID must resolve in the active snapshot |
| `POST .../feedback-progress/reset` | `runtime.control` | Uses the common command validator and admission service |
| `GET .../feedback-sequence` | `controlplane.read` | Project and journey IDs must resolve |
| `PUT .../feedback-sequence` | `host.publish` | Loopback plus capability; concurrency and collection bounds remain enforced |
| `GET /api/solution/meta` | `controlplane.read` | Protected state |
| `GET /api/solution` | `controlplane.read` | Protected state and response-size observability |
| `PUT /api/solution` | `host.publish` | Loopback plus capability; schema and size validated |
| `POST /api/runtime/commands/*` | `runtime.control` | Common validator, limiter, idempotency, and admission service |
| `GET /api/runtime/commands/pending` | `host.consume` | Loopback plus capability; never available to remote clients |
| Security administration endpoints | `security.manage` | Host-only and loopback-restricted |

Authentication failures return `401`. An authenticated principal without the capability returns `403`. An expired, rotated, revoked, malformed, or unknown credential is not an authenticated principal and returns `401`.

## SignalR policy matrix

Both hub endpoints require an authenticated access token during negotiation and connection.

| Hub operation | Capability | Equivalent REST policy |
| --- | --- | --- |
| Connect to `PhotoHub` and receive `PhotoUploaded` | `controlplane.read` | Photo/status reads |
| Connect to `RuntimeHub` | `controlplane.read`, or a host token | Runtime reads |
| `RegisterRemote` | `client.presence` | Client self-registration |
| `RegisterHost` | `host.consume` | Pending-command consumer ownership |
| `PushSnapshot` | `host.publish` | `PUT /api/runtime/snapshot` |
| `SetSignalAspect` | `runtime.control` | `POST /api/runtime/commands/signal-aspect` |
| `SetLocomotiveDrive` | `runtime.control` | `POST /api/runtime/commands/locomotive/drive` |
| `SetLocomotiveFunction` | `runtime.control` | `POST /api/runtime/commands/locomotive/function` |

`RegisterHost` also requires the `host` principal type, a loopback connection, and successful exclusive-host ownership. `RegisterRemote` derives identity from the access-token subject and does not accept an authorization-relevant client ID.

SignalR group membership never grants a capability. Server-to-client notifications are sent only to groups populated after policy checks. Hub method authorization and live credential checks happen before parsing or forwarding a command.

## REST and SignalR policy equivalence

Equivalent operations must call one application service in this order:

1. authenticate the access token;
2. load live credential state and evaluate the named capability;
3. consume the same per-credential and global rate-limit permits;
4. enforce transport-independent payload and value validation;
5. apply the same idempotency and replay decision;
6. attempt the same bounded command admission;
7. return a transport adapter over one result model.

REST uses problem details with a stable machine code. SignalR returns or throws a sanitized result with the same machine code. The initial codes are `unauthenticated`, `forbidden`, `invalid_command`, `duplicate_command`, `throttled`, `queue_full`, `command_expired`, and `host_unavailable`.

No controller or hub may forward directly to `IMobaRuntime`, a host connection, or the fallback queue outside this boundary.

## Boundary validation

Validation occurs after authentication but before idempotency storage or queue admission. HTTP server request limits and SignalR receive limits also provide a pre-model-binding outer bound.

### Runtime commands

| Value | Rule |
| --- | --- |
| Locomotive address | Integer 1 through 9999 |
| Speed | Integer 0 through 126 |
| Function index | Integer 0 through 31 |
| Signal ID | Non-empty GUID that resolves to an active runtime signal |
| Signal aspect | A defined `SignalAspect` value supported by the resolved signal; undefined numeric enum values are rejected |
| Journey ID | Non-empty GUID that resolves to an active runtime journey |
| Command ID | Non-empty UUID supplied once per logical command; scoped to credential ID |
| Unknown JSON members | Rejected on security-sensitive request models |

Signal, journey, and project membership are checked against one immutable runtime snapshot used for the admission decision. Resolution is repeated by the host immediately before hardware execution so deleted or deactivated objects fail safely.

### Payload bounds

| Payload class | Initial maximum |
| --- | ---: |
| Pairing, token, presence, runtime-setting, and runtime-command request | 16 KiB; command bodies additionally target 4 KiB |
| SignalR remote-control invocation | 16 KiB |
| Runtime snapshot publication | 2 MiB |
| Feedback sequence publication | 256 KiB and at most 512 steps |
| Solution publication | 16 MiB |
| Photo upload | Existing 10 MiB limit |
| Device display name | 128 Unicode scalar values after normalization |

Limits are configuration-backed but startup validation prevents zero, negative, or unreasonably large values. Increasing a limit requires load evidence and review; clients cannot negotiate larger values.

## Replay and idempotency

- TLS protects access tokens and command bodies in transit.
- Access tokens have a unique token ID and short expiry. Token replay does not bypass capability, live credential, rate-limit, or command-idempotency checks.
- Every control request carries a command ID. The server keys idempotency by `(credential ID, command ID)` and retains the terminal admission result for five minutes in a bounded cache of 2,048 entries.
- A duplicate returns the original admission result and never adds another queue entry.
- Refresh credential rotation detects replay at the credential-family boundary and revokes the family.
- Pairing request nonces and single-use pairing secrets prevent pairing-message replay.
- SignalR reconnect does not replay completed invocations automatically. Client retry logic reuses the original command ID.

## Rate limits

All ingress queues in the ASP.NET rate limiter use `QueueLimit = 0`; callers receive immediate backpressure. Limits partition on server-assigned credential ID after authentication and normalized remote IP before authentication. User-controlled headers, client IDs, device names, and token strings are not partition keys.

| Surface | Initial limit | Partition | Over-limit behavior |
| --- | --- | --- | --- |
| Minimal health | 30 requests/minute | Remote IP | `429` with `Retry-After` |
| Pairing submission | 5 failed attempts per 10 minutes; 10 total requests per window | Remote IP plus pairing-window ID | Close window after fifth failure; ten-minute cooldown |
| Token refresh | 10 requests/minute | Credential ID, chained with 30/minute per IP | `429`; no rotation occurs |
| Authenticated reads | Token bucket: burst 30, replenish 2/second | Credential ID | `429` |
| Non-command writes | 30 requests/minute | Credential ID | `429` |
| Runtime commands | Token bucket: burst 20, replenish 10/second | Credential ID, chained with 100/second global | Stable `throttled` result |
| SignalR connections | 3 concurrent connections | Credential ID | Reject negotiation or newest connection |
| Host connection | 1 active host | MOBApi instance | Reject second host; never replace silently |

REST and SignalR command invocations consume from the same runtime-command limiter. Reconnecting does not create a fresh credential partition. Pairing, authentication, and global limits apply before expensive cryptographic or model work where the framework permits.

These values are conservative initial defaults, not performance claims. RF-18 load verification must confirm or tune them without weakening the invariants.

## Bounded command admission and execution

The unbounded `ConcurrentQueue` and direct SignalR forwarding will be replaced by one application-level admission service backed by a bounded `Channel<AuthorizedRuntimeCommand>`.

Initial policy:

- global capacity: 128 admitted commands;
- per-credential outstanding limit: 16;
- multiple writers and one ordered reader;
- strict FIFO among admitted commands, with no priority inferred from role or transport;
- non-blocking ingress using `TryWrite`; HTTP request threads and hub invocations never wait for queue space;
- no silent `DropOldest` or `DropNewest` behavior;
- a command is accepted only if an authenticated host consumer is operational;
- queue-full returns `429` / `queue_full` with a bounded `Retry-After`; host unavailable returns `503` / `host_unavailable`;
- REST returns `202` plus command ID only after successful admission; SignalR returns the same admitted result;
- accepted commands have server-assigned deadlines: two seconds for locomotive drive/function commands and five seconds for signal and journey commands;
- an expired command is recorded and not executed;
- host execution revalidates the command's active-object preconditions, credential status, capability version, and deadline;
- cancellation or disconnect after acceptance does not silently remove the command; its terminal status remains queryable during the idempotency window.

Queue depth, oldest age, admission latency, execution latency, expiry, and rejection counts are observable. Payload contents are not telemetry dimensions.

## Rotation, revocation, and capability changes

### Routine refresh rotation

- Every refresh succeeds at most once and atomically writes its successor before returning it.
- If the response is lost, reuse of the predecessor is treated as possible theft and revokes the family. The client must pair again.
- MOBAsmart writes the new refresh credential to secure storage before discarding its in-memory predecessor and reconnects with a new access token.

### Operator rotation

The host can rotate one device without changing its role. Rotation revokes the current family, aborts its hub connections, and opens a device-specific re-pairing window. No grace token can submit commands after the rotation transaction commits.

### Revocation

- Revocation records operator reason, server time, credential ID fingerprint, and prior role without storing the secret.
- The live registry rejects future REST requests immediately.
- All SignalR connection IDs mapped to that credential are aborted.
- Queued but unexecuted commands from that credential are marked revoked and skipped by the consumer.
- Revocation remains recorded so an old refresh credential cannot recreate the principal.

### Role or capability change

Changing a role increments the credential capability version, invalidates the refresh family, aborts active hubs, and requires a new access token. Downgrading from `remote-control` to `read-only` also invalidates and skips queued control commands.

### Reset and recovery

If the protected registry or key material is missing, corrupt, moved to another OS user, or cannot be decrypted, MOBApi starts in remote-control-disabled recovery mode. Minimal health remains available, local MOBAflow operation remains available, and the owner may explicitly reset security state. Reset creates a new server identity, revokes all former credentials, and requires re-pairing. The system never creates a permissive empty registry.

## Failure behavior

| Condition | REST | SignalR | Queue/runtime effect |
| --- | --- | --- | --- |
| Missing or invalid access token | `401` | Negotiation/invocation rejected | None |
| Authenticated but missing capability | `403` | `forbidden` | None |
| Expired, rotated, or revoked credential | `401` | Connection aborted or `unauthenticated` | Pending commands skipped |
| Invalid or oversized input | `400`, `413`, or `422` with stable code | `invalid_command` | Never admitted |
| Per-client/global throttle | `429` plus `Retry-After` | `throttled` plus retry delay | Never admitted |
| Queue full | `429` plus `Retry-After` | `queue_full` | Never admitted |
| No operational host | `503` | `host_unavailable` | Never admitted |
| Command expires after admission | Queryable terminal status | Queryable terminal status | Not executed |
| Security store unavailable | Remote operations `503`; health reports degraded | Connection rejected | Remote control disabled |

Responses never reveal whether a different credential ID exists, why a token verifier failed, internal exception details, file paths, key identifiers, or stored device metadata.

## Telemetry and audit

Security events use structured logging with stable event IDs and low-cardinality metrics.

Required events:

- pairing window opened, approved, rejected, expired, cancelled, and locked;
- authentication failure by reason category, successful token refresh, refresh replay, and token expiry;
- authorization denial with operation and required capability;
- credential created, rotated, role-changed, revoked, expired, and reset;
- invalid value or payload category;
- per-client and global throttling;
- command admitted, duplicate, queue-full, expired, skipped after revocation, executed, and failed;
- host registration conflict, disconnect, reconnect, and unavailable state;
- security-store or server-identity degradation and rollback-mode activation.

Permitted structured properties include UTC timestamp, event ID, correlation ID, transport, normalized operation name, required capability, principal type, non-reversible credential fingerprint, reason category, response code, queue depth, payload byte count, and latency. Remote IP is recorded only according to the existing local log privacy policy and never used as a metric label.

Forbidden log and metric data includes:

- `Authorization` headers, access tokens, refresh credentials, pairing secrets, bootstrap secrets, private keys, verifiers, and full certificate material;
- the `access_token` query value required by some SignalR transports;
- request/response bodies, photo contents, solution JSON, runtime snapshot JSON, and raw command payloads;
- unbounded credential IDs, IP addresses, device names, signal IDs, journey IDs, or command IDs as metric labels.

ASP.NET request logging must redact the SignalR `access_token` query value or raise the relevant hosting log category above information level. Production SignalR detailed errors remain disabled. Automated redaction tests use canary secret values and assert that none reach captured logs.

Initial metrics are counters for each outcome plus gauges for active authenticated connections and queue depth, and histograms for admission and execution latency. Alert thresholds are operational documentation owned by the later enforcement slice.

## Compatibility migration

Migration is staged so new clients can work with old servers while a new server never silently restores anonymous hardware control.

### Phase 0: Design and inventory

This document and its issue plan are delivered. Runtime behavior is unchanged.

### Phase 1: Additive security foundation

- Add protected server identity, credential registry, token service, policies, telemetry, and public health capability advertisement.
- Keep existing endpoints behaviorally unchanged only inside development/test while policy parity tests are built.
- No client receives or logs a production credential yet.

### Phase 2: Protect the host

- MOBAflow and MOBApi establish the per-launch host identity.
- Host publication, command consumption, host hub registration, pairing administration, and credential administration require host capabilities.
- Loopback-only checks remain as defense in depth but no longer authorize by themselves.
- Failure to establish the host identity disables remote control and publication, not local Z21 operation.

### Phase 3: Ship pairing-capable clients

- MOBAsmart understands HTTPS discovery, fingerprint pinning, pairing, secure storage, token refresh, and authenticated SignalR reconnect.
- A new client may use the existing unauthenticated protocol only with an explicitly detected legacy server and only before it has ever paired with that server instance.
- After successful pairing, the client records a no-downgrade marker and never falls back to HTTP or anonymous access for that instance.
- The existing preferences client ID remains display metadata and is not converted into authority.

### Phase 4: Enforce control and migrate reads

- All runtime commands, photo writes, host operations, and security administration require credentials with no compatibility bypass.
- Anonymous control returns `401`; a read-only principal attempting control returns `403`.
- Anonymous reads remain available during the measured compatibility window, but expose no client,
  version, server-identity, runtime, hardware, or security details through the public health surface.
- MOBAsmart sends the non-secret `X-MOBAflow-Client-Release` header on authenticated REST and
  SignalR traffic. Enforcement requires matching traffic from the selected stable release on both
  transports, fourteen consecutive defect-free days, no open critical authentication, refresh,
  reconnect, or read-parity defect, and a concrete readiness-evidence comment in issue #50.
- Fixing a critical defect restarts the full observation window. Elapsed time without matching
  authenticated traffic can never complete the gate.
- After enforcement, legacy REST and SignalR entry points return the same machine-readable
  `client_upgrade_required` reason instead of silently restoring anonymous access.
- Pairing readiness and the migration gate are visible before anonymous reads are disabled for
  upgraded installations.

### Phase 5: Remove legacy access

- Disable and remove `LegacyAnonymousReads` after migration telemetry and support evidence meet the issue gate.
- Remove HTTP LAN binding and legacy anonymous client registration.
- Update user guidance and operational runbooks.

Compatibility never includes anonymous control, host publication, queue consumption, pairing administration, credential administration, or automatic trust of a changed certificate fingerprint.

## Rollback

The design supports functional rollback without reintroducing anonymous hardware control.

- This design-only slice can be reverted with no runtime effect.
- Authentication components remain additive until host and remote parity tests pass.
- After enforcement, a local administrator may activate anonymous read-only rollback for at most
  seven days. The protected expiry survives restart, expires automatically, emits an immediate audit
  warning, a startup warning while active, and an operational gauge. It has no effect on command,
  host, photo-write, pairing-administration, credential-administration, or security endpoints.
- If authentication, TLS identity, revocation checks, or command admission is unhealthy, MOBApi disables remote control and reports degraded minimal health. MOBAflow continues local Z21 operation.
- Rolling back a client does not delete server revocation state or the no-downgrade marker. A legacy client that cannot authenticate must remain disconnected.
- Rolling back across a credential-registry schema requires a tested read-compatible migration or an explicit security reset; it never treats unreadable state as an empty allow-all store.
- Emergency rollback after suspected credential or identity compromise rotates the server identity, revokes every refresh family, clears accepted remote commands, and requires re-pairing.

## Verification requirements

### Authentication and pairing

- anonymous control and protected reads return `401`;
- a valid read-only token reads allowed state but receives `403` for every control path;
- pairing secrets expire, are single-use, compare in constant time, and lock after the configured failures;
- a fingerprint mismatch prevents any credential transmission;
- expired access and refresh credentials cannot control;
- refresh rotation succeeds once, and replay revokes the family;
- restart, secure-storage loss, corrupt backup restoration, and security-store corruption follow the documented recovery behavior.

### Authorization parity

- every REST/SignalR operation pair shares one capability and produces equivalent allow/deny results;
- host, remote-control, and read-only matrices are table-driven tests;
- a role downgrade or revocation aborts active hubs and prevents queued execution;
- loopback without a host credential receives `401` or `403` as appropriate.

### Validation and admission

- addresses 0 and 10000, speeds -1 and 127, and function indices -1 and 32 are rejected before queue admission;
- empty or unknown signal/journey IDs and undefined enum values are rejected;
- oversized REST and SignalR payloads fail at the documented boundary;
- REST and SignalR duplicates return the same original result and execute once;
- per-client and global throttles cannot be reset by reconnecting or changing client metadata;
- queue capacity 128 and per-client outstanding capacity 16 are enforced under concurrency;
- overflow is explicit, accepted FIFO order is stable, expired commands do not execute, and no accepted command disappears silently.

### Telemetry and rollback

- all required failure categories create structured events and bounded-cardinality metrics;
- canary tokens, secrets, query strings, and bodies never appear in captured logs;
- legacy read mode cannot enable a write or command path;
- security-store and server-identity failures disable remote control while local runtime remains usable;
- migration and rollback tests cover a legacy client, paired client, rotated server identity, and revoked device.

## Options considered

### Continue trusting the LAN or loopback

Rejected. LAN membership is not identity, and loopback does not distinguish the MOBAflow owner from another local process. Neither supports roles, rotation, revocation, or audit.

### One static API key for all clients

Rejected. A shared key cannot attribute, downgrade, rotate, or revoke one device. It also creates a long-lived SignalR query-string secret and expands the effect of one device compromise.

### Long-lived bearer tokens without refresh rotation

Rejected. Revocation would depend on every handler consulting mutable state, theft would have a long useful lifetime, and replay would be difficult to detect. Short access tokens plus rotating refresh families reduce exposure and make replay visible.

### Mutual TLS for every remote client

Deferred. It provides strong device authentication but significantly complicates certificate issuance, Android client-key lifecycle, Kestrel certificate negotiation, and recovery. Pinned server TLS plus device-specific rotating credentials meets the current LAN threat model with a smaller implementation surface. The credential model does not prevent a future sender-constrained upgrade.

### Different REST and SignalR security paths

Rejected. Separate policies and queues are the source of drift and bypass risk. Transport adapters must converge before command admission.

### Waiting or dropping when the queue is full

Rejected. Waiting ties up request/hub resources and increases command staleness. Dropping hides whether hardware control was applied. Immediate explicit rejection is deterministic and observable.

## Consequences

Positive consequences:

- hardware control becomes attributable, revocable, and least-privilege;
- REST and SignalR behavior becomes testably equivalent;
- bounded ingress protects the runtime from accidental or hostile overload;
- local operation remains available when remote security is degraded;
- future RF-09 and RF-18 work receives explicit negative and load-test contracts.

Costs and constraints:

- MOBApi needs persistent protected security state and an HTTPS identity;
- MOBAflow and MOBAsmart require coordinated authentication and reconnect changes;
- users must pair devices and re-pair after security reset or emergency identity rotation;
- active SignalR connections require live-state checks and connection tracking;
- compatibility requires a staged release and owner-visible migration state.

## Authoritative references

- [ASP.NET Core policy-based authorization](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/policies?view=aspnetcore-10.0)
- [Authentication and authorization in ASP.NET Core SignalR](https://learn.microsoft.com/en-us/aspnet/core/signalr/authn-and-authz?view=aspnetcore-10.0)
- [Security considerations in ASP.NET Core SignalR](https://learn.microsoft.com/en-us/aspnet/core/signalr/security?view=aspnetcore-10.0)
- [ASP.NET Core rate limiting middleware](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit?view=aspnetcore-10.0)
- [.NET bounded queue service with channels](https://learn.microsoft.com/en-us/dotnet/core/extensions/queue-service)
- [.NET MAUI secure storage](https://learn.microsoft.com/en-us/dotnet/maui/platform-integration/storage/secure-storage?view=net-maui-10.0)
- [ASP.NET Core Data Protection key storage providers](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/implementation/key-storage-providers?view=aspnetcore-10.0)
- [Kestrel HTTPS endpoint configuration](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/endpoints?view=aspnetcore-10.0)
- [RFC 9700: Best Current Practice for OAuth 2.0 Security](https://datatracker.ietf.org/doc/html/rfc9700)
- [NIST SP 800-63B: Authentication and Authenticator Management](https://pages.nist.gov/800-63-4/sp800-63b.html)
