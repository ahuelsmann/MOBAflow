// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;

using Moba.Domain.Enum;

/// <summary>
/// Simplified ViewModel for the Editor page.
/// All data and commands are now in MainWindowViewModel - this is just a thin wrapper for UI-specific state.
/// </summary>
public partial class EditorPageViewModel : ObservableObject
{
    /// <summary>
    /// Reference to the main window ViewModel which contains all data and commands.
    /// </summary>
    public MainWindowViewModel MainWindowViewModel { get; }

    [ObservableProperty]
    private int _selectedTabIndex;
    
    /// <summary>
    /// Available ColorScheme enum values for ComboBox binding.
    /// </summary>
    public Array ColorSchemeValues => Enum.GetValues(typeof(ColorScheme));
    
    /// <summary>
    /// Available PassengerClass enum values for ComboBox binding.
    /// </summary>
    public Array PassengerClassValues => Enum.GetValues(typeof(PassengerClass));
    
    /// <summary>
    /// Available CargoType enum values for ComboBox binding.
    /// </summary>
    public Array CargoTypeValues => Enum.GetValues(typeof(CargoType));
    
    /// <summary>
    /// Gets the project name to display in the header.
    /// </summary>
    public string ProjectName => MainWindowViewModel.CurrentProjectViewModel?.Name ?? "(No Project Loaded)";

    public EditorPageViewModel(MainWindowViewModel mainWindowViewModel)
    {
        MainWindowViewModel = mainWindowViewModel ?? throw new ArgumentNullException(nameof(mainWindowViewModel));
    }
}