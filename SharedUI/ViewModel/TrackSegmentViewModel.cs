// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using Moba.TrackPlan.Domain;
using Moba.TrackPlan.Geometry;

/// <summary>
/// ViewModel for a track segment in the topology-based track plan.
/// Position and PathData are computed by the TrackLayoutRenderer, not stored.
/// </summary>
public partial class TrackSegmentViewModel : ObservableObject
{
    private readonly TrackSegment _segment;

    public TrackSegmentViewModel(TrackSegment segment)
    {
        ArgumentNullException.ThrowIfNull(segment);
        _segment = segment;
    }

    public TrackSegment Model => _segment;

    #region Domain Properties (from TrackSegment)

    public string Id => _segment.Id;
    public string ArticleCode => _segment.ArticleCode;
    public string? Name => _segment.Name;
    public string? Layer => _segment.Layer;

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

    public bool HasInPort => _segment.AssignedInPort.HasValue;
    public string InPortDisplayText => _segment.AssignedInPort?.ToString() ?? string.Empty;

    public bool IsOccupied => _segment.IsOccupied;

    #endregion

    #region Render Properties (proxy to Model.WorldTransform)

    public Transform2D WorldTransform
    {
        get => _segment.WorldTransform;
        set
        {
            if (_segment.WorldTransform != value)
            {
                _segment.WorldTransform = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(LabelX));
                OnPropertyChanged(nameof(LabelY));
                OnPropertyChanged(nameof(InPortLabelY));
            }
        }
    }

    [ObservableProperty]
    private string? pathData = "M 0,0";

    #endregion

    #region UI State

    [ObservableProperty]
    private bool isSelected;

    [ObservableProperty]
    private bool isTriggered;

    [ObservableProperty]
    private bool isDragging;

    [ObservableProperty]
    private bool isPartOfDragGroup;

    [ObservableProperty]
    private bool isSnapTarget;

    #endregion

    #region Drag Support

    public double DragOffsetX { get; set; }
    public double DragOffsetY { get; set; }

    public void MoveBy(double deltaX, double deltaY)
    {
        _ = deltaX;
        _ = deltaY;
    }

    #endregion

    #region Computed Properties for UI

    public double LabelX => WorldTransform.TranslateX - 10;
    public double LabelY => WorldTransform.TranslateY - 6;
    public double InPortLabelY => LabelY - 14;

    #endregion
}
