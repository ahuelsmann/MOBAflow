// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Microsoft.Graphics.Canvas;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

using System.Diagnostics;
using TrackLibrary.PikoA;

using TrackPlan.Renderer;

using Windows.System;
using Windows.UI;
using Windows.UI.Core;

using Path = Path;

public sealed partial class TrackPlanPage
{
    private bool _showGrid;
    private bool _showValidationOverlay = true;
    private bool _showTrackLabels = true;
    private bool _initialFitPending = true;

    private const double MinCanvasWidthPx = 2000;
    private const double MinCanvasHeightPx = 1500;

    private void InitializeEditorFeatures()
    {
        FitButton.Click += (_, _) => FitToContent();
        ResetZoomButton.Click += (_, _) => ResetZoom();
        GridToggle.Checked += (_, _) => ToggleGrid(true);
        GridToggle.Unchecked += (_, _) => ToggleGrid(false);
        ValidationOverlayToggle.Checked += (_, _) => ToggleValidationOverlay(true);
        ValidationOverlayToggle.Unchecked += (_, _) => ToggleValidationOverlay(false);
        TrackLabelsToggle.Checked += (_, _) => ToggleTrackLabels(true);
        TrackLabelsToggle.Unchecked += (_, _) => ToggleTrackLabels(false);
        ActualThemeChanged += (_, _) =>
        {
            // Rebuild toolbox previews so their Stroke/Border/Background brushes resolve against
            // the new theme (these are plain Brush instances set in code and do not auto-update
            // like {ThemeResource} bindings). The Win2D canvas picks up the new colors via its
            // Draw handler.
            PopulateToolbox();
            RefreshCanvas();
        };

        _showGrid = GridToggle.IsChecked == true;
        _showValidationOverlay = ValidationOverlayToggle.IsChecked != false;
        _showTrackLabels = TrackLabelsToggle.IsChecked != false;
        UpdateValidationOverlayVisibility();
        UpdateCommandStates();
        UpdatePanCursor();
    }

    private void ToggleGrid(bool isEnabled)
    {
        _showGrid = isEnabled;
        RefreshCanvas();
    }

    private void ToggleValidationOverlay(bool isEnabled)
    {
        _showValidationOverlay = isEnabled;
        UpdateValidationOverlayVisibility();
        RefreshCanvas();
    }

    private void ToggleTrackLabels(bool isEnabled)
    {
        _showTrackLabels = isEnabled;
        RefreshCanvas();
    }

