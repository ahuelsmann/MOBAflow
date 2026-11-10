// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Moba.SharedUI.ViewModel;

/// <summary>
/// Overview page showing the Lap Counter Dashboard with Z21 connection and track statistics.
/// Binds the Z21 status and telemetry to the shell, the counters to the host-owned counter state and the
/// REST API and synchronization rows to the host-owned system status.
/// </summary>
internal sealed partial class OverviewPage
{
    public MainWindowViewModel ViewModel { get; }

    /// <summary>Gets the host-owned feedback-point counter state shared with the Event Manager.</summary>
    public InPortCounterViewModel Counters { get; }

    /// <summary>Gets the host-owned REST API, synchronization and solution-loaded status shared with the status bar.</summary>
    public SystemStatusViewModel SystemStatus { get; }

    public OverviewPage(MainWindowViewModel viewModel, InPortCounterViewModel counters, SystemStatusViewModel systemStatus)
    {
        ViewModel = viewModel;
        Counters = counters;
        SystemStatus = systemStatus;
        InitializeComponent();
    }
}
