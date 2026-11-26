// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.MOBAdemo.Navigation;

using Moba.Test.MOBAdemo.Build;

/// <summary>
/// The demo's <c>navigation.json</c> must equal the WinUI navigation: the page registration, the feature toggle
/// registry and the labels of the shipped appsettings (issue #430 slice 5, spec FR-022, plan T4).
/// </summary>
[TestFixture]
internal sealed class NavigationDriftTests
{
    private static readonly string[] DemoKeys = ["traincontrol", "workflows", "trackplaneditor", "signalbox"];

    private static readonly string[] BoldKeys = ["traincontrol", "journeys"];

    private static readonly string[] SignalBoxOnly = ["signalbox"];

    private static readonly string[] SpecTitles =
    [
        "Overview",
        "Train Control",
        "Journeys", "Timetable", "Workflows", "Event Manager", "Journey Map",
        "Solution", "Stations", "Locomotives", "Passenger Wagons", "Goods Wagons", "Trains",
        "Track Plan", "Signal Box", "ESP32 Display", "Matrix Images",
        "Monitor", "Recorder",
        "Help", "Info", "Settings",
    ];

    private static IReadOnlyList<NavigationEntryData> App(string? registration = null, string? appSettings = null) =>
        NavigationSource.FromApp(
            registration ?? NavigationSource.RegistrationText,
            NavigationSource.CategoryText,
            appSettings ?? NavigationSource.AppSettingsText);

    private static (IReadOnlyList<NavigationEntryData> Entries, IReadOnlyList<string> DemoKeys) Json() =>
        NavigationSource.FromJson(File.ReadAllText(NavigationSource.NavigationJsonPath));

    private static string PageOverviewMarkdown() =>
        File.ReadAllText(Path.Combine(BuildScript.RepositoryRoot, "docs", "wiki", "PAGE-OVERVIEW.md"));

    private static IEnumerable<string> ExpectedDesktopCells() =>
        App().Select(entry => entry.Badge is null ? $"**{entry.Title}**" : $"**{entry.Title}** ({entry.Badge})");

    private static IEnumerable<string> DesktopPageCells(string markdown)
    {
        var lines = markdown.ReplaceLineEndings("\n").Split('\n');
        var desktop = Array.IndexOf(lines, "## MOBAflow Desktop");
        var mobile = Array.IndexOf(lines, "## MOBAsmart tabs");
        if (desktop < 0 || mobile <= desktop)
            throw new InvalidDataException("PAGE-OVERVIEW.md must place the desktop section before the MOBAsmart tabs.");

        return lines.Skip(desktop + 1).Take(mobile - desktop - 1)
            .Where(line => line.StartsWith("| **", StringComparison.Ordinal))
            .Select(line => line.Split('|', 3)[1].Trim());
    }

    [Test]
    public void WikiDesktopPageCells_EqualAppTitlesAndBadgesInOrder()
    {
        Assert.That(DesktopPageCells(PageOverviewMarkdown()), Is.EqualTo(ExpectedDesktopCells()),
            "Keep the desktop rows in docs/wiki/PAGE-OVERVIEW.md in sync with the application navigation.");
    }

