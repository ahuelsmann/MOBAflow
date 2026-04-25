// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Domain;

public partial class MainWindowViewModel
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteProjectStationCommand))]
    [NotifyCanExecuteChangedFor(nameof(AddPlatformToProjectStationCommand))]
    private StationViewModel? _selectedProjectStation;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteProjectStationPlatformCommand))]
    private PlatformViewModel? _selectedProjectStationPlatform;

    [ObservableProperty]
    private object? _stationsPageSelectedObject;

    public string StationsPagePropertiesTitle
    {
        get
        {
            if (StationsPageSelectedObject is StationViewModel) return "Station Properties";
            if (StationsPageSelectedObject is PlatformViewModel) return "Platform Properties";
            return "Properties";
        }
    }

    partial void OnSelectedProjectStationChanged(StationViewModel? value)
    {
        StationsPageSelectedObject = value;
        SelectedProjectStationPlatform = null;
        if (value != null)
        {
            value.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    partial void OnSelectedProjectStationPlatformChanged(PlatformViewModel? value)
    {
        if (value != null)
        {
            StationsPageSelectedObject = value;
            value.PropertyChanged += OnViewModelPropertyChanged;
        }
        else if (SelectedProjectStation != null)
        {
            StationsPageSelectedObject = SelectedProjectStation;
        }
    }

    partial void OnStationsPageSelectedObjectChanged(object? value)
    {
        _ = value;
        OnPropertyChanged(nameof(StationsPagePropertiesTitle));
    }

    [RelayCommand(CanExecute = nameof(CanAddProjectStation))]
    private void AddProjectStation()
    {
        if (SelectedProject == null) return;

        var station = new Station { Name = "New Station" };
        station.Platforms.Add(new Platform { Name = "Platform 1", Number = 1 });
        SelectedProject.Model.Stations.Add(station);
        var stationVm = new StationViewModel(station, SelectedProject.Model);
        SelectedProject.Stations.Add(stationVm);
        SelectedProjectStation = stationVm;
        ObserveBackgroundTask(_mobaRuntime.ActivateProjectAsync(SelectedProject.Model), "Activate project runtime");
    }

    [RelayCommand(CanExecute = nameof(CanDeleteProjectStation))]
    private void DeleteProjectStation()
    {
        if (SelectedProject == null || SelectedProjectStation == null) return;

        SelectedProject.Model.Stations.Remove(SelectedProjectStation.Model);
        SelectedProject.Stations.Remove(SelectedProjectStation);
        SelectedProjectStation = null;
        SelectedProjectStationPlatform = null;
        ObserveBackgroundTask(_mobaRuntime.ActivateProjectAsync(SelectedProject.Model), "Activate project runtime");
    }

    [RelayCommand(CanExecute = nameof(CanAddPlatformToProjectStation))]
    private void AddPlatformToProjectStation()
    {
        if (SelectedProject == null || SelectedProjectStation == null) return;

        var platform = new Platform
        {
            Name = $"Platform {SelectedProjectStation.Model.Platforms.Count + 1}",
            Number = (uint)(SelectedProjectStation.Model.Platforms.Count + 1)
        };
        SelectedProjectStation.Model.Platforms.Add(platform);
        SelectedProjectStation.RefreshPlatforms();
        SelectedProjectStationPlatform = SelectedProjectStation.Platforms.LastOrDefault();
        ObserveBackgroundTask(_mobaRuntime.ActivateProjectAsync(SelectedProject.Model), "Activate project runtime");
    }

    [RelayCommand(CanExecute = nameof(CanDeleteProjectStationPlatform))]
    private void DeleteProjectStationPlatform()
    {
        if (SelectedProject == null || SelectedProjectStation == null || SelectedProjectStationPlatform == null) return;

        SelectedProjectStation.Model.Platforms.Remove(SelectedProjectStationPlatform.Model);
        if (SelectedProjectStation.Model.PlatformId == SelectedProjectStationPlatform.Model.Id)
        {
            SelectedProjectStation.PlatformId = null;
        }

        SelectedProjectStation.RefreshPlatforms();
        SelectedProjectStationPlatform = null;
        ObserveBackgroundTask(_mobaRuntime.ActivateProjectAsync(SelectedProject.Model), "Activate project runtime");
    }

    private bool CanAddProjectStation() => SelectedProject != null;

    private bool CanDeleteProjectStation() => SelectedProjectStation != null;

    private bool CanAddPlatformToProjectStation() => SelectedProjectStation != null;

    private bool CanDeleteProjectStationPlatform() => SelectedProjectStationPlatform != null;
}
