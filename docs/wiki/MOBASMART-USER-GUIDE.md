# MOBAsmart user guide

MOBAsmart is the Android companion for MOBAflow. It provides direct Z21 feedback
and locomotive operation while optionally using MOBAflow as the shared source
for projects, rolling stock, signal-box data and runtime state.

## What you need

- An Android device running Android 8.0 / API 26 or newer.
- The phone and Z21 on the same private LAN for direct control.
- For synchronized features: MOBAflow Desktop with MOBApi running on a PC in
  the same LAN.

Do not expose the Z21 or MOBApi to the public internet.

## Connections at a glance

MOBAsmart uses two related connections:

| Status | Purpose |
| --- | --- |
| **Z21** | Direct feedback, telemetry, track power and low-latency locomotive commands |
| **MOBAflow** | Solution/fleet sync, photos, signal-box plan, runtime snapshots and remote command fallback through MOBApi |

The app discovers the Z21 on the LAN. MOBAflow is trusted once by scanning the
short-lived QR code under **MOBAflow Settings / REST API**. The MOBAflow
connection can then be enabled or disabled with the switch on the Counter tab.

When both routes are available, locomotive commands prefer the direct local Z21
connection. Signal-box and shared domain state use the active MOBAflow session.

## The five tabs

### Counter

The Counter tab contains connection state and the lap-counter setup.

- View Z21 temperature and supply telemetry while connected.
- Turn track power on or off.
- Select how many feedback points should be counted.
- Set the target lap count.
- Enable a timer filter to suppress duplicate feedback events.
- Inspect lap count, last feedback and timing statistics for each input.
- Reset all counters.
- Toggle light/dark theme.
- Capture a photo and upload it to the connected MOBAflow library.

The camera button requires a reachable MOBApi endpoint. The photo is stored by
the desktop-side API, not only on the phone.

### SignalBox

The SignalBox tab displays the signals and switches from the selected MOBAflow
project. Choose a supported signal aspect or switch state to operate it.

The page can show cached elements after startup, but live, authoritative state
requires a connected MOBAflow runtime and its active Z21 connection.

### Engines

The Engines tab shows the synchronized project locomotives, including photos
served by MOBApi. Connect to MOBAflow at least once to populate and refresh the
mobile fleet cache.

Selecting a locomotive here makes it available to the Control tab.

### Control

The Control tab is the mobile throttle:

- emergency stop;
- speed slider and speed presets;
- direction control; and
- functions F0-F31 with the names and symbols from the project.

Controls are available when the app can execute through a local Z21 connection
or an active MOBAflow runtime session.

### Pairing

The Pairing tab contains one primary action: **Scan MOBAflow QR code**. The QR
code supplies the private-LAN endpoint and pinned server identity without manual
typing or fingerprint comparison. Every paired MOBAsmart installation receives
administrator access for viewing and controlling the layout.

After scanning, compare the six-digit confirmation code shown by both apps and
approve the device under **MOBAflow Settings / REST API**. MOBAsmart stores the
protected credential and starts solution and runtime synchronization immediately.

## Background operation

MOBAsmart uses an Android foreground service while a Z21 or MOBAflow session
needs to stay alive. Android may ask for notification permission, and the app
shows a persistent notification while the service is active.

Device vendors can still impose aggressive battery restrictions. If updates
stop when the screen turns off, allow background activity for MOBAsmart in the
device battery settings and keep Wi-Fi active during the operating session.

## Cached data and synchronization

The app stores a mobile cache of the last synchronized solution, locomotive
fleet and signal-box elements. This improves startup and lets pages display the
last known catalog while the network reconnects.

Cached values are not a second editable solution. MOBAflow remains authoritative
for project content, and live runtime snapshots replace cached operating state
when the SignalR connection is available.

## Troubleshooting

### Z21 stays offline

- Confirm the phone and Z21 use the same LAN and are not separated by a guest
  network.
- Disable mobile-data/VPN routing temporarily if it intercepts local traffic.
- Restart discovery by reopening the app after the Z21 is ready.
- Ensure no firewall or access-point rule blocks Z21 UDP traffic.

### MOBAflow shows Search or OFF

- **OFF** means the MOBAflow connection switch is disabled.
- **Search** means it is enabled but MOBApi has not been reached yet.
- Verify that MOBAflow reports a healthy REST API in its status bar.
- Keep the phone and PC on the same LAN and allow TCP port `5001` plus discovery
  traffic through the PC firewall.
- Tap the MOBAflow status to retry discovery.

### Signal Box or Engines is empty

- Open a MOBAflow solution and select a project containing the relevant data.
- Open Pairing and confirm that the device reports **Connected administrator**.
- Confirm that MOBAsmart has connected to MOBApi and the runtime hub.
- For Signal Box, verify that the selected project has a signal-box plan.
- Reconnect once to refresh an older mobile cache.

### Counters do not advance

- Verify that the direct Z21 status is ON.
- Increase the number of configured feedback points if the input lies outside
  the visible range.
- Check whether the timer-filter interval is suppressing legitimate repeated
  events.

### Photo upload fails

- Confirm the MOBAflow status is ON and that MOBApi can write to its configured
  photo directory.
- Grant the Android camera permission when requested.
- Check the PC firewall and avoid guest-network client isolation.

## Privacy and safety

MOBAsmart processes layout data on the local device and network. It does not need
a cloud account. Photos are transferred to the MOBApi host selected on the LAN.

Track-power, locomotive and signal controls affect physical hardware. Keep an
emergency-stop option within reach and follow the
[hardware safety guidance](../HARDWARE-DISCLAIMER.md).

## More documentation

- [MOBAflow Desktop guide](MOBAFLOW-USER-GUIDE.md)
- [Installation](INSTALLATION.md)
- [Track statistics quick start](QUICK-START-TRACK-STATISTICS.md)
- [Project reference](../PROJECT-REFERENCE.md)
