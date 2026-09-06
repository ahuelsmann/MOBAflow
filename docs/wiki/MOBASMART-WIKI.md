# MOBAsmart technical notes

The maintained user documentation is the
[MOBAsmart user guide](MOBASMART-USER-GUIDE.md). This page records only the
technical behavior that is useful when diagnosing the Android app.

## Runtime model

MOBAsmart runs the shared `IMobaRuntime` locally and can also attach to the
MOBAflow runtime through MOBApi.

```text
Z21 <---- direct UDP ----> MOBAsmart local runtime
  ^                              |
  |                              | REST + SignalR
  +------ MOBAflow runtime <---- MOBApi
```

- Direct Z21 feedback drives the Counter tab.
- Locomotive commands prefer the local Z21 route when it is connected.
- MOBApi supplies the active solution, fleet, photos, feedback sequences and
  runtime snapshots.
- Signal-box commands use the MOBAflow session when it is active.
- The mobile cache supplies last-known project data during startup or temporary
  network loss; it is not authoritative for live state.

## Discovery and transport

MOBAsmart discovers MOBApi with the shared discovery protocol and falls back to
bounded LAN probing. Discovery advertises the compatibility HTTP endpoint and
the certificate-pinned HTTPS identity used by the explicit pairing flow. The
normal MOBApi HTTP port is `5001`; authenticated transport uses the advertised
HTTPS endpoint.

The runtime connection uses:

- REST for health, solution data, runtime settings, snapshots and commands;
- `/runtime-hub` for live runtime and solution notifications; and
- `/photos-hub` plus photo endpoints for rolling-stock images.

Authenticated remote reads and commands are still being rolled out, so pairing
does not yet replace every compatibility connection path.

Local-network VPNs, Android proxy routing and guest Wi-Fi isolation can prevent
discovery even when both devices appear to have internet access.

## Background behavior

The Android foreground service is kept active while a local Z21 connection or a
MOBAflow runtime session needs to survive backgrounding. Lifecycle handling
disconnects and reconnects the local Z21 to avoid leaving stale clients at the
command station.

## Settings and cache

App preferences are stored in the Android app-data directory. The synchronized
solution and selected runtime metadata are stored separately below the
`mobile-cache` directory managed by `MobileSolutionStore`.

These are implementation details; users should normally configure connections
through the app rather than editing files directly.

## Related documentation

- [MOBAsmart user guide](MOBASMART-USER-GUIDE.md)
- [MOBAflow/Z21 synchronization](../Z21-MOBAFLOW-SYNC.md)
- [Project reference](../PROJECT-REFERENCE.md)
- [Architecture](../ARCHITECTURE.md)
