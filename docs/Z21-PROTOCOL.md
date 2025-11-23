# Z21 LAN Protocol (Reference)

## Quick reference
- Protocol: UDP (port 21105)
- Device: Roco Z21 / z21 protocol v1.13
- Use case in MOBAflow: Track feedback (R-BUS), system state messages, command sending
- **Keepalive**: Required every ~30s to prevent Z21 timeout/crash

## Connection Requirements
⚠️ **Important**: The Z21 expects regular communication!
- Without messages for ~60 seconds, the Z21 marks the connection as inactive
- Multiple inactive connections can cause the Z21 to become unstable or crash
- **Solution**: MOBAflow sends `LAN_X_GET_STATUS` every 30 seconds automatically

## Packet types used
- `0F-00-80-00` — R-BUS feedback events (track occupancy)
- `14-00-84-00` — System state change (voltage, temp, currents)
- `07-00-40-00` — XBus messages (track power / status)
- `08-00-50-00` — Broadcast flags acknowledgement
- `07-00-40-00-21-24-05` — LAN_X_GET_STATUS (keepalive, sent every 30s)

## Implementation notes
- The `Backend.Z21` class exposes events:
  - `event Feedback? Received` — raised for feedback packets (wraps FeedbackResult)
  - `event SystemStateChanged? OnSystemStateChanged` — raised for system state
- All UDP receives occur on a background thread. Do NOT perform UI updates here.
- Provide a simulation method (`SimulateFeedback(inPort)`) for offline testing.
- **Keepalive timer is managed automatically** (started on Connect, stopped on Disconnect).

## Testing tips
- Use `Z21.SimulateFeedback` to reproduce feedback handling without hardware.
- Monitor raw UDP with `tcpdump` / `Wireshark` when debugging networking issues.
- Ensure Z21 and host are in same subnet (/24 assumption used by helper methods).
- Keepalive messages can be observed in debug logs (LogLevel: Trace).

## Links
- Official Z21 LAN Protocol PDF: https://www.z21.eu/media/Kwc_Basic_DownloadTag_Component/47-1652-959-downloadTag/default/69bad87e/1699290251/z21-lan-protokoll.pdf

---

(Keep this file short; implementation details live in `Backend/Z21.cs` and `Backend/Manager/*`.)