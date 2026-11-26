// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
using System.Collections.Frozen;

namespace Moba.WinUI.ViewModel;

/// <summary>A help category with its sections, in display order.</summary>
internal sealed record HelpCategory(string Title, bool IsExpandedByDefault, string[] Sections);

/// <summary>
/// The desktop help topics: the category tree and the text of each section.
/// </summary>
internal static class HelpTopics
{
    /// <summary>The section shown when the help page opens without a requested section.</summary>
    public const string DefaultSection = "Welcome";

    /// <summary>The text shown for a section that has no help text.</summary>
    public const string MissingSectionText = "Select a topic from the menu.";

    /// <summary>Gets the help categories in display order.</summary>
    public static HelpCategory[] Categories { get; } =
    [
        new("Getting Started", true, ["Welcome", "Installation", "Z21 Connection", "First Ride"]),
        new("Train Control", false, ["Speed Control", "Functions F0-F31", "Direction", "Emergency Stop"]),
        new("Automation", false, ["Create Journey", "Define Stations", "Timetable", "Workflows", "Event Manager", "Journey Map", "Speech Announcements"]),
        new("Signal Box", false, ["Track Plan Editor", "Signals", "Switches", "Feedback Points", "ESP32 Display", "Matrix Images"]),
        new("Track Plan", false, ["Track Libraries", "Place Tracks", "Connections"]),
        new("Monitoring", false, ["Monitor", "Recorder"]),
        new("Configuration", false, ["Settings", "Feature Toggles", "REST API", "MOBAsmart", "Piper TTS Setup"]),
        new("Troubleshooting", false, ["No Connection", "Locomotive Not Responding", "Feedback Points Missing"])
    ];

