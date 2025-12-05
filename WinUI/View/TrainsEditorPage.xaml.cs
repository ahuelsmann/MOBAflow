// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Microsoft.UI.Xaml.Controls;
using Moba.SharedUI.ViewModel;

/// <summary>
/// Trains editor page with ListView + PropertyGrid.
/// Receives MainWindowViewModel from Frame navigation.
/// </summary>
public sealed partial class TrainsEditorPage : Page
{
    private MainWindowViewModel? _viewModel;
    
    public MainWindowViewModel? ViewModel
    {
        get => _viewModel;
        private set
        {
            _viewModel = value;
            DataContext = value;
        }
    }

    public TrainsEditorPage()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Called by Frame.Navigate(typeof(TrainsEditorPage), viewModel)
    /// </summary>
    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        
        if (e.Parameter is MainWindowViewModel viewModel)
        {
            ViewModel = viewModel;
            System.Diagnostics.Debug.WriteLine("TrainsEditorPage received MainWindowViewModel");
        }
    }
}
