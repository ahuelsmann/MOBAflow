# MOBAflow Desktop user guide

MOBAflow Desktop is the Windows control center for a model railroad operated
through a Roco Z21. It combines live control with the editable layout model used
by MOBAsmart and MOBApi.

> [!CAUTION]
> MOBAflow sends commands to real hardware. Read the
> [hardware and liability notes](../HARDWARE-DISCLAIMER.md) before operating a
> layout.

## First session

1. Create a solution or open an existing `.json` solution file.
2. On **Solution**, assign one Z21 to each project with **Z21 Finder**, or enter
   the IP address and UDP port in the project's properties (`21105` by default).
3. Each project starts its own runtime and connects to its assigned Z21.
4. Select a project to inspect its connection and track-power state in the
   status bar; the other projects keep running.
5. If you use MOBAsmart, keep **Auto-start REST API with MOBAflow** enabled or
   start MOBApi separately.

MOBAflow can automatically reopen the last solution. Settings and solution data
are separate: app preferences live in the local settings file, while projects,
rolling stock, journeys and plans are stored in the solution JSON.

## Projects, Z21 assignment and safe switching

Each project represents one layout with its own runtime and its own Z21. All
projects of the loaded solution run at the same time. Selecting a project changes
what the pages show and which layout receives commands from those pages; it does
not stop or restart the other projects.

On **Solution**, expand **Z21 Finder** and search the network. It lists unassigned
command stations with their IP address and serial number. Drag one onto a project,
or double-click it to assign it to the selected project. You can also edit the
project's Z21 IP address and UDP port in its properties. Z21 endpoints belong to
projects, not to **Settings**; MOBAflow never searches for or replaces an assigned
Z21 automatically.

Each Z21 belongs to one project. If projects contain the same address, **Messages**
reports the conflict: the first project in solution order connects, and later
projects using that address stay disconnected. A project without an address also
stays disconnected while the other projects run normally.

Before opening or creating another solution, deleting a connected project or
closing MOBAflow, **Stop all trains?** asks for confirmation. Confirming sets all
known locomotives of the affected runtimes to speed 0 before those runtimes are
discarded; cancelling keeps the solution and runtimes unchanged. Changing a
connected project's Z21 assignment stops its known locomotives before replacing
its runtime on the new endpoint. Reloading a solution reuses existing open
connections after the stop sequence.

Saved master-data changes refresh the runtime's definitions without restarting
journeys or cancelling running workflows. Journey progress and current signal
aspects remain runtime state. Feedback counters are stored separately per project,
outside the solution JSON.

## Main areas

### Overview and Monitor

**Overview** shows the selected project's operating state, including Z21 telemetry,
track-power state, feedback counters, lap progress and connected clients.

**Monitor** shows continuous traffic and activity. The expandable **Messages**
area at the bottom of the main window contains structural project diagnostics,
such as invalid references or conflicting data.

### Rolling stock and trains

- **Locomotives:** maintain names, digital addresses, photos and function
  assignments. The management area also supports decoder/CV
  snapshots and printable locomotive passports.
- **Passenger Wagons** and **Goods Wagons:** maintain the wagon libraries.
- **Trains:** compose ordered consists from locomotives and both wagon types.
- **Train Control:** select a project locomotive, set direction and speed, use
  presets, trigger emergency stop and switch functions F0-F31.

MOBAflow reports digital-address conflicts in project diagnostics. Resolve them
before operating the affected locomotives.

#### Show locomotive usage

Select a locomotive and use the magnifying-glass button in the locomotive list
header, or choose **Show usage** in its context menu. **Messages** opens with each
train and vehicle position that refers to this locomotive. Positions start at 1;
repeated entries are shown separately. The query includes unsaved edits and does
not save, delete, reset or operate the layout.

**Edited project data** and **Loaded runtime definition (A)** are separate sources.
The runtime result comes from the existing project's current loaded definition,
even if that project is disconnected. Its observation time is shown. Missing
locomotive references are errors; valid uses are neutral information. Choose
**Show usage** again to capture a new comparison and observation time. The
**Refresh** button in **Messages** refreshes project diagnostics but keeps the
existing usage observation and its time; it does not run **Show usage** again.

