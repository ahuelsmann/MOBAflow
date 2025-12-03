// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Moba.Domain;
using Moba.Domain.Enum;
using Moba.SharedUI.Helper;

/// <summary>
/// MainWindowViewModel - Train, Locomotive, and Wagon Management
/// Handles CRUD operations for Trains, Locomotives, and Wagons (both global library and train composition).
/// </summary>
public partial class MainWindowViewModel
{
    #region Train CRUD Commands

    [RelayCommand]
    private void AddTrain()
    {
        if (CurrentProjectViewModel == null) return;

        var train = EntityEditorHelper.AddEntity(
            CurrentProjectViewModel.Model.Trains,
            CurrentProjectViewModel.Trains,
            () => new Train { Name = "New Train" },
            model => new TrainViewModel(model));

        SelectedTrain = train;
        HasUnsavedChanges = true;
    }

    [RelayCommand(CanExecute = nameof(CanDeleteTrain))]
    private void DeleteTrain()
    {
        if (CurrentProjectViewModel == null) return;

        EntityEditorHelper.DeleteEntity(
            SelectedTrain,
            CurrentProjectViewModel.Model.Trains,
            CurrentProjectViewModel.Trains,
            () => { SelectedTrain = null; HasUnsavedChanges = true; });
    }

    private bool CanDeleteTrain() => SelectedTrain != null;

    #endregion

    #region Locomotive CRUD Commands

    [ObservableProperty]
    private LocomotiveViewModel? selectedLocomotive;

    [RelayCommand]
    private void AddLocomotive()
    {
        if (CurrentProjectViewModel == null) return;

        var locomotive = EntityEditorHelper.AddEntity(
            CurrentProjectViewModel.Model.Locomotives,
            CurrentProjectViewModel.Locomotives,
            () => new Locomotive { Name = "New Locomotive", DigitalAddress = 3 },
            model => new LocomotiveViewModel(model));

        SelectedLocomotive = locomotive;
        HasUnsavedChanges = true;
    }

    [RelayCommand(CanExecute = nameof(CanDeleteLocomotive))]
    private void DeleteLocomotive()
    {
        if (CurrentProjectViewModel == null) return;

        EntityEditorHelper.DeleteEntity(
            SelectedLocomotive,
            CurrentProjectViewModel.Model.Locomotives,
            CurrentProjectViewModel.Locomotives,
            () => { SelectedLocomotive = null; HasUnsavedChanges = true; });
    }

    private bool CanDeleteLocomotive() => SelectedLocomotive != null;

    #endregion

    #region Wagon CRUD Commands

    [ObservableProperty]
    private WagonViewModel? selectedWagon;

    [RelayCommand]
    private void AddPassengerWagon()
    {
        if (CurrentProjectViewModel == null) return;

        var newWagon = new PassengerWagon
        {
            Name = "New Passenger Wagon",
            WagonClass = PassengerClass.Second
        };

        CurrentProjectViewModel.Model.PassengerWagons.Add(newWagon);

        var wagonVM = new PassengerWagonViewModel(newWagon);
        CurrentProjectViewModel.Wagons.Add(wagonVM);
        SelectedWagon = wagonVM;

        HasUnsavedChanges = true;
    }

    [RelayCommand]
    private void AddGoodsWagon()
    {
        if (CurrentProjectViewModel == null) return;

        var newWagon = new GoodsWagon
        {
            Name = "New Goods Wagon",
            Cargo = CargoType.General
        };

        CurrentProjectViewModel.Model.GoodsWagons.Add(newWagon);

        var wagonVM = new GoodsWagonViewModel(newWagon);
        CurrentProjectViewModel.Wagons.Add(wagonVM);
        SelectedWagon = wagonVM;

        HasUnsavedChanges = true;
    }

    [RelayCommand(CanExecute = nameof(CanDeleteWagon))]
    private void DeleteWagon()
    {
        if (SelectedWagon == null || CurrentProjectViewModel == null) return;

        // Remove from appropriate model collection
        if (SelectedWagon.Model is PassengerWagon pw)
            CurrentProjectViewModel.Model.PassengerWagons.Remove(pw);
        else if (SelectedWagon.Model is GoodsWagon gw)
            CurrentProjectViewModel.Model.GoodsWagons.Remove(gw);

        CurrentProjectViewModel.Wagons.Remove(SelectedWagon);
        SelectedWagon = null;

        HasUnsavedChanges = true;
    }

    private bool CanDeleteWagon() => SelectedWagon != null;

    #endregion

    #region Train Composition Commands

    [RelayCommand]
    private void AddLocomotiveToComposition()
    {
        if (SelectedTrain == null) return;

        // TODO: Enhance to select from global library
        var newLocomotive = new Locomotive
        {
            Name = "New Locomotive",
            DigitalAddress = 3
        };

        SelectedTrain.Model.Locomotives.Add(newLocomotive);
        var locomotiveVM = new LocomotiveViewModel(newLocomotive);
        SelectedTrain.Locomotives.Add(locomotiveVM);

        HasUnsavedChanges = true;
    }

    [RelayCommand]
    private void AddWagonToComposition()
    {
        if (SelectedTrain == null) return;

        // TODO: Enhance to select from global library
        var newWagon = new PassengerWagon
        {
            Name = "New Passenger Wagon",
            WagonClass = PassengerClass.Second
        };

        SelectedTrain.Model.Wagons.Add(newWagon);
        var wagonVM = new PassengerWagonViewModel(newWagon);
        SelectedTrain.Wagons.Add(wagonVM);

        HasUnsavedChanges = true;
    }

    #endregion
}
