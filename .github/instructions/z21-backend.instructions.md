---
description: 'Z21 packet parsing, connection lifecycle and railroad safety.'
applyTo: 'Backend/Z21*.cs,Backend/Protocol/Z21*.cs,Backend/Interface/IZ21.cs'
---

# Z21 changes

Follow [AGENTS.md](../../AGENTS.md) for workflow, threading and validation scope.
Keep protocol changes small and verify packet routing and state transitions together.

## Source map

- `Backend/Z21.cs`: construction, connection and disposal.
- `Backend/Z21.Receive.cs`: `OnUdpReceived`, raw callbacks and background EventBus publication.
- `Backend/Z21.Keepalive.cs`, `Backend/Z21.SystemStatePolling.cs`: connection confirmation and timers.
- `Backend/Z21.Commands.cs`, `Backend/Z21.Drive.cs`: outgoing commands.
- `Backend/Protocol/Z21MessageParser.cs`, `Z21Command.cs`, `Z21Protocol.cs`: parsing, encoding and constants.
- `Backend/Service/Z21Monitor.cs`: packet diagnostics.

## Packet routing

Read the complete affected handler before changing packet routing. Preserve independent top-level packet-type
branches and their returns. In particular, SystemState (`0x84`) must not be nested under RBus (`0x80`):
that makes the SystemState branch unreachable and can prevent connection confirmation.

| LAN header | Purpose |
| --- | --- |
| `0x40` | X-Bus commands/status/loco information |
| `0x80` | R-Bus occupancy feedback |
| `0x84` | System state (current, voltage, temperature) |
| `0x88` | RailCom data |
| `0x10` | Serial number |
| `0x1A` | Hardware information |

Reuse `Z21Protocol` constants and parser/builders. Validate lengths before accessing fields and preserve
byte order, DCC address encoding and X-Bus checksums. Test malformed packets as well as valid ones.

## Connection and traffic

- Confirm connection after a valid response; a successful UDP send alone does not establish a responding Z21.
- Preserve broadcast-driven updates and the disabled-by-default system-state polling setting.
  Configured fallback polling begins after connection confirmation, not during the handshake.
- Preserve keepalive, disconnect/reconnect and timer disposal behavior. Do not enable all broadcast flags or
  add frequent polling without a requirement and measured need. Historical packet-rate estimates are not limits.
- Keep the UDP callback responsive and backend code independent of UI dispatchers.
- When diagnosing failures, inspect received headers, parser results, connection transitions and timer state
  using the existing monitor/logging. Avoid unrelated changes to parsing while adding diagnostics.

## Locomotive and UI state

- Loading a preset starts speed at zero with the established forward default. Preserve address/function settings;
  do not restore speed or automatically issue movement commands on startup.
- Preserve fail-safe/operator-acknowledgement behavior. Derive observed track state from runtime feedback rather
  than independently overwriting it in a UI command.
- Changes to speed, speed steps or maximum speed must notify all dependent display properties (`SpeedKmh`,
  `MaxSpeedStep`, etc.) where used. Verify actual property definitions instead of copying approximate formulas.

## Validation

Use `Test/Mocks/FakeUdpClientWrapper.cs` and the relevant `Test/Backend/Z21*Tests.cs` fixtures for packet, event,
connection and command regressions. Include runtime safety tests if connection or track-power behavior changes.
Build the affected backend/consumer targets using the root validation policy.

A hardware session may additionally check connection, telemetry, locomotive feedback and authorized track-power
or drive commands. Hardware checks are conditional on availability and authorization; report any remaining gap.
Do not turn every source edit into an automatic live railway test.