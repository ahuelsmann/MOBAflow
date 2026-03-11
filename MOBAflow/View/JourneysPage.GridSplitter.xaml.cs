using Moba.Common.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MOBAflow.Helpers;
using MOBAflow.View;
using SharedUI.Interface;
using SharedUI.ViewModel;
using System.Linq;

namespace Moba.WinUI.View;

/// <summary>
/// Code-behind for JourneysPage with GridSplitter and VisualStateManager integration.
/// This is the new implementation using CommunityToolkit GridSplitter and MVVM layout management.
/// </summary>
public sealed partial class JourneysPage : Page
{
    private readonly JourneysPageLayoutViewModel _layoutViewModel;
    private readonly GridSplitterLayoutManager _layoutManager;
    private MainWindowViewModel? _mainWindowViewModel;

    /// <summary>
    /// Gets the main ViewModel for the page.
    /// </summary>
    public MainWindowViewModel ViewModel => _mainWindowViewModel ??= 
        (Application.Current as App)?.ServiceProvider?.GetRequiredService<MainWindowViewModel>() ?? 
        new MainWindowViewModel();

    /// <summary>
    /// Gets the layout ViewModel for managing column states and VisualStateManager.
    /// </summary>
    public JourneysPageLayoutViewModel LayoutViewModel => _layoutViewModel;

    public JourneysPage()
    {
        this.InitializeComponent();

        // Initialize layout management
        var settingsService = (Application.Current as App)?.ServiceProvider?.GetRequiredService<SettingsService>();
        var settings = (Application.Current as App)?.ServiceProvider?.GetRequiredService<AppSettings>() ?? new AppSettings();
        
        _layoutViewModel = new JourneysPageLayoutViewModel(settings, settingsService);
        _layoutManager = new GridSplitterLayoutManager(_layoutViewModel);

        // Set up layout ViewModel
        _layoutViewModel.AssociatedControl = MainLayoutGrid;
        _layoutViewModel.PropertyChanged += OnLayoutViewModelPropertyChanged;

        // Register splitters with the layout manager
        RegisterSplitters();

        // Load existing layout and apply to grid
        _layoutViewModel.LoadLayout();
        _layoutViewModel.ApplyToJourneysGrid(MainLayoutGrid);
        _layoutViewModel.UpdateJourneysVisualState();

        // Set up grid size changed handler for dynamic updates
        MainLayoutGrid.SizeChanged += OnMainLayoutGridSizeChanged;
    }

    /// <summary>
    /// Registers all GridSplitter controls with the layout manager.
    /// </summary>
    private void RegisterSplitters()
    {
        // Register splitters with their column indices
        _layoutManager.RegisterSplitter("JourneyListSplitter", JourneyListSplitter, 0, 2);
        _layoutManager.RegisterSplitter("CityLibrarySplitter", CityLibrarySplitter, 2, 8);
        _layoutManager.RegisterSplitter("WorkflowLibrarySplitter", WorkflowLibrarySplitter, 8, 15);
    }

