// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;

using Moba.Domain.TrackPlan;

/// <summary>
/// ViewModel wrapper for a TrackSegment.
/// Provides selection state and feedback visualization for UI binding.
/// </summary>
public partial class TrackSegmentViewModel : ObservableObject
{
    private readonly TrackSegment _segment;

    public TrackSegmentViewModel(TrackSegment segment)
    {
        ArgumentNullException.ThrowIfNull(segment);
        _segment = segment;
    }

    #region Domain Properties (1:1 mapping)

    /// <summary>
    /// Unique identifier for this segment.
    /// </summary>
    public string Id => _segment.Id;

    /// <summary>
    /// Display name for this segment.
    /// </summary>
    public string Name => _segment.Name;

    /// <summary>
    /// Type of track segment.
    /// </summary>
    public TrackSegmentType Type => _segment.Type;

    /// <summary>
    /// Piko A-Gleis article code (e.g., "G231", "R2", "WL").
    /// </summary>
    public string ArticleCode => _segment.ArticleCode;

    /// <summary>
    /// SVG path data for rendering this segment.
    /// </summary>
    public string PathData => _segment.PathData;

    /// <summary>
    /// X coordinate of the segment's center point.
    /// </summary>
    public double CenterX => _segment.CenterX;

    /// <summary>
    /// Y coordinate of the segment's center point.
    /// </summary>
    public double CenterY => _segment.CenterY;

    /// <summary>
    /// Rotation angle in degrees.
    /// </summary>
    public double Rotation => _segment.Rotation;

    /// <summary>
    /// Track number for multi-track stations.
    /// </summary>
    public string? TrackNumber => _segment.TrackNumber;

    /// <summary>
    /// Layer this segment belongs to.
    /// </summary>
    public string Layer => _segment.Layer;

    #endregion

    #region ViewModel Properties (UI state)

    /// <summary>
    /// Indicates whether this segment is currently selected in the UI.
    /// </summary>
    [ObservableProperty]
    private bool isSelected;

    /// <summary>
    /// Indicates whether this segment's sensor is currently triggered (feedback active).
    /// </summary>
    [ObservableProperty]
    private bool isTriggered;

    /// <summary>
    /// Assigned InPort for feedback sensor. Null if no sensor assigned.
    /// </summary>
    public uint? AssignedInPort
    {
        get => _segment.AssignedInPort;
        set
        {
            if (_segment.AssignedInPort != value)
            {
                _segment.AssignedInPort = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasInPort));
                OnPropertyChanged(nameof(InPortDisplayText));
            }
        }
    }

    /// <summary>
    /// Indicates whether this segment has an InPort assigned.
    /// </summary>
    public bool HasInPort => _segment.AssignedInPort.HasValue;

    /// <summary>
    /// Display text for the InPort (e.g., "[1]" or empty).
    /// </summary>
    public string InPortDisplayText => _segment.AssignedInPort.HasValue
        ? $"[{_segment.AssignedInPort.Value}]"
        : string.Empty;

    #endregion

    #region Computed Properties for Styling

    /// <summary>
    /// Stroke thickness based on selection state.
    /// Thicker lines make tracks easier to click with mouse.
    /// </summary>
    public double StrokeThickness => IsSelected ? 35 : 25;

    /// <summary>
    /// Display text combining article code and InPort.
    /// </summary>
    public string DisplayLabel => HasInPort
        ? $"{ArticleCode} [{AssignedInPort}]"
        : ArticleCode;

    #endregion

    /// <summary>
    /// Updates triggered state based on feedback from Z21.
    /// </summary>
    /// <param name="inPort">The InPort that was triggered.</param>
    /// <param name="isOccupied">Whether the track is occupied.</param>
    public void UpdateFeedback(uint inPort, bool isOccupied)
    {
        if (AssignedInPort == inPort)
        {
            IsTriggered = isOccupied;
        }
    }

    partial void OnIsSelectedChanged(bool value)
    {
        OnPropertyChanged(nameof(StrokeThickness));
    }
}
