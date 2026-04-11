namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Domain;

using Helper;

using System.Collections.ObjectModel;
using System.Threading;

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
            value.PropertyChanged += OnViewModelPropertyChanged;
            value.VehiclesModified += SelectedTrain_VehiclesModified;
            value.RefreshVehicleItems();
        }

        OnPropertyChanged(nameof(SelectedVehicles));
        OnPropertyChanged(nameof(TrainsCompositionTitle));
    }

    private void SelectedTrain_VehiclesModified(object? sender, EventArgs e)
    {
        if (Volatile.Read(ref _solutionAutoSaveSuppressionCount) > 0)
        {
            return;
        }

        _ = SaveSolutionInternalAsync();
    }

    partial void OnTrainSearchTextChanged(string value)
    {
        _ = value;
        OnPropertyChanged(nameof(FilteredTrains));
    }

    partial void OnLocomotiveSearchTextChanged(string value)
    {
        _ = value;
        OnPropertyChanged(nameof(FilteredLocomotiveLibrary));
    }

    partial void OnPassengerWagonSearchTextChanged(string value)
    {
        _ = value;
        OnPropertyChanged(nameof(FilteredPassengerWagonLibrary));
    }

    partial void OnGoodsWagonSearchTextChanged(string value)
    {
        _ = value;
        OnPropertyChanged(nameof(FilteredGoodsWagonLibrary));
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

    public List<LocomotiveViewModel> FilteredLocomotiveLibrary
    {
        get
        {
            if (SelectedProject == null)
            {
                return [];
            }

            return string.IsNullOrWhiteSpace(LocomotiveSearchText)
                ? [.. SelectedProject.Locomotives]
                : [.. SelectedProject.Locomotives.Where(locomotive => locomotive.Name.Contains(LocomotiveSearchText, StringComparison.OrdinalIgnoreCase))];
        }
    }

    public List<PassengerWagonViewModel> FilteredPassengerWagonLibrary
    {
        get
        {
            if (SelectedProject == null)
            {
                return [];
            }

            return string.IsNullOrWhiteSpace(PassengerWagonSearchText)
                ? [.. SelectedProject.PassengerWagons]
                : [.. SelectedProject.PassengerWagons.Where(wagon => wagon.Name.Contains(PassengerWagonSearchText, StringComparison.OrdinalIgnoreCase))];
        }
    }

    public List<GoodsWagonViewModel> FilteredGoodsWagonLibrary
    {
        get
        {
            if (SelectedProject == null)
            {
                return [];
            }

            return string.IsNullOrWhiteSpace(GoodsWagonSearchText)
                ? [.. SelectedProject.GoodsWagons]
                : [.. SelectedProject.GoodsWagons.Where(wagon => wagon.Name.Contains(GoodsWagonSearchText, StringComparison.OrdinalIgnoreCase))];
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
        _ = SaveSolutionInternalAsync();
    }

    public void SynchronizeSelectedVehicles()
    {
        if (SelectedTrain == null)
        {
            return;
        }

        SelectedTrain.SynchronizeVehiclesFromItems();
        OnPropertyChanged(nameof(SelectedVehicles));
        _ = SaveSolutionInternalAsync();
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
        _ = SaveSolutionInternalAsync();
    }
}
