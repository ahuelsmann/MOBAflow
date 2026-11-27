// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.WinUI;

using System.Xml.Linq;

#if WINDOWS
using Moba.MOBAflow.Controls;
#endif

[TestFixture]
[Category("UI")]
internal static class CollapsibleColumnTests
{
    private static readonly XNamespace Xaml = "http://schemas.microsoft.com/winfx/2006/xaml";

    [TestCase("CollapsibleColumnHelper", "Unpin panel")]
    [TestCase("CollapsibleColumnCollection", "Collapse list")]
    [TestCase("CollapsibleColumnProperties", "Collapse panel")]
    public static void GenericXaml_CollapsedTab_IsFocusableButtonWithAccessibilityMetadata(
        string controlName,
        string expectedCollapseName)
    {
        var genericXaml = LoadGenericXaml();
        var targetType = $"controls:{controlName}";

        var template = genericXaml
            .Descendants()
            .Single(element =>
                element.Name.LocalName == "ControlTemplate"
                && element.Attribute("TargetType")?.Value == targetType);

        var collapsedTab = template
            .Descendants()
            .Single(element => element.Attribute(Xaml + "Name")?.Value == "CollapsedTab");

        var collapseButton = template
            .Descendants()
            .Single(element => element.Attribute(Xaml + "Name")?.Value == "CollapseButton");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(collapsedTab.Name.LocalName, Is.EqualTo("Button"));
            Assert.That(collapsedTab.Attribute("IsTabStop")?.Value, Is.EqualTo("True"));
            Assert.That(collapsedTab.Attribute("UseSystemFocusVisuals")?.Value, Is.EqualTo("True"));
            Assert.That(collapsedTab.Attribute("AutomationProperties.Name")?.Value, Is.EqualTo("{TemplateBinding Header}"));
            Assert.That(collapsedTab.Attribute("ToolTipService.ToolTip")?.Value, Is.EqualTo("{TemplateBinding Header}"));
            Assert.That(collapsedTab.Attribute("Visibility")?.Value, Is.EqualTo("Collapsed"));
            Assert.That(collapseButton.Name.LocalName, Is.EqualTo("Button"));
            Assert.That(collapseButton.Attribute("AutomationProperties.Name")?.Value, Is.EqualTo(expectedCollapseName));
        }
    }

#if WINDOWS
    [TestCase(null, "Expand panel")]
    [TestCase("", "Expand panel")]
    [TestCase("   ", "Expand panel")]
    [TestCase("Panel", "Expand Panel")]
    [TestCase("Workflows", "Expand Workflows panel")]
    [TestCase("  Properties  ", "Expand Properties panel")]
    [TestCase("Signal Box", "Expand Signal Box panel")]
    public static void FormatExpandTabAccessibleName_FormatsHeaderCleanly(string? header, string expectedName)
    {
        Assert.That(CollapsibleColumnBase.FormatExpandTabAccessibleName(header), Is.EqualTo(expectedName));
    }
#endif

    private static XDocument LoadGenericXaml()
    {
        var repositoryRoot = FindRepositoryRoot();
        return XDocument.Load(Path.Combine(repositoryRoot, "MOBAflow", "Themes", "Generic.xaml"));
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Moba.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new DirectoryNotFoundException("Could not locate the MOBAflow repository root.");
    }
}
