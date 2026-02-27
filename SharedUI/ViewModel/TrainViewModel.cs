// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain;
using Domain.Enum;
using Interface;
using System.Collections.ObjectModel;

/// <summary>
/// ViewModel wrapper for Train model with CRUD operations for Locomotives and Wagons.
/// Uses Project for resolving locomotive/wagon references via GUID lookups.
/// </summary>
public sealed partial class TrainViewModel : ObservableObject, IViewModelWrapper<Train>
{
    #region Fields
    // Model
    private readonly Train _model;
    
    // Context
    private readonly Project _project;
    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="TrainViewModel"/> class.
    /// </summary>
    /// <param name="model">The train domain model.</param>
    /// <param name="project">The parent project that owns the locomotives and wagons.</param>
    public TrainViewModel(Train model, Project project)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(project);
        _model = model;
        _project = project;
    }

    /// <summary>
    /// Gets the underlying domain model (for IViewModelWrapper interface).
    /// </summary>
    public Train Model => _model;

    /// <summary>
    /// Gets the unique identifier of the train.
    /// </summary>
    public Guid Id => _model.Id;

    /// <summary>
    /// Gets or sets a value indicating whether this train uses double traction (two locomotives).
    /// </summary>
    public bool IsDoubleTraction
    {
        get => _model.IsDoubleTraction;
        set => SetProperty(_model.IsDoubleTraction, value, _model, (m, v) => m.IsDoubleTraction = v);
    }

    /// <summary>
    /// Gets or sets the display name of the train.
    /// </summary>
    public string Name
    {
        get => _model.Name;
        set => SetProperty(_model.Name, value, _model, (m, v) => m.Name = v);
    }

    /// <summary>
    /// Gets or sets an optional description for the train.
    /// </summary>
    public string Description
    {
        get => _model.Description;
        set => SetProperty(_model.Description, value, _model, (m, v) => m.Description = v);
    }

    /// <summary>
    /// Gets or sets the train type (for example passenger or freight).
    /// </summary>
    public TrainType TrainType
    {
        get => _model.TrainType;
        set => SetProperty(_model.TrainType, value, _model, (m, v) => m.TrainType = v);
    }

    /// <summary>
    /// Gets or sets the service type (for example regional, intercity, freight).
    /// </summary>
    public ServiceType ServiceType
    {
        get => _model.ServiceType;
        set => SetProperty(_model.ServiceType, value, _model, (m, v) => m.ServiceType = v);
    }

    /// <summary>
    /// Gets all available TrainType enum values for ComboBox binding.
    /// </summary>
    public IEnumerable<TrainType> TrainTypeValues => Enum.GetValues<TrainType>();

    /// <summary>
    /// Gets all available ServiceType enum values for ComboBox binding.
    /// </summary>
    public IEnumerable<ServiceType> ServiceTypeValues => Enum.GetValues<ServiceType>();

    /// <summary>
    /// Gets or sets the list of locomotive IDs (direct Domain property access).
    /// </summary>
    public List<Guid> LocomotiveIds
    {
        get => _model.LocomotiveIds;
        set => SetProperty(_model.LocomotiveIds, value, _model, (m, v) => m.LocomotiveIds = v);
    }

    /// <summary>
    /// Gets or sets the list of wagon IDs (direct Domain property access).
    /// </summary>
    public List<Guid> WagonIds
    {
        get => _model.WagonIds;
        set => SetProperty(_model.WagonIds, value, _model, (m, v) => m.WagonIds = v);
    }

    /// <summary>
    /// Locomotives resolved from Project.Locomotives using LocomotiveIds.
    /// </summary>
    public ObservableCollection<LocomotiveViewModel> Locomotives =>
        new(
            _model.LocomotiveIds
                .Select(id => _project.Locomotives.FirstOrDefault(l => l.Id == id))
                .Where(l => l != null)
                .Select(l => new LocomotiveViewModel(l!))
        );

    /// <summary>
    /// Wagons resolved from Project wagons (PassengerWagons + GoodsWagons) using WagonIds.
    /// </summary>
    public ObservableCollection<WagonViewModel> Wagons =>
        new(
            _model.WagonIds
                .Select(id => {
                    // Try PassengerWagons first
                    Wagon? wagon = (Wagon?)_project.PassengerWagons.FirstOrDefault(w => w.Id == id) ?? _project.GoodsWagons.FirstOrDefault(w => w.Id == id);
                    return wagon;
                })
                .Where(w => w != null)
                .Select(w => new WagonViewModel(w!))
        );

    /// <summary>
    /// Gets the primary (first) locomotive for this train, or null if no locomotives.
    /// </summary>
    public LocomotiveViewModel? Locomotive => Locomotives.FirstOrDefault();

    [RelayCommand]
    private void AddLocomotive()
    {
        var newLocomotive = new Locomotive { Name = "New Locomotive" };
        
        // Add to Project master list
        _project.Locomotives.Add(newLocomotive);
        
        // Add ID to Train
        _model.LocomotiveIds.Add(newLocomotive.Id);
        
        // Notify UI to refresh collection
        OnPropertyChanged(nameof(Locomotives));
    }

    [RelayCommand]
    private void DeleteLocomotive(LocomotiveViewModel locomotiveVm)
    {
        // Remove ID from Train
        _model.LocomotiveIds.Remove(locomotiveVm.Model.Id);
        
        // Note: We don't remove from Project.Locomotives (might be used elsewhere)
        
        // Notify UI to refresh collection
        OnPropertyChanged(nameof(Locomotives));
    }

    [RelayCommand]
    private void AddWagon()
    {
        var newWagon = new PassengerWagon { Name = "New Wagon" };
        
        // Add to Project master list
        _project.PassengerWagons.Add(newWagon);
        
        // Add ID to Train
        _model.WagonIds.Add(newWagon.Id);
        
        // Notify UI to refresh collection
        OnPropertyChanged(nameof(Wagons));
    }

    [RelayCommand]
    private void DeleteWagon(WagonViewModel wagonVm)
    {
        // Remove ID from Train
        _model.WagonIds.Remove(wagonVm.Model.Id);
        
        // Note: We don't remove from Project wagons (might be used elsewhere)
        
        // Notify UI to refresh collection
        OnPropertyChanged(nameof(Wagons));
    }

    /// <summary>
    /// Renumbers all locomotives in the train composition.
    /// </summary>
    private void RenumberLocomotives()
    {
        var locomotives = Locomotives.ToList();
        for (int i = 0; i < locomotives.Count; i++)
        {
            locomotives[i].Pos = (uint)(i + 1);
        }
    }

    /// <summary>
    /// Renumbers all wagons in the train composition.
    /// </summary>
    private void RenumberWagons()
    {
        var wagons = Wagons.ToList();
        for (int i = 0; i < wagons.Count; i++)
        {
            wagons[i].Pos = (uint)(i + 1);
        }
    }

    /// <summary>
    /// Handles locomotive reordering after drag and drop.
    /// Updates Model.LocomotiveIds to match new UI order.
    /// </summary>
    [RelayCommand]
    public void LocomotivesReordered()
    {
        // Get current UI order
        var currentLocomotives = Locomotives.ToList();
        
        // Update Model.LocomotiveIds to match ViewModel order
        Model.LocomotiveIds.Clear();
        foreach (var locomotiveVm in currentLocomotives)
        {
            Model.LocomotiveIds.Add(locomotiveVm.Model.Id);
        }

        // Renumber based on new order
        RenumberLocomotives();
        
        // Notify UI
        OnPropertyChanged(nameof(Locomotives));
    }

    /// <summary>
    /// Handles wagon reordering after drag and drop.
    /// Updates Model.WagonIds to match new UI order.
    /// </summary>
    [RelayCommand]
    public void WagonsReordered()
    {
        // Get current UI order
        var currentWagons = Wagons.ToList();
        
        // Update Model.WagonIds to match ViewModel order
        Model.WagonIds.Clear();
        foreach (var wagonVm in currentWagons)
        {
            Model.WagonIds.Add(wagonVm.Model.Id);
        }

        // Renumber based on new order
        RenumberWagons();
        
        // Notify UI
        OnPropertyChanged(nameof(Wagons));
    }

    /// <summary>
    /// Refreshes the Locomotives and Wagons collections.
    /// Call this after external changes to Project master lists.
    /// </summary>
    public void RefreshCollections()
    {
        OnPropertyChanged(nameof(Locomotives));
        OnPropertyChanged(nameof(Wagons));
        OnPropertyChanged(nameof(Locomotive));
    }
}
