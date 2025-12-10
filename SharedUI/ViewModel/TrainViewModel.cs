// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Domain;

using Moba.Domain.Enum;
using Interface;

using System.Collections.ObjectModel;

/// <summary>
/// ViewModel wrapper for Train model with CRUD operations for Locomotives and Wagons.
/// Uses Project for resolving locomotive/wagon references via GUID lookups.
/// </summary>
public partial class TrainViewModel : ObservableObject, IViewModelWrapper<Train>
{
    [ObservableProperty]
    private Train model;

    private readonly Project _project;

    public TrainViewModel(Train model, Project project)
    {
        Model = model;
        _project = project;
    }

    /// <summary>
    /// Gets the unique identifier of the train.
    /// </summary>
    public Guid Id => Model.Id;

    public bool IsDoubleTraction
    {
        get => Model.IsDoubleTraction;
        set => SetProperty(Model.IsDoubleTraction, value, Model, (m, v) => m.IsDoubleTraction = v);
    }

    public string Name
    {
        get => Model.Name;
        set => SetProperty(Model.Name, value, Model, (m, v) => m.Name = v);
    }

    public string Description
    {
        get => Model.Description ?? string.Empty;
        set => SetProperty(Model.Description, value, Model, (m, v) => m.Description = v);
    }

    public TrainType TrainType
    {
        get => Model.TrainType;
        set => SetProperty(Model.TrainType, value, Model, (m, v) => m.TrainType = v);
    }

    public ServiceType ServiceType
    {
        get => Model.ServiceType;
        set => SetProperty(Model.ServiceType, value, Model, (m, v) => m.ServiceType = v);
    }

    /// <summary>
    /// Gets all available TrainType enum values for ComboBox binding.
    /// </summary>
    public IEnumerable<TrainType> TrainTypeValues => System.Enum.GetValues<TrainType>();

    /// <summary>
    /// Gets all available ServiceType enum values for ComboBox binding.
    /// </summary>
    public IEnumerable<ServiceType> ServiceTypeValues => System.Enum.GetValues<ServiceType>();

    /// <summary>
    /// Gets or sets the list of locomotive IDs (direct Domain property access).
    /// </summary>
    public List<Guid> LocomotiveIds
    {
        get => Model.LocomotiveIds;
        set => SetProperty(Model.LocomotiveIds, value, Model, (m, v) => m.LocomotiveIds = v);
    }

    /// <summary>
    /// Gets or sets the list of wagon IDs (direct Domain property access).
    /// </summary>
    public List<Guid> WagonIds
    {
        get => Model.WagonIds;
        set => SetProperty(Model.WagonIds, value, Model, (m, v) => m.WagonIds = v);
    }

    /// <summary>
    /// Locomotives resolved from Project.Locomotives using LocomotiveIds.
    /// </summary>
    public ObservableCollection<LocomotiveViewModel> Locomotives =>
        new ObservableCollection<LocomotiveViewModel>(
            Model.LocomotiveIds
                .Select(id => _project.Locomotives.FirstOrDefault(l => l.Id == id))
                .Where(l => l != null)
                .Select(l => new LocomotiveViewModel(l!))
        );

    /// <summary>
    /// Wagons resolved from Project wagons (PassengerWagons + GoodsWagons) using WagonIds.
    /// </summary>
    public ObservableCollection<WagonViewModel> Wagons =>
        new ObservableCollection<WagonViewModel>(
            Model.WagonIds
                .Select(id => {
                    // Try PassengerWagons first
                    Wagon? wagon = _project.PassengerWagons.FirstOrDefault(w => w.Id == id);
                    // Then try GoodsWagons
                    if (wagon == null)
                        wagon = _project.GoodsWagons.FirstOrDefault(w => w.Id == id);
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
        Model.LocomotiveIds.Add(newLocomotive.Id);
        
        // Notify UI to refresh collection
        OnPropertyChanged(nameof(Locomotives));
    }

    [RelayCommand]
    private void DeleteLocomotive(LocomotiveViewModel locomotiveVM)
    {
        if (locomotiveVM == null) return;

        // Remove ID from Train
        Model.LocomotiveIds.Remove(locomotiveVM.Model.Id);
        
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
        Model.WagonIds.Add(newWagon.Id);
        
        // Notify UI to refresh collection
        OnPropertyChanged(nameof(Wagons));
    }

    [RelayCommand]
    private void DeleteWagon(WagonViewModel wagonVM)
    {
        if (wagonVM == null) return;

        // Remove ID from Train
        Model.WagonIds.Remove(wagonVM.Model.Id);
        
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
    /// Handles locomotive reordering after drag & drop.
    /// Updates Model.LocomotiveIds to match new UI order.
    /// </summary>
    [RelayCommand]
    public void LocomotivesReordered()
    {
        // Get current UI order
        var currentLocomotives = Locomotives.ToList();
        
        // Update Model.LocomotiveIds to match ViewModel order
        Model.LocomotiveIds.Clear();
        foreach (var locomotiveVM in currentLocomotives)
        {
            Model.LocomotiveIds.Add(locomotiveVM.Model.Id);
        }

        // Renumber based on new order
        RenumberLocomotives();
        
        // Notify UI
        OnPropertyChanged(nameof(Locomotives));
    }

    /// <summary>
    /// Handles wagon reordering after drag & drop.
    /// Updates Model.WagonIds to match new UI order.
    /// </summary>
    [RelayCommand]
    public void WagonsReordered()
    {
        // Get current UI order
        var currentWagons = Wagons.ToList();
        
        // Update Model.WagonIds to match ViewModel order
        Model.WagonIds.Clear();
        foreach (var wagonVM in currentWagons)
        {
            Model.WagonIds.Add(wagonVM.Model.Id);
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