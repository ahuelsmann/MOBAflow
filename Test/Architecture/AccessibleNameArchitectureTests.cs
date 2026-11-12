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

    private static List<int> FindUnnamedButtons(string xaml)
    {
        var document = XDocument.Parse(xaml, LoadOptions.SetLineInfo);
        return document.Descendants()
            .Where(element => (ButtonElements.Contains(element.Name.LocalName) && !HasAccessibleName(element))
                || (element.Name.LocalName == "ToggleSwitch" && !ToggleSwitchHasAccessibleName(element)))
            .Select(element => ((IXmlLineInfo)element).LineNumber)
            .ToList();
    }

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
}
