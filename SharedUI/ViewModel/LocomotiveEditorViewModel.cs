// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Moba.Domain;
using Moba.SharedUI.Service;
using System.Collections.ObjectModel;

/// <summary>
/// ViewModel for the Locomotives tab in the Editor.
/// Grid editor for the global Locomotives library.
/// </summary>
public partial class LocomotiveEditorViewModel : ObservableObject
{
    private readonly Project _project;
    private readonly ValidationService? _validationService;

    [ObservableProperty]
    private ObservableCollection<Locomotive> _locomotives;

    [ObservableProperty]
    private Locomotive? _selectedLocomotive;

    [ObservableProperty]
    private string? _validationError;

    public LocomotiveEditorViewModel(Project project, ValidationService? validationService = null)
    {
        _project = project;
        _validationService = validationService;
        _locomotives = new ObservableCollection<Locomotive>(project.Locomotives);
    }

    [RelayCommand]
    private void AddLocomotive()
    {
        var newLocomotive = new Locomotive
        {
            Name = "New Locomotive",
            DigitalAddress = 3,
            Manufacturer = "Unknown"
        };
        
        _project.Locomotives.Add(newLocomotive);
        Locomotives.Add(newLocomotive);
        SelectedLocomotive = newLocomotive;
        ValidationError = null;
    }

    [RelayCommand(CanExecute = nameof(CanDeleteLocomotive))]
    private void DeleteLocomotive()
    {
        if (SelectedLocomotive == null) return;

        // Validate deletion
        if (_validationService != null)
        {
            var validationResult = _validationService.CanDeleteLocomotive(SelectedLocomotive);
            if (!validationResult.IsValid)
            {
                ValidationError = validationResult.ErrorMessage;
                return;
            }
        }

        _project.Locomotives.Remove(SelectedLocomotive);
        Locomotives.Remove(SelectedLocomotive);
        SelectedLocomotive = null;
        ValidationError = null;
    }

    private bool CanDeleteLocomotive() => SelectedLocomotive != null;
    
    partial void OnSelectedLocomotiveChanged(Locomotive? value)
    {
        ValidationError = null;
        
        // Notify Delete command that CanExecute might have changed
        DeleteLocomotiveCommand.NotifyCanExecuteChanged();
    }
}
