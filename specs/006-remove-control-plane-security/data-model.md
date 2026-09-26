# Data Model: Remove the MOBApi control-plane security

## Remote command (`RuntimeCommandEnvelope`, existing)

| Command type | Required fields | Validation |
| --- | --- | --- |
| `SetLocomotiveDrive` | `LocomotiveAddress`, `Speed`, `Forward` | address 1-9999, speed 0-126 |
| `SetLocomotiveFunction` | `LocomotiveAddress`, `FunctionIndex`, `FunctionIsOn` | address 1-9999, function 0-31 |
| `SetSignalAspect` | `SignalId`, `SignalAspect` | non-empty identifier, defined aspect |
| `ResetJourney` | `JourneyId` | non-empty identifier |

- `Type` must be a defined `RuntimeCommandType` value. Fields that do not belong to the type are ignored.
- `ClientId` stays display metadata; it is never an authorization or validation input.

## Admission result (new)

- `Accepted`, `Invalid` (with a short English reason) or `QueueFull`.
- REST maps `Invalid` to `400` and `QueueFull` to `429`; SignalR maps both to `HubException`.

## Remote command queue (existing, changed)

- Bounded to 128 entries, first in first out, non-blocking enqueue.
- The desktop consumer continues to dequeue in order; nothing is dropped after acceptance.

## Removed data

- Credential registry, protected document store, data-protection keys and server certificate on the
  PC; remote-control credential store on the phone. Existing files are ignored, not migrated or deleted.
- Pairing and security configuration sections; removed JSON fields are ignored on load.
- Discovery response fields for server instance and fingerprint.
