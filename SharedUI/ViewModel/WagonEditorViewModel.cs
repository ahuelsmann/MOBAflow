// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Moba.Backend.Model;
using Moba.SharedUI.Service;
using System.Collections.ObjectModel;
using System.Linq;

/// <summary>
/// ViewModel for the Wagons tab in the Editor.
/// Grid editor for the global Wagons library (Passenger and Goods).
/// </summary>
public partial class WagonEditorViewModel : ObservableObject
{
    private readonly Project _project;
    private readonly ValidationService? _validationService;

    [ObservableProperty]
    private ObservableCollection<Wagon> _wagons;

    [ObservableProperty]
    private Wagon? _selectedWagon;

    [ObservableProperty]
    private string? _validationError;

    public WagonEditorViewModel(Project project, ValidationService? validationService = null)
    {
        _project = project;
        _validationService = validationService;
        // Combine both PassengerWagons and GoodsWagons into one collection
        var allWagons = project.PassengerWagons.Cast<Wagon>()
            .Concat(project.GoodsWagons.Cast<Wagon>())
            .ToList();
        _wagons = new ObservableCollection<Wagon>(allWagons);
    }

    [RelayCommand]
    private void AddPassengerWagon()
    {
        var newWagon = new PassengerWagon
        {
            Name = "New Passenger Wagon",
            Manufacturer = "Unknown",
            WagonClass = Backend.Model.Enum.PassengerClass.Second
        };
        
        _project.PassengerWagons.Add(newWagon);
        Wagons.Add(newWagon);
        SelectedWagon = newWagon;
        ValidationError = null;
    }

    [RelayCommand]
    private void AddGoodsWagon()
    {
        var newWagon = new GoodsWagon
        {
            Name = "New Goods Wagon",
            Manufacturer = "Unknown"
        };
        
        _project.GoodsWagons.Add(newWagon);
        Wagons.Add(newWagon);
        SelectedWagon = newWagon;
        ValidationError = null;
    }

    [RelayCommand(CanExecute = nameof(CanDeleteWagon))]
    private void DeleteWagon()
    {
        if (SelectedWagon == null) return;

        // Validate deletion
        if (_validationService != null)
        {
            var validationResult = _validationService.CanDeleteWagon(SelectedWagon);
            if (!validationResult.IsValid)
            {
                ValidationError = validationResult.ErrorMessage;
                return;
            }
        }

        if (SelectedWagon is PassengerWagon passengerWagon)
        {
            _project.PassengerWagons.Remove(passengerWagon);
        }
        else if (SelectedWagon is GoodsWagon goodsWagon)
        {
            _project.GoodsWagons.Remove(goodsWagon);
        }
        
        Wagons.Remove(SelectedWagon);
        SelectedWagon = null;
        ValidationError = null;
    }

    private bool CanDeleteWagon() => SelectedWagon != null;
}
