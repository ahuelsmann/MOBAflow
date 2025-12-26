// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SharedUI.ViewModel;
using System.Diagnostics;
using Windows.ApplicationModel.DataTransfer;

/// <summary>
/// Track Plan Editor Page - Topology-based track plan editor.
/// Simplified code-behind for the new topology-based architecture.
/// </summary>
public sealed partial class TrackPlanEditorPage : Page
{
    public TrackPlanEditorPage(TrackPlanEditorViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    public TrackPlanEditorViewModel ViewModel => (TrackPlanEditorViewModel)DataContext;

    #region Drag & Drop from Library

    private void TrackLibrary_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
    {
        if (e.Items.FirstOrDefault() is TrackTemplateViewModel template)
        {
            e.Data.SetText(template.ArticleCode);
            e.Data.RequestedOperation = DataPackageOperation.Copy;
            Debug.WriteLine($"🎯 Dragging: {template.ArticleCode}");
        }
    }

    private void TrackCanvas_DragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = DataPackageOperation.Copy;
        e.DragUIOverride.Caption = "Add track";
    }

    private async void TrackCanvas_Drop(object sender, DragEventArgs e)
    {
        if (e.DataView.Contains(StandardDataFormats.Text))
        {
            var articleCode = await e.DataView.GetTextAsync();
            var template = ViewModel.TrackLibrary.FirstOrDefault(t => t.ArticleCode == articleCode);
            if (template != null)
            {
                ViewModel.AddSegmentCommand.Execute(template);
                Debug.WriteLine($"✅ Dropped: {articleCode}");
            }
        }
    }

    #endregion

    #region Selection

    private void TrackCanvas_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        // Click on empty area deselects
        ViewModel.SelectedSegment = null;
    }

    private void Segment_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is TrackSegmentViewModel segment)
        {
            ViewModel.SelectedSegment = segment;
            e.Handled = true;
            Debug.WriteLine($"🔵 Selected: {segment.ArticleCode}");
        }
    }

    #endregion
}
