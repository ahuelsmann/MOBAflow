// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using Domain;
using Interface;

/// <summary>
/// Base ViewModel for signal box elements.
/// Provides two-way binding between domain model and UI.
/// </summary>
public class SbElementViewModel : ObservableObject, IViewModelWrapper<SbElement>
{
    private readonly SbElement _model;

    /// <summary>
    /// Initializes a new instance of the <see cref="SbElementViewModel"/> class.
    /// </summary>
    /// <param name="model">The underlying signal box element domain model.</param>
    public SbElementViewModel(SbElement model)
    {
        ArgumentNullException.ThrowIfNull(model);
        _model = model;
    }

    /// <summary>
    /// Gets the underlying domain model.
    /// </summary>
    public SbElement Model => _model;

    /// <summary>
    /// Gets the unique identifier.
    /// </summary>
    public Guid Id => _model.Id;

    /// <summary>
    /// Gets or sets the X grid position.
    /// </summary>
    public int X
    {
        get => _model.X;
        set => SetProperty(_model.X, value, _model, (m, v) => m.X = v);
    }

    /// <summary>
    /// Gets or sets the Y grid position.
    /// </summary>
    public int Y
    {
        get => _model.Y;
        set => SetProperty(_model.Y, value, _model, (m, v) => m.Y = v);
    }

    /// <summary>
    /// Gets or sets the rotation in degrees (0, 90, 180, 270).
    /// </summary>
    public int Rotation
    {
        get => _model.Rotation;
        set => SetProperty(_model.Rotation, value, _model, (m, v) => m.Rotation = v);
    }

    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string Name
    {
        get => _model.Name;
        set => SetProperty(_model.Name, value, _model, (m, v) => m.Name = v);
    }

    /// <summary>
    /// Gets or sets the current state.
    /// </summary>
    public SignalBoxElementState State
    {
        get => _model.State;
        set => SetProperty(_model.State, value, _model, (m, v) => m.State = v);
    }
}

/// <summary>
/// ViewModel for straight track elements.
/// </summary>
public sealed class SbTrackStraightViewModel : SbElementViewModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SbTrackStraightViewModel"/> class.
    /// </summary>
    /// <param name="model">The underlying straight track domain model.</param>
    public SbTrackStraightViewModel(SbTrackStraight model) : base(model) { }

    /// <summary>
    /// Gets the typed model.
    /// </summary>
    public new SbTrackStraight Model => (SbTrackStraight)base.Model;
}

/// <summary>
/// ViewModel for curved track elements.
/// </summary>
public sealed class SbTrackCurveViewModel : SbElementViewModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SbTrackCurveViewModel"/> class.
    /// </summary>
    /// <param name="model">The underlying curved track domain model.</param>
    public SbTrackCurveViewModel(SbTrackCurve model) : base(model) { }

    /// <summary>
    /// Gets the typed model.
    /// </summary>
    public new SbTrackCurve Model => (SbTrackCurve)base.Model;
}

/// <summary>
/// ViewModel for switch elements.
/// Manages DCC address and position state.
/// </summary>
public sealed class SbSwitchViewModel : SbElementViewModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SbSwitchViewModel"/> class.
    /// </summary>
    /// <param name="model">The underlying switch domain model.</param>
    public SbSwitchViewModel(SbSwitch model) : base(model) { }

    /// <summary>
    /// Gets the typed model.
    /// </summary>
    public new SbSwitch Model => (SbSwitch)base.Model;

    /// <summary>
    /// Gets or sets the DCC address (0 = not configured).
    /// </summary>
    public int Address
    {
        get => Model.Address;
        set => SetProperty(Model.Address, value, Model, (m, v) => m.Address = v);
    }

    /// <summary>
    /// Gets or sets the switch position.
    /// </summary>
    public SwitchPosition SwitchPosition
    {
        get => Model.SwitchPosition;
        set => SetProperty(Model.SwitchPosition, value, Model, (m, v) => m.SwitchPosition = v);
    }
}

/// <summary>
/// ViewModel for signal elements.
/// Manages DCC address, system type, and current aspect.
/// </summary>
public sealed class SbSignalViewModel : SbElementViewModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SbSignalViewModel"/> class.
    /// </summary>
    /// <param name="model">The underlying signal domain model.</param>
    public SbSignalViewModel(SbSignal model) : base(model) { }

    /// <summary>
    /// Gets the typed model.
    /// </summary>
    public new SbSignal Model => (SbSignal)base.Model;

    /// <summary>
    /// Gets or sets the DCC address (0 = not configured).
    /// </summary>
    public int Address
    {
        get => Model.Address;
        set => SetProperty(Model.Address, value, Model, (m, v) => m.Address = v);
    }

    /// <summary>
    /// Gets or sets the signal system type.
    /// </summary>
    public SignalSystemType SignalSystem
    {
        get => Model.SignalSystem;
        set => SetProperty(Model.SignalSystem, value, Model, (m, v) => m.SignalSystem = v);
    }

    /// <summary>
    /// Gets or sets the current signal aspect.
    /// </summary>
    public SignalAspect SignalAspect
    {
        get => Model.SignalAspect;
        set => SetProperty(Model.SignalAspect, value, Model, (m, v) => m.SignalAspect = v);
    }
}

/// <summary>
/// ViewModel for detector elements.
/// Manages feedback address for occupancy detection.
/// </summary>
public sealed class SbDetectorViewModel : SbElementViewModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SbDetectorViewModel"/> class.
    /// </summary>
    /// <param name="model">The underlying detector domain model.</param>
    public SbDetectorViewModel(SbDetector model) : base(model) { }

    /// <summary>
    /// Gets the typed model.
    /// </summary>
    public new SbDetector Model => (SbDetector)base.Model;

    /// <summary>
    /// Gets or sets the feedback address (0 = not configured).
    /// </summary>
    public int FeedbackAddress
    {
        get => Model.FeedbackAddress;
        set => SetProperty(Model.FeedbackAddress, value, Model, (m, v) => m.FeedbackAddress = v);
    }
}
