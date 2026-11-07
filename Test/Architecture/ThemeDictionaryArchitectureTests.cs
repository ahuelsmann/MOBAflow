// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Architecture;

using System.Xml.Linq;

/// <summary>
/// Windows contrast themes look up the "HighContrast" theme dictionary. A desktop brush missing there falls back
/// unpredictably, and a literal color there ignores the colors the user chose for their contrast theme.
/// </summary>
[TestFixture]
internal sealed class ThemeDictionaryArchitectureTests
{
    private static readonly XNamespace Xaml = "http://schemas.microsoft.com/winfx/2006/xaml";
    private static readonly string[] ExpectedThemes = ["Light", "Dark", "HighContrast"];

    [Test]
    public void DesktopThemeDictionaries_DefineTheSameBrushesInLightDarkAndHighContrast()
    {
        var themes = LoadThemeDictionaries();

        Assert.That(themes.Keys, Is.EquivalentTo(ExpectedThemes));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(Keys(themes["Dark"]), Is.EquivalentTo(Keys(themes["Light"])), "Dark differs from Light.");
            Assert.That(Keys(themes["HighContrast"]), Is.EquivalentTo(Keys(themes["Light"])), "HighContrast differs from Light.");
        }
    }

    [Test]
    public void HighContrastBrushes_UseOnlyWindowsContrastThemeColors()
    {
        var literal = LoadThemeDictionaries()["HighContrast"].Elements()
            .Where(brush => !(((string?)brush.Attribute("Color"))?.StartsWith("{ThemeResource SystemColor", StringComparison.Ordinal) ?? false))
            .Select(brush => (string?)brush.Attribute(Xaml + "Key"))
            .ToList();

        Assert.That(literal, Is.Empty, "Map these HighContrast brushes to a SystemColor...Color theme resource.");
    }

    private static List<string?> Keys(XElement dictionary) =>
        dictionary.Elements().Select(element => (string?)element.Attribute(Xaml + "Key")).ToList();

    private static Dictionary<string, XElement> LoadThemeDictionaries()
    {
        var app = XDocument.Load(Path.Combine(FindRepositoryRoot(), "MOBAflow", "App.xaml"));
        return app.Descendants()
            .Single(element => element.Name.LocalName == "ResourceDictionary.ThemeDictionaries")
            .Elements()
            .ToDictionary(dictionary => (string?)dictionary.Attribute(Xaml + "Key") ?? string.Empty);
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
