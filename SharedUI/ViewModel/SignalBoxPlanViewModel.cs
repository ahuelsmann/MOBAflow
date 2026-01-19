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
    /// Observable collection of element ViewModels.
    /// </summary>
    public ObservableCollection<SignalBoxPlanElementViewModel> Elements { get; } = [];

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
            Elements.Add(new SignalBoxPlanElementViewModel(element));

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
                private SignalBoxPlanElementViewModel? _selectedElement;

                /// <summary>
                /// Whether an element is currently selected.
                /// </summary>
                public bool HasSelection => SelectedElement is not null;

                /// <summary>
                /// Adds a new element to the plan.
                /// </summary>
                public SignalBoxPlanElementViewModel AddElement(SignalBoxSymbol symbol, int x, int y, int rotation = 0)
                {
                    var element = new SignalBoxPlanElement
                    {
                        Symbol = symbol,
                        X = x,
                        Y = y,
                        Rotation = rotation,
                        Address = GetNextAddress(symbol)
                    };
                    _model.Elements.Add(element);
                    var vm = new SignalBoxPlanElementViewModel(element);
                    Elements.Add(vm);
                    OnPropertyChanged(nameof(ElementCount));
                    return vm;
                }

                /// <summary>
                /// Gets the next available address for a given symbol type.
                /// </summary>
                public int GetNextAddress(SignalBoxSymbol symbol)
                {
                    var isSignal = IsSignalSymbol(symbol);
                    var isSwitch = IsSwitchSymbol(symbol);

                    var relevantElements = Elements.Where(e =>
                        (isSignal && IsSignalSymbol(e.Symbol)) ||
                        (isSwitch && IsSwitchSymbol(e.Symbol)));

                    return relevantElements.Any() ? relevantElements.Max(e => e.Address) + 1 : 1;
                }

                /// <summary>
                /// Finds an element at the specified grid position.
                /// </summary>
                public SignalBoxPlanElementViewModel? HitTest(int gridX, int gridY)
                    => Elements.FirstOrDefault(e => e.X == gridX && e.Y == gridY);

                /// <summary>
                /// Finds an element by its ID.
                /// </summary>
                public SignalBoxPlanElementViewModel? FindById(Guid id)
                    => Elements.FirstOrDefault(e => e.Id == id);

                /// <summary>
                /// Selects an element.
                /// </summary>
                public void Select(SignalBoxPlanElementViewModel? element)
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
                public void RemoveElement(SignalBoxPlanElementViewModel elementVm)
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
                /// Checks if a symbol represents a signal.
                /// </summary>
                public static bool IsSignalSymbol(SignalBoxSymbol symbol) => symbol switch
                {
                    // Ks System
                    SignalBoxSymbol.SignalKsMain or
                    SignalBoxSymbol.SignalKsDistant or
                    SignalBoxSymbol.SignalKsCombined or
                    // H/V System
                    SignalBoxSymbol.SignalHvMain or
                    SignalBoxSymbol.SignalHvDistant or
                    SignalBoxSymbol.SignalHvCombined or
                    // Hl System (DDR)
                    SignalBoxSymbol.SignalHlMain or
                    SignalBoxSymbol.SignalHlDistant or
                    // Semaphore (Formsignale)
                    SignalBoxSymbol.SignalFormMain or
                    SignalBoxSymbol.SignalFormDistant or
                    // Shunting signals
                    SignalBoxSymbol.SignalSh or
                    SignalBoxSymbol.SignalDwarf or
                    // Protection signals
                    SignalBoxSymbol.SignalBlock or
                    SignalBoxSymbol.SignalEntry or
                    SignalBoxSymbol.SignalExit => true,
                    _ => false
                };

                /// <summary>
                /// Checks if a symbol represents a switch.
                /// </summary>
                public static bool IsSwitchSymbol(SignalBoxSymbol symbol) => symbol switch
                {
                    SignalBoxSymbol.SwitchSimpleLeft or
                    SignalBoxSymbol.SwitchSimpleRight or
                    SignalBoxSymbol.SwitchCurvedLeft or
                    SignalBoxSymbol.SwitchCurvedRight or
                    SignalBoxSymbol.SwitchThreeWay or
                    SignalBoxSymbol.SwitchDoubleSlip or
                    SignalBoxSymbol.SwitchSingleSlip or
                    SignalBoxSymbol.SwitchDiamond or
                    SignalBoxSymbol.SwitchScissor => true,
                    _ => false
                };

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

/// <summary>
/// ViewModel wrapper for SignalBoxPlanElement.
/// </summary>
public partial class SignalBoxPlanElementViewModel : ObservableObject, IViewModelWrapper<SignalBoxPlanElement>
{
    private readonly SignalBoxPlanElement _model;

    public SignalBoxPlanElementViewModel(SignalBoxPlanElement model)
    {
        _model = model;
    }

    public SignalBoxPlanElement Model => _model;

    public Guid Id => _model.Id;

    public SignalBoxSymbol Symbol
    {
        get => _model.Symbol;
        set => SetProperty(_model.Symbol, value, _model, (m, v) => m.Symbol = v);
    }

    public int X
    {
        get => _model.X;
        set => SetProperty(_model.X, value, _model, (m, v) => m.X = v);
    }

    public int Y
    {
        get => _model.Y;
        set => SetProperty(_model.Y, value, _model, (m, v) => m.Y = v);
    }

    public int Rotation
    {
        get => _model.Rotation;
        set => SetProperty(_model.Rotation, value, _model, (m, v) => m.Rotation = v);
    }

    public string Name
    {
        get => _model.Name;
        set => SetProperty(_model.Name, value, _model, (m, v) => m.Name = v);
    }

    public int Address
    {
        get => _model.Address;
        set => SetProperty(_model.Address, value, _model, (m, v) => m.Address = v);
    }

    public int FeedbackAddress
    {
        get => _model.FeedbackAddress;
        set => SetProperty(_model.FeedbackAddress, value, _model, (m, v) => m.FeedbackAddress = v);
    }

        public SignalSystemType SignalSystem
        {
            get => _model.SignalSystem;
            set => SetProperty(_model.SignalSystem, value, _model, (m, v) => m.SignalSystem = v);
        }

        public SignalAspect SignalAspect
        {
            get => _model.SignalAspect;
            set => SetProperty(_model.SignalAspect, value, _model, (m, v) => m.SignalAspect = v);
        }

        public SwitchPosition SwitchPosition
        {
            get => _model.SwitchPosition;
            set => SetProperty(_model.SwitchPosition, value, _model, (m, v) => m.SwitchPosition = v);
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
