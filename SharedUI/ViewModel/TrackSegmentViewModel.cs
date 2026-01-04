namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using TrackPlan.Domain;
using TrackPlan.Geometry;

public partial class TrackSegmentViewModel : ObservableObject
{
    public TrackSegment Model { get; }

    public string Id
    {
        get => Model.Id;
        set => Model.Id = value;
    }

    public string ArticleCode
    {
        get => Model.ArticleCode;
        set => Model.ArticleCode = value;
    }

    public Transform2D WorldTransform
    {
        get => Model.WorldTransform;
        set => Model.WorldTransform = value;
    }

    public uint? AssignedInPort
    {
        get => Model.AssignedInPort;
        set => Model.AssignedInPort = value;
    }

    public bool HasInPort => Model.AssignedInPort.HasValue;

    [ObservableProperty] private bool isSelected;
    [ObservableProperty] private bool isDragging;
    [ObservableProperty] private bool isPartOfDragGroup;
    [ObservableProperty] private double dragOffsetX;
    [ObservableProperty] private double dragOffsetY;
    [ObservableProperty] private string pathData = "M 0,0";

    public TrackSegmentViewModel(TrackSegment model)
    {
        Model = model;
    }

    public void MoveBy(double dx, double dy)
    {
        var wt = Model.WorldTransform;
        Model.WorldTransform = new Transform2D
        {
            TranslateX = wt.TranslateX + dx,
            TranslateY = wt.TranslateY + dy,
            RotationDegrees = wt.RotationDegrees
        };
    }
}