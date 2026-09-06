# MOBAflow Desktop user guide

MOBAflow Desktop is the Windows control center for a model railroad operated
through a Roco Z21. It combines live control with the editable layout model used
by MOBAsmart and MOBApi.

> [!CAUTION]
> MOBAflow sends commands to real hardware. Read the
> [hardware and liability notes](../HARDWARE-DISCLAIMER.md) before operating a
> layout.

## First session

1. Open **Settings** and verify the Z21 IP address and UDP port.
2. Create a solution or open an existing `.json` solution file.
3. Select the project you want to operate.
4. Connect to the Z21 and verify the connection and track-power state in the
   status bar.
5. If you use MOBAsmart, keep **Auto-start REST API with MOBAflow** enabled or
   start MOBApi separately.

MOBAflow can automatically reopen the last solution. Settings and solution data
are separate: app preferences live in the local settings file, while projects,
rolling stock, journeys and plans are stored in the solution JSON.

## Main areas

### Overview and Monitor

**Overview** shows the active operating state, including Z21 telemetry,
track-power state, feedback counters, lap progress and connected clients.

**Monitor** shows continuous traffic and activity. The expandable **Messages**
area at the bottom of the main window contains structural project diagnostics,
such as invalid references or conflicting data.

### Rolling stock and trains

- **Locomotives:** maintain names, digital addresses, photos and function
  assignments. The management area also supports maintenance plans, decoder/CV
  snapshots and printable locomotive passports.
- **Passenger Wagons** and **Goods Wagons:** maintain the wagon libraries.
- **Trains:** compose ordered consists from locomotives and both wagon types.
- **Train Control:** select a project locomotive, set direction and speed, use
  presets, trigger emergency stop and switch functions F0-F31.

MOBAflow reports digital-address conflicts in project diagnostics. Resolve them
before operating the affected locomotives.

### Stations, journeys and feedback sequences

**Stations** are reusable project entities with platforms and optional city
metadata. **Journeys** reference an ordered list of those stations.

A journey advances through an explicit **feedback sequence**. Each step records
the expected feedback input and can be associated with a station transition.
This allows repeated inputs and route changes to be represented without relying
on one global journey input.

Use **Event Manager** to edit the ordered feedback steps visually. Journey
progress is kept in runtime state and synchronized through MOBApi for connected
mobile clients. **Journey Map** visualizes the active route and progress.

At the last stop, a journey can stop, restart or continue with another journey,
depending on its configured behavior.

### Workflows

Workflow 2.0 represents automation as a graph of typed steps:

- **Action** performs one configured action and continues to its next step.
- **Delay** waits for the configured milliseconds before continuing.
- **Condition** selects a true or false target from feedback, journey, or station context.
- **Parallel** runs named branches and waits at an explicit join step.
- **Nested workflow** calls another workflow and then continues.
- **Terminate** ends the run as succeeded, cancelled, or failed.

Action steps currently support:

- spoken announcement;
- WAV audio playback;
- raw Z21 command;
- PowerShell script;
- signal aspect selection;
- train destination display refresh; and
- active-journey stop transition.

The matrix action type exists in the data model but does not currently have a
runtime handler.

Use **Workflows** for library-focused authoring. Create or duplicate a workflow,
add and reorder steps, select a step, and edit its typed properties in the
editor pane. Set every successor, branch, join, nested-workflow, and failure
target to a valid step or workflow ID. The first step is the workflow entry by
default. Deleting a referenced workflow is blocked until every Event Manager or
nested-workflow reference is removed or reassigned.

Use **Event Manager** when authoring in journey context. It exposes the same
workflow collection and selection, so edits made on either page are immediately
visible on the other. Select a journey feedback occurrence and choose **Assign
selected workflow to feedback step** to link it.

Choose **Validate** before operating a workflow. Validation reports structural,
reference, payload, retry, recursion, and parallel-resource conflicts without
running the graph. Choose **Dry run** to traverse a valid graph and list planned
effects without waiting or contacting Z21, audio, speech, scripts, displays, or
journey mutation handlers. **Recent trace** shows correlated lifecycle entries
for the selected workflow; traces are memory-only and reset with the process.

Execution stops safely when it is cancelled, the project changes, the runtime
disconnects, or the application shuts down. A step-specific failure policy
overrides the workflow default and can stop, continue, follow a failure branch,
or retry a bounded number of times.

### Track Plan

The visual track-plan editor supports:

- Piko A track pieces from the toolbox;
- drag and drop, positioning and rotation;
- snap-to-connect and explicit disconnect;
- feedback-point assignment and topology validation;
- Undo/Redo, zoom, fit-to-view and selection tools;
- AnyRail XML import; and
- SVG export.

