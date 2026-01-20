// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using Domain;
using Interface;
using System.Collections.ObjectModel;

/// <summary>
/// ViewModel wrapper for SignalBoxPlan domain model.
/// Provides observable collections for elements, connections, and routes.
/// </summary>
public partial class SignalBoxPlanViewModel : ObservableObject, IViewModelWrapper<SignalBoxPlan>
{
    private readonly SignalBoxPlan _model;

    public SignalBoxPlanViewModel(SignalBoxPlan model)
    {
        _model = model;
        Refresh();
    }

    /// <summary>
    /// Gets the underlying domain model (for IViewModelWrapper interface).
    /// </summary>
    public SignalBoxPlan Model => _model;

    /// <summary>
    /// Gets the unique identifier of the plan.
    /// </summary>
    public Guid Id => _model.Id;

    /// <summary>
    /// Gets or sets the name of the signal box plan.
    /// </summary>
    public string Name
    {
        get => _model.Name;
        set => SetProperty(_model.Name, value, _model, (m, v) => m.Name = v);
    }

    /// <summary>
    /// Gets or sets the grid width in cells.
    /// </summary>
    public int GridWidth
    {
        get => _model.GridWidth;
        set => SetProperty(_model.GridWidth, value, _model, (m, v) => m.GridWidth = v);
    }

    /// <summary>
    /// Gets or sets the grid height in cells.
    /// </summary>
    public int GridHeight
    {
        get => _model.GridHeight;
        set => SetProperty(_model.GridHeight, value, _model, (m, v) => m.GridHeight = v);
    }

    /// <summary>
    /// Gets or sets the cell size in pixels.
    /// </summary>
    public int CellSize
    {
        get => _model.CellSize;
        set => SetProperty(_model.CellSize, value, _model, (m, v) => m.CellSize = v);
    }

    /// <summary>
    /// Observable collection of element ViewModels (typed hierarchy).
    /// </summary>
    public ObservableCollection<SbElementViewModel> Elements { get; } = [];

    /// <summary>
    /// Observable collection of route ViewModels.
    /// </summary>
    public ObservableCollection<SignalBoxRouteViewModel> Routes { get; } = [];

    /// <summary>
    /// Gets the count of elements.
    /// </summary>
    public int ElementCount => Elements.Count;

    /// <summary>
    /// Gets the count of routes.
    /// </summary>
    public int RouteCount => Routes.Count;

    /// <summary>
    /// Refreshes all collections from the underlying model.
    /// </summary>
    public void Refresh()
    {
        Elements.Clear();
        foreach (var element in _model.Elements)
            Elements.Add(SbElementViewModel.Create(element));

        Routes.Clear();
        foreach (var route in _model.Routes)
            Routes.Add(new SignalBoxRouteViewModel(route));

        OnPropertyChanged(nameof(ElementCount));
        OnPropertyChanged(nameof(RouteCount));
    }

    /// <summary>
    /// Currently selected element in the editor.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelection))]
    private SbElementViewModel? _selectedElement;

    /// <summary>
    /// Whether an element is currently selected.
    /// </summary>
    public bool HasSelection => SelectedElement is not null;

    /// <summary>
    /// Adds a straight track element.
    /// </summary>
    public SbTrackStraightViewModel AddTrackStraight(int x, int y, int rotation = 0)
    {
        var element = new SbTrackStraight { X = x, Y = y, Rotation = rotation };
        _model.Elements.Add(element);
        var vm = new SbTrackStraightViewModel(element);
        Elements.Add(vm);
        OnPropertyChanged(nameof(ElementCount));
        return vm;
    }

    /// <summary>
    /// Adds a curved track element.
    /// </summary>
    public SbTrackCurveViewModel AddTrackCurve(int x, int y, int rotation = 0)
    {
        var element = new SbTrackCurve { X = x, Y = y, Rotation = rotation };
        _model.Elements.Add(element);
        var vm = new SbTrackCurveViewModel(element);
        Elements.Add(vm);
        OnPropertyChanged(nameof(ElementCount));
        return vm;
    }

    /// <summary>
    /// Adds a switch element with auto-assigned address.
    /// </summary>
    public SbSwitchViewModel AddSwitch(int x, int y, int rotation = 0)
    {
        var element = new SbSwitch { X = x, Y = y, Rotation = rotation, Address = GetNextSwitchAddress() };
        _model.Elements.Add(element);
        var vm = new SbSwitchViewModel(element);
        Elements.Add(vm);
        OnPropertyChanged(nameof(ElementCount));
        return vm;
    }