    /// <summary>
    /// Handles property changes in the layout ViewModel.
    /// </summary>
    private void OnLayoutViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PageLayoutViewModel.AssociatedControl))
        {
            // Update visual state when associated control changes
            _layoutViewModel.UpdateJourneysVisualState();
        }
    }

    /// <summary>
    /// Handles size changes in the main layout grid.
    /// </summary>
    private void OnMainLayoutGridSizeChanged(object sender, SizeChangedEventArgs e)
    {
        // Update column states from grid when size changes
        _layoutViewModel.UpdateFromJourneysGrid(MainLayoutGrid);
        _layoutViewModel.UpdateJourneysVisualState();
    }

    /// <summary>
    /// Handles drag start for Journey ListView.
    /// </summary>
    private void JourneyListView_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
    {
        // Implementation for drag start
        if (e.Items.Count > 0 && e.Items[0] is Domain.Journey journey)
        {
            e.Data.SetText(journey.Name);
        }
    }

    /// <summary>
    /// Handles double tap on City ListView.
    /// </summary>
    private void CityListView_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
    {
        if (sender is ListView listView && listView.SelectedItem is Domain.City city)
        {
            // Add city to current journey or handle as needed
            ViewModel.AddCityToJourneyCommand.Execute(city);
        }
    }

    /// <summary>
    /// Handles drag start for City ListView.
    /// </summary>
    private void CityListView_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
    {
        if (e.Items.Count > 0 && e.Items[0] is Domain.City city)
        {
            e.Data.SetText(city.Name);
        }
    }

    /// <summary>
    /// Handles double tap on Workflow ListView.
    /// </summary>
    private void WorkflowListView_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
    {
        if (sender is ListView listView && listView.SelectedItem is Domain.Workflow workflow)
        {
            // Add workflow to current journey or handle as needed
            ViewModel.AddWorkflowToJourneyCommand.Execute(workflow);
        }
    }

    /// <summary>
    /// Handles drag start for Workflow ListView.
    /// </summary>
    private void WorkflowListView_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
    {
        if (e.Items.Count > 0 && e.Items[0] is Domain.Workflow workflow)
        {
            e.Data.SetText(workflow.Name);
        }
    }

    /// <summary>
    /// Toggles the expanded state of the Journey List column.
    /// </summary>
    public void ToggleJourneyListColumn()
    {
        _layoutViewModel.JourneyListColumn.IsExpanded = !_layoutViewModel.JourneyListColumn.IsExpanded;
        _layoutViewModel.UpdateJourneysVisualState();
        _layoutViewModel.PersistLayout();
    }

    /// <summary>
    /// Toggles the expanded state of the City Library column.
    /// </summary>
    public void ToggleCityLibraryColumn()
    {
        _layoutViewModel.CityLibraryColumn.IsExpanded = !_layoutViewModel.CityLibraryColumn.IsExpanded;
        _layoutViewModel.UpdateJourneysVisualState();
        _layoutViewModel.PersistLayout();
    }

    /// <summary>
    /// Toggles the expanded state of the Workflow Library column.
    /// </summary>
    public void ToggleWorkflowLibraryColumn()
    {
        _layoutViewModel.WorkflowLibraryColumn.IsExpanded = !_layoutViewModel.WorkflowLibraryColumn.IsExpanded;
        _layoutViewModel.UpdateJourneysVisualState();
        _layoutViewModel.PersistLayout();
    }

    /// <summary>
    /// Resets the layout to default configuration.
    /// </summary>
    public void ResetLayout()
    {
        // Reset all columns to their default widths and expanded states
        _layoutViewModel.JourneyListColumn.LoadFromColumnState(new ColumnState
        {
            Width = 250,
            IsExpanded = true,
            MinWidth = 150,
            DefaultWidth = 250
        });

        _layoutViewModel.JourneyContentColumn.LoadFromColumnState(new ColumnState
        {
            Width = 400,
            IsExpanded = true,
            MinWidth = 300,
            DefaultWidth = 400
        });

        _layoutViewModel.CityLibraryColumn.LoadFromColumnState(new ColumnState
        {
            Width = 300,
            IsExpanded = true,
            MinWidth = 200,
            DefaultWidth = 300
        });

        _layoutViewModel.WorkflowLibraryColumn.LoadFromColumnState(new ColumnState
        {
            Width = 300,
            IsExpanded = true,
            MinWidth = 200,
            DefaultWidth = 300
        });

        // Apply changes and persist
        _layoutViewModel.ApplyToJourneysGrid(MainLayoutGrid);
        _layoutViewModel.UpdateJourneysVisualState();
        _layoutViewModel.PersistLayout();
    }

    /// <summary>
    /// Cleanup when the page is unloaded.
    /// </summary>
    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        
        // Persist current layout
        _layoutViewModel.PersistLayout();
        
        // Cleanup event handlers
        MainLayoutGrid.SizeChanged -= OnMainLayoutGridSizeChanged;
        _layoutViewModel.PropertyChanged -= OnLayoutViewModelPropertyChanged;
        _layoutManager.Dispose();
    }
}
