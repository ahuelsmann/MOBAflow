// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

using Moba.SharedUI.ViewModel;

/// <summary>
/// Page for configuring project entities (Journeys, Stations, Workflows, Trains) using table-like editing.
/// </summary>
public sealed partial class ProjectConfigurationPage : Page
{
    public ProjectConfigurationPage()
    {
        InitializeComponent();
    }

    public ProjectConfigurationPageViewModel? ViewModel { get; private set; }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is ProjectConfigurationPageViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = ViewModel;
            
            // Debug output
            System.Diagnostics.Debug.WriteLine("ProjectConfigurationPage loaded");
            System.Diagnostics.Debug.WriteLine($"   MainWindowViewModel connected: {ViewModel.MainWindowViewModel != null}");
            System.Diagnostics.Debug.WriteLine($"   CurrentProject: {ViewModel.MainWindowViewModel.CurrentProjectViewModel?.Name ?? "null"}");
            
            // Notify bindings to update
            Bindings.Update();
        }
    }
}
