// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Service;

using Common.Configuration;
using Common.Navigation;
using Microsoft.Extensions.DependencyInjection;
using SharedUI.Interface;
using Moba.SharedUI.ViewModel;
using View;
using ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Metadata for a registered page with navigation information.
/// </summary>
internal record PageMetadata(
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

        // Register standard transient pages (these were previously auto-discovered)
        services.AddTransient<LocomotivesPage>();
        pages.Add(new PageMetadata("locomotives", "Locomotives", "\uE7C0", typeof(LocomotivesPage), NavigationCategory.Solution, 25, "IsLocomotivesPageAvailable", "LocomotivesPageLabel", null, false));

        services.AddTransient<PassengerWagonPage>();
        pages.Add(new PageMetadata("passengerwagons", "Passenger Wagons", "\uE7C0", typeof(PassengerWagonPage), NavigationCategory.Solution, 26, "IsPassengerWagonsPageAvailable", "PassengerWagonsPageLabel", null, false));

        services.AddTransient<SolutionPage>();
        pages.Add(new PageMetadata("solution", "Solution", "\uE8B7", typeof(SolutionPage), NavigationCategory.Solution, 10, "IsSolutionPageAvailable", "SolutionPageLabel", null, false));

        services.AddTransient<HelpPage>();
        pages.Add(new PageMetadata("help", "Help", "\uE897", typeof(HelpPage), NavigationCategory.Help, 10, null, null, null, false));

        services.AddTransient<SettingsPage>();
        pages.Add(new PageMetadata("settings", "Settings", "\uE115", typeof(SettingsPage), NavigationCategory.Help, 30, "IsSettingsPageAvailable", "SettingsPageLabel", null, false));

        services.AddTransient<OverviewPage>();
        pages.Add(new PageMetadata("overview", "Overview", "\uE80F", typeof(OverviewPage), NavigationCategory.Core, 10, "IsOverviewPageAvailable", "OverviewPageLabel", null, false));

        services.AddTransient<WorkflowsPage>();
        pages.Add(new PageMetadata("workflows", "Workflows", "\uE945", typeof(WorkflowsPage), NavigationCategory.Solution, 20, "IsWorkflowsPageAvailable", "WorkflowsPageLabel", null, false));

        services.AddTransient<GoodsWagonPage>();
        pages.Add(new PageMetadata("goodswagons", "Goods Wagons", "\uE7C0", typeof(GoodsWagonPage), NavigationCategory.Solution, 27, "IsGoodsWagonsPageAvailable", "GoodsWagonsPageLabel", null, false));

        services.AddTransient<TrackPlanPage>();
        pages.Add(new PageMetadata("trackplaneditor", "Track Plan", "\uE7F9", typeof(TrackPlanPage), NavigationCategory.TrackManagement, 10, "IsTrackPlanEditorPageAvailable", "TrackPlanEditorPageLabel", null, false));

        services.AddTransient<TrainControlPage>();
        pages.Add(new PageMetadata("traincontrol", "Train Control", "\uEC49", typeof(TrainControlPage), NavigationCategory.TrainControl, 10, "IsTrainControlPageAvailable", "TrainControlPageLabel", null, true));

        services.AddTransient<JourneyMapPage>();
        pages.Add(new PageMetadata("journeymap", "Journey Map", "\uE81D", typeof(JourneyMapPage), NavigationCategory.Journey, 20, "IsJourneyMapPageAvailable", "JourneyMapPageLabel", null, false));

        services.AddTransient<InfoPage>();
        pages.Add(new PageMetadata("info", "Info", "\uE946", typeof(InfoPage), NavigationCategory.Help, 20, null, null, null, false));

        services.AddTransient<MonitorPage>();
        pages.Add(new PageMetadata("monitor", "Monitor", "\uE7F4", typeof(MonitorPage), NavigationCategory.Monitoring, 10, "IsMonitorPageAvailable", "MonitorPageLabel", null, false));

        // Manual registrations for pages with custom DI requirements
        // JourneysPage: requires AppSettings + ISettingsService injection
        services.AddTransient<JourneysPage>(sp => new JourneysPage(
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

        // SignalBoxPage: requires custom viewmodel injection
        services.AddTransient<SignalBoxPage>(sp => new SignalBoxPage(
            sp.GetRequiredService<MainWindowViewModel>(),
            sp.GetRequiredService<ISkinProvider>(),
            sp.GetRequiredService<SkinSelectorViewModel>(),
            sp.GetRequiredService<ViessmannSignalService>()));
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

        // DockingPage: requires separate ViewModel registration
        services.AddTransient<DockingPageViewModel>();
        services.AddTransient<DockingPage>();
        pages.Add(new PageMetadata(
            Tag: "docking",
            Title: "Docking",
            Icon: null,
            PageType: typeof(DockingPage),
            Category: NavigationCategory.Monitoring,
            Order: 20,
            FeatureToggleKey: "IsDockingPageAvailable",
            BadgeLabelKey: "DockingPageLabel",
            PathIconData: "M2,3 L6,3 L6,7 L2,7 Z M8,3 L12,3 L12,7 L8,7 Z M2,9 L6,9 L6,13 L2,13 Z M8,9 L12,9 L12,13 L8,13 Z",
            IsBold: false));

        // Return sorted
        return pages
            .OrderBy(p => (int)p.Category)
            .ThenBy(p => p.Order)
            .ToList();
    }
}
