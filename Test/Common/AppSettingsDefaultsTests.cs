// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Common;

using Moba.Common.Configuration;

/// <summary>
/// Tests for default configuration values so config refactors don't break apps silently.
/// </summary>
[TestFixture]
internal class AppSettingsDefaultsTests
{
    [Test]
    public void Application_PhotoStoragePath_default_is_empty()
    {
        var app = new ApplicationSettings();
        Assert.That(app.PhotoStoragePath, Is.EqualTo(string.Empty));
    }

    [Test]
    public void Application_AutoStartWebApp_default_is_true()
    {
        var app = new ApplicationSettings();
        Assert.That(app.AutoStartWebApp, Is.True);
    }

    [Test]
    public void RestApi_Port_default_is_5001()
    {
        var rest = new RestApiSettings();
        Assert.That(rest.Port, Is.EqualTo(5001));
    }

    [Test]
    public void RestApi_CurrentIpAddress_default_is_empty_until_discovered()
    {
        var rest = new RestApiSettings();
        Assert.That(rest.CurrentIpAddress, Is.EqualTo(string.Empty));
    }

    [Test]
    public void Speech_Piper_paths_default_to_empty()
    {
        var speech = new SpeechSettings();

        Assert.Multiple(() =>
        {
            Assert.That(speech.PiperExecutablePath, Is.EqualTo(string.Empty));
            Assert.That(speech.PiperModelPath, Is.EqualTo(string.Empty));
            Assert.That(speech.PiperConfigPath, Is.EqualTo(string.Empty));
        });
    }

    [Test]
    public void Speech_EnablePronunciationNormalization_default_is_true()
    {
        var speech = new SpeechSettings();
        Assert.That(speech.EnablePronunciationNormalization, Is.True);
    }

    [Test]
    public void Speech_PiperSentenceSilenceSeconds_default_is_0_25()
    {
        var speech = new SpeechSettings();
        Assert.That(speech.PiperSentenceSilenceSeconds, Is.EqualTo(0.25));
    }

    [Test]
    public void Speech_PronunciationReplacements_default_is_empty()
    {
        var speech = new SpeechSettings();
        Assert.That(speech.PronunciationReplacements, Is.Not.Null);
        Assert.That(speech.PronunciationReplacements, Is.Empty);
    }

    [Test]
    public void Z21_DefaultPort_default_is_21105()
    {
        var z21 = new Z21Settings();
        Assert.That(z21.DefaultPort, Is.EqualTo("21105"));
    }

    [Test]
    public void AppSettings_Application_default_is_not_null()
    {
        var settings = new AppSettings();
        Assert.That(settings.Application, Is.Not.Null);
    }

    [Test]
    public void AppSettings_RestApi_default_is_not_null()
    {
        var settings = new AppSettings();
        Assert.That(settings.RestApi, Is.Not.Null);
    }

    [Test]
    public void Counter_TimerIntervalSeconds_default_is_10()
    {
        var counter = new CounterSettings();
        Assert.That(counter.TimerIntervalSeconds, Is.EqualTo(10.0));
    }

    [Test]
    public void Counter_TargetLapCount_default_is_10()
    {
        var counter = new CounterSettings();
        Assert.That(counter.TargetLapCount, Is.EqualTo(10));
    }

    [Test]
    public void Counter_UseTimerFilter_default_is_true()
    {
        var counter = new CounterSettings();
        Assert.That(counter.UseTimerFilter, Is.True);
    }

    [Test]
    public void Z21_CurrentIpAddress_default_is_non_empty()
    {
        var z21 = new Z21Settings();
        Assert.That(z21.CurrentIpAddress, Is.Not.Empty);
    }

    [Test]
    public void Application_IsDarkMode_default_is_true()
    {
        var app = new ApplicationSettings();
        Assert.That(app.IsDarkMode, Is.True);
    }

