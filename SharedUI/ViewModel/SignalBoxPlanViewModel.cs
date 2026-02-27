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
public sealed partial class SignalBoxPlanViewModel : ObservableObject, IViewModelWrapper<SignalBoxPlan>
{
    private readonly SignalBoxPlan _model;

    /// <summary>
    /// Initializes a new instance of the <see cref="SignalBoxPlanViewModel"/> class.
    /// </summary>
    /// <param name="model">The underlying signal box plan domain model.</param>
    public SignalBoxPlanViewModel(SignalBoxPlan model)
    {
        ArgumentNullException.ThrowIfNull(model);
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
        get => _model.Grid.Width;
        set => _model.Grid = _model.Grid with { Width = value };
    }

    /// <summary>
    /// Gets or sets the grid height in cells.
    /// </summary>
    public int GridHeight
    {
        get => _model.Grid.Height;
        set => _model.Grid = _model.Grid with { Height = value };
    }

    /// <summary>
    /// Gets or sets the cell size in pixels.
    /// </summary>
    public int CellSize
    {
        get => _model.Grid.CellSize;
        set => _model.Grid = _model.Grid with { CellSize = value };
    }

    /// <summary>
    /// Observable collection of elements.
    /// </summary>
    public ObservableCollection<SbElement> Elements { get; } = [];

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
            Elements.Add(element);

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
    private SbElement? _selectedElement;

    /// <summary>
    /// Whether an element is currently selected.
    /// </summary>
    public bool HasSelection => SelectedElement is not null;

    /// <summary>
    /// Adds a straight track element.
    /// </summary>
    public SbTrackStraight AddTrackStraight(int x, int y, int rotation = 0)
    {
        var element = new SbTrackStraight { X = x, Y = y, Rotation = rotation };
        _model.AddElement(element);
        Elements.Add(element);
        OnPropertyChanged(nameof(ElementCount));
        return element;
    }

    /// <summary>
    /// Adds a curved track element.
    /// </summary>
    public SbTrackCurve AddTrackCurve(int x, int y, int rotation = 0)
    {
        var element = new SbTrackCurve { X = x, Y = y, Rotation = rotation };
        _model.AddElement(element);
        Elements.Add(element);
        OnPropertyChanged(nameof(ElementCount));
        return element;
    }

    /// <summary>
    /// Adds a switch element with auto-assigned address.
    /// </summary>
    public SbSwitch AddSwitch(int x, int y, int rotation = 0)
    {
        var element = new SbSwitch { X = x, Y = y, Rotation = rotation, Address = GetNextSwitchAddress() };
        _model.AddElement(element);
        Elements.Add(element);
        OnPropertyChanged(nameof(ElementCount));
        return element;
    }

    /// <summary>
    /// Adds a signal element with auto-assigned address.
    /// </summary>
    public SbSignal AddSignal(int x, int y, int rotation = 0, SignalSystemType system = SignalSystemType.Ks)
    {
        var element = new SbSignal { X = x, Y = y, Rotation = rotation, Address = GetNextSignalAddress(), SignalSystem = system };
        _model.AddElement(element);
        Elements.Add(element);
        OnPropertyChanged(nameof(ElementCount));
        return element;
    }

    /// <summary>
    /// Adds a detector element with auto-assigned feedback address.
    /// </summary>
    public SbDetector AddDetector(int x, int y, int rotation = 0)
    {
        var element = new SbDetector { X = x, Y = y, Rotation = rotation, FeedbackAddress = GetNextFeedbackAddress() };
        _model.AddElement(element);
        Elements.Add(element);
        OnPropertyChanged(nameof(ElementCount));
        return element;
    }

    /// <summary>
    /// Gets the next available address for switches.
    /// </summary>
    public int GetNextSwitchAddress()
    {
        var switches = Elements.OfType<SbSwitch>().ToList();
        return switches.Count > 0 ? switches.Max(e => e.Address) + 1 : 1;
    }

    /// <summary>
    /// Gets the next available address for signals.
    /// </summary>
    public int GetNextSignalAddress()
    {
        var signals = Elements.OfType<SbSignal>().ToList();
        return signals.Count > 0 ? signals.Max(e => e.Address) + 1 : 1;
    }

    /// <summary>
    /// Gets the next available feedback address for detectors.
    /// </summary>
    public int GetNextFeedbackAddress()
    {
        var detectors = Elements.OfType<SbDetector>().ToList();
        return detectors.Count > 0 ? detectors.Max(e => e.FeedbackAddress) + 1 : 1;
    }

    /// <summary>
    /// Finds an element at the specified grid position.
    /// </summary>
    public SbElement? HitTest(int gridX, int gridY)
        => Elements.FirstOrDefault(e => e.X == gridX && e.Y == gridY);

    /// <summary>
    /// Finds an element by its ID.
    /// </summary>
    public SbElement? FindById(Guid id)
        => Elements.FirstOrDefault(e => e.Id == id);

    /// <summary>
    /// Selects an element.
    /// </summary>
    public void Select(SbElement? element)
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
    /// Removes an element from the plan with cascading cleanup of connections and routes.
    /// </summary>
    public void RemoveElement(SbElement element)
    {
        if (SelectedElement?.Id == element.Id)
            SelectedElement = null;

        _model.RemoveElement(element.Id);
        Elements.Remove(element);
        OnPropertyChanged(nameof(ElementCount));

        // Refresh routes since cascading removal may have removed some
        Routes.Clear();
        foreach (var route in _model.Routes)
            Routes.Add(new SignalBoxRouteViewModel(route));
        OnPropertyChanged(nameof(RouteCount));
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
        _model.AddRoute(route);
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
        _model.RemoveRoute(routeVm.Id);
        Routes.Remove(routeVm);
        OnPropertyChanged(nameof(RouteCount));
    }
}

/// <summary>
/// ViewModel wrapper for SignalBoxRoute.
/// </summary>
public sealed class SignalBoxRouteViewModel : ObservableObject, IViewModelWrapper<SignalBoxRoute>
{
    private readonly SignalBoxRoute _model;

    /// <summary>
    /// Initializes a new instance of the <see cref="SignalBoxRouteViewModel"/> class.
    /// </summary>
    /// <param name="model">The underlying route domain model.</param>
    public SignalBoxRouteViewModel(SignalBoxRoute model)
    {
        ArgumentNullException.ThrowIfNull(model);
        _model = model;
    }

    /// <summary>
    /// Gets the underlying route domain model.
    /// </summary>
    public SignalBoxRoute Model => _model;

    /// <summary>
    /// Gets the unique identifier of this route.
    /// </summary>
    public Guid Id => _model.Id;

    /// <summary>
    /// Gets or sets the display name of this route.
    /// </summary>
    public string Name
    {
        get => _model.Name;
        set => SetProperty(_model.Name, value, _model, (m, v) => m.Name = v);
    }

    /// <summary>
    /// Gets or sets the identifier of the start signal for this route.
    /// </summary>
    public Guid StartSignalId
    {
        get => _model.StartSignalId;
        set => SetProperty(_model.StartSignalId, value, _model, (m, v) => m.StartSignalId = v);
    }

    /// <summary>
    /// Gets or sets the identifier of the end signal for this route.
    /// </summary>
    public Guid EndSignalId
    {
        get => _model.EndSignalId;
        set => SetProperty(_model.EndSignalId, value, _model, (m, v) => m.EndSignalId = v);
    }

    /// <summary>
    /// Gets the ordered list of element identifiers that belong to this route.
    /// </summary>
    public List<Guid> ElementIds => _model.ElementIds;
}
