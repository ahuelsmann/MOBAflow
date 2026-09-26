# Data Model: Remove the MOBApi control-plane security

## Remote command (`RuntimeCommandEnvelope`, existing)

| Command type | Required fields | Validation |
| --- | --- | --- |
| `SetLocomotiveDrive` | `LocomotiveAddress`, `Speed`, `Forward` | address 1-9999, speed 0-126 |
| `SetLocomotiveFunction` | `LocomotiveAddress`, `FunctionIndex`, `FunctionIsOn` | address 1-9999, function 0-31 |
| `SetSignalAspect` | `SignalId`, `SignalAspect` | non-empty GUID, defined aspect |
| `ResetJourney` | `JourneyId` | non-empty GUID |

- `Type` must be a defined `RuntimeCommandType` value. Fields that do not belong to the type are ignored.
- All command fields have fixed-size types; no separate payload-size limit exists beyond the unchanged
  ASP.NET Core request and SignalR message size limits.
- `ClientId` stays display metadata; it is never an authorization or validation input.

## Client presence (existing, changed)

- `api/clients/register|unregister` and `RuntimeHub.RegisterRemote` identify a client by the trimmed,
  non-empty `ClientId` of the request instead of a credential claim. A missing `ClientId` is rejected
  with `400` (REST) or `HubException` (SignalR).

## Admission result (new)

- `Accepted`, `Invalid` (with a short English reason) or `QueueFull`.
- REST maps `Invalid` to `400` and `QueueFull` to `429`; SignalR maps both to `HubException`.

## Remote command queue (existing, changed)

- Holds admitted commands only while no runtime host is connected through SignalR; with a connected
  host, SignalR commands are forwarded directly and bypass the queue.
- Bounded to 128 entries, first in first out, non-blocking enqueue.
- The desktop consumer continues to dequeue in order; nothing is dropped after acceptance.

## Removed data

- Credential registry, protected document store, data-protection keys and server certificate on the
  PC; remote-control credential store on the phone. Existing files are ignored, not migrated or deleted.
- Pairing and security configuration sections; removed JSON fields are ignored on load.
- Discovery response fields for server instance and fingerprint.
