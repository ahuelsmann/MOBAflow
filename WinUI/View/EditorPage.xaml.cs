// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Microsoft.UI.Xaml.Controls;
using Moba.SharedUI.ViewModel;

/// <summary>
/// Editor page with tabs for Journeys, Workflows, Trains, Locomotives, Wagons, and Settings.
/// Simplified to work with MainWindowViewModel directly.
/// </summary>
public sealed partial class EditorPage : Page
{
    public EditorPageViewModel ViewModel { get; }

    public EditorPage(EditorPageViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        
        // Subscribe to Loaded event to refresh when page becomes visible
        Loaded += EditorPage_Loaded;
    }

    private void EditorPage_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("EditorPage loaded - forcing bindings update");
        
        // Force refresh when page becomes visible
        Bindings.Update();
        
        System.Diagnostics.Debug.WriteLine("EditorPage bindings updated");
    }
    
    private void CityItem_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        if (sender is Microsoft.UI.Xaml.FrameworkElement element && element.DataContext is Moba.Domain.City city)
        {
            // Use MainWindowViewModel command to add station from city
            ViewModel.MainWindowViewModel.AddStationFromCityCommand.Execute(city);
            
            // Close the flyout
            if (element.FindName("CityListView") is Microsoft.UI.Xaml.Controls.ListView listView)
            {
                var flyout = Microsoft.UI.Xaml.Controls.Primitives.FlyoutBase.GetAttachedFlyout(listView);
                flyout?.Hide();
            }
        }
    }
}
