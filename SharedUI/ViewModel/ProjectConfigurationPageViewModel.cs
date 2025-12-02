// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using CommunityToolkit.Mvvm.ComponentModel;

/// <summary>
/// ViewModel for the Project Configuration page.
/// Simplified - all data and commands now in MainWindowViewModel.
/// Note: Application-wide settings (like AutoLoadLastSolution) are in SettingsPage, not here.
/// </summary>
public partial class ProjectConfigurationPageViewModel : ObservableObject
{
    /// <summary>
    /// Reference to the main window ViewModel which contains all data and commands.
    /// </summary>
    public MainWindowViewModel MainWindowViewModel { get; }

    public ProjectConfigurationPageViewModel(MainWindowViewModel mainWindowViewModel)
    {
        MainWindowViewModel = mainWindowViewModel ?? throw new ArgumentNullException(nameof(mainWindowViewModel));
    }
}
