// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.MOBAdemo.Navigation;

using System.Text.RegularExpressions;

/// <summary>
/// The demo's <c>navigation.json</c> must equal the WinUI navigation: the page registration, the feature toggle
/// registry and the labels of the shipped appsettings (issue #430 slice 5, spec FR-022, plan T4).
/// </summary>
[TestFixture]
internal sealed class NavigationDriftTests
{
    private static readonly string[] DemoKeys = ["traincontrol", "workflows", "trackplaneditor", "signalbox"];

    private static IReadOnlyList<NavigationEntryData> App(string? registration = null, string? appSettings = null) =>
        NavigationSource.FromApp(
            registration ?? NavigationSource.RegistrationText,
            NavigationSource.CategoryText,
            appSettings ?? NavigationSource.AppSettingsText);

    private static (IReadOnlyList<NavigationEntryData> Entries, IReadOnlyList<string> DemoKeys) Json() =>
        NavigationSource.FromJson(File.ReadAllText(NavigationSource.NavigationJsonPath));

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
        Assert.That(App().Select(e => e.Title), Is.EqualTo(new[]
        {
            "Overview",
            "Train Control",
            "Journeys", "Timetable", "Workflows", "Event Manager", "Journey Map",
            "Solution", "Stations", "Locomotives", "Passenger Wagons", "Goods Wagons", "Trains",
            "Track Plan", "Signal Box", "ESP32 Display", "Matrix Images",
            "Monitor", "Recorder",
            "Help", "Info", "Settings",
        }));
    }

    [Test]
    public void App_BoldEntriesAndPathIcon_AreAsSpecified()
    {
        var app = App();
        Assert.That(app.Where(e => e.IsBold).Select(e => e.Key), Is.EquivalentTo(new[] { "traincontrol", "journeys" }));
        Assert.That(app.Where(e => e.PathIconData is not null).Select(e => e.Key), Is.EqualTo(new[] { "signalbox" }));
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
        Assert.That(registration, Does.Contain(find));
        Assert.That(App(registration.Replace(find, replace, StringComparison.Ordinal)), Is.Not.EqualTo(Json().Entries));
    }

    [Test]
    public void CommentedOutRegistration_NoLongerEqualsNavigationJson()
    {
        var registration = NavigationSource.RegistrationText;
        var info = "pages.Add(new PageMetadata(\"info\", \"Info\", \"\\uE946\", typeof(InfoPage), NavigationCategory.Help, 20, null, null, false));";
        Assert.That(registration, Does.Contain(info));

        Assert.That(
            App(registration.Replace(info, "// " + info, StringComparison.Ordinal)),
            Is.Not.EqualTo(Json().Entries));
        Assert.That(
            App(registration.Replace(info, "/* " + info + " */", StringComparison.Ordinal)),
            Is.Not.EqualTo(Json().Entries));
    }

    [Test]
    public void ConditionalCompilationSpacedAddOrUnsupportedEscape_FailsLoudly()
    {
        var registration = NavigationSource.RegistrationText;
        var info = "pages.Add(new PageMetadata(\"info\", \"Info\", \"\\uE946\", typeof(InfoPage), NavigationCategory.Help, 20, null, null, false));";
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

    [Test]
    public void JsonWithMissingOrMistypedField_FailsLoudly()
    {
        var json = File.ReadAllText(NavigationSource.NavigationJsonPath);
        Assert.That(
            () => NavigationSource.FromJson(json.Replace("\"badge\"", "\"badges\"", StringComparison.Ordinal)),
            Throws.InvalidOperationException);
        Assert.That(
            () => NavigationSource.FromJson(Regex.Replace(json, "\"badge\": null", "\"badge\": false")),
            Throws.InvalidOperationException);
    }

    [Test]
    public void ChangedBadgeLabel_NoLongerEqualsNavigationJson()
    {
        var settings = NavigationSource.AppSettingsText.Replace(
            "\"Timetable\": { \"IsAvailable\": true, \"Label\": \"Preview\" }",
            "\"Timetable\": { \"IsAvailable\": true, \"Label\": \"\" }",
            StringComparison.Ordinal);
        Assert.That(settings, Is.Not.EqualTo(NavigationSource.AppSettingsText));
        Assert.That(App(appSettings: settings), Is.Not.EqualTo(Json().Entries));
    }

    [Test]
    public void RemovedOrAddedRegistration_NoLongerEqualsNavigationJson()
    {
        var registration = NavigationSource.RegistrationText;
        var info = "pages.Add(new PageMetadata(\"info\", \"Info\", \"\\uE946\", typeof(InfoPage), NavigationCategory.Help, 20, null, null, false));";
        Assert.That(registration, Does.Contain(info));

        Assert.That(
            App(registration.Replace(info, string.Empty, StringComparison.Ordinal)).Count,
            Is.EqualTo(Json().Entries.Count - 1));
        Assert.That(
            App(registration.Replace(info, info + info.Replace("\"info\"", "\"info2\"", StringComparison.Ordinal), StringComparison.Ordinal)),
            Is.Not.EqualTo(Json().Entries));
    }

    [Test]
    public void RegistrationThroughAHelperOrTargetTypedNew_FailsLoudly()
    {
        var registration = NavigationSource.RegistrationText;
        var extra = "pages.Add(Make(\"x\"));";
        var withHelper = registration.Replace("// Return sorted", extra + "\n        // Return sorted", StringComparison.Ordinal);
        Assert.That(withHelper, Is.Not.EqualTo(registration));
        Assert.That(() => App(withHelper), Throws.InvalidOperationException.With.Message.Contains("not understood"));

        var targetTyped = registration.Replace(
            "// Return sorted",
            "PageMetadata hidden = new(\"x\", \"X\", null, typeof(object), NavigationCategory.Help, 1, null, null, false);\n        // Return sorted",
            StringComparison.Ordinal);
        Assert.That(() => App(targetTyped), Throws.InvalidOperationException.With.Message.Contains("not understood"));
    }
}
