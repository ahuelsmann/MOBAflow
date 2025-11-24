// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Moba.Backend.Model;
using Moba.SharedUI.Service;
using System.Collections.ObjectModel;

/// <summary>
/// ViewModel for the Trains tab in the Editor.
/// Composition editor for Trains (Locomotives + Wagons).
/// </summary>
public partial class TrainEditorViewModel : ObservableObject
{
    private readonly Project _project;
    private readonly ValidationService? _validationService;

    [ObservableProperty]
    private ObservableCollection<Train> _trains;

    [ObservableProperty]
    private Train? _selectedTrain;

    [ObservableProperty]
    private ObservableCollection<Locomotive> _locomotives;

    [ObservableProperty]
    private ObservableCollection<Wagon> _wagons;

    [ObservableProperty]
    private string? _validationError;

    public TrainEditorViewModel(Project project, ValidationService? validationService = null)
    {
        _project = project;
        _validationService = validationService;
        _trains = new ObservableCollection<Train>(project.Trains);
        _locomotives = new ObservableCollection<Locomotive>();
        _wagons = new ObservableCollection<Wagon>();
    }

    [RelayCommand]
    private void AddTrain()
    {
        var newTrain = new Train
        {
            Name = "New Train"
        };
        
        _project.Trains.Add(newTrain);
        Trains.Add(newTrain);
        SelectedTrain = newTrain;
        ValidationError = null;
    }

    [RelayCommand(CanExecute = nameof(CanDeleteTrain))]
    private void DeleteTrain()
    {
        if (SelectedTrain == null) return;

        // Validate deletion
        if (_validationService != null)
        {
            var validationResult = _validationService.CanDeleteTrain(SelectedTrain);
            if (!validationResult.IsValid)
            {
                ValidationError = validationResult.ErrorMessage;
                return;
            }
        }

        _project.Trains.Remove(SelectedTrain);
        Trains.Remove(SelectedTrain);
        SelectedTrain = null;
        ValidationError = null;
    }

    private bool CanDeleteTrain() => SelectedTrain != null;

    [RelayCommand]
    private void AddLocomotiveToComposition()
    {
        if (SelectedTrain == null) return;

        // This will be enhanced to select from global library
        var newLocomotive = new Locomotive
        {
            Name = "New Locomotive",
            DigitalAddress = 3,
            Pos = (uint)(SelectedTrain.Locomotives.Count + 1)
        };

        SelectedTrain.Locomotives.Add(newLocomotive);
        Locomotives.Add(newLocomotive);
    }

    [RelayCommand]
    private void AddWagonToComposition()
    {
        if (SelectedTrain == null) return;

        // This will be enhanced to select from global library
        var newWagon = new PassengerWagon
        {
            Name = "New Passenger Wagon",
            Pos = (uint)(SelectedTrain.Wagons.Count + 1),
            WagonClass = Backend.Model.Enum.PassengerClass.Second
        };

        SelectedTrain.Wagons.Add(newWagon);
        Wagons.Add(newWagon);
    }

    partial void OnSelectedTrainChanged(Train? value)
    {
        // Update Locomotives and Wagons collections when Train selection changes
        Locomotives.Clear();
        Wagons.Clear();
        
        if (value != null)
        {
            foreach (var locomotive in value.Locomotives)
            {
                Locomotives.Add(locomotive);
            }
            
            foreach (var wagon in value.Wagons)
            {
                Wagons.Add(wagon);
            }
        }
        ValidationError = null;
        
        // Notify Delete command that CanExecute might have changed
        DeleteTrainCommand.NotifyCanExecuteChanged();
    }
}
