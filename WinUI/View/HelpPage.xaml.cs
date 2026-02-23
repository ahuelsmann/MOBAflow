namespace Moba.WinUI.View;

using Common.Navigation;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Navigation;

[NavigationItem(
    Tag = "help",
    Title = "Help",
    Icon = "\uE897",
    Category = NavigationCategory.Help,
    Order = 10)]
internal sealed partial class HelpPage
{
    private readonly Dictionary<TreeViewNode, string> _sections = [];

    public HelpPage()
    {
        InitializeComponent();
        Loaded += (s, e) => InitializePage();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is string section)
        {
            ShowContent(section);
        }
    }

    private void InitializePage()
    {
        // Getting Started
        var gettingStarted = new TreeViewNode { Content = "Getting Started", IsExpanded = true };
        AddSection(gettingStarted, "Welcome");
        AddSection(gettingStarted, "Installation");
        AddSection(gettingStarted, "Z21 Connection");
        AddSection(gettingStarted, "First Ride");
        NavigationTreeView.RootNodes.Add(gettingStarted);

        // Train Control
        var trainControl = new TreeViewNode { Content = "Train Control", IsExpanded = false };
        AddSection(trainControl, "Speed Control");
        AddSection(trainControl, "Functions F0-F28");
        AddSection(trainControl, "Direction");
        AddSection(trainControl, "Emergency Stop");
        NavigationTreeView.RootNodes.Add(trainControl);

        // Automation
        var automation = new TreeViewNode { Content = "Automation", IsExpanded = false };
        AddSection(automation, "Create Journey");
        AddSection(automation, "Define Stations");
        AddSection(automation, "Workflows");
        AddSection(automation, "Speech Announcements");
        NavigationTreeView.RootNodes.Add(automation);

        // Signal Box
        var signalBox = new TreeViewNode { Content = "Signal Box", IsExpanded = false };
        AddSection(signalBox, "Track Plan Editor");
        AddSection(signalBox, "Signals");
        AddSection(signalBox, "Switches");
        AddSection(signalBox, "Feedback Points");
        NavigationTreeView.RootNodes.Add(signalBox);

        // Track Plan
        var trackPlan = new TreeViewNode { Content = "Track Plan", IsExpanded = false };
        AddSection(trackPlan, "Track Libraries");
        AddSection(trackPlan, "Place Tracks");
        AddSection(trackPlan, "Connections");
        NavigationTreeView.RootNodes.Add(trackPlan);

        // Configuration
        var config = new TreeViewNode { Content = "Configuration", IsExpanded = false };
        AddSection(config, "Azure Speech Setup");
        NavigationTreeView.RootNodes.Add(config);

        // Troubleshooting
        var troubleshooting = new TreeViewNode { Content = "Troubleshooting", IsExpanded = false };
        AddSection(troubleshooting, "No Connection");
        AddSection(troubleshooting, "Locomotive Not Responding");
        AddSection(troubleshooting, "Feedback Points Missing");
        NavigationTreeView.RootNodes.Add(troubleshooting);

        ShowContent("Welcome");
    }

    private void AddSection(TreeViewNode parent, string topic)
    {
        var node = new TreeViewNode { Content = topic };
        _sections[node] = topic;
        parent.Children.Add(node);
    }

    private void OnNavigationItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
    {
        if (args.InvokedItem is TreeViewNode node && _sections.TryGetValue(node, out var section))
        {
            ShowContent(section);
        }
    }

    private void ShowContent(string section)
    {
        HeaderTextBlock.Text = section;
        ContentRichTextBlock.Blocks.Clear();

        var content = section switch
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

                The Z21 is addressed via UDP on port 21105.

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
                - Presets: Stop, Slow, Normal, Fast
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
                - Azure Cognitive Services (online, more natural)

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
                Signal Box - Electronic Interlocking:

                The track plan editor allows:
                - Graphical display of your layout
                - Place tracks, signals, switches
                - Control via mouse click

                Operation:
                - Drag & drop from toolbar
                - Right-click for context menu
                - Double-click to control
                - Right mouse button to move plan
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
            "Azure Speech Setup" => """
                Setting up Azure Speech Services:

                Azure Speech Services provide high-quality text-to-speech for MOBAflow.

                Step 1: Create Azure Account
                Visit https://portal.azure.com and sign in with your Microsoft account.

                Step 2: Create Speech Resource
                1. Click "Create a resource"
                2. Search for "Speech"
                3. Click "Create"
                4. Fill in details:
                   - Resource name: e.g., "mobaflow-speech"
                   - Region: Choose closest region (e.g., "Germany West Central")
                   - Pricing tier: Free (S0) or Pay-As-You-Go (Standard)
                5. Click "Review + Create" → "Create"

                Step 3: Get Your API Key
                1. Go to your new Speech resource
                2. Click "Keys and Endpoint" in left menu
                3. Copy "Key 1" or "Key 2"
                4. Paste into MOBAflow Settings → Speech Synthesis → Azure Speech Key

                Step 4: Configure Region
                1. In the same "Keys and Endpoint" page, copy the Region value
                   (e.g., "germanywestcentral")
                2. Paste into MOBAflow Settings → Speech Synthesis → Azure Region

                Step 5: Test
                Go to Settings → Speech Synthesis and click "Test Speech" button.

                Pricing:
                - Free tier: 5,000 characters/month
                - Standard: Charged per 1,000 characters

                Troubleshooting:
                - Check API key is correct (no extra spaces)
                - Verify region matches resource location
                - Ensure internet connection is active
                """,

            // Troubleshooting
            "No Connection" => """
                Problem: Cannot connect to Z21

                Checklist:
                1. Is Z21 powered on? (LED is lit)
                2. Is PC on the same network?
                3. Is IP address correct? (default: 192.168.0.111)
                4. Can you access Z21 Maintenance Tool?
                5. Is UDP port 21105 blocked by firewall?

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

            _ => "Select a topic from the menu."
        };

        var para = new Paragraph { Margin = new Thickness(0) };
        foreach (var line in content.Split('\n'))
        {
            para.Inlines.Add(new Run { Text = line });
            para.Inlines.Add(new LineBreak());
        }

        ContentRichTextBlock.Blocks.Clear();
        ContentRichTextBlock.Blocks.Add(para);
    }
}
