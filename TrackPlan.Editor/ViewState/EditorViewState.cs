// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.TrackPlan.Editor.ViewState;

public sealed class EditorViewState
{
    public Guid? SelectedEdgeId { get; set; }

    public int? SelectedFeedbackPointNumber { get; set; }

    public bool ShowFeedbackPoint { get; set; } = true;

    /// <summary>
    /// Controls visibility of track code labels (e.g., "G231", "R9", "BWL").
    /// </summary>
    public bool ShowCodeLabels { get; set; } = true;

    /// <summary>
    /// Show/hide horizontal and vertical rulers (fixed at top/left edges).
    /// </summary>
    public bool ShowFixedRulers { get; set; } = true;

    /// <summary>
    /// Show/hide movable ruler (can be positioned and rotated by user).
    /// </summary>
    public bool ShowMovableRuler { get; set; } = false;

    /// <summary>
    /// Position of movable ruler (if shown).
    /// </summary>
    public (double X, double Y)? MovableRulerPosition { get; set; }

    /// <summary>
    /// Rotation of movable ruler in degrees (0-359).
    /// </summary>
    public double MovableRulerRotationDeg { get; set; } = 0;

    /// <summary>
    /// Opacity of movable ruler (0.0 = transparent, 1.0 = opaque).
    /// </summary>
    public double MovableRulerOpacity { get; set; } = 0.8;

    /// <summary>
    /// Whether user is currently dragging the movable ruler.
    /// </summary>
    public bool IsDraggingMovableRuler { get; set; } = false;

    /// <summary>
    /// Show/hide port hover animation (scale + glow effects).
    /// </summary>
    public bool ShowPortHoverAnimation { get; set; } = true;
}