    private void UpdateValidationOverlayVisibility()
    {
        if (ValidationLegendPanel != null)
            ValidationLegendPanel.Visibility = _showValidationOverlay ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateCommandStates()
    {
        FitButton.IsEnabled = _plan.Segments.Count > 0;
        ExportDropDownButton.IsEnabled = _plan.Segments.Count > 0;
        ExportSvgInBrowserMenuItem.IsEnabled = _plan.Segments.Count > 0;
    }

    private void FitToContent()
    {
        var bounds = ComputeContentBoundsMm();
        if (bounds == null)
        {
            ViewModel.StatusText = "No track plan available to fit.";
            return;
        }

        RecalculateDrawOffset();
        UpdateCanvasSize();

        var (minX, minY, maxX, maxY) = bounds.Value;
        var (offsetX, offsetY) = GetDrawOffset();
        var widthMm = Math.Max(1, maxX - minX + (2 * ContentMarginMm));
        var heightMm = Math.Max(1, maxY - minY + (2 * ContentMarginMm));
        var viewportWidthPx = Math.Max(100, CanvasScrollViewer.ActualWidth - 32);
        var viewportHeightPx = Math.Max(100, CanvasScrollViewer.ActualHeight - 32);
        var zoom = Math.Clamp(
            Math.Min(viewportWidthPx / (widthMm * ScaleMmToPx), viewportHeightPx / (heightMm * ScaleMmToPx)),
            0.1,
            3.0);

        var contentCenterX = ((minX + maxX) / 2 + offsetX) * ScaleMmToPx;
        var contentCenterY = ((minY + maxY) / 2 + offsetY) * ScaleMmToPx;
        var scrollX = Math.Max(0, (contentCenterX * zoom) - (viewportWidthPx / 2));
        var scrollY = Math.Max(0, (contentCenterY * zoom) - (viewportHeightPx / 2));

        _zoomSyncing = true;
        ZoomSlider.Value = zoom;
        ZoomPercentText.Text = $"{zoom * 100:F0}%";
        CanvasScrollViewer.ChangeView(scrollX, scrollY, (float)zoom, disableAnimation: true);
        _zoomSyncing = false;

        RefreshCanvas();
        ViewModel.StatusText = "Track plan fitted.";
    }

    private void ResetZoom()
    {
        ZoomSlider.Value = 1.0;
        CanvasScrollViewer.ChangeView(null, null, 1.0f, disableAnimation: true);
        ViewModel.StatusText = "Zoom set to 100%.";
    }

    private (double WidthPx, double HeightPx) ComputeCanvasSizePx()
    {
        var bounds = ComputeContentBoundsMm();
        if (bounds == null)
            return (MinCanvasWidthPx, MinCanvasHeightPx);

        var widthMm = bounds.Value.MaxX - bounds.Value.MinX + (2 * ContentMarginMm);
        var heightMm = bounds.Value.MaxY - bounds.Value.MinY + (2 * ContentMarginMm);
        return (
            Math.Max(MinCanvasWidthPx, widthMm * ScaleMmToPx),
            Math.Max(MinCanvasHeightPx, heightMm * ScaleMmToPx));
    }

    private void UpdateCanvasSize()
    {
        var (widthPx, heightPx) = ComputeCanvasSizePx();
        CanvasContainer.Width = widthPx;
        CanvasContainer.Height = heightPx;
        GraphCanvasControl.Width = widthPx;
        GraphCanvasControl.Height = heightPx;
        OverlayCanvas.Width = widthPx;
        OverlayCanvas.Height = heightPx;
    }

    private void TryInitialFitOnLoad()
    {
        if (!_initialFitPending)
            return;

        if (CanvasScrollViewer.ActualWidth <= 0 || CanvasScrollViewer.ActualHeight <= 0)
            return;

        _initialFitPending = false;
        UpdateCanvasSize();

        if (_settings.Layout.TrackPlanPage.FitOnLoad && _plan.Segments.Count > 0)
            FitToContent();
    }

    private void OnZoomInAccelerator(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        _ = sender;
        if (FocusManager.GetFocusedElement(XamlRoot) is TextBox or NumberBox)
            return;

        AdjustZoom(ZoomStep);
        args.Handled = true;
    }

    private void OnZoomOutAccelerator(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        _ = sender;
        if (FocusManager.GetFocusedElement(XamlRoot) is TextBox or NumberBox)
            return;

        AdjustZoom(-ZoomStep);
        args.Handled = true;
    }

    private void OnFitAccelerator(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        _ = sender;
        if (FocusManager.GetFocusedElement(XamlRoot) is TextBox or NumberBox)
            return;

        FitToContent();
        args.Handled = true;
    }

    private void OnResetZoomAccelerator(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        _ = sender;
        if (FocusManager.GetFocusedElement(XamlRoot) is TextBox or NumberBox)
            return;

        ResetZoom();
        args.Handled = true;
    }

    private void OnDisconnectAccelerator(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        _ = sender;
        if (FocusManager.GetFocusedElement(XamlRoot) is TextBox or NumberBox)
            return;

        if (!ViewModel.CanDisconnectSelectedTrack)
            return;

        ViewModel.DisconnectSelectedTrackCommand.Execute(null);
        args.Handled = true;
    }

    private void DrawGrid(CanvasDrawingSession drawingSession, double offsetX, double offsetY)
    {
        var (widthPx, heightPx) = ComputeCanvasSizePx();
        var widthPxResolved = GraphCanvasControl?.ActualWidth > 0 ? GraphCanvasControl.ActualWidth : widthPx;
        var heightPxResolved = GraphCanvasControl?.ActualHeight > 0 ? GraphCanvasControl.ActualHeight : heightPx;
        var worldMinX = -offsetX;
        var worldMaxX = (widthPxResolved / ScaleMmToPx) - offsetX;
        var worldMinY = -offsetY;
        var worldMaxY = (heightPxResolved / ScaleMmToPx) - offsetY;
        var startX = Math.Floor(worldMinX / 100.0) * 100.0;
        var startY = Math.Floor(worldMinY / 100.0) * 100.0;
        var minorColor = ThemeResourceResolver.ResolveColor(this, "TrackPlanGridMinorBrush", Color.FromArgb(255, 236, 236, 236));
        var majorColor = ThemeResourceResolver.ResolveColor(this, "TrackPlanGridMajorBrush", Color.FromArgb(255, 208, 208, 208));

        for (var worldX = startX; worldX <= worldMaxX; worldX += 100.0)
        {
            var displayX = (float)((worldX + offsetX) * ScaleMmToPx);
            var isMajor = Math.Abs(worldX % 500.0) < 0.001;
            drawingSession.DrawLine(displayX, 0, displayX, (float)heightPxResolved, isMajor ? majorColor : minorColor, isMajor ? 1.5f : 1f);
        }

        for (var worldY = startY; worldY <= worldMaxY; worldY += 100.0)
        {
            var displayY = (float)((worldY + offsetY) * ScaleMmToPx);
            var isMajor = Math.Abs(worldY % 500.0) < 0.001;
            drawingSession.DrawLine(0, displayY, (float)widthPxResolved, displayY, isMajor ? majorColor : minorColor, isMajor ? 1.5f : 1f);
        }
    }

    private void ExportCurrentTrackPlanToBrowser()
    {
        if (_plan.Segments.Count == 0)
        {
            ViewModel.StatusText = "There is no current track plan to export.";
            return;
        }

        var scene = TrackPlanRenderSceneBuilder.Build(_plan.Segments);
        var svg = new PlacedTrackPlanSvgRenderer().Render(scene, showGrid: _showGrid);
        var path = Path.Combine(Path.GetTempPath(), "trackplan-current.html");
        new SvgExporter().Export(svg, path);

        if (OperatingSystem.IsWindows())
            Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });

        ViewModel.StatusText = $"SVG opened: {path}";
    }

    private bool IsCtrlPressed()
    {
        return InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
    }

    private bool IsShiftPressed()
    {
        return InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
    }
}
