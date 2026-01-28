// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.View;

/// <summary>
/// Helper for implementing standard drag-and-drop with distance threshold.
/// Based on Microsoft AutomaticDragHelper pattern from WinUI source code.
/// 
/// Pattern: Click selects, drag only starts when mouse moves beyond threshold.
/// Threshold: 8 pixels (SM_CXDRAG * 2.0 multiplier from Windows system metrics).
/// </summary>
public sealed class DragThresholdHelper
{
    /// <summary>
    /// Standard drag threshold from Microsoft AutomaticDragHelper.
    /// SM_CXDRAG (4px) * UIELEMENT_MOUSE_DRAG_THRESHOLD_MULTIPLIER (2.0) = 8px
    /// </summary>
    public const double DragThresholdPixels = 8.0;

    private bool _isWaitingForThreshold;
    private Windows.Foundation.Point _startPosition;

    /// <summary>
    /// Call on PointerPressed to start tracking potential drag.
    /// </summary>
    public void BeginTracking(Windows.Foundation.Point position)
    {
        _isWaitingForThreshold = true;
        _startPosition = position;
    }

    /// <summary>
    /// Call on PointerMoved to check if threshold is crossed.
    /// Returns true if drag should start.
    /// </summary>
    public bool ShouldStartDrag(Windows.Foundation.Point currentPosition)
    {
        if (!_isWaitingForThreshold)
            return false;

        var deltaX = Math.Abs(currentPosition.X - _startPosition.X);
        var deltaY = Math.Abs(currentPosition.Y - _startPosition.Y);

        return deltaX > DragThresholdPixels || deltaY > DragThresholdPixels;
    }

    /// <summary>
    /// Call when drag actually starts or when PointerReleased without drag.
    /// </summary>
    public void Reset()
    {
        _isWaitingForThreshold = false;
    }

    /// <summary>
    /// Returns true if currently waiting for threshold to be crossed.
    /// </summary>
    public bool IsWaiting => _isWaitingForThreshold;
}
