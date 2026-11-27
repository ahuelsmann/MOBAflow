// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Architecture;

using System.Xml;
using System.Xml.Linq;

/// <summary>
/// Narrator reads a button by its accessible name; a tooltip is not one. Every desktop button needs visible text,
/// a <c>Label</c> or an explicit <c>AutomationProperties.Name</c>. A <c>ToggleSwitch</c> needs a <c>Header</c>, on/off
/// text or an explicit name; a text block placed next to it is not its name.
/// </summary>
[TestFixture]
internal sealed class AccessibleNameArchitectureTests
{
    private static readonly HashSet<string> ButtonElements =
    [
        "Button", "AppBarButton", "AppBarToggleButton", "DropDownButton", "HyperlinkButton", "RepeatButton",
        "SplitButton", "ToggleButton", "ToggleSplitButton",
        // The app's own button types.
        "SelectionToggleButton"
    ];

    [Test]
    public void DesktopButtons_HaveAnAccessibleName()
    {
        var root = FindRepositoryRoot();
        var desktop = Path.Combine(root, "MOBAflow");
        var unnamed = Directory.EnumerateFiles(desktop, "*.xaml", SearchOption.AllDirectories)
            .Where(path => !IsBuildOutput(Path.GetRelativePath(desktop, path)))
            .SelectMany(path => FindUnnamedButtons(File.ReadAllText(path))
                .Select(line => $"{Path.GetRelativePath(root, path).Replace('\\', '/')}:{line}"))
            .ToList();

        Assert.That(unnamed, Is.Empty,
            "Add AutomationProperties.Name (the tooltip text is a good start) to these icon-only buttons.");
    }

    [TestCase("""<Button><FontIcon Glyph="&#xE710;" /></Button>""", true)]
    [TestCase("""<Button ToolTipService.ToolTip="Add"><FontIcon Glyph="&#xE710;" /></Button>""", true)]
    [TestCase("""<Button AutomationProperties.Name="Add"><FontIcon Glyph="&#xE710;" /></Button>""", false)]
    [TestCase("""<Button Content="Add" />""", false)]
    [TestCase("""<AppBarButton Label="Add" />""", false)]
    [TestCase("""<Button><StackPanel><FontIcon Glyph="&#xE710;" /><TextBlock Text="Add" /></StackPanel></Button>""", false)]
    [TestCase("""<DropDownButton><DropDownButton.Content><TextBlock Text="Export" /></DropDownButton.Content></DropDownButton>""", false)]
    [TestCase("""<Button><Button.Flyout><Flyout><TextBlock Text="Details" /></Flyout></Button.Flyout><FontIcon Glyph="&#xE710;" /></Button>""", true)]
    [TestCase("""<ToggleSwitch />""", true)]
    [TestCase("""<ToggleSwitch xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" OnContent="{x:Null}" OffContent="{x:Null}" />""", true)]
    [TestCase("""<ToggleSwitch ToolTipService.ToolTip="Track power" />""", true)]
    [TestCase("""<ToggleSwitch Header="Track power" />""", false)]
    [TestCase("""<ToggleSwitch OnContent="Forward" OffContent="Backward" />""", false)]
    [TestCase("""<ToggleSwitch AutomationProperties.Name="Track power" />""", false)]
    public void FindUnnamedButtons_ReportsOnlyButtonsWithoutAName(string button, bool reported)
    {
        var xaml = $"""
            <UserControl xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
            {button}
            </UserControl>
            """;

        Assert.That(FindUnnamedButtons(xaml), reported ? Has.Count.EqualTo(1) : Is.Empty);
    }