The currently shipped catalog is Piko A. Other track systems are not yet
included.

### Signal Box

The Signal Box editor manages signals, switches and routes. Signal aspects can
be operated against a connected Z21. Viessmann multiplex mappings and per-output
polarity are available for supported signal configurations.

### Matrix Images and ESP32 Display

**Matrix Images** provides a 5x5 color editor whose images are stored in the
project. To test one network display, enter its IP address and UDP port under
**Settings > ESP32 Display**, then open **ESP32 Display** and select **Connect and
negotiate**. The page reads the resolution, protocol version, firmware identity,
adapter identity, supported formats and rotations, and current health directly
from the device.

The standard test pattern stays disabled until the endpoint is valid and the
current connection has negotiated live capabilities. Brightness and the
device-rendered pattern are enabled only when the device advertises those
commands. After an endpoint change, application restart, device reboot, or stale
session response, reconnect before sending another command. MOBAflow saves only
the configured IP address and port; it does not save Wi-Fi credentials,
capabilities, or session IDs.

The `MOBAdisplay` library can render RGB565 data and send validated protocol
v1.0 frame transactions over UDP. The PlatformIO firmware under
`MOBAdisplay/esp32/` negotiates device capabilities, provisions Wi-Fi
credentials through its protected setup boundary, and presents only complete
frames. End-to-end destination-display workflows remain preview-stage because
the registered workflow handler currently skips output when no display service
is configured. The older `MobaDisplay.ino` sketch is only a hardware color test.

## Announcements

MOBAflow supports two local speech engines:

- **Piper TTS** with a local executable and ONNX voice model; and
- **System Speech (Windows SAPI)** as a Windows fallback.

Configure and test the selected engine in **Settings → Speech Synthesis**. See
the [Piper setup guide](PIPER-TTS-SETUP.md) for installation details. Speech
language and voice are independent from the English application UI.

## MOBAsmart and MOBApi

MOBApi is the local REST and SignalR bridge. MOBAflow can start its own isolated
MOBApi process and publish the current solution, runtime settings and snapshots.
MOBAsmart then discovers the endpoint on the LAN.

The bridge supports solution synchronization, runtime state, remote commands,
journey progress, feedback sequences, client registration and rolling-stock
photos. Protected desktop-host communication and certificate-pinned MOBAsmart
pairing are available while authenticated remote-read and command enforcement
is still being completed. The bridge is designed for a trusted private LAN and
must not be exposed to the public internet.

## Useful Track Plan keys

The Track Plan page supports the shortcuts implemented directly by that editor:

| Key | Action |
| --- | --- |
| Delete or Backspace | Delete the selection |
| Ctrl+Z / Ctrl+Y | Undo / Redo |
| Ctrl++ / Ctrl+- | Zoom in / out |
| Ctrl+0 | Fit the plan to the viewport |
| Ctrl+1 | Reset zoom to 100% |
| R | Disconnect the selected track connection |

The Event Manager also supports Delete for the selected feedback step. Other
global shortcuts are not currently defined.

## Troubleshooting

### Z21 does not connect

- Confirm that the PC and Z21 are on the same private network.
- Verify the configured IP address and UDP port, normally `21105`.
- Allow MOBAflow through Windows Firewall.
- Check **Monitor** and **Messages** before restarting hardware.

### MOBAsmart cannot find MOBAflow

- Verify that the REST API status in the desktop status bar is healthy.
- Keep the phone and PC on the same LAN and avoid guest-network isolation.
- Allow TCP port `5001` and the discovery traffic through the PC firewall.
- Tap the MOBAflow status in MOBAsmart to retry discovery.

### A journey does not advance

- Confirm that the journey is active.
- Compare incoming feedback in **Monitor** with the ordered sequence in
  **Event Manager**.
- Check project diagnostics for missing stations or invalid references.
- Review timer filtering if legitimate events arrive very close together.

### A workflow fails

- Run **Validate** and resolve every reported step/reference issue first.
- Use **Dry run** to confirm the selected branch and planned effects without
  contacting external systems.
- Confirm that payloads and referenced files exist and that required
  station/journey/feedback context is available.
- Review **Recent trace** for the failing workflow and step, then check
  **Monitor** and **Messages** for the corresponding external-system error.

## More documentation

- [Installation](INSTALLATION.md)
- [MOBAsmart guide](MOBASMART-USER-GUIDE.md)
- [Viessmann signal mapping](VIESSMANN-SIGNAL-MAPPING.md)
- [Project reference](../PROJECT-REFERENCE.md)
- [Third-party notices](../THIRD-PARTY-NOTICES.md)
