// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Domain;

using Helper;

using System.Collections.ObjectModel;

public partial class MainWindowViewModel
{
    private static readonly ObservableCollection<VehicleItemViewModel> EmptyVehicleItems = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteTrainCommand))]
    private TrainViewModel? _selectedTrain;

    [ObservableProperty]
    private VehicleItemViewModel? _selectedVehicle;

    [ObservableProperty]
    private string _trainSearchText = string.Empty;

    partial void OnSelectedTrainChanging(TrainViewModel? value)
    {
        _ = value;
        if (_selectedTrain != null)
        {
            _selectedTrain.VehiclesModified -= SelectedTrain_VehiclesModified;
        }
    }

    partial void OnSelectedTrainChanged(TrainViewModel? value)
    {
        SelectedVehicle = null;

        if (value != null)
        {
            _session.TrackChanges(value);
            value.VehiclesModified += SelectedTrain_VehiclesModified;
            value.RefreshVehicleItems();
        }

        OnPropertyChanged(nameof(SelectedVehicles));
        OnPropertyChanged(nameof(TrainsCompositionTitle));
    }

    private void SelectedTrain_VehiclesModified(object? sender, EventArgs e)
    {
        if (_session.IsAutoSaveSuppressed)
        {
            return;
        }

        ObserveBackgroundTask(SaveSolutionInternalAsync(), "Auto-save solution");
        RefreshProjectRuntimeSnapshot();
    }

    /// <summary>
    /// Refreshes the runtime's snapshot of the selected project after an editor change, without re-activation.
    /// </summary>
    private void RefreshProjectRuntimeSnapshot()
    {
        if (SelectedProject is { } project)
        {
            ObserveBackgroundTask(_runtimeConnection.UpdateProjectAsync(project.Model), "Update project runtime");
        }
    }

    partial void OnTrainSearchTextChanged(string value)
    {
        _ = value;
        OnPropertyChanged(nameof(FilteredTrains));
    }

    public ObservableCollection<VehicleItemViewModel> SelectedVehicles => SelectedTrain?.VehicleItems ?? EmptyVehicleItems;

    public string TrainsCompositionTitle => SelectedTrain == null
        ? "Train Composition"
        : $"Train Composition: {SelectedTrain.Name}";

    public List<TrainViewModel> FilteredTrains
    {
        get
        {
            if (SelectedProject == null)
            {
                return [];
            }

            return string.IsNullOrWhiteSpace(TrainSearchText)
                ? [.. SelectedProject.Trains]
                : [.. SelectedProject.Trains.Where(train => train.Name.Contains(TrainSearchText, StringComparison.OrdinalIgnoreCase))];
        }
    }

    [RelayCommand]
    private void AddTrain()
    {
        if (SelectedProject == null)
        {
            return;
        }

        var train = EntityEditorHelper.AddEntity(
            SelectedProject.Model.Trains,
            SelectedProject.Trains,
            () => new Train { Name = $"Train {SelectedProject.Model.Trains.Count + 1}" },
            model => new TrainViewModel(model, SelectedProject));

        SelectedTrain = train;
        OnPropertyChanged(nameof(FilteredTrains));
    }

    [RelayCommand(CanExecute = nameof(CanDeleteTrain))]
    private void DeleteTrain()
    {
        if (SelectedProject == null)
        {
            return;
        }

        EntityEditorHelper.DeleteEntity(
            SelectedTrain,
            SelectedProject.Model.Trains,
            SelectedProject.Trains,
            () => SelectedTrain = SelectedProject.Trains.FirstOrDefault());

        OnPropertyChanged(nameof(FilteredTrains));
        OnPropertyChanged(nameof(SelectedVehicles));
    }

    private bool CanDeleteTrain() => SelectedTrain != null;

    public void AddLocomotiveToSelectedTrain(LocomotiveViewModel? locomotive, int insertIndex = -1)
    {
        AddVehicleToSelectedTrain(locomotive, insertIndex, static (train, value, index) => train.InsertLocomotive(value, index));
    }

    public void AddPassengerWagonToSelectedTrain(PassengerWagonViewModel? wagon, int insertIndex = -1)
    {
        AddVehicleToSelectedTrain(wagon, insertIndex, static (train, value, index) => train.InsertPassengerWagon(value, index));
    }

    public void AddGoodsWagonToSelectedTrain(GoodsWagonViewModel? wagon, int insertIndex = -1)
    {
        AddVehicleToSelectedTrain(wagon, insertIndex, static (train, value, index) => train.InsertGoodsWagon(value, index));
    }

    public void RemoveSelectedVehicle(VehicleItemViewModel? item)
    {
        if (item?.RemoveCommand == null)
        {
            return;
        }

        item.RemoveCommand.Execute(null);
        OnPropertyChanged(nameof(SelectedVehicles));
        ObserveBackgroundTask(SaveSolutionInternalAsync(), "Auto-save solution");
    }

    public void SynchronizeSelectedVehicles()
    {
        if (SelectedTrain == null)
        {
            return;
        }

        SelectedTrain.SynchronizeVehiclesFromItems();
        OnPropertyChanged(nameof(SelectedVehicles));
        ObserveBackgroundTask(SaveSolutionInternalAsync(), "Auto-save solution");
    }

    private void AddVehicleToSelectedTrain<TVehicle>(
        TVehicle? vehicle,
        int insertIndex,
        Action<TrainViewModel, TVehicle, int> insertAction)
        where TVehicle : class
    {
        if (SelectedTrain == null || vehicle == null)
        {
            return;
        }

        insertAction(SelectedTrain, vehicle, insertIndex);
        OnPropertyChanged(nameof(SelectedVehicles));
        ObserveBackgroundTask(SaveSolutionInternalAsync(), "Auto-save solution");
    }
}
