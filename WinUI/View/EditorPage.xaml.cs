// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Microsoft.UI.Xaml.Controls;
using Moba.SharedUI.ViewModel;

/// <summary>
/// Editor page with tabs for Journeys, Workflows, Trains, Locomotives, Wagons, and Settings.
/// </summary>
public sealed partial class EditorPage : Page
{
    public EditorPageViewModel ViewModel { get; }

    public EditorPage(EditorPageViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        
        // Subscribe to property changes to update x:Bind bindings
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        
        // Subscribe to Loaded event to refresh when page becomes visible
        Loaded += EditorPage_Loaded;
    }

    private void EditorPage_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("🔄 EditorPage loaded - forcing bindings update");
        
        // Force refresh when page becomes visible
        // This ensures we show the latest data if Solution was loaded while we were on another page
        Bindings.Update();
        
        System.Diagnostics.Debug.WriteLine($"✅ EditorPage bindings updated - JourneyEditor has {ViewModel.JourneyEditor.Journeys.Count} journeys");
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // When JourneyEditor (or any other editor) changes, update all bindings
        if (e.PropertyName == nameof(EditorPageViewModel.JourneyEditor) ||
            e.PropertyName == nameof(EditorPageViewModel.WorkflowEditor) ||
            e.PropertyName == nameof(EditorPageViewModel.TrainEditor) ||
            e.PropertyName == nameof(EditorPageViewModel.LocomotiveEditor) ||
            e.PropertyName == nameof(EditorPageViewModel.WagonEditor) ||
            e.PropertyName == nameof(EditorPageViewModel.SettingsEditor))
        {
            System.Diagnostics.Debug.WriteLine($"📄 EditorPage: {e.PropertyName} changed - updating bindings");
            
            // Force x:Bind to re-evaluate all bindings
            Bindings.Update();
            
            System.Diagnostics.Debug.WriteLine($"✅ EditorPage: Bindings updated");
        }
    }

    private void CityItem_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        if (sender is Microsoft.UI.Xaml.FrameworkElement element && element.DataContext is Moba.Domain.City city)
        {
            ViewModel.JourneyEditor.AddStationFromCityCommand.Execute(city);
            
            // Close the flyout
            if (element.FindName("CityListView") is Microsoft.UI.Xaml.Controls.ListView listView)
            {
                var flyout = Microsoft.UI.Xaml.Controls.Primitives.FlyoutBase.GetAttachedFlyout(listView);
                flyout?.Hide();
            }
        }
    }
}