The coverage summary remains visible when Information rows are hidden. Normal
project diagnostics also keep an Unknown coverage status visible independently of
severity filters when invalid identities prevent a precise analysis, even without
using Show usage.
**Retained definitions (B)** and **Operational use (C)** are **Unknown / Not checked
in this slice**. A zero-reference result applies only to the source just read;
it is not a claim that the locomotive is unused everywhere or safe to delete.
Unavailable owners, unsupported remote queries and changed definitions remain
explicitly unknown. Loading another solution or changing the selection clears
the previous query. Model changes also clear it without automatically starting
another usage query. Choose **Show usage** again to inspect the changed data.

### Recorder

**Recorder** captures supported runtime events and operator commands in a
timeline. Use **Start**, **Pause**, **Resume** and **Stop**, add markers or notes,
and export or import a `.mobarecording.json` file for later inspection.

Replay uses separate in-memory state, not the live layout. Disconnect the Z21
before using playback, single-step or seek. Replay does not send recorded
commands to hardware or run audio, announcements, scripts or display actions.
Only supported event and command types are captured; this is not a complete
recording of every UI interaction.

### Stations, journeys and events

**Stations** are reusable project entities with platforms and optional city
metadata. **Journeys** reference an ordered list of those stations.

A journey is either **active** or inactive. When feedback arrives, MOBAflow
evaluates the events of every active journey. Each event assigns a workflow to an
InPort and a count. MOBAflow counts every InPort separately and saves its count
locally, outside the solution file. Counts are restored after an app restart;
loading a solution or activating a journey does not reset them. An event runs
when an accepted feedback activation brings its InPort counter to the configured
count. Restoring or manually setting a count does not trigger a workflow.
List order does not define the trigger sequence.

Use **Event Manager** to edit a journey's events. **Journey Map** visualizes the
route and progress.

Stops change only through the **Change journey stop** workflow action. Advancing
past the last stop leaves the journey there. To return to the first stop, use
an action that selects that stop explicitly; there is no automatic wrap-around.

### Workflows

A workflow is a reusable, ordered list of actions. An event triggers the workflow
and supplies its execution context, including the related journey and current stop
when available. The same workflow can be assigned to several events.

Actions run from top to bottom. Each action finishes before the next starts;
**Delay after** adds a pause in milliseconds after that action. Actions currently support:

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
add and reorder actions, select an action, and edit its settings in the properties
pane. Use the up/down commands or drag an action to change its execution order.
Workflow settings contain its name and description. Deleting a referenced workflow
is blocked until its event assignments are removed or reassigned.

Earlier graph workflows must be recreated as action lists. Conditions, parallel
branches, nested workflows, and retry policies are no longer supported. Existing
complex definitions are not automatically flattened into a different sequence.

Use **Event Manager** to assign existing workflows to journey events. Compact rows
show the InPort, trigger count and workflow. Use **+** / **-** above the list to add
or delete events, and edit the selected event in **Properties**. **Values** contains
the searchable workflow library: drop a workflow onto a row to assign it, or onto
the free area to create an event. Double-clicking a workflow assigns it to the
selected event as well. Drag an event to reorder it; hold **Ctrl** to copy it.
Use **Values panel** and **Properties panel** in the event command bar's **More**
menu to toggle either panel with the keyboard.

Use the **Active** switch to activate or deactivate the selected journey; the
setting is saved with the solution. Events remain editable while a journey is
active, and changes take effect immediately. **Journey options** offers **Reset
all InPort counters**, which starts counting from zero again. Stop changes are
workflow actions; an event does not advance a stop implicitly. Workflow authoring and diagnostics remain on the **Workflows** page.

In **Overview**, each counter row also offers **Set** and **Reset**. Use a
non-negative whole number to correct one count. These commands update the saved
count and clear that input's timing history, but do not simulate feedback or run
journey events. A reset can let a later activation reach an event's count again.

Choose **Validate** before operating a workflow. Validation checks identifiers,
the action list, and action settings without running any actions. Invalid or
unsupported actions can be removed and recreated in the editor.
Choose **Dry run** to list a valid workflow's planned
effects without waiting or contacting Z21, audio, speech, scripts, displays, or
journey mutation handlers. **Recent trace** shows correlated lifecycle entries
for the selected workflow; traces are memory-only and reset with the process.

