# Backend Z21 Overview

## Sequence
1. ConnectAsync(ip)
2. Send LAN_SYSTEMSTATE_GETDATA
3. Send LAN_SET_BROADCASTFLAGS (all)
4. Start keepalive timer (30s interval)
5. Receive loop via UDP wrapper events

## Keepalive Mechanism
- **Purpose**: Prevents Z21 timeout and crashes
- **Interval**: 30 seconds
- **Command**: LAN_X_GET_STATUS (0x07-00-40-00-21-24-05)
- **Why needed**: Z21 expects regular communication; without it:
  - Connection times out after ~60 seconds
  - Multiple inactive connections can cause Z21 to crash
  - Z21 may become unstable with accumulated inactive clients

## Message Mapping
- LAN_X_HEADER (0x40 0x00): X-Bus messages
  - X_STATUS (0x61), X_STATUS_CHANGED (0x62) → parsed flags
- LAN_RBUS_DATACHANGED (0x80 0x00): feedback events → `FeedbackResult`
- LAN_SYSTEMSTATE (0x84 0x00): system state → mapped to `SystemState`
- LAN_SYSTEMSTATE_GETDATA (0x85 0x00): handshake on connect
- LAN_X_GET_STATUS: keepalive message (sent every 30s)

## Notes
- Use `Z21Protocol` for constants/headers.
- Parsing lives in `Z21MessageParser` and returns DTOs for tests and clarity.
- UDP I/O via `IUdpClientWrapper` (DI-injected).
- Keepalive timer is automatically managed (started on Connect, stopped on Disconnect/Dispose).
