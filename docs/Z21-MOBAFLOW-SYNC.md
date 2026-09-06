# Z21 Broadcast vs. MOBAflow Sync

This document records the architecture decision for synchronizing MOBAsmart and MOBAflow.
It complements [ARCHITECTURE.md](ARCHITECTURE.md) and version 1.13 of the Z21
LAN specification. The specification is not redistributed in this repository.

## Summary

| Layer | Role | Can Z21 replace it? |
|-------|------|---------------------|
| Z21 UDP | DCC transport, selective LAN broadcasts | — |
| Solution sync | Project catalog (GUIDs, fleet, signal-box plan) | No |
| RuntimeHub commands | Route mobile control through MOBAflow | No (multiplexer mapping) |
| RuntimeHub snapshots | Domain state (signals, journeys, fail-safe) | Partially |
| Local Z21 on MOBAsmart | Locomotive speed/functions when connected | Yes (with subscription limits) |

**Decision:** Keep MOBAflow sync. Z21 broadcasts are complementary, not a substitute.

## Z21 LAN broadcasts (what the central station actually shares)

Broadcast flags are set **per client** (IP + port) via `LAN_SET_BROADCASTFLAGS` and must be
re-applied after every connect. Constants live in
[`Common/Z21/Z21BroadcastRequirements.cs`](../Common/Z21/Z21BroadcastRequirements.cs).

MOBAflow subscribes with `Driving | Rbus | SystemState` (not `AllLocoInfo`).

| Message | Requires | Delivers |
|---------|----------|----------|
| `LAN_X_LOCO_INFO` | Driving flag + loco address subscription (max 16) | Speed, direction, F0–F31 |
| `LAN_X_TURNOUT_INFO` | Driving flag | Function address + P/A position only |
| `LAN_X_BC_*` | Driving flag | Track power, e-stop, short circuit |
| `LAN_RMBUS_DATACHANGED` | Rbus flag | Occupancy feedback |
| `LAN_X_SET_EXT_ACCESSORY` | — | No broadcast to other clients |

Turnout feedback does **not** carry KS signal aspects (`Hp0`, `Vr0`, …). Viessmann multiplex
signals are mapped in MOBAflow from `SignalAspect` to several `SetTurnout` commands.

Protocol behaviour is verified in
[`Test/Backend/Z21BroadcastProtocolTests.cs`](../Test/Backend/Z21BroadcastProtocolTests.cs).

## MOBAflow sync layers

```text
MOBAsmart                    MOBApi                     MOBAflow
    |                          |                            |
    |-- Solution GET --------->|                            |
    |<-- project JSON ---------|                            |
    |                          |                            |
    |-- RuntimeHub remote ---->|-- forward commands ------->|
    |<-- slim snapshot --------|<-- push snapshot -----------|
    |                          |                            |
    |-- Z21 UDP (local) ------------------------------------> Z21
```

### 1. Solution sync

[`SolutionRemoteLoader`](../SharedUI/Service/SolutionRemoteLoader.cs) downloads the active
project: signal GUIDs, names, fleet metadata, stellwerk layout. The Z21 has no knowledge of
this domain model.

### 2. Runtime command forwarding

[`MobileRuntimeCoordinator`](../SharedUI/Service/MobileRuntimeCoordinator.cs) routes
`SetSignalAspect`, `SetLocomotiveDrive`, and `SetLocomotiveFunction` to MOBAflow when a
MOBApi session is active. MOBAflow performs multiplexer resolution and Z21 I/O.

### 3. Runtime snapshot sync (slim profile)

[`RuntimeSnapshotRemoteFilter`](../Common/Runtime/RuntimeSnapshotRemoteFilter.cs) strips
`LocomotiveStates` before MOBApi / SignalR transport. Mobile clients read locomotive feedback
from their **local Z21 connection** when connected
([`TrainControlViewModel`](../SharedUI/ViewModel/TrainControlViewModel.cs), hybrid mode).

Snapshots still carry:

- `SignalBoxElements` (semantic aspects)
- `JourneyStates`
- `LocomotiveFleet` (catalog, not live drive state)
- Z21 telemetry and fail-safe flags

## Why full removal of sync is not viable

1. **Signals:** KS aspects are application semantics; Z21 only reports turnout P/A bits.
2. **Project objects:** GUIDs, names, and layout are not on the Z21 bus.
3. **Journeys / automation:** MOBAflow-only runtime state.
4. **Offline mobile:** Without direct Z21, MOBAsmart depends on MOBAflow snapshots entirely.
5. **Loco subscription limit:** 16 addresses per client without the high-traffic `AllLocoInfo` flag.

## Observability

`GET /api/status` exposes runtime sync diagnostics via
[`RuntimeStatusBuilder`](../MOBApi/Service/RuntimeStatusBuilder.cs):

- `lastSnapshotPayloadBytes`
- `totalSnapshotBroadcastCount`
- `lastSnapshotBroadcastAt`
- snapshot cache element counts

Use these fields as a baseline before further payload optimisation.

## Related tests

| Test file | Purpose |
|-----------|---------|
| `Test/Backend/Z21BroadcastProtocolTests.cs` | Protocol prerequisites, turnout vs loco routing |
| `Test/Common/RuntimeSnapshotRemoteFilterTests.cs` | Slim snapshot shape and payload size |
| `Test/MOBApi/RuntimeBroadcastMetricsTests.cs` | Broadcast metrics |
| `Test/SharedUI/ViewModelCharacterizationTests.cs` | Hybrid loco state from local Z21 |
