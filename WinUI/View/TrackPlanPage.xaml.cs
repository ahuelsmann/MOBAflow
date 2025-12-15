// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

using Moba.SharedUI.ViewModel;

/// <summary>
/// Track Plan page showing the physical layout with clickable track segments.
/// Click on a segment to select it and assign an InPort for feedback sensors.
/// </summary>
public sealed partial class TrackPlanPage : Page
{
    public TrackPlanViewModel ViewModel { get; }

    public TrackPlanPage(TrackPlanViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }

    /// <summary>
    /// Handles click on a track segment to select it.
    /// </summary>
    private void Segment_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is TrackSegmentViewModel segment)
        {
            ViewModel.SelectSegmentCommand.Execute(segment);
            e.Handled = true;
        }
    }

    /// <summary>
    /// Handles click on empty canvas area to deselect current segment.
    /// </summary>
    private void TrackCanvas_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        // Only deselect if click was directly on canvas, not on a segment
        if (!e.Handled)
        {
            ViewModel.DeselectSegmentCommand.Execute(null);
        }
    }
}