Execution stops safely when it is cancelled, its project's runtime is discarded
or disconnected, or the application shuts down. Selecting another project in the
UI keeps existing project workflows running. An action failure stops the workflow;
later actions do not run. Review the trace, correct the action settings, and let
a subsequent event start a new execution.

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

Without a mouse, press **Tab** to move from element to element on the plan; the
focused element shows a frame, and Narrator reads its kind, name and state, for
example "Signal A1, Ks1". **Enter** or **Space** selects it, and the properties
panel then offers its controls.

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
MOBApi process and publish the current solution and snapshots of every project.
MOBAsmart then discovers the endpoint on the LAN and selects one project with
**Project** on its Counter tab. Its direct Z21 connection uses that project's
address and port; snapshots and remote commands name the same project. Selecting
another project on the phone closes its previous Z21 connection while MOBAflow
keeps that project's runtime running.

The bridge supports solution synchronization, runtime state, remote commands,
journey progress, client registration and rolling-stock
photos. It needs no pairing or sign-in: every device in the home network can
connect, and MOBApi rejects remote commands with invalid values. See the
[project reference](../PROJECT-REFERENCE.md#mobapi-endpoints). The bridge is
designed for a trusted private LAN and must not be exposed to the public
internet.

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

With an Event Manager row focused, **Up/Down/Home/End** navigate, **Delete** removes
the event, **Ctrl+Delete** removes its workflow, **Ctrl+D** duplicates it and
**Alt+Up/Down** move it. These shortcuts do not delete events while a property input
is focused.

## Operator shortcuts

These shortcuts work on every page:

| Key | Action |
| --- | --- |
| Pause | Emergency stop: the locomotive selected in Train Control goes to speed step 0 |
| Space | Same emergency stop, unless a button, switch, list, menu or text input has focus; then Space keeps its normal meaning |
| Ctrl+P or F10 | Switch track power on or off |

The emergency stop affects only the locomotive selected in Train Control; it does
not stop other locomotives. Both shortcuts do nothing while the command is
unavailable, for example without a Z21 connection or a selected locomotive.

## Troubleshooting

### Z21 does not connect

- Confirm that the PC and Z21 are on the same private network.
- Verify the selected project's Z21 IP address and UDP port, normally `21105`.
- Resolve duplicate-Z21 assignments shown in **Messages**; each project needs its
  own Z21.
- Allow MOBAflow through Windows Firewall.
- Check **Monitor** and **Messages** before restarting hardware.

### MOBAsmart cannot find MOBAflow

- Verify that the REST API status in the desktop status bar is healthy.
- Keep the phone and PC on the same LAN and avoid guest-network isolation.
- Allow TCP port `5001` and the discovery traffic through the PC firewall.
- Tap the MOBAflow status in MOBAsmart to retry discovery.

### A journey does not advance

- Confirm that the journey is active.
- Compare incoming feedback in **Monitor** with the InPort and count configured in
  **Event Manager**. Counts continue from their saved values, independently of
  when the app was started or a journey was activated.
- If the counter load reports an error, counting stays disabled to avoid using
  unknown values. Resolve the storage problem and retry, or deliberately use
  **Reset all InPort counters** to replace the saved counts with zero.
- Check project diagnostics for missing stations or invalid references.
- Review timer filtering if legitimate events arrive very close together.

### A workflow fails

- Run **Validate** and resolve every reported action issue first.
- Use **Dry run** to confirm the action order and planned effects without
  contacting external systems.
- Confirm that payloads and referenced files exist and that required
  station/journey/feedback context is available.
- Review **Recent trace** for the failing workflow and action, then check
  **Monitor** and **Messages** for the corresponding external-system error.

## More documentation

- [Installation](INSTALLATION.md)
- [MOBAsmart guide](MOBASMART-USER-GUIDE.md)
- [Viessmann signal mapping](VIESSMANN-SIGNAL-MAPPING.md)
- [Project reference](../PROJECT-REFERENCE.md)
- [Third-party notices](../THIRD-PARTY-NOTICES.md)
