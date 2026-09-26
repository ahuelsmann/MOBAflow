# Contract: MOBApi REST and SignalR after RF-19

## Transport

- Plain HTTP on the configured port (default 5001). The separate HTTPS port is removed.
- No `Authorization` header is required or evaluated; old headers are ignored.
- No endpoint is restricted by the caller's network address.

## Removed endpoints

- `api/control-plane/token/*`, `api/control-plane/pairing/*`, `api/control-plane/security/*`
  (including `read-migration/*`) and `api/control-plane/host/*`.

## Retained endpoints (now anonymous)

- `api/clients/register|unregister`, `api/runtime/meta|snapshot`, `api/runtime-settings`,
  `api/solution` (meta, get, put), `api/status`, `api/photos/health|file|upload`,
  `api/runtime/journeys/{id}/feedback-progress` (get, reset), `api/runtime/commands/*`.
- Hubs `/runtime-hub` and `/photos-hub` with their existing methods.

## Remote commands

| Transport | Invalid value | Queue full | Accepted |
| --- | --- | --- | --- |
| REST `api/runtime/commands/*` | `400` with `{ "error": "<reason>" }` | `429` with `{ "error": "queue_full" }` | `202` (unchanged) |
| REST `api/runtime/journeys/{id}/feedback-progress/reset` | `400` for an empty identifier | `429` with `{ "error": "queue_full" }` | `202` (unchanged) |
| SignalR `SetLocomotiveDrive`, `SetLocomotiveFunction`, `SetSignalAspect` | `HubException` with reason | `HubException` "Command queue is full." | completes |

Both transports validate before forwarding to the host or queueing.

## Discovery response

- Carries address and HTTP port only; server instance ID, HTTPS port and fingerprint are removed.
