// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Service;

using Common.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moba.WinUI.ViewModel;
using SharedUI.Interface;
using SharedUI.Shell;
using SharedUI.ViewModel;
using View;

/// <summary>
/// Centralizes page and navigation registrations for the WinUI app.
/// </summary>
public static class NavigationRegistration
{
    /// <summary>
    /// Registers WinUI pages and navigation metadata.
    /// </summary>
    public static void RegisterPages(IServiceCollection services, NavigationRegistry navigationRegistry)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(navigationRegistry);

        // Core: Overview
        services.AddTransient<OverviewPage>();
        navigationRegistry.Register("overview", "Overview", "\uE80F", typeof(OverviewPage), "Shell",
            NavigationCategory.Core, 10, "IsOverviewPageAvailable", "OverviewPageLabel");

        // TrainControl: Train Control (bold, prominent)
        services.AddTransient<TrainControlPage>();
        navigationRegistry.Register("traincontrol", "Train Control", "\uEC49", typeof(TrainControlPage), "Shell",
            NavigationCategory.TrainControl, 10, "IsTrainControlPageAvailable", "TrainControlPageLabel", null, true);

        // Journey: Journeys (bold)
        services.AddTransient<JourneysPage>();
        navigationRegistry.Register("journeys", "Journeys", "\uE7C1", typeof(JourneysPage), "Shell",
            NavigationCategory.Journey, 10, "IsJourneysPageAvailable", "JourneysPageLabel", null, true);

        // Journey: Journey Map
        services.AddTransient<JourneyMapPage>();
        navigationRegistry.Register("journeymap", "Journey Map", "\uE81D", typeof(JourneyMapPage), "Shell",
            NavigationCategory.Journey, 20, "IsJourneyMapPageAvailable", "JourneyMapPageLabel");

        // Solution: Solution, Workflows, Trains
        services.AddTransient<SolutionPage>();
        navigationRegistry.Register("solution", "Solution", "\uE8B7", typeof(SolutionPage), "Shell",
            NavigationCategory.Solution, 10, "IsSolutionPageAvailable", "SolutionPageLabel");

        services.AddTransient<WorkflowsPage>();
        navigationRegistry.Register("workflows", "Workflows", "\uE945", typeof(WorkflowsPage), "Shell",
            NavigationCategory.Solution, 20, "IsWorkflowsPageAvailable", "WorkflowsPageLabel");

        services.AddTransient<TrainsPage>();
        navigationRegistry.Register("trains", "Trains", "\uE7C0", typeof(TrainsPage), "Shell",
            NavigationCategory.Solution, 30, "IsTrainsPageAvailable", "TrainsPageLabel");

        // TrackManagement: MOBAtps, MOBAesb
        services.AddTransient<TrackPlanPage>();
        navigationRegistry.Register("trackplaneditor", "MOBAtps", null, typeof(TrackPlanPage), "Shell",
            NavigationCategory.TrackManagement, 10, "IsTrackPlanEditorPageAvailable", "TrackPlanEditorPageLabel",
            "M2,3 L4,3 L14,13 L12,13 Z M12,3 L14,3 L4,13 L2,13 Z");

        services.AddTransient<SignalBoxPage>(sp => new SignalBoxPage(
            sp.GetRequiredService<MainWindowViewModel>(),
            sp.GetRequiredService<ISkinProvider>(),
            sp.GetRequiredService<SkinSelectorViewModel>()));
        navigationRegistry.Register("signalbox", "MOBAesb", null, typeof(SignalBoxPage), "Shell",
            NavigationCategory.TrackManagement, 20, null, null,
            "M7,2 A2,2 0 1,1 11,2 A2,2 0 1,1 7,2 M3,10 A2,2 0 1,1 7,10 A2,2 0 1,1 3,10 M11,10 A2,2 0 1,1 15,10 A2,2 0 1,1 11,10");

        // Monitoring: Monitor
        services.AddTransient<MonitorPage>();
        navigationRegistry.Register("monitor", "Monitor", "\uE7F4", typeof(MonitorPage), "Shell",
            NavigationCategory.Monitoring, 10, "IsMonitorPageAvailable", "MonitorPageLabel");

        // Help: Help, Info, Settings
        services.AddTransient<HelpPage>();
        navigationRegistry.Register("help", "Help", "\uE897", typeof(HelpPage), "Shell",
            NavigationCategory.Help, 10);

        services.AddTransient<InfoPage>();
        navigationRegistry.Register("info", "Info", "\uE946", typeof(InfoPage), "Shell",
            NavigationCategory.Help, 20);

        services.AddTransient<SettingsPage>();
        navigationRegistry.Register("settings", "Settings", "\uE115", typeof(SettingsPage), "Shell",
            NavigationCategory.Help, 30, "IsSettingsPageAvailable", "SettingsPageLabel");
    }
}