    private const string TopicsCatalog = """
        === Welcome
        Welcome to MOBAflow - Your control center for model railways!

        MOBAflow connects directly to your Roco/Fleischmann Z21 digital command station and offers:

        - Locomotive control with speed and functions
        - Automated journeys with station announcements
        - Workflow automation for complex procedures
        - Electronic Signal Box (ESTW) for signals and switches
        - Track plan editor with various track libraries

        Select a topic on the left to learn more.

        === Installation
        System Requirements:
        - Windows 11 22H2 (build 22621) or newer (64-bit)
        - .NET 10 SDK (as configured in global.json)
        - Network connection to Z21

        Build & Run from Source:
        1. Clone MOBAflow from GitHub
        2. Build with: dotnet build MOBAflow/MOBAflow.csproj
        3. Start with: dotnet run --project MOBAflow/MOBAflow.csproj
           (or open Moba.slnx in Visual Studio)

        Updates:
        MOBAflow does not include an automatic updater; pull new source
        versions from GitHub when needed.

        === Z21 Connection
        Connect to Z21:

        1. Make sure your Z21 is powered on
        2. Connect your PC to the same network
        3. Open the Solution page and expand Z21 Finder to search the network
        4. Drag an unassigned Z21 onto a project, or double-click to assign it
           to the selected project (or enter IP address and UDP port per project)

        Each project runs independently with its own Z21 assignment (default IP: 192.168.0.111).
        The Z21 uses the UDP port configured in the project's properties (21105 by default).
        Z21 Finder also probes UDP 21106 as an alternative Z21 port.
        MOBApi discovery on the PC also uses UDP 21106, with a different protocol.

        Troubleshooting:
        - Check your network connection
        - Make sure no firewall is blocking
        - Confirm each project uses a unique Z21 address
        - Terminate Z21 app on smartphone beforehand

        === First Ride
        Your first locomotive ride:

        1. Assign a Z21 to your project on the Solution page
        2. Go to Train Control
        3. Select a locomotive or enter the DCC address
        4. Switch on track power (Track Power, Ctrl+P or F10)
        5. Move the speed slider

        Tips:
        - F0 usually switches lights
        - Use the Direction toggle switch
        - Use EMERGENCY STOP (Pause or Space) in case of emergency

        === Speed Control
        Speed control:

        The speedometer displays the current speed step (0-126 in 128-step mode).

        Operation:
        - Slider for smooth control
        - Select a project locomotive from the list
        - Mouse wheel for fine adjustment

        DCC Speed Steps:
        - 14 steps (older decoders, speed steps 0-13)
        - 28 steps (standard, speed steps 0-27)
        - 128 steps (fine control, speed steps 0-126)

        === Functions F0-F31
        Locomotive functions:

        F0: Headlight/Marker light
        F1-F4: Often sound (horn, whistle, etc.)
        F5-F8: Additional sounds or lighting
        F9-F31: Additional functions depending on decoder

        Operation:
        - Click function button to toggle
        - Illuminated button = function active
        - Double-click for momentary functions

        === Direction
        Change direction:

        - Use the Direction toggle switch (Forward / Backward) in Train Control
        - If Acceleration Ramp is enabled and speed > 0, the locomotive ramps down
          to 0 before reversing and ramping back up

        Note:
        The direction depends on the decoder setting.
        If installed incorrectly, direction may be reversed.

        === Emergency Stop
        Emergency stop function:

        EMERGENCY STOP button: Immediately sets the selected locomotive in Train Control
        to speed step 0
        - Only the locomotive selected in Train Control is stopped
        - Other locomotives and track power are not affected (toggle track power with Ctrl+P or F10)

        Keyboard shortcuts (available across pages when a Z21 is connected and a locomotive is selected):
        - Pause
        - Space (unless a button, toggle switch, list, menu, or text input has focus)

        After emergency stop:
        1. Fix the cause
        2. The selected locomotive will not resume automatically; raise speed again when safe

        === Create Journey
        Define a journey:

        1. Create a new project or open existing one
        2. Go to "Journeys"
        3. Click "New Journey"
        4. Enter a name
        5. Add stations

        A journey consists of:
        - Assigned locomotive (DCC address)
        - List of stations
        - Announcement template

        === Define Stations
        Stations in a journey:

        Each station is a reusable project entity with:
        - Name (e.g., "Hamburg Central") and optional city metadata
        - Platforms, each with an InPort feedback address

        Workflow triggers:
        Feedback triggers are configured in the Event Manager:
        1. Add an event to an active journey
        2. Set the InPort and trigger count
        3. Assign a workflow to run when the InPort counter reaches that count

        === Timetable
        Timetable (Preview):

        Plan services and manage the current operating session:
        - Define services with station stops
        - Record arrivals and departures during operation

        Note: Timetable is marked with a Preview badge in the shipped settings
        and can be shown or hidden under Settings → Feature Toggles.

        === Workflows
        Workflow automation:

        A workflow is a reusable, ordered list of actions that run sequentially from top to bottom:
        - Spoken announcement
        - WAV audio playback
        - Raw Z21 command
        - PowerShell script
        - Signal aspect selection
        - Train destination display refresh
        - Active-journey stop transition

        Timing & authoring:
        - Each action finishes before the next starts
        - Delay after adds a pause in milliseconds after an action
        - Edit workflows in List or Graph view, verify with Validate or Dry run,
          and assign them to journey events in the Event Manager

        === Event Manager
        Event Manager (Preview):

        Assign reusable workflows to the events of an active journey:
        - Each event links an InPort feedback address and trigger count to a workflow
        - Use + / - or drag workflows from the Values library onto a row or free area
        - Keyboard shortcuts: Up/Down/Home/End navigate, Delete removes an event,
          Ctrl+Delete removes its workflow, Ctrl+D duplicates, Alt+Up/Down reorder
        - Persisted InPort counters continue across restarts until reset in
          Journey options (Reset all InPort counters)

        Note: Event Manager carries a Preview badge in the shipped settings.

        === Journey Map
        Journey Map:

        Visualizes the route and current station progress of the selected journey:
        - Displays the ordered stations of the selected journey
        - Active stops advance only through the "Change journey stop" workflow action
          without automatic wrap-around

        === Speech Announcements
        Station announcements:

        MOBAflow supports Text-to-Speech (TTS):
        - Windows Speech API (offline)
        - Piper TTS (offline, open source)

        Announcement templates:
        Use placeholders:
        {StationName} - Name of current station
        {NextStation} - Next station
        {TrainName} - Name of train
        {Time} - Current time

        Example: "Next stop: {NextStation}"

        === Track Plan Editor
        Track Plan Editor:

        The track plan editor allows:
        - Graphical display of your layout
        - Place Piko A track pieces, signals, and switches
        - Snap-to-connect, feedback-point (InPort) assignment, and validation
        - SVG export

        Operation:
        - Drag & drop track pieces from the toolbox
        - Drag the rotation handle to rotate a selected piece
        - Right-click for context menu

        Keyboard shortcuts:
        - Delete or Backspace: Delete the selection
        - Ctrl+Z / Ctrl+Y: Undo / Redo
        - Ctrl++ / Ctrl+-: Zoom in / out
        - Ctrl+0: Fit the plan to the viewport
        - Ctrl+1: Reset zoom to 100%
        - R: Disconnect the selected track connection

        Block occupancy is an observation only. MOBAflow does not reserve routes,
        lock turnouts or prevent collisions. The operator is responsible for safe operation.

        === Signals
        Signal control:

        Supported signal systems:
        - KS signals (combined signals)
        - H/V signals (main/distant signals)
        - Shunting signals (Sh/Ra)

        Signal aspects:
        - Hp0: Stop
        - Ks1: Clear (green)
        - Ks2: Clear with speed restriction
        - White light: Information

        Double-click on signal to change aspect.

        === Switches
        Switch control:

        Switch types:
        - Simple switch (left/right)
        - Three-way switch
        - Crossing switch (DKW)

        Operation:
        - Double-click to toggle position
        - Green = straight
        - Yellow = diverging

        Switch address is configured in properties.

        === Feedback Points
        Feedback points / Track occupancy detection:

        Feedback points detect where trains are located.
        The Z21 automatically sends feedback when occupancy changes.

        Types:
        - Current detectors (axle counters)
        - Reed switches
        - Light barriers

        Configuration:
        Each feedback point has an address (1-1024).
        This is assigned to stations in MOBAflow.

        === ESP32 Display
        ESP32 Display:

        Configure and test networked ESP32-S3 displays over UDP (protocol v1.0):
        1. Configure the display IP address and UDP port under Settings → ESP32 Display
        2. Open ESP32 Display and click "Connect and negotiate"
        3. Inspect device resolution, protocol capabilities, firmware/adapter identity,
           and health telemetry directly from the device
        4. Send test patterns once capabilities are negotiated

        Privacy note: MOBAflow stores only the IP address and UDP port;
        it never stores Wi-Fi credentials or session IDs.

        === Matrix Images
        Matrix Images:

        Create and edit 5x5 color LED matrix images stored with the project:
        - Left-click a pixel to set its color; right-click to clear it
        - Deleting an image asks for confirmation first (Cancel is default)

        === Track Libraries
        Available track libraries:

        - Piko A Track (active)
        - More coming...

        Libraries contain:
        - Straight tracks (various lengths)
        - Curves (various radii/angles)
        - Switches
        - Crossings
        - Buffers

        Tracks are displayed to scale.

        === Place Tracks
        Place tracks in editor:

        1. Select a track from the Piko A library
        2. Drag it onto the track plan
        3. Drag the rotation handle to rotate a selected piece
        4. Connect with other tracks (press R to disconnect a selected connection)

        Tips:
        - Undo / Redo with Ctrl+Z / Ctrl+Y
        - Delete selected pieces with Delete or Backspace
        - Zoom with Ctrl++ / Ctrl+-, fit with Ctrl+0, or reset to 100% with Ctrl+1

        === Connections
        Track connections:

        Tracks connect automatically when:
        - Ends are close enough
        - Angles are compatible

        Display:
        - Green: Connected
        - Red: Not connected
        - Yellow: Warning (angle mismatch)

        Unconnected ends should be closed with buffers.

        === Monitor
        Monitor:

        Follow live Z21 traffic and runtime activity as it happens:
        - Observe incoming feedback messages and command activity
        - Structural project diagnostics are shown in the expandable Messages
          area at the bottom of the main window

        === Recorder
        Recorder:

        Capture supported runtime events and operator commands on a timeline:
        - Use Start, Pause, Resume, and Stop controls
        - Add markers or notes during a recording session
        - Export or import recordings as .mobarecording.json files

        Replay safety:
        - Disconnect the Z21 before using playback, single-step, or seek
        - Replay runs against separate in-memory state: it never sends commands to hardware
          and never triggers audio, announcements, scripts, or display actions

        === Settings
        Settings:

        Configure application-wide preferences stored locally outside the solution JSON:
        - Z21 connection timing (per-project Z21 IP and port live on Solution)
        - ESP32 display endpoint (IP address and UDP port)
        - Speech synthesis engine (Windows Speech API or Piper TTS)
        - REST API auto-start with MOBAflow and application behavior
        - Layout reset, feedback point settings, health check, and feature toggles

        === Feature Toggles
        Feature Toggles:

        Under Settings → Feature Toggles, show or hide optional navigation pages:
        - Changes take effect immediately without restarting MOBAflow
        - Overview is always available and cannot be disabled
        - Pages carrying a Preview badge (Timetable and Event Manager by default)
          are usable features that may still change

        === REST API
        REST API (MOBApi):

        Local REST and SignalR bridge connecting MOBAflow and MOBAsmart on the LAN:
        - Runs on port 5001 by default (optional Auto-start REST API with MOBAflow)
        - Health and client sync state are visible on Overview and the status bar
        - Publishes the loaded solution, runtime snapshots, remote drive/signal
          commands, journey progress, and locomotive photos

        Security: Designed for a trusted private LAN without cloud accounts;
        must not be exposed to the public internet.

        === MOBAsmart
        MOBAsmart:

        Android companion app with four tabs (Counter, SignalBox, Engines, Control):
        - Connects directly to the Roco Z21 for track power and mobile control
        - Discovers MOBAflow via MOBApi on the local network to select a project,
          sync solution data and locomotive photos, operate signals/switches,
          and drive locomotives with speed slider and presets

        === Piper TTS Setup
        Setting up Piper TTS:

        Piper TTS provides local text-to-speech for MOBAflow without cloud API keys.

        Step 1: Install Piper
        1. Visit https://github.com/OHF-Voice/piper1-gpl
        2. Install with: py -m pip install piper-tts
        3. Use the generated piper.exe from your Python environment,
           for example .venv\Scripts\piper.exe

        Step 2: Download a voice model
        1. Visit https://huggingface.co/rhasspy/piper-voices
        2. Download a German .onnx voice model
        3. Keep the matching .json config file if one is provided

        Step 3: Configure MOBAflow
        1. Go to Settings → Speech Synthesis
        2. Select "Piper TTS"
        3. Set Piper Executable to piper.exe
        4. Set Piper Model to the .onnx voice model
        5. Optionally set Piper Config to the .json file

        Step 4: Test
        Go to Settings → Speech Synthesis and click "Test Speech" button.

        Troubleshooting:
        - Check that piper.exe exists
        - Check that the .onnx model path is correct
        - Run piper.exe --help in a terminal
        - Use Windows SAPI as fallback if the audio device fails

        === No Connection
        Problem: Cannot connect to Z21

        Checklist:
        1. Is Z21 powered on? (LED is lit)
        2. Is PC on the same network?
        3. Is IP address correct? (default: 192.168.0.111)
        4. Can you access Z21 Maintenance Tool?
        5. Is the project's configured Z21 UDP port blocked by firewall? (21105 by default)

        Test:
        - Ping 192.168.0.111 in command prompt
        - Test Z21 app on smartphone
        - Restart Z21 briefly

        Solution: Usually Z21 needs brief power cycle

        === Locomotive Not Responding
        Problem: Locomotive does not respond to commands

        Checklist:
        1. Is track power switched on? (Track Power)
        2. Is correct DCC address set?
        3. Is locomotive on the track?
        4. Is decoder compatible?

        Test:
        - Try different locomotive
        - Test locomotive on programming track
        - Check CV1 (address)

        Common cause: Wrong address or contact problems

        === Feedback Points Missing
        Problem: Feedback points not recognized

        Checklist:
        1. Does Z21 recognize feedback module?
        2. Is correct address configured?
        3. Is occupancy detector correctly wired?
        4. Is locomotive drawing current in this section?

        Test:
        - Check in Z21 Maintenance Tool
        - Manually trigger contact

        Note: Feedback is received via UDP broadcast
        """;

    private static readonly FrozenDictionary<string, string> ContentBySection = ParseTopics();

    /// <summary>Gets the help text of <paramref name="section"/>, or <see cref="MissingSectionText"/>.</summary>
    public static string GetContent(string section)
    {
        return section is not null && ContentBySection.TryGetValue(section, out var content)
            ? content
            : MissingSectionText;
    }

    private static FrozenDictionary<string, string> ParseTopics()
    {
        var topics = new Dictionary<string, string>(StringComparer.Ordinal);
        string? currentSection = null;
        var currentLines = new List<string>();

        foreach (var rawLine in TopicsCatalog.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            if (rawLine.StartsWith("=== ", StringComparison.Ordinal))
            {
                if (currentSection is not null)
                {
                    topics[currentSection] = string.Join('\n', currentLines).Trim();
                    currentLines.Clear();
                }

                currentSection = rawLine[4..];
                continue;
            }

            if (currentSection is not null)
            {
                currentLines.Add(rawLine);
            }
        }

        if (currentSection is not null)
        {
            topics[currentSection] = string.Join('\n', currentLines).Trim();
        }

        return topics.ToFrozenDictionary(StringComparer.Ordinal);
    }
}