    [Test]
    public void Layout_CollapsibleColumn_defaults_are_expanded()
    {
        var layout = new LayoutSettings();

        Assert.That(layout.StationsPage.IsStationsListExpanded, Is.True);
        Assert.That(layout.StationsPage.IsPlatformsListExpanded, Is.True);
        Assert.That(layout.StationsPage.IsWorkflowLibraryExpanded, Is.True);
        Assert.That(layout.StationsPage.IsPropertiesExpanded, Is.True);
        Assert.That(layout.WorkflowsPage.IsWorkflowListExpanded, Is.True);
        Assert.That(layout.WorkflowsPage.IsActionsExpanded, Is.True);
        Assert.That(layout.WorkflowsPage.IsPropertiesExpanded, Is.True);
        Assert.That(layout.LocomotivesPage.IsListExpanded, Is.True);
        Assert.That(layout.LocomotivesPage.IsPropertiesExpanded, Is.True);
        Assert.That(layout.PassengerWagonPage.IsListExpanded, Is.True);
        Assert.That(layout.PassengerWagonPage.IsPropertiesExpanded, Is.True);
        Assert.That(layout.GoodsWagonPage.IsListExpanded, Is.True);
        Assert.That(layout.GoodsWagonPage.IsPropertiesExpanded, Is.True);
        Assert.That(layout.TrainsPage.IsTrainListExpanded, Is.True);
        Assert.That(layout.TrainsPage.IsLocomotiveLibraryExpanded, Is.True);
        Assert.That(layout.TrainsPage.IsPassengerLibraryExpanded, Is.True);
        Assert.That(layout.TrainsPage.IsGoodsLibraryExpanded, Is.True);
        Assert.That(layout.TrainsPage.IsPropertiesExpanded, Is.True);
        Assert.That(layout.SolutionPage.IsProjectListExpanded, Is.True);
        Assert.That(layout.SolutionPage.IsPropertiesExpanded, Is.True);
        Assert.That(layout.HelpPage.IsNavigationExpanded, Is.True);
        Assert.That(layout.HelpPage.IsDocumentationExpanded, Is.True);
        Assert.That(layout.MatrixPage.IsColorPaletteExpanded, Is.True);
        Assert.That(layout.MatrixPage.IsImagesExpanded, Is.True);
        Assert.That(layout.SignalBoxPage.IsToolboxExpanded, Is.True);
        Assert.That(layout.SignalBoxPage.IsPropertiesExpanded, Is.True);
        Assert.That(layout.TrackPlanPage.IsToolboxExpanded, Is.True);
        Assert.That(layout.TrackPlanPage.IsPropertiesExpanded, Is.True);
        Assert.That(layout.MonitorPage.IsTrafficExpanded, Is.True);
        Assert.That(layout.MonitorPage.IsActivityLogExpanded, Is.True);
    }

    [Test]
    public void Layout_CollapsibleColumn_width_defaults_are_positive()
    {
        var layout = new LayoutSettings();

        Assert.That(layout.StationsPage.StationsColumnWidth, Is.GreaterThan(0));
        Assert.That(layout.StationsPage.PlatformsColumnWidth, Is.GreaterThan(0));
        Assert.That(layout.StationsPage.WorkflowLibraryColumnWidth, Is.GreaterThan(0));
        Assert.That(layout.StationsPage.PropertiesColumnStarValue, Is.GreaterThan(0));
        Assert.That(layout.WorkflowsPage.WorkflowListColumnWidth, Is.GreaterThan(0));
        Assert.That(layout.WorkflowsPage.ActionsColumnWidth, Is.GreaterThan(0));
        Assert.That(layout.WorkflowsPage.PropertiesColumnStarValue, Is.GreaterThan(0));
        Assert.That(layout.LocomotivesPage.ListColumnWidth, Is.GreaterThan(0));
        Assert.That(layout.LocomotivesPage.PropertiesColumnStarValue, Is.GreaterThan(0));
        Assert.That(layout.PassengerWagonPage.ListColumnWidth, Is.GreaterThan(0));
        Assert.That(layout.PassengerWagonPage.PropertiesColumnStarValue, Is.GreaterThan(0));
        Assert.That(layout.GoodsWagonPage.ListColumnWidth, Is.GreaterThan(0));
        Assert.That(layout.GoodsWagonPage.PropertiesColumnStarValue, Is.GreaterThan(0));
        Assert.That(layout.TrainsPage.TrainListColumnStarValue, Is.GreaterThan(0));
        Assert.That(layout.TrainsPage.LocomotiveLibraryColumnStarValue, Is.GreaterThan(0));
        Assert.That(layout.TrainsPage.PassengerLibraryColumnStarValue, Is.GreaterThan(0));
        Assert.That(layout.TrainsPage.GoodsLibraryColumnStarValue, Is.GreaterThan(0));
        Assert.That(layout.TrainsPage.PropertiesColumnStarValue, Is.GreaterThan(0));
        Assert.That(layout.SolutionPage.ProjectListColumnStarValue, Is.GreaterThan(0));
        Assert.That(layout.SolutionPage.PropertiesColumnStarValue, Is.GreaterThan(0));
        Assert.That(layout.HelpPage.NavigationColumnWidth, Is.GreaterThan(0));
        Assert.That(layout.HelpPage.DocumentationColumnStarValue, Is.GreaterThan(0));
        Assert.That(layout.MatrixPage.ColorPaletteColumnWidth, Is.GreaterThan(0));
        Assert.That(layout.MatrixPage.ImagesColumnStarValue, Is.GreaterThan(0));
        Assert.That(layout.SignalBoxPage.ToolboxColumnWidth, Is.GreaterThan(0));
        Assert.That(layout.SignalBoxPage.CanvasColumnStarValue, Is.GreaterThan(0));
        Assert.That(layout.SignalBoxPage.PropertiesColumnStarValue, Is.GreaterThan(0));
        Assert.That(layout.TrackPlanPage.ToolboxColumnWidth, Is.GreaterThan(0));
        Assert.That(layout.TrackPlanPage.PropertiesColumnWidth, Is.GreaterThan(0));
        Assert.That(layout.MonitorPage.TrafficColumnStarValue, Is.GreaterThan(0));
        Assert.That(layout.MonitorPage.ActivityLogColumnStarValue, Is.GreaterThan(0));
    }
}