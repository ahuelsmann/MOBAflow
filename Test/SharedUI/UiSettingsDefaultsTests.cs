// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Moba.Common.Configuration;
using Moba.SharedUI.Configuration;
using System.Text.Json;

/// <summary>
/// Defaults and serialization of the SharedUI settings root and its UI sections, so settings refactors
/// do not change what both apps load or save.
/// </summary>
[TestFixture]
[Category("Unit")]
internal sealed class UiSettingsDefaultsTests
{
    [Test]
    public void DisplaySettings_Should_DefaultAddressToEmptyAndPortToProtocolPort_WhenConstructed()
    {
        var display = new DisplaySettings();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(display.Esp32IpAddress, Is.EqualTo(string.Empty));
            Assert.That(display.Port, Is.EqualTo(4210));
        }
    }

    [Test]
    public void UiSettings_Should_RoundTripOnlyEndpointState_WhenDisplaySerialized()
    {
        var settings = new UiSettings
        {
            Display = new DisplaySettings
            {
                Esp32IpAddress = "192.0.2.36",
                Port = 4211
            }
        };

        var json = JsonSerializer.Serialize(settings);
        var restored = JsonSerializer.Deserialize<UiSettings>(json);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(restored, Is.Not.Null);
            Assert.That(restored!.Display.Esp32IpAddress, Is.EqualTo("192.0.2.36"));
            Assert.That(restored.Display.Port, Is.EqualTo(4211));
            Assert.That(typeof(DisplaySettings).GetProperties(),
                Has.Exactly(2).Items,
                "Display settings must not persist negotiated capabilities or session state.");
        }
    }

    [Test]
    public void UiSettings_Should_KeepSectionNames_WhenNeutralAndUiSectionsShareOneFile()
    {
        var locomotiveId = Guid.NewGuid();
        var settings = new UiSettings();
        settings.Counter.TargetLapCount = 7;
        settings.Layout.SolutionPage.IsProjectListExpanded = false;
        settings.WinUiTrainControlHost.SelectedLocomotiveFromProjectId = locomotiveId;
        FeatureToggleRegistry.TrySetPageAvailability(settings.FeatureToggles, FeatureTogglePages.Monitor, value: false);

        var json = JsonSerializer.Serialize(settings);
        var neutral = JsonSerializer.Deserialize<AppSettings>(json);
        var restored = JsonSerializer.Deserialize<UiSettings>(json);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(json, Does.Contain("\"Layout\"").And.Contain("\"FeatureToggles\"").And.Contain("\"Counter\""));
            Assert.That(neutral!.Counter.TargetLapCount, Is.EqualTo(7));
            Assert.That(restored!.Counter.TargetLapCount, Is.EqualTo(7));
            Assert.That(restored.Layout.SolutionPage.IsProjectListExpanded, Is.False);
            Assert.That(restored.WinUiTrainControlHost.SelectedLocomotiveFromProjectId, Is.EqualTo(locomotiveId));
            Assert.That(restored.MauiTrainControlHost.SelectedLocomotiveFromProjectId, Is.Null);
            Assert.That(FeatureToggleRegistry.GetPageAvailabilityOrDefault(restored.FeatureToggles, FeatureTogglePages.Monitor), Is.False);
            Assert.That(FeatureToggleRegistry.GetPageAvailabilityOrDefault(restored.FeatureToggles, FeatureTogglePages.Trains), Is.True);
        }
    }

    [Test]
    public void FeatureToggles_Should_DefaultTimetableToAvailablePreview_WhenNoEntryIsStored()
    {
        var toggles = new FeatureToggleSettings();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(toggles.Pages, Is.Empty);
            Assert.That(toggles.IsEventManagerVisualEditorAvailable, Is.True);
            Assert.That(FeatureToggleRegistry.GetPageAvailabilityOrDefault(toggles, FeatureTogglePages.Timetable), Is.True);
            Assert.That(FeatureToggleRegistry.GetBadgeLabel(toggles, FeatureTogglePages.Timetable), Is.EqualTo("Preview"));
        }
    }

    [Test]
    public void Layout_CollapsibleColumn_defaults_are_expanded()
    {
        var layout = new LayoutSettings();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(layout.StationsPage.IsStationsListExpanded, Is.True);
            Assert.That(layout.StationsPage.IsPlatformsListExpanded, Is.True);
            Assert.That(layout.StationsPage.IsWorkflowLibraryExpanded, Is.True);
            Assert.That(layout.StationsPage.IsPropertiesExpanded, Is.True);
            Assert.That(layout.WorkflowsPage.IsWorkflowListExpanded, Is.True);
            Assert.That(layout.WorkflowsPage.IsActionsExpanded, Is.True);
            Assert.That(layout.WorkflowsPage.IsPropertiesExpanded, Is.True);
            Assert.That(layout.WorkflowsPage.IsGraphViewActive, Is.True);
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
            Assert.That(layout.TrackPlanPage.FitOnLoad, Is.True);
            Assert.That(layout.MonitorPage.IsTrafficExpanded, Is.True);
            Assert.That(layout.MonitorPage.IsActivityLogExpanded, Is.True);
        }
    }

    [Test]
    public void Layout_CollapsibleColumn_width_defaults_are_positive()
    {
        var layout = new LayoutSettings();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(layout.StationsPage.StationsColumnWidth, Is.GreaterThan(0));
            Assert.That(layout.StationsPage.PlatformsColumnWidth, Is.GreaterThan(0));
            Assert.That(layout.StationsPage.WorkflowLibraryColumnWidth, Is.GreaterThan(0));
            Assert.That(layout.StationsPage.PropertiesColumnStarValue, Is.GreaterThan(0));
            Assert.That(layout.WorkflowsPage.WorkflowListColumnWidth, Is.GreaterThan(0));
            Assert.That(layout.WorkflowsPage.ActionsColumnStarValue, Is.GreaterThan(0));
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
            Assert.That(layout.TimetablePage.ServicesColumnStarValue, Is.GreaterThan(0));
            Assert.That(layout.TimetablePage.DetailsColumnStarValue, Is.GreaterThan(0));
        }
    }
}