    [Test]
    public void TrackPlanPageButtons_HaveWinUiCompatibleAccessibleNames()
    {
        var root = FindRepositoryRoot();
        var trackPlanPath = Path.Combine(root, "MOBAflow", "View", "TrackPlanPage.xaml");
        var xaml = File.ReadAllText(trackPlanPath);
        var unnamed = FindUnnamedButtonsStrictWinUi(xaml);

        Assert.That(unnamed, Is.Empty,
            "TrackPlanPage buttons with non-string content (e.g., StackPanel/Border/FontIcon/SymbolIcon) or symbol-only text must declare explicit AutomationProperties.Name because WinUI ContentPresenter sets AccessibilityView=Raw.");

        var document = XDocument.Parse(xaml, LoadOptions.SetLineInfo);
        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
        var slidersWithoutName = document.Descendants()
            .Where(element => element.Name.LocalName == "Slider"
                && !element.Attributes().Any(attribute =>
                    attribute.Name.LocalName is "AutomationProperties.Name" or "AutomationProperties.LabeledBy"
                    || (attribute.Name.LocalName == "Header" && !string.IsNullOrWhiteSpace(attribute.Value))))
            .Select(element => ((IXmlLineInfo)element).LineNumber)
            .ToList();

        Assert.That(slidersWithoutName, Is.Empty,
            "Sliders in TrackPlanPage.xaml must expose Header or AutomationProperties.Name.");

        var elementsByXName = document.Descendants()
            .Where(element => element.Attribute(x + "Name") is not null)
            .GroupBy(element => element.Attribute(x + "Name")!.Value)
            .ToDictionary(group => group.Key, group => group.First());

        var commandBar = document.Descendants().FirstOrDefault(e => e.Name.LocalName == "CommandBar");
        Assert.That(commandBar, Is.Not.Null, "TrackPlanPage.xaml must contain a CommandBar.");
        Assert.That(commandBar!.Attribute("AutomationProperties.Name")?.Value, Is.Not.Null.And.Not.Empty);

        var overlayCanvas = elementsByXName["OverlayCanvas"];
        using (Assert.EnterMultipleScope())
        {
            foreach (var key in new[]
            {
                "ExportDropDownButton",
                "FitButton",
                "ResetZoomButton",
                "RotateLeftButton",
                "RotateRightButton",
                "ZoomOutButton",
                "ZoomInButton",
                "ZoomSlider",
                "CanvasScrollViewer"
            })
            {
                Assert.That(elementsByXName.ContainsKey(key), Is.True, $"Missing x:Name={key}");
                var name = elementsByXName[key].Attribute("AutomationProperties.Name")?.Value;
                Assert.That(string.IsNullOrWhiteSpace(name), Is.False, $"{key} must declare a non-empty AutomationProperties.Name.");
            }

            Assert.That(overlayCanvas.Name.LocalName, Is.EqualTo("AccessibleCanvas"),
                "OverlayCanvas must use AccessibleCanvas so WinUI exposes a UIA peer.");
            Assert.That(string.IsNullOrWhiteSpace(overlayCanvas.Attribute("AutomationProperties.Name")?.Value), Is.False);
            Assert.That(overlayCanvas.Attribute("IsTabStop")?.Value, Is.EqualTo("True"));
            Assert.That(overlayCanvas.Attribute("UseSystemFocusVisuals")?.Value, Is.EqualTo("True"));
        }
    }

    [TestCase("""<DropDownButton><DropDownButton.Content><StackPanel><SymbolIcon Symbol="OpenFile" /><TextBlock Text="Export" /></StackPanel></DropDownButton.Content></DropDownButton>""", true)]
    [TestCase("""<DropDownButton AutomationProperties.Name="Export options"><DropDownButton.Content><StackPanel><SymbolIcon Symbol="OpenFile" /><TextBlock Text="Export" /></StackPanel></DropDownButton.Content></DropDownButton>""", false)]
    [TestCase("""<Button AutomationProperties.Name="" Content="+" />""", true)]
    [TestCase("""<Button xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" AutomationProperties.Name="{x:Null}" Content="+" />""", true)]
    [TestCase("""<Button xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" Content="{x:Null}" />""", true)]
    [TestCase("""<Button Content="+" />""", true)]
    [TestCase("""<Button Content="−" />""", true)]
    [TestCase("""<Button AutomationProperties.Name="Zoom in" Content="+" />""", false)]
    [TestCase("""<Button><Border /></Button>""", true)]
    [TestCase("""<Button><FontIcon Glyph="&#xE710;" /></Button>""", true)]
    [TestCase("""<Button Content="Create Section" />""", false)]
    public void FindUnnamedButtonsStrictWinUi_ReportsNonTextualAndSymbolOnlyButtons(string button, bool reported)
    {
        var xaml = $"""
            <UserControl xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
            {button}
            </UserControl>
            """;

        Assert.That(FindUnnamedButtonsStrictWinUi(xaml), reported ? Has.Count.EqualTo(1) : Is.Empty);
    }

    private static List<int> FindUnnamedButtons(string xaml)
    {
        var document = XDocument.Parse(xaml, LoadOptions.SetLineInfo);
        return document.Descendants()
            .Where(element => (ButtonElements.Contains(element.Name.LocalName) && !HasAccessibleName(element))
                || (element.Name.LocalName == "ToggleSwitch" && !ToggleSwitchHasAccessibleName(element)))
            .Select(element => ((IXmlLineInfo)element).LineNumber)
            .ToList();
    }

    private static List<int> FindUnnamedButtonsStrictWinUi(string xaml)
    {
        var document = XDocument.Parse(xaml, LoadOptions.SetLineInfo);
        return document.Descendants()
            .Where(element => ButtonElements.Contains(element.Name.LocalName) && !HasWinUiUiAutomationName(element))
            .Select(element => ((IXmlLineInfo)element).LineNumber)
            .ToList();
    }