    /// <summary>
    /// Adds a signal element with auto-assigned address.
    /// </summary>
    public SbSignalViewModel AddSignal(int x, int y, int rotation = 0, SignalSystemType system = SignalSystemType.Ks)
    {
        var element = new SbSignal { X = x, Y = y, Rotation = rotation, Address = GetNextSignalAddress(), SignalSystem = system };
        _model.Elements.Add(element);
        var vm = new SbSignalViewModel(element);
        Elements.Add(vm);
        OnPropertyChanged(nameof(ElementCount));
        return vm;
    }

    /// <summary>
    /// Adds a detector element with auto-assigned feedback address.
    /// </summary>
    public SbDetectorViewModel AddDetector(int x, int y, int rotation = 0)
    {
        var element = new SbDetector { X = x, Y = y, Rotation = rotation, FeedbackAddress = GetNextFeedbackAddress() };
        _model.Elements.Add(element);
        var vm = new SbDetectorViewModel(element);
        Elements.Add(vm);
        OnPropertyChanged(nameof(ElementCount));
        return vm;
    }

    /// <summary>
    /// Gets the next available address for switches.
    /// </summary>
    public int GetNextSwitchAddress()
    {
        var switches = Elements.OfType<SbSwitchViewModel>().ToList();
        return switches.Count > 0 ? switches.Max(e => e.Address) + 1 : 1;
    }

    /// <summary>
    /// Gets the next available address for signals.
    /// </summary>
    public int GetNextSignalAddress()
    {
        var signals = Elements.OfType<SbSignalViewModel>().ToList();
        return signals.Count > 0 ? signals.Max(e => e.Address) + 1 : 1;
    }

    /// <summary>
    /// Gets the next available feedback address for detectors.
    /// </summary>
    public int GetNextFeedbackAddress()
    {
        var detectors = Elements.OfType<SbDetectorViewModel>().ToList();
        return detectors.Count > 0 ? detectors.Max(e => e.FeedbackAddress) + 1 : 1;
    }

    /// <summary>
    /// Finds an element at the specified grid position.
    /// </summary>
    public SbElementViewModel? HitTest(int gridX, int gridY)
        => Elements.FirstOrDefault(e => e.X == gridX && e.Y == gridY);

    /// <summary>
    /// Finds an element by its ID.
    /// </summary>
    public SbElementViewModel? FindById(Guid id)
        => Elements.FirstOrDefault(e => e.Id == id);

    /// <summary>
    /// Selects an element.
    /// </summary>
    public void Select(SbElementViewModel? element)
    {
        SelectedElement = element;
    }

    /// <summary>
    /// Clears the current selection.
    /// </summary>
    public void ClearSelection()
    {
        SelectedElement = null;
    }

    /// <summary>
    /// Removes an element from the plan.
    /// </summary>
    public void RemoveElement(SbElementViewModel elementVm)
    {
        if (SelectedElement?.Id == elementVm.Id)
            SelectedElement = null;

        _model.Elements.Remove(elementVm.Model);
        Elements.Remove(elementVm);
        OnPropertyChanged(nameof(ElementCount));
    }

    /// <summary>
    /// Removes the currently selected element.
    /// </summary>
    public void RemoveSelectedElement()
    {
        if (SelectedElement is not null)
            RemoveElement(SelectedElement);
    }

    /// <summary>
    /// Adds a new route to the plan.
    /// </summary>
    public SignalBoxRouteViewModel AddRoute(string name, Guid startSignalId, Guid endSignalId)
    {
        var route = new SignalBoxRoute
        {
            Name = name,
            StartSignalId = startSignalId,
            EndSignalId = endSignalId
        };
        _model.Routes.Add(route);
        var vm = new SignalBoxRouteViewModel(route);
        Routes.Add(vm);
        OnPropertyChanged(nameof(RouteCount));
        return vm;
    }

