// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Behavior;

using Microsoft.UI.Xaml;

/// <summary>
/// Drag & Drop Handler für DockingManager Panels.
/// Visualisiert Drop-Zonen und validiert Drop-Positionen.
/// </summary>
public static class DockingDropBehavior
{
    #region Attached Properties

    public static readonly DependencyProperty AllowDockDropProperty =
        DependencyProperty.RegisterAttached(
            "AllowDockDrop",
            typeof(bool),
            typeof(DockingDropBehavior),
            new PropertyMetadata(false, OnAllowDockDropChanged));

    public static readonly DependencyProperty DockPositionProperty =
        DependencyProperty.RegisterAttached(
            "DockPosition",
            typeof(DockPosition),
            typeof(DockingDropBehavior),
            new PropertyMetadata(DockPosition.Center));

    #endregion

    #region Attached Property Methods

    public static bool GetAllowDockDrop(DependencyObject obj) =>
        (bool)obj.GetValue(AllowDockDropProperty);

    public static void SetAllowDockDrop(DependencyObject obj, bool value) =>
        obj.SetValue(AllowDockDropProperty, value);

    public static DockPosition GetDockPosition(DependencyObject obj) =>
        (DockPosition)obj.GetValue(DockPositionProperty);

    public static void SetDockPosition(DependencyObject obj, DockPosition value) =>
        obj.SetValue(DockPositionProperty, value);

    #endregion

    #region Event Handler

    private static void OnAllowDockDropChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is UIElement element && (bool)e.NewValue)
        {
            element.AllowDrop = true;
            element.DragOver += Element_DragOver;
            element.Drop += Element_Drop;
            element.DragLeave += Element_DragLeave;
        }
    }

    #endregion

    #region Drag & Drop Handlers

    private static void Element_DragOver(object sender, DragEventArgs e)
    {
        if (sender is FrameworkElement element)
        {
            var position = GetDragPosition(e, element);
            SetDockPosition(element, position);

            e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Move;
            
            // Visual Feedback: Highlight Zone
            HighlightDropZone(element, position);
        }
    }

    private static void Element_Drop(object sender, DragEventArgs e)
    {
        if (sender is FrameworkElement element)
        {
            var position = GetDockPosition(element);
            // Tab/Panel wird an neue Position verschoben (wird von DockingManager gehandhabt)
        }
    }

    private static void Element_DragLeave(object sender, DragEventArgs e)
    {
        if (sender is FrameworkElement element)
        {
            ClearDropZoneHighlight(element);
        }
    }

    #endregion

    #region Helper Methods

    private static DockPosition GetDragPosition(DragEventArgs e, FrameworkElement element)
    {
        var point = e.GetPosition(element);
        var width = element.ActualWidth;
        var height = element.ActualHeight;

        // Bereiche: 30% von jeder Kante = Drop-Zone
        const double threshold = 0.3;

        var leftThreshold = width * threshold;
        var rightThreshold = width * (1 - threshold);
        var topThreshold = height * threshold;
        var bottomThreshold = height * (1 - threshold);

        if (point.X < leftThreshold)
            return DockPosition.Left;
        if (point.X > rightThreshold)
            return DockPosition.Right;
        if (point.Y < topThreshold)
            return DockPosition.Top;
        if (point.Y > bottomThreshold)
            return DockPosition.Bottom;

        return DockPosition.Center;
    }

    private static void HighlightDropZone(FrameworkElement element, DockPosition position)
    {
        // Appliziere visuelles Feedback basierend auf Drop-Position
        // könnte mit VisualState oder OpacityMask implementiert werden
        element.Opacity = 0.8;
    }

    private static void ClearDropZoneHighlight(FrameworkElement element)
    {
        element.Opacity = 1.0;
    }

    #endregion
}

/// <summary>
/// Enum für Dock-Positionen.
/// </summary>
public enum DockPosition
{
    Left,
    Right,
    Top,
    Bottom,
    Center
}
