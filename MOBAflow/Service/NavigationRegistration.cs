// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Service;

using Common.Configuration;
using Common.Navigation;

using Controls.SignalBox;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using SharedUI.Interface;
using SharedUI.ViewModel;

using System;
using System.Collections.Generic;
using System.Linq;

using View;

using ViewModel;

/// <summary>
/// Metadata for a registered page with navigation information.
/// </summary>
public record PageMetadata(
    string Tag,
    string Title,
    string? Icon,
    Type PageType,
    NavigationCategory Category,
    int Order,
    string? FeatureToggleKey,
    string? BadgeLabelKey,
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
        pages.Add(new PageMetadata("locomotives", "Locomotives", "\uE7C0", typeof(LocomotivesPage), NavigationCategory.Solution, 25, "IsLocomotivesPageAvailable", "LocomotivesPageLabel", null, false));

        services.AddSingleton<PassengerWagonPage>();
        pages.Add(new PageMetadata("passengerwagons", "Passenger Wagons", "\uE7C0", typeof(PassengerWagonPage), NavigationCategory.Solution, 26, "IsPassengerWagonsPageAvailable", "PassengerWagonsPageLabel", null, false));

        services.AddSingleton<SolutionPage>();
        pages.Add(new PageMetadata("solution", "Solution", "\uE8B7", typeof(SolutionPage), NavigationCategory.Solution, 10, "IsSolutionPageAvailable", "SolutionPageLabel", null, false));

        services.AddSingleton<HelpPage>();
        pages.Add(new PageMetadata("help", "Help", "\uE897", typeof(HelpPage), NavigationCategory.Help, 10, null, null, null, false));

        services.AddSingleton<SettingsPage>();
        pages.Add(new PageMetadata("settings", "Settings", "\uE115", typeof(SettingsPage), NavigationCategory.Help, 30, null, null, null, false));

        services.AddSingleton<OverviewPage>();
        pages.Add(new PageMetadata("overview", "Overview", "\uE80F", typeof(OverviewPage), NavigationCategory.Core, 10, "IsOverviewPageAvailable", "OverviewPageLabel", null, false));

        services.AddSingleton<WorkflowsPage>();
        pages.Add(new PageMetadata("workflows", "Workflows", "\uE945", typeof(WorkflowsPage), NavigationCategory.Solution, 20, "IsWorkflowsPageAvailable", "WorkflowsPageLabel", null, false));

        services.AddSingleton<StationsPage>();
        pages.Add(new PageMetadata("stations", "Stations", "\uEC06", typeof(StationsPage), NavigationCategory.Solution, 21, null, null, null, false));

        services.AddSingleton<GoodsWagonPage>();
        pages.Add(new PageMetadata("goodswagons", "Goods Wagons", "\uE7C0", typeof(GoodsWagonPage), NavigationCategory.Solution, 27, "IsGoodsWagonsPageAvailable", "GoodsWagonsPageLabel", null, false));

        services.AddSingleton<TrainsPage>();
        pages.Add(new PageMetadata("trains", "Trains", "\uE7C0", typeof(TrainsPage), NavigationCategory.Solution, 28, "IsTrainsPageAvailable", "TrainsPageLabel", null, false));

        services.AddSingleton<TrackPlanPage>();
        pages.Add(new PageMetadata("trackplaneditor", "Track Plan", "\uE7F9", typeof(TrackPlanPage), NavigationCategory.TrackManagement, 10, "IsTrackPlanEditorPageAvailable", "TrackPlanEditorPageLabel", null, false));

        services.AddSingleton<TrainControlPage>();
        pages.Add(new PageMetadata("traincontrol", "Train Control", "\uEC49", typeof(TrainControlPage), NavigationCategory.TrainControl, 10, "IsTrainControlPageAvailable", "TrainControlPageLabel", null, true));

        services.AddSingleton<JourneyMapPage>();
        pages.Add(new PageMetadata("journeymap", "Journey Map", "\uE81D", typeof(JourneyMapPage), NavigationCategory.Journey, 20, "IsJourneyMapPageAvailable", "JourneyMapPageLabel", null, false));

        services.AddSingleton<InfoPage>();
        pages.Add(new PageMetadata("info", "Info", "\uE946", typeof(InfoPage), NavigationCategory.Help, 20, null, null, null, false));

        services.AddSingleton<MonitorPage>();
        pages.Add(new PageMetadata("monitor", "Monitor", "\uE7F4", typeof(MonitorPage), NavigationCategory.Monitoring, 10, "IsMonitorPageAvailable", "MonitorPageLabel", null, false));

        services.AddSingleton<MatrixPage>();
        services.AddSingleton(sp => new MatrixPageViewModel(
            sp.GetRequiredService<MainWindowViewModel>(),
            sp.GetRequiredService<ILogger<MatrixPageViewModel>>()));
        pages.Add(new PageMetadata(
            Tag: "matrix",
            Title: "Matrix Images",
            Icon: "\uE8A9",
            PageType: typeof(MatrixPage),
            Category: NavigationCategory.TrackManagement,
            Order: 40,
            FeatureToggleKey: "IsMatrixPageAvailable",
            BadgeLabelKey: "MatrixPageLabel",
            PathIconData: null,
            IsBold: false));

        services.AddSingleton<DisplayViewModel>();
        services.AddSingleton<DisplayPage>();
        pages.Add(new PageMetadata(
            Tag: "display",
            Title: "Display Configurations",
            Icon: "\uE7F4",
            PageType: typeof(DisplayPage),
            Category: NavigationCategory.TrackManagement,
            Order: 30,
            FeatureToggleKey: "IsDisplayPageAvailable",
            BadgeLabelKey: "DisplayPageLabel",
            PathIconData: null,
            IsBold: false));

        // Manual registrations for pages with custom DI requirements
        // JourneysPage: requires AppSettings + ISettingsService injection
        services.AddSingleton(sp => new JourneysPage(
            sp.GetRequiredService<MainWindowViewModel>(),
            sp.GetRequiredService<AppSettings>(),
            sp.GetService<ISettingsService>()));
        pages.Add(new PageMetadata(
            Tag: "journeys",
            Title: "Journeys",
            Icon: "\uE7C1",
            PageType: typeof(JourneysPage),
            Category: NavigationCategory.Journey,
            Order: 10,
            FeatureToggleKey: "IsJourneysPageAvailable",
            BadgeLabelKey: "JourneysPageLabel",
            PathIconData: null,
            IsBold: true));

        services.AddSingleton<EventManagerViewModel>();
        services.AddSingleton(sp => new EventManagerPage(
            sp.GetRequiredService<EventManagerViewModel>(),
            sp.GetRequiredService<AppSettings>(),
            sp.GetService<ISettingsService>(),
            sp.GetService<ILogger<EventManagerPage>>()));
        pages.Add(new PageMetadata(
            Tag: "eventmanager",
            Title: "Event Manager",
            Icon: "\uE945",
            PageType: typeof(EventManagerPage),
            Category: NavigationCategory.Journey,
            Order: 15,
            FeatureToggleKey: "IsEventManagerPageAvailable",
            BadgeLabelKey: "EventManagerPageLabel",
            PathIconData: null,
            IsBold: false));

        // SignalBoxPage: requires custom runtime services
        services.AddSingleton(sp => new SignalBoxPage(
            sp.GetRequiredService<MainWindowViewModel>(),
            sp.GetRequiredService<ViessmannSignalService>(),
            sp.GetRequiredService<AppSettings>(),
            sp.GetService<ISettingsService>(),
            sp.GetService<ILogger<SignalBoxPage>>(),
            sp.GetService<ILogger<SignalBoxPropertiesControl>>(),
            sp.GetService<ILogger<SignalBoxCanvasControl>>()));
        pages.Add(new PageMetadata(
            Tag: "signalbox",
            Title: "Signal Box",
            Icon: null,
            PageType: typeof(SignalBoxPage),
            Category: NavigationCategory.TrackManagement,
            Order: 20,
            FeatureToggleKey: "IsSignalBoxPageAvailable",
            BadgeLabelKey: "SignalBoxPageLabel",
            PathIconData: "M7,2 A2,2 0 1,1 11,2 A2,2 0 1,1 7,2 M3,10 A2,2 0 1,1 7,10 A2,2 0 1,1 3,10 M11,10 A2,2 0 1,1 15,10 A2,2 0 1,1 11,10",
            IsBold: false));

        // Return sorted
        return pages
            .OrderBy(p => (int)p.Category)
            .ThenBy(p => p.Order)
            .ToList();
    }
}
