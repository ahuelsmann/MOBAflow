// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Behavior;

using Microsoft.UI.Xaml;

/// <summary>
/// Drag and drop handler for DockingManager panels.
/// Visualizes drop zones and validates drop positions.
/// </summary>
public static class DockingDropBehavior
{
    #region Attached Properties

    /// <summary>
    /// Attached property to enable/disable dock drop functionality.
    /// </summary>
    public static readonly DependencyProperty AllowDockDropProperty =
        DependencyProperty.RegisterAttached(
            "AllowDockDrop",
            typeof(bool),
            typeof(DockingDropBehavior),
            new PropertyMetadata(false, OnAllowDockDropChanged));

    /// <summary>
    /// Attached property to get/set the dock position.
    /// </summary>
    public static readonly DependencyProperty DockPositionProperty =
        DependencyProperty.RegisterAttached(
            "DockPosition",
            typeof(DockPosition),
            typeof(DockingDropBehavior),
            new PropertyMetadata(DockPosition.Center));

    #endregion

    #region Attached Property Methods

    /// <summary>
    /// Gets the AllowDockDrop attached property value.
    /// </summary>
    /// <param name="obj">The dependency object.</param>
    /// <returns>True if dock drop is allowed, otherwise false.</returns>
    public static bool GetAllowDockDrop(DependencyObject obj) =>
        (bool)obj.GetValue(AllowDockDropProperty);

    /// <summary>
    /// Sets the AllowDockDrop attached property value.
    /// </summary>
    /// <param name="obj">The dependency object.</param>
    /// <param name="value">The value to set.</param>
    public static void SetAllowDockDrop(DependencyObject obj, bool value) =>
        obj.SetValue(AllowDockDropProperty, value);

    /// <summary>
    /// Gets the DockPosition attached property value.
    /// </summary>
    /// <param name="obj">The dependency object.</param>
    /// <returns>The current dock position.</returns>
    public static DockPosition GetDockPosition(DependencyObject obj) =>
        (DockPosition)obj.GetValue(DockPositionProperty);

    /// <summary>
    /// Sets the DockPosition attached property value.
    /// </summary>
    /// <param name="obj">The dependency object.</param>
    /// <param name="value">The dock position value to set.</param>
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
            _ = GetDockPosition(element);
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

    private static void HighlightDropZone(FrameworkElement element, DockPosition _position)
    {
        // Apply visual feedback based on drop position
        // could be implemented with VisualState or OpacityMask
        element.Opacity = 0.8;
    }

    private static void ClearDropZoneHighlight(FrameworkElement element)
    {
        element.Opacity = 1.0;
    }

    #endregion
}

/// <summary>
/// Enum for dock positions.
/// </summary>
public enum DockPosition
{
    /// <summary>
    /// Dock to the left side.
    /// </summary>
    Left,

    /// <summary>
    /// Dock to the right side.
    /// </summary>
    Right,

    /// <summary>
    /// Dock to the top side.
    /// </summary>
    Top,

    /// <summary>
    /// Dock to the bottom side.
    /// </summary>
    Bottom,

    /// <summary>
    /// Dock to the center area.
    /// </summary>
    Center
}
