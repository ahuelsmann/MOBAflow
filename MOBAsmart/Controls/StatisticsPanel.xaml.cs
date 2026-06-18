// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI.Controls;

public partial class StatisticsPanel
{
    public StatisticsPanel()
    {
        InitializeComponent();
    }

    public event EventHandler? ResetCountersClicked;

    private void ResetCountersButton_Clicked(object? sender, EventArgs e) =>
        ResetCountersClicked?.Invoke(sender, e);
}