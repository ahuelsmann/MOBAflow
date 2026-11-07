// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Moba.SharedUI.ViewModel;

/// <summary>
/// Overview page showing the Lap Counter Dashboard with Z21 connection and track statistics.
/// Binds the Z21 status and telemetry to the shell and the counters to the host-owned counter state.
/// </summary>
internal sealed partial class OverviewPage
{
    public MainWindowViewModel ViewModel { get; }

    /// <summary>Gets the host-owned feedback-point counter state shared with the Event Manager.</summary>
    public InPortCounterViewModel Counters { get; }

    public OverviewPage(MainWindowViewModel viewModel, InPortCounterViewModel counters)
    {
        ViewModel = viewModel;
        Counters = counters;
        InitializeComponent();
    }
}
