// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Moba.Domain;
using Moba.Domain.Enum;
using Moba.SharedUI.Enum;
using Moba.SharedUI.Interface;

using System.Collections.ObjectModel;

/// <summary>
/// ViewModel wrapper for Train model with CRUD operations for Locomotives and Wagons.
/// </summary>
public partial class TrainViewModel : ObservableObject, IViewModelWrapper<Train>
{
    [ObservableProperty]
    private Train model;

    public MobaType EntityType => MobaType.Train;

    private readonly IUiDispatcher? _dispatcher;

    public TrainViewModel(Train model, IUiDispatcher? dispatcher = null)
    {
        Model = model;
        _dispatcher = dispatcher;

        // Wrap existing locomotives and wagons in ViewModels
        Locomotives = new ObservableCollection<LocomotiveViewModel>(
            model.Locomotives.Select(l => new LocomotiveViewModel(l, dispatcher))
        );

        Wagons = new ObservableCollection<WagonViewModel>(
            model.Wagons.Select(w => new WagonViewModel(w, dispatcher))
        );
    }

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

    public ObservableCollection<LocomotiveViewModel> Locomotives { get; }

    public ObservableCollection<WagonViewModel> Wagons { get; }

    /// <summary>
    /// Gets the primary (first) locomotive for this train, or null if no locomotives.
    /// </summary>
    public LocomotiveViewModel? Locomotive => Locomotives.FirstOrDefault();

    [RelayCommand]
    private void AddLocomotive()
    {
        var newLocomotive = new Locomotive { Name = "New Locomotive" };
        Model.Locomotives.Add(newLocomotive);

        var locomotiveVM = new LocomotiveViewModel(newLocomotive, _dispatcher);
        Locomotives.Add(locomotiveVM);
    }

    [RelayCommand]
    private void DeleteLocomotive(LocomotiveViewModel locomotiveVM)
    {
        if (locomotiveVM == null) return;

        Model.Locomotives.Remove(locomotiveVM.Model);
        Locomotives.Remove(locomotiveVM);
    }

    [RelayCommand]
    private void AddWagon()
    {
        var newWagon = new Wagon { Name = "New Wagon" };
        Model.Wagons.Add(newWagon);

        var wagonVM = new WagonViewModel(newWagon, _dispatcher);
        Wagons.Add(wagonVM);
    }

    [RelayCommand]
    private void DeleteWagon(WagonViewModel wagonVM)
    {
        if (wagonVM == null) return;

        Model.Wagons.Remove(wagonVM.Model);
        Wagons.Remove(wagonVM);
    }

    /// <summary>
    /// Renumbers all locomotives in the train composition.
    /// </summary>
    private void RenumberLocomotives()
    {
        for (int i = 0; i < Locomotives.Count; i++)
        {
            Locomotives[i].Pos = (uint)(i + 1);
        }
    }

    /// <summary>
    /// Renumbers all wagons in the train composition.
    /// </summary>
    private void RenumberWagons()
    {
        for (int i = 0; i < Wagons.Count; i++)
        {
            Wagons[i].Pos = (uint)(i + 1);
        }
    }

    /// <summary>
    /// Handles locomotive reordering after drag & drop.
    /// </summary>
    [RelayCommand]
    public void LocomotivesReordered()
    {
        // Update Model.Locomotives to match ViewModel order
        Model.Locomotives.Clear();
        foreach (var locomotiveVM in Locomotives)
        {
            Model.Locomotives.Add(locomotiveVM.Model);
        }

        // Renumber based on new order
        RenumberLocomotives();
    }

    /// <summary>
    /// Handles wagon reordering after drag & drop.
    /// </summary>
    [RelayCommand]
    public void WagonsReordered()
    {
        // Update Model.Wagons to match ViewModel order
        Model.Wagons.Clear();
        foreach (var wagonVM in Wagons)
        {
            Model.Wagons.Add(wagonVM.Model);
        }

        // Renumber based on new order
        RenumberWagons();
    }
}