    private static bool HasWinUiUiAutomationName(XElement button) =>
        button.Attributes().Any(attribute =>
            ((attribute.Name.LocalName is "AutomationProperties.Name" or "AutomationProperties.LabeledBy")
                && IsUsableTextValue(attribute.Value))
            || ((attribute.Name.LocalName is "Content" or "Label")
                && IsUsableTextValue(attribute.Value)
                && attribute.Value.Any(char.IsLetterOrDigit)));

    private static bool IsUsableTextValue(string value) =>
        !string.IsNullOrWhiteSpace(value) &&
        !string.Equals(value.Trim(), "{x:Null}", StringComparison.Ordinal);

    private static bool HasAccessibleName(XElement button)
    {
        if (button.Attributes().Any(attribute =>
                attribute.Name.LocalName is "AutomationProperties.Name" or "AutomationProperties.LabeledBy"
                || (attribute.Name.LocalName is "Content" or "Label" && !string.IsNullOrWhiteSpace(attribute.Value))))
        {
            return true;
        }

        // Text inside the button content names it; text in other property elements such as a flyout or tooltip does not.
        return button.Elements()
            .Where(child => !child.Name.LocalName.Contains('.', StringComparison.Ordinal)
                || child.Name.LocalName.EndsWith(".Content", StringComparison.Ordinal))
            .DescendantsAndSelf()
            .Any(element => element.Name.LocalName == "TextBlock");
    }

    private static bool ToggleSwitchHasAccessibleName(XElement toggleSwitch) =>
        toggleSwitch.Attributes().Any(attribute =>
            attribute.Name.LocalName is "AutomationProperties.Name" or "AutomationProperties.LabeledBy"
            || (attribute.Name.LocalName is "Header" or "OnContent" or "OffContent"
                && !string.IsNullOrWhiteSpace(attribute.Value)
                && attribute.Value != "{x:Null}"))
        || toggleSwitch.Elements().Any(child => child.Name.LocalName is "ToggleSwitch.Header" or "ToggleSwitch.OnContent");

    private static bool IsBuildOutput(string relativePath)
    {
        var first = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)[0];
        return first is "bin" or "obj";
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Moba.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Moba.slnx was not found above the test directory.");
    }

#if WINDOWS
    [Test]
    public void AccessibleCanvas_DeclaresCustomPeerWithGroupAndLandmarkOverrides()
    {
        var canvasType = typeof(Moba.MOBAflow.View.AccessibleCanvas);
        var peerMethod = canvasType.GetMethod(
            "OnCreateAutomationPeer",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.DeclaredOnly);

        var peerType = typeof(Moba.MOBAflow.View.AccessibleCanvas.AccessibleCanvasAutomationPeer);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(peerMethod, Is.Not.Null, "AccessibleCanvas must override OnCreateAutomationPeer.");
            Assert.That(peerType.IsSubclassOf(typeof(Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer)), Is.True);
            Assert.That(Moba.MOBAflow.View.AccessibleCanvas.AccessibleCanvasAutomationPeer.ExpectedControlType,
                Is.EqualTo(Microsoft.UI.Xaml.Automation.Peers.AutomationControlType.Group));
            Assert.That(Moba.MOBAflow.View.AccessibleCanvas.AccessibleCanvasAutomationPeer.ExpectedLandmarkType,
                Is.EqualTo(Microsoft.UI.Xaml.Automation.Peers.AutomationLandmarkType.Custom));
            Assert.That(Moba.MOBAflow.View.AccessibleCanvas.AccessibleCanvasAutomationPeer.DefaultLocalizedControlType,
                Is.EqualTo("track plan canvas"));
            Assert.That(Moba.MOBAflow.View.AccessibleCanvas.AccessibleCanvasAutomationPeer.DefaultLocalizedLandmarkType,
                Is.EqualTo("Track plan"));

            foreach (var methodName in new[]
            {
                "GetAutomationControlTypeCore",
                "GetLandmarkTypeCore",
                "GetLocalizedControlTypeCore",
                "GetLocalizedLandmarkTypeCore",
                "GetNameCore",
                "IsControlElementCore",
                "IsContentElementCore"
            })
            {
                var overrideMethod = peerType.GetMethod(
                    methodName,
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.DeclaredOnly);
                Assert.That(overrideMethod, Is.Not.Null, $"Expected UIA override {methodName} on AccessibleCanvasAutomationPeer.");
            }
        }
    }

    [TestCase("Track plan canvas", "fallback", "Track plan canvas")]
    [TestCase("   ", "Canvas fallback", "Canvas fallback")]
    [TestCase(null, null, "Track plan canvas")]
    public void ResolveAutomationName_PrefersAttachedAutomationNameAndFallsBackCleanly(
        string? attached,
        string? fallback,
        string expected)
    {
        Assert.That(
            Moba.MOBAflow.View.AccessibleCanvas.AccessibleCanvasAutomationPeer.ResolveAutomationName(attached, fallback),
            Is.EqualTo(expected));
    }
#endif
}
