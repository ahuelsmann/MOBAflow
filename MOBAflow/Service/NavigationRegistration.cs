// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Service;

using Controls.SignalBox;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using SharedUI.Configuration;
using SharedUI.Interface;
using SharedUI.Service;
using SharedUI.ViewModel;

using System;
using System.Collections.Generic;
using System.Linq;

using TrackPlan.Editor;

using View;

using ViewModel;

/// <summary>
/// Metadata for a registered page with navigation information.
/// </summary>
internal sealed record PageMetadata(
    string Tag,
    string Title,
    string? Icon,
    Type PageType,
    NavigationCategory Category,
    int Order,
    string? FeatureToggleKey,
    string? PathIconData,
    bool IsBold);

/// <summary>
/// Centralizes page and navigation registrations for the WinUI app.
/// Uses static registrations to avoid reflection penalties at startup.
/// </summary>
internal static class NavigationRegistration
{
    /// <summary>
    /// Registers all pages.
    /// Returns combined page metadata for NavigationView building.
    /// </summary>
    public static List<PageMetadata> RegisterPages(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        var pages = new List<PageMetadata>();

        // Register standard singleton pages (created once, reused on every navigation)
        services.AddSingleton<LocomotivesPage>();
        pages.Add(new PageMetadata("locomotives", "Locomotives", "\uE7C0", typeof(LocomotivesPage), NavigationCategory.Solution, 25, FeatureTogglePages.Locomotives, null, false));

        services.AddSingleton<PassengerWagonPage>();
        pages.Add(new PageMetadata("passengerwagons", "Passenger Wagons", "\uE7C0", typeof(PassengerWagonPage), NavigationCategory.Solution, 26, FeatureTogglePages.PassengerWagons, null, false));

        services.AddSingleton<Z21AssignmentViewModel>();
        services.AddSingleton<SolutionPage>();
        pages.Add(new PageMetadata("solution", "Solution", "\uE8B7", typeof(SolutionPage), NavigationCategory.Solution, 10, FeatureTogglePages.Solution, null, false));

        services.AddSingleton<HelpPage>();
        pages.Add(new PageMetadata("help", "Help", "\uE897", typeof(HelpPage), NavigationCategory.Help, 10, null, null, false));

        services.AddSingleton<SettingsPage>();
        pages.Add(new PageMetadata("settings", "Settings", "\uE115", typeof(SettingsPage), NavigationCategory.Help, 30, null, null, false));

        services.AddSingleton<OverviewPage>();
        pages.Add(new PageMetadata("overview", "Overview", "\uE80F", typeof(OverviewPage), NavigationCategory.Core, 10, FeatureTogglePages.Overview, null, false));

        services.AddSingleton<WorkflowsPage>();
        pages.Add(new PageMetadata("workflows", "Workflows", "\uE945", typeof(WorkflowsPage), NavigationCategory.Journey, 14, FeatureTogglePages.Workflows, null, false));

        services.AddSingleton<StationsPage>();
        pages.Add(new PageMetadata("stations", "Stations", "\uEC06", typeof(StationsPage), NavigationCategory.Solution, 21, FeatureTogglePages.Stations, null, false));

        services.AddSingleton<GoodsWagonPage>();
        pages.Add(new PageMetadata("goodswagons", "Goods Wagons", "\uE7C0", typeof(GoodsWagonPage), NavigationCategory.Solution, 27, FeatureTogglePages.GoodsWagons, null, false));

        services.AddSingleton<TrainsPage>();
        pages.Add(new PageMetadata("trains", "Trains", "\uE7C0", typeof(TrainsPage), NavigationCategory.Solution, 28, FeatureTogglePages.Trains, null, false));

        services.AddSingleton(serviceProvider => new TrackPlanPage(
            serviceProvider.GetRequiredService<TrackPlanPageViewModels>(),
            serviceProvider.GetRequiredService<ISolutionSession>(),
            new TrackPlanPageEditor(
                serviceProvider.GetRequiredService<EditableTrackPlan>(),
                serviceProvider.GetRequiredService<SegmentPlacementGeometry>()),
            serviceProvider.GetRequiredService<TrackPlanFeedbackHighlighter>(),
            serviceProvider.GetRequiredService<LayoutPersistenceService>(),
            serviceProvider.GetRequiredService<ILogger<TrackPlanPage>>()));
        pages.Add(new PageMetadata("trackplaneditor", "Track Plan", "\uE7F9", typeof(TrackPlanPage), NavigationCategory.TrackManagement, 10, FeatureTogglePages.TrackPlanEditor, null, false));

        services.AddSingleton<TrainControlPage>();
        pages.Add(new PageMetadata("traincontrol", "Train Control", "\uEC49", typeof(TrainControlPage), NavigationCategory.TrainControl, 10, FeatureTogglePages.TrainControl, null, true));

        services.AddSingleton<JourneyMapPage>();
        pages.Add(new PageMetadata("journeymap", "Journey Map", "\uE81D", typeof(JourneyMapPage), NavigationCategory.Journey, 20, FeatureTogglePages.JourneyMap, null, false));

        services.AddSingleton<InfoPage>();
        pages.Add(new PageMetadata("info", "Info", "\uE946", typeof(InfoPage), NavigationCategory.Help, 20, null, null, false));

        services.AddSingleton<MonitorPage>();
        pages.Add(new PageMetadata("monitor", "Monitor", "\uE7F4", typeof(MonitorPage), NavigationCategory.Monitoring, 10, FeatureTogglePages.Monitor, null, false));

        services.AddSingleton<RecorderPage>();
        pages.Add(new PageMetadata(
            Tag: "recorder",
            Title: "Recorder",
            Icon: "\uE7C8",
            PageType: typeof(RecorderPage),
            Category: NavigationCategory.Monitoring,
            Order: 20,
            FeatureToggleKey: FeatureTogglePages.Recorder,
            PathIconData: null,
            IsBold: false));

        services.AddSingleton<MatrixPage>();
        services.AddSingleton(sp => new MatrixPageViewModel(
            sp.GetRequiredService<ISolutionSession>(),
            sp.GetRequiredService<IDialogService>(),
            sp.GetRequiredService<ILogger<MatrixPageViewModel>>()));
        pages.Add(new PageMetadata(
            Tag: "matrix",
            Title: "Matrix Images",
            Icon: "\uE8A9",
            PageType: typeof(MatrixPage),
            Category: NavigationCategory.TrackManagement,
            Order: 40,
            FeatureToggleKey: FeatureTogglePages.Matrix,
            PathIconData: null,
            IsBold: false));

        services.AddSingleton<DisplayViewModel>();
        services.AddSingleton<DisplayPage>();
        pages.Add(new PageMetadata(
            Tag: "display",
            Title: "ESP32 Display",
            Icon: "\uE7F4",
            PageType: typeof(DisplayPage),
            Category: NavigationCategory.TrackManagement,
            Order: 30,
            FeatureToggleKey: FeatureTogglePages.Display,
            PathIconData: null,
            IsBold: false));

        // Manual registrations for pages with custom DI requirements
        // JourneysPage: explicit factory for its page ViewModel and the layout persistence boundary
        services.AddSingleton(sp => new JourneysPage(
            sp.GetRequiredService<JourneysPageViewModel>(),
            sp.GetRequiredService<LayoutPersistenceService>(),
            sp.GetRequiredService<ILogger<JourneysPage>>()));
        pages.Add(new PageMetadata(
            Tag: "journeys",
            Title: "Journeys",
            Icon: "\uE7C1",
            PageType: typeof(JourneysPage),
            Category: NavigationCategory.Journey,
            Order: 10,
            FeatureToggleKey: FeatureTogglePages.Journeys,
            PathIconData: null,
            IsBold: true));

        services.AddSingleton<TimetablePageViewModel>();
        // A transient page owns WinUI layout lifecycle while the singleton ViewModel preserves timetable state.
        services.AddTransient(sp => new TimetablePage(
            sp.GetRequiredService<TimetablePageViewModel>(),
            sp.GetRequiredService<LayoutPersistenceService>(),
            sp.GetRequiredService<ILogger<TimetablePage>>()));
        pages.Add(new PageMetadata(
            Tag: "timetable",
            Title: "Timetable",
            Icon: "\uE787",
            PageType: typeof(TimetablePage),
            Category: NavigationCategory.Journey,
            Order: 12,
            FeatureToggleKey: FeatureTogglePages.Timetable,
            PathIconData: null,
            IsBold: false));

        services.AddSingleton(sp => new EventManagerViewModel(
            sp.GetRequiredService<IProjectContext>(),
            sp.GetRequiredService<WorkflowLibraryViewModel>(),
            sp.GetRequiredService<JourneysPageViewModel>(),
            sp.GetRequiredService<InPortCounterViewModel>().ResetCountersCommand));
        services.AddSingleton(sp => new EventManagerPage(
            sp.GetRequiredService<EventManagerViewModel>(),
            sp.GetRequiredService<LayoutPersistenceService>(),
            sp.GetRequiredService<ILogger<EventManagerPage>>()));
        pages.Add(new PageMetadata(
            Tag: "eventmanager",
            Title: "Event Manager",
            Icon: "\uE945",
            PageType: typeof(EventManagerPage),
            Category: NavigationCategory.Journey,
            Order: 15,
            FeatureToggleKey: FeatureTogglePages.EventManager,
            PathIconData: null,
            IsBold: false));

        // SignalBoxPage: requires custom runtime services
        services.AddSingleton(sp => new SignalBoxPage(
            sp.GetRequiredService<SignalBoxPageViewModel>(),
            sp.GetRequiredService<InterlockingControlViewModel>(),
            sp.GetRequiredService<SignalBoxPropertiesViewModel>(),
            sp.GetRequiredService<LayoutPersistenceService>(),
            sp.GetRequiredService<ILogger<SignalBoxPage>>(),
            sp.GetRequiredService<ILogger<SignalBoxCanvasControl>>()));
        pages.Add(new PageMetadata(
            Tag: "signalbox",
            Title: "Signal Box",
            Icon: null,
            PageType: typeof(SignalBoxPage),
            Category: NavigationCategory.TrackManagement,
            Order: 20,
            FeatureToggleKey: FeatureTogglePages.SignalBox,
            PathIconData: "M7,2 A2,2 0 1,1 11,2 A2,2 0 1,1 7,2 M3,10 A2,2 0 1,1 7,10 A2,2 0 1,1 3,10 M11,10 A2,2 0 1,1 15,10 A2,2 0 1,1 11,10",
            IsBold: false));

        // Return sorted
        return pages
            .OrderBy(p => (int)p.Category)
            .ThenBy(p => p.Order)
            .ToList();
    }
}