    /// <summary>
    /// Removes a route from the plan.
    /// </summary>
    public void RemoveRoute(SignalBoxRouteViewModel routeVm)
    {
        _model.Routes.Remove(routeVm.Model);
        Routes.Remove(routeVm);
        OnPropertyChanged(nameof(RouteCount));
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// ELEMENT VIEWMODELS - Typed Hierarchy
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// Abstract base ViewModel for all signal box elements.
/// </summary>
public abstract partial class SbElementViewModel : ObservableObject
{
    /// <summary>
    /// Gets the underlying domain model.
    /// </summary>
    public abstract SbElement Model { get; }

    /// <summary>Unique identifier.</summary>
    public Guid Id => Model.Id;

    /// <summary>Grid position X (column).</summary>
    public int X
    {
        get => Model.X;
        set => SetProperty(Model.X, value, Model, (m, v) => m.X = v);
    }

    /// <summary>Grid position Y (row).</summary>
    public int Y
    {
        get => Model.Y;
        set => SetProperty(Model.Y, value, Model, (m, v) => m.Y = v);
    }

    /// <summary>Rotation in degrees.</summary>
    public int Rotation
    {
        get => Model.Rotation;
        set => SetProperty(Model.Rotation, value, Model, (m, v) => m.Rotation = v);
    }

    /// <summary>Display name.</summary>
    public string Name
    {
        get => Model.Name;
        set => SetProperty(Model.Name, value, Model, (m, v) => m.Name = v);
    }

    /// <summary>
    /// Factory method to create the appropriate ViewModel for a domain element.
    /// </summary>
    public static SbElementViewModel Create(SbElement element) => element switch
    {
        SbTrackStraight track => new SbTrackStraightViewModel(track),
        SbTrackCurve curve => new SbTrackCurveViewModel(curve),
        SbSwitch sw => new SbSwitchViewModel(sw),
        SbSignal sig => new SbSignalViewModel(sig),
        SbDetector det => new SbDetectorViewModel(det),
        _ => throw new ArgumentException($"Unknown element type: {element.GetType().Name}", nameof(element))
    };
}

/// <summary>
/// ViewModel for straight track elements.
/// </summary>
public sealed partial class SbTrackStraightViewModel : SbElementViewModel
{
    private readonly SbTrackStraight _model;
    public SbTrackStraightViewModel(SbTrackStraight model) => _model = model;
    public override SbElement Model => _model;
}

/// <summary>
/// ViewModel for curved track elements.
/// </summary>
public sealed partial class SbTrackCurveViewModel : SbElementViewModel
{
    private readonly SbTrackCurve _model;
    public SbTrackCurveViewModel(SbTrackCurve model) => _model = model;
    public override SbElement Model => _model;
}

/// <summary>
/// ViewModel for switch elements.
/// </summary>
public sealed partial class SbSwitchViewModel : SbElementViewModel
{
    private readonly SbSwitch _model;
    public SbSwitchViewModel(SbSwitch model) => _model = model;
    public override SbElement Model => _model;

    /// <summary>DCC address for Z21 control.</summary>
    public int Address
    {
        get => _model.Address;
        set => SetProperty(_model.Address, value, _model, (m, v) => m.Address = v);
    }

    /// <summary>Current switch position.</summary>
    public SwitchPosition SwitchPosition
    {
        get => _model.SwitchPosition;
        set => SetProperty(_model.SwitchPosition, value, _model, (m, v) => m.SwitchPosition = v);
    }
}

/// <summary>
/// ViewModel for signal elements.
/// </summary>
public sealed partial class SbSignalViewModel : SbElementViewModel
{
    private readonly SbSignal _model;
    public SbSignalViewModel(SbSignal model) => _model = model;
    public override SbElement Model => _model;

    /// <summary>DCC address for Z21 control.</summary>
    public int Address
    {
        get => _model.Address;
        set => SetProperty(_model.Address, value, _model, (m, v) => m.Address = v);
    }

    /// <summary>Signal system type.</summary>
    public SignalSystemType SignalSystem
    {
        get => _model.SignalSystem;
        set => SetProperty(_model.SignalSystem, value, _model, (m, v) => m.SignalSystem = v);
    }

    /// <summary>Current signal aspect.</summary>
    public SignalAspect SignalAspect
    {
        get => _model.SignalAspect;
        set => SetProperty(_model.SignalAspect, value, _model, (m, v) => m.SignalAspect = v);
    }
}

/// <summary>
/// ViewModel for detector elements.
/// </summary>
public sealed partial class SbDetectorViewModel : SbElementViewModel
{
    private readonly SbDetector _model;
    public SbDetectorViewModel(SbDetector model) => _model = model;
    public override SbElement Model => _model;

    /// <summary>Feedback address for occupancy detection.</summary>
    public int FeedbackAddress
    {
        get => _model.FeedbackAddress;
        set => SetProperty(_model.FeedbackAddress, value, _model, (m, v) => m.FeedbackAddress = v);
    }
}

/// <summary>
/// ViewModel wrapper for SignalBoxRoute.
/// </summary>
public partial class SignalBoxRouteViewModel : ObservableObject, IViewModelWrapper<SignalBoxRoute>
{
    private readonly SignalBoxRoute _model;

    public SignalBoxRouteViewModel(SignalBoxRoute model)
    {
        _model = model;
    }

    public SignalBoxRoute Model => _model;

    public Guid Id => _model.Id;

    public string Name
    {
        get => _model.Name;
        set => SetProperty(_model.Name, value, _model, (m, v) => m.Name = v);
    }

    public Guid StartSignalId
    {
        get => _model.StartSignalId;
        set => SetProperty(_model.StartSignalId, value, _model, (m, v) => m.StartSignalId = v);
    }

    public Guid EndSignalId
    {
        get => _model.EndSignalId;
        set => SetProperty(_model.EndSignalId, value, _model, (m, v) => m.EndSignalId = v);
    }

    public List<Guid> ElementIds => _model.ElementIds;
}