    [Test]
    public void RenamedOrMissingWikiPage_NoLongerMatchesAppNavigation()
    {
        var markdown = PageOverviewMarkdown();
        var helpRow = markdown.ReplaceLineEndings("\n").Split('\n')
            .Single(line => line.StartsWith("| **Help** |", StringComparison.Ordinal));
        var renamed = markdown.Replace(helpRow, helpRow.Replace("**Help**", "**Renamed Help**", StringComparison.Ordinal), StringComparison.Ordinal);
        var missing = markdown.Replace(helpRow, string.Empty, StringComparison.Ordinal);
        var expected = ExpectedDesktopCells().ToArray();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(DesktopPageCells(markdown), Is.EqualTo(expected));
            Assert.That(DesktopPageCells(renamed), Is.Not.EqualTo(expected));
            Assert.That(DesktopPageCells(missing), Is.Not.EqualTo(expected));
        }
    }

    [Test]
    public void NavigationJson_EqualsTheAppNavigation_InKeyOrderAndEveryField()
    {
        // Records compare by value, so key, title, icon, category, order, bold flag, path icon, toggle key and badge all count.
        Assert.That(Json().Entries, Is.EqualTo(App()));
    }

    [Test]
    public void NavigationJson_MarksExactlyTheFourDemoPages()
    {
        Assert.That(Json().DemoKeys, Is.EquivalentTo(DemoKeys));
    }

    [Test]
    public void App_HasTheEntriesOfTheSpecGroups()
    {
        Assert.That(App().Select(e => e.Title), Is.EqualTo(SpecTitles));
    }

    [Test]
    public void App_BoldEntriesAndPathIcon_AreAsSpecified()
    {
        var app = App();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(app.Where(e => e.IsBold).Select(e => e.Key), Is.EquivalentTo(BoldKeys));
            Assert.That(app.Where(e => e.PathIconData is not null).Select(e => e.Key), Is.EqualTo(SignalBoxOnly));
        }
    }

    [TestCase("\"locomotives\", \"Locomotives\"", "\"locomotives\", \"Renamed\"", TestName = "ChangedTitle")]
    [TestCase("NavigationCategory.Solution, 25", "NavigationCategory.Solution, 5", TestName = "ChangedOrder")]
    [TestCase("\"\\uEC49\"", "\"\\uEC4A\"", TestName = "ChangedIcon")]
    [TestCase("FeatureTogglePages.TrainControl, null, true", "FeatureTogglePages.TrainControl, null, false", TestName = "ChangedBoldFlag")]
    [TestCase("Tag: \"recorder\"", "Tag: \"recorders\"", TestName = "ChangedKey")]
    [TestCase("NavigationCategory.Help, 30", "NavigationCategory.Core, 30", TestName = "ChangedCategory")]
    public void ChangedRegistration_NoLongerEqualsNavigationJson(string find, string replace)
    {
        var registration = NavigationSource.RegistrationText;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(registration, Does.Contain(find));
            Assert.That(App(registration.Replace(find, replace, StringComparison.Ordinal)), Is.Not.EqualTo(Json().Entries));
        }
    }

    [Test]
    public void CommentedOutRegistration_NoLongerEqualsNavigationJson()
    {
        var registration = NavigationSource.RegistrationText;
        var info = "pages.Add(new PageMetadata(\"info\", \"Info\", \"\\uE946\", typeof(InfoPage), NavigationCategory.Help, 20, null, null, false));";
        using (Assert.EnterMultipleScope())
        {
            Assert.That(registration, Does.Contain(info));

            Assert.That(
                App(registration.Replace(info, "// " + info, StringComparison.Ordinal)),
                Is.Not.EqualTo(Json().Entries));
            Assert.That(
                App(registration.Replace(info, "/* " + info + " */", StringComparison.Ordinal)),
                Is.Not.EqualTo(Json().Entries));
        }
    }

    [Test]
    public void ConditionalCompilationSpacedAddOrUnsupportedEscape_FailsLoudly()
    {
        var registration = NavigationSource.RegistrationText;
        var info = "pages.Add(new PageMetadata(\"info\", \"Info\", \"\\uE946\", typeof(InfoPage), NavigationCategory.Help, 20, null, null, false));";
        using (Assert.EnterMultipleScope())
        {
            Assert.That(registration, Does.Contain(info));

            Assert.That(
                () => App(registration.Replace(info, "#if false\n" + info + "\n#endif", StringComparison.Ordinal)),
                Throws.InvalidOperationException);
            Assert.That(
                () => App(registration.Replace(info, info + "\n        pages.Add (pages[0]);", StringComparison.Ordinal)),
                Throws.InvalidOperationException.With.Message.Contains("not understood"));
            Assert.That(
                () => App(registration.Replace("\"Info\", \"\\uE946\"", "\"Info\\t\", \"\\uE946\"", StringComparison.Ordinal)),
                Throws.InvalidOperationException.With.Message.Contains("escape"));
        }
    }

    [Test]
    public void JsonWithMissingOrMistypedField_FailsLoudly()
    {
        var json = File.ReadAllText(NavigationSource.NavigationJsonPath);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                () => NavigationSource.FromJson(json.Replace("\"badge\"", "\"badges\"", StringComparison.Ordinal)),
                Throws.InvalidOperationException);
            Assert.That(
                () => NavigationSource.FromJson(json.Replace("\"badge\": null", "\"badge\": false", StringComparison.Ordinal)),
                Throws.InvalidOperationException);
        }
    }

    [Test]
    public void PageSwitchedOffInAppSettings_IsHiddenLikeTheApp()
    {
        var settings = NavigationSource.AppSettingsText.Replace(
            "\"Recorder\": { \"IsAvailable\": true",
            "\"Recorder\": { \"IsAvailable\": false",
            StringComparison.Ordinal);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(settings, Is.Not.EqualTo(NavigationSource.AppSettingsText));

            var app = App(appSettings: settings);
            Assert.That(app.Select(e => e.Key), Does.Not.Contain("recorder"));
            Assert.That(app, Has.Count.EqualTo(Json().Entries.Count - 1));
        }
    }

    [Test]
    public void ChangedBadgeLabel_NoLongerEqualsNavigationJson()
    {
        var settings = NavigationSource.AppSettingsText.Replace(
            "\"Timetable\": { \"IsAvailable\": true, \"Label\": \"Preview\" }",
            "\"Timetable\": { \"IsAvailable\": true, \"Label\": \"\" }",
            StringComparison.Ordinal);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(settings, Is.Not.EqualTo(NavigationSource.AppSettingsText));
            Assert.That(App(appSettings: settings), Is.Not.EqualTo(Json().Entries));
        }
    }

    [Test]
    public void RemovedOrAddedRegistration_NoLongerEqualsNavigationJson()
    {
        var registration = NavigationSource.RegistrationText;
        var info = "pages.Add(new PageMetadata(\"info\", \"Info\", \"\\uE946\", typeof(InfoPage), NavigationCategory.Help, 20, null, null, false));";
        using (Assert.EnterMultipleScope())
        {
            Assert.That(registration, Does.Contain(info));

            Assert.That(
                App(registration.Replace(info, string.Empty, StringComparison.Ordinal)),
                Has.Count.EqualTo(Json().Entries.Count - 1));
            Assert.That(
                App(registration.Replace(info, info + info.Replace("\"info\"", "\"info2\"", StringComparison.Ordinal), StringComparison.Ordinal)),
                Is.Not.EqualTo(Json().Entries));
        }
    }

    [Test]
    public void RegistrationThroughAHelperOrTargetTypedNew_FailsLoudly()
    {
        var registration = NavigationSource.RegistrationText;
        var extra = "pages.Add(Make(\"x\"));";
        var withHelper = registration.Replace("// Return sorted", extra + "\n        // Return sorted", StringComparison.Ordinal);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(withHelper, Is.Not.EqualTo(registration));
            Assert.That(() => App(withHelper), Throws.InvalidOperationException.With.Message.Contains("not understood"));

            var targetTyped = registration.Replace(
                "// Return sorted",
                "PageMetadata hidden = new(\"x\", \"X\", null, typeof(object), NavigationCategory.Help, 1, null, null, false);\n        // Return sorted",
                StringComparison.Ordinal);
            Assert.That(() => App(targetTyped), Throws.InvalidOperationException.With.Message.Contains("not understood"));
        }
    }
}
