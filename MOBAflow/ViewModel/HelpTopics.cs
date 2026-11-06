// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
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
        new("Train Control", false, ["Speed Control", "Functions F0-F28", "Direction", "Emergency Stop"]),
        new("Automation", false, ["Create Journey", "Define Stations", "Workflows", "Speech Announcements"]),
        new("Signal Box", false, ["Track Plan Editor", "Signals", "Switches", "Feedback Points"]),
        new("Track Plan", false, ["Track Libraries", "Place Tracks", "Connections"]),
        new("Configuration", false, ["Piper TTS Setup"]),
        new("Troubleshooting", false, ["No Connection", "Locomotive Not Responding", "Feedback Points Missing"])
    ];

    /// <summary>Gets the help text of <paramref name="section"/>, or <see cref="MissingSectionText"/>.</summary>
    public static string GetContent(string section)
    {
        return section switch
        {
            // Getting Started
            "Welcome" => """
                Welcome to MOBAflow - Your control center for model railways!

                MOBAflow connects directly to your Roco/Fleischmann Z21 digital command station and offers:

                - Locomotive control with speed and functions
                - Automated journeys with station announcements
                - Workflow automation for complex procedures
                - Electronic Signal Box (ESTW) for signals and switches
                - Track plan editor with various track libraries

                Select a topic on the left to learn more.
                """,

            "Installation" => """
                System Requirements:
                - Windows 10/11 (64-bit)
                - .NET 10 Runtime
                - Network connection to Z21

                Installation:
                1. Download MOBAflow from GitHub
                2. Extract the archive
                3. Start MOBAflow.exe
                4. .NET Runtime will be automatically installed on first run

                Updates:
                MOBAflow automatically checks for updates at startup.
                """,

            "Z21 Connection" => """
                Connect to Z21:

                1. Make sure your Z21 is powered on
                2. Connect your PC to the same network
                3. Enter the IP address of the Z21 (default: 192.168.0.111)
                4. Click "Connect"

                The Z21 uses the UDP port configured in the project's properties (21105 by default).
                Z21 Finder also probes UDP 21106 as an alternative Z21 port.
                MOBApi discovery on the PC also uses UDP 21106, with a different protocol.

                Troubleshooting:
                - Check your network connection
                - Make sure no firewall is blocking
                - Terminate Z21 app on smartphone beforehand
                """,

            "First Ride" => """
                Your first locomotive ride:

                1. Connect to the Z21
                2. Go to Train Control
                3. Select a locomotive or enter the DCC address
                4. Switch on track power (Track Power)
                5. Move the speed slider

                Tips:
                - F0 usually switches lights
                - Use the direction switch
                - Press STOP button in case of emergency
                """,

            // Train Control
            "Speed Control" => """
                Speed control:

                The speedometer displays the current speed step (0-126).
                
                Operation:
                - Slider for smooth control
                - Select a project locomotive from the list
                - Mouse wheel for fine adjustment

                DCC Speed Steps:
                - 14 steps (older decoders)
                - 28 steps (standard)
                - 126 steps (recommended)
                """,

            "Functions F0-F28" => """
                Locomotive functions:

                F0: Headlight/Marker light
                F1-F4: Often sound (horn, whistle, etc.)
                F5-F8: Additional sounds or lighting
                F9-F28: Additional functions depending on decoder

                Operation:
                - Click function button to toggle
                - Illuminated button = function active
                - Double-click for momentary functions
                """,

            "Direction" => """
                Change direction:

                - Click on the direction arrow
                - Or press spacebar
                - If speed > 0, locomotive will brake first

                Note:
                The direction depends on the decoder setting.
                If installed incorrectly, direction may be reversed.
                """,

            "Emergency Stop" => """
                Emergency stop function:

                STOP button: Immediate stop of all locomotives
                - Track power is cut off
                - All speeds set to 0

                Keyboard shortcut: Escape or F12

                After emergency stop:
                1. Fix the cause
                2. Switch track power back on
                3. Locomotives will not resume automatically
                """,

            // Automation
            "Create Journey" => """
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
                """,

            "Define Stations" => """
                Stations in a journey:

                Each station has:
                - Name (e.g., "Hamburg Central")
                - Feedback point ID (track sensor)
                - Departure left/right (for announcements)

                Workflow trigger:
                When a train reaches the feedback point:
                1. Speed is reduced
                2. Announcement is played
                3. Optional workflow starts
                """,

            "Workflows" => """
                Workflow automation:

                A workflow is a sequence of actions:
                - Control locomotive (speed, functions)
                - Wait (time or feedback point)
                - Play announcement
                - Switch/control signals

                Execution modes:
                - Sequential: Actions one after another
                - Parallel: Actions simultaneously
                - Loop: Repetition
                """,

            "Speech Announcements" => """
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
                """,

            // Signal Box
            "Track Plan Editor" => """
                Signal Box - Direct Layout Operation:

                The track plan editor allows:
                - Graphical display of your layout
                - Place tracks, signals, switches
                - Control via mouse click

                Operation:
                - Drag & drop from toolbar
                - Right-click for context menu
                - Double-click to control
                - Right mouse button to move plan

                Block occupancy is an observation only. MOBAflow does not reserve routes,
                lock turnouts or prevent collisions. The operator is responsible for safe operation.
                """,

            "Signals" => """
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
                """,

            "Switches" => """
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
                """,

            "Feedback Points" => """
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
                """,

            // Track Plan
            "Track Libraries" => """
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
                """,

            "Place Tracks" => """
                Place tracks in editor:

                1. Select a track from library
                2. Drag it onto the track plan
                3. Rotate with R key or right-click
                4. Connect with other tracks

                Tips:
                - Grid snapping can be disabled
                - Multi-select with Ctrl+Click
                - Copy with Ctrl+C, Paste with Ctrl+V
                """,

            "Connections" => """
                Track connections:

                Tracks connect automatically when:
                - Ends are close enough
                - Angles are compatible

                Display:
                - Green: Connected
                - Red: Not connected
                - Yellow: Warning (angle mismatch)

                Unconnected ends should be closed with buffers.
                """,

            // Configuration
            "Piper TTS Setup" => """
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
                """,

            // Troubleshooting
            "No Connection" => """
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
                """,

            "Locomotive Not Responding" => """
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
                """,

            "Feedback Points Missing" => """
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
                """,

            _ => MissingSectionText
        };
    }
}
