using System.Linq;
// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Moba.Domain;
using Moba.SharedUI.ViewModel;
using Moba.WinUI.Service;
using MainWindowViewModel = Moba.SharedUI.ViewModel.WinUI.MainWindowViewModel;

public sealed partial class MainWindow
{
    public MainWindowViewModel ViewModel { get; }
    public CounterViewModel CounterViewModel { get; }
    
#pragma warning disable CS8618 // Field is initialized in constructor via DI
    private readonly HealthCheckService _healthCheckService;
#pragma warning restore CS8618
    private readonly Moba.SharedUI.Service.IUiDispatcher _uiDispatcher;

    public MainWindow(
        MainWindowViewModel viewModel, 
        CounterViewModel counterViewModel,
        HealthCheckService healthCheckService, 
        Moba.SharedUI.Service.IUiDispatcher uiDispatcher)
    {
        ViewModel = viewModel;
        CounterViewModel = counterViewModel;
        _healthCheckService = healthCheckService;
        _uiDispatcher = uiDispatcher;
        
        InitializeComponent();

        // Set first nav item as selected (Overview)
        MainNavigation.SelectedItem = MainNavigation.MenuItems[0]; // Overview

        ViewModel.ExitApplicationRequested += OnExitApplicationRequested;
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        Closed += MainWindow_Closed;

        // Subscribe to health check events
        if (_healthCheckService != null)
        {
            _healthCheckService.HealthStatusChanged += OnHealthStatusChanged;
            _healthCheckService.StartPeriodicChecks();
            
            // Initial status display
            UpdateHealthStatus(_healthCheckService.SpeechServiceStatus);
        }

        // Navigate to Overview page on startup
        NavigateToOverview();
    }

    /// <summary>
    /// Navigate to Overview page (Lap Counter Dashboard).
    /// </summary>
    private void NavigateToOverview()
    {
        try
        {
            var counterViewModel = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<CounterViewModel>(
                ((App)Microsoft.UI.Xaml.Application.Current).Services);
            
            var overviewPage = new OverviewPage(counterViewModel);
            ContentFrame.Content = overviewPage;
            ContentFrame.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
            LegacyContent.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Failed to navigate to Overview: {ex.Message}");
            // Fallback: Show legacy content
            ContentFrame.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
            LegacyContent.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
        }
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.IsZ21Connected))
        {
            Z21StatusIcon.Glyph = ViewModel.IsZ21Connected ? "\uE8EB" : "\uF384";
        }
        else if (e.PropertyName == nameof(MainWindowViewModel.Solution))
        {
            // Solution was loaded - refresh EditorPageViewModel if it exists
            System.Diagnostics.Debug.WriteLine("🔄 Solution changed - refreshing EditorPageViewModel");
            
            try
            {
                var editorViewModel = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetService<SharedUI.ViewModel.EditorPageViewModel>(
                    ((App)Microsoft.UI.Xaml.Application.Current).Services);
                
                if (editorViewModel != null)
                {
                    editorViewModel.Refresh();
                    System.Diagnostics.Debug.WriteLine("✅ EditorPageViewModel refreshed after Solution change");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Failed to refresh EditorPageViewModel: {ex.Message}");
            }
        }
    }

    private void MainWindow_Closed(object sender, Microsoft.UI.Xaml.WindowEventArgs args)
    {
        _healthCheckService?.StopPeriodicChecks();
        ViewModel.OnWindowClosing();
    }

    private static void OnExitApplicationRequested(object? sender, EventArgs e)
    {
        Microsoft.UI.Xaml.Application.Current.Exit();
    }

    private void SolutionTreeView_SelectionChanged(TreeView sender, TreeViewSelectionChangedEventArgs args)
    {
        if (args.AddedItems.Count > 0 && args.AddedItems[0] is SharedUI.ViewModel.TreeNodeViewModel node)
        {
            ViewModel.OnNodeSelected(node);
        }
    }

    private void TreeView_RightTapped(object sender, Microsoft.UI.Xaml.Input.RightTappedRoutedEventArgs e)
    {
        // Context menu is shown automatically by ContextFlyout
    }

    private void TreeView_DragItemsCompleted(TreeView sender, TreeViewDragItemsCompletedEventArgs args)
    {
        // Called after drag & drop reorder is complete
        // Sync ViewModel with TreeView order and trigger save
        
        if (args.Items.Count == 0) return;
        
        var draggedNode = args.Items[0] as SharedUI.ViewModel.TreeNodeViewModel;
        if (draggedNode == null) return;

        // Handle Station reordering in Journey
        if (draggedNode.DataContext is SharedUI.ViewModel.StationViewModel)
        {
            var journeyVM = ViewModel.FindParentJourneyViewModel(draggedNode);
            if (journeyVM != null)
            {
                // Find the parent Journey node in TreeView
                var journeyNode = FindTreeNodeForViewModel(ViewModel.TreeNodes, journeyVM);
                if (journeyNode != null)
                {
                    // Sync ViewModel.Stations with TreeView order
                    journeyVM.Stations.Clear();
                    foreach (var child in journeyNode.Children)
                    {
                        if (child.DataContext is SharedUI.ViewModel.StationViewModel stationVM)
                        {
                            journeyVM.Stations.Add(stationVM);
                        }
                    }

                    // Trigger reorder logic (renumber + save)
                    journeyVM.StationsReorderedCommand.Execute(null);
                    
                    System.Diagnostics.Debug.WriteLine($"✅ Stations reordered in Journey '{journeyVM.Name}'");
                }
            }
        }
        // Handle Action reordering in Workflow
        else if (draggedNode.DataType == typeof(WorkflowAction))
        {
            // Find parent Workflow
            var workflowNode = FindParentNodeByType(ViewModel.TreeNodes, draggedNode, typeof(Workflow));
            if (workflowNode?.DataContext is Workflow workflow)
            {
                // Sync Workflow.Actions with TreeView order
                workflow.Actions.Clear();
                foreach (var child in workflowNode.Children)
                {
                    if (child.DataContext is WorkflowAction action)
                    {
                        workflow.Actions.Add(action);
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"✅ Actions reordered in Workflow '{workflow.Name}'");
                ViewModel.RefreshTreeView();
            }
        }
        // Handle Locomotive reordering in Train
        else if (draggedNode.DataContext is SharedUI.ViewModel.LocomotiveViewModel)
        {
            var trainNode = FindParentNodeByType(ViewModel.TreeNodes, draggedNode, typeof(Train));
            if (trainNode?.DataContext is SharedUI.ViewModel.TrainViewModel trainVM)
            {
                // Sync ViewModel.Locomotives with TreeView order
                trainVM.Locomotives.Clear();
                foreach (var child in trainNode.Children)
                {
                    if (child.DataContext is SharedUI.ViewModel.LocomotiveViewModel locomotiveVM)
                    {
                        trainVM.Locomotives.Add(locomotiveVM);
                    }
                }

                // Trigger reorder logic
                trainVM.LocomotivesReorderedCommand.Execute(null);
                
                System.Diagnostics.Debug.WriteLine($"✅ Locomotives reordered in Train '{trainVM.Name}'");
            }
        }
        // Handle Wagon reordering in Train
        else if (draggedNode.DataContext is SharedUI.ViewModel.WagonViewModel)
        {
            var trainNode = FindParentNodeByType(ViewModel.TreeNodes, draggedNode, typeof(Train));
            if (trainNode?.DataContext is SharedUI.ViewModel.TrainViewModel trainVM)
            {
                // Sync ViewModel.Wagons with TreeView order
                trainVM.Wagons.Clear();
                foreach (var child in trainNode.Children)
                {
                    if (child.DataContext is SharedUI.ViewModel.WagonViewModel wagonVM)
                    {
                        trainVM.Wagons.Add(wagonVM);
                    }
                }

                // Trigger reorder logic
                trainVM.WagonsReorderedCommand.Execute(null);
                
                System.Diagnostics.Debug.WriteLine($"✅ Wagons reordered in Train '{trainVM.Name}'");
            }
        }
    }

    /// <summary>
    /// Finds the TreeNode that contains the specified ViewModel in its DataContext.
    /// </summary>
    private SharedUI.ViewModel.TreeNodeViewModel? FindTreeNodeForViewModel(
        System.Collections.ObjectModel.ObservableCollection<SharedUI.ViewModel.TreeNodeViewModel> nodes,
        object viewModel)
    {
        foreach (var node in nodes)
        {
            if (node.DataContext == viewModel)
                return node;

            var result = FindTreeNodeForViewModel(node.Children, viewModel);
            if (result != null) return result;
        }

        return null;
    }

    /// <summary>
    /// Finds a parent node of a specific type in the tree hierarchy.
    /// </summary>
    private SharedUI.ViewModel.TreeNodeViewModel? FindParentNodeByType(
        System.Collections.ObjectModel.ObservableCollection<SharedUI.ViewModel.TreeNodeViewModel> nodes,
        SharedUI.ViewModel.TreeNodeViewModel targetNode,
        Type parentType)
    {
        foreach (var node in nodes)
        {
            // Check if this node is the parent we're looking for
            if (node.Children.Contains(targetNode) && node.DataType == parentType)
            {
                return node;
            }

            // Recurse into children
            var result = FindParentNodeByType(node.Children, targetNode, parentType);
            if (result != null) return result;
        }

        return null;
    }

    private void MoveStationUp_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (ViewModel.CurrentSelectedNode?.DataContext is not SharedUI.ViewModel.StationViewModel stationVM)
            return;

        // Find parent JourneyViewModel
        var journeyVM = ViewModel.FindParentJourneyViewModel(ViewModel.CurrentSelectedNode);
        if (journeyVM != null)
        {
            // Use ViewModel command
            journeyVM.MoveStationUpCommand.Execute(stationVM);
        }
    }

    private void MoveStationDown_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (ViewModel.CurrentSelectedNode?.DataContext is not SharedUI.ViewModel.StationViewModel stationVM)
            return;

        // Find parent JourneyViewModel
        var journeyVM = ViewModel.FindParentJourneyViewModel(ViewModel.CurrentSelectedNode);
        if (journeyVM != null)
        {
            // Use ViewModel command
            journeyVM.MoveStationDownCommand.Execute(stationVM);
        }
    }

    private void OnHealthStatusChanged(object? sender, HealthStatusChangedEventArgs e)
    {
        // Update UI on dispatcher thread using IUiDispatcher
        _uiDispatcher.InvokeOnUi(() =>
        {
            UpdateHealthStatus(e.StatusMessage);
        });
    }

    private void UpdateHealthStatus(string statusMessage)
    {
        // Display only "Azure Speech" - status is shown via icon only
        SpeechHealthText.Text = "Azure Speech";
        
        // Set tooltip with detailed status
        Microsoft.UI.Xaml.Controls.ToolTipService.SetToolTip(SpeechHealthPanel, statusMessage);
        
        // Update icon and color based on health status
        if (statusMessage.Contains("✅") || statusMessage.Contains("Ready"))
        {
            SpeechHealthIcon.Glyph = "\uE930"; // Checkmark circle
            SpeechHealthIcon.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                Microsoft.UI.Colors.Green);
        }
        else if (statusMessage.Contains("⚠️") || statusMessage.Contains("Not Configured"))
        {
            SpeechHealthIcon.Glyph = "\uE7BA"; // Warning
            SpeechHealthIcon.Foreground = Microsoft.UI.Xaml.Application.Current.Resources["SystemFillColorCautionBrush"] as Microsoft.UI.Xaml.Media.Brush;
        }
        else if (statusMessage.Contains("❌") || statusMessage.Contains("Failed"))
        {
            SpeechHealthIcon.Glyph = "\uE711"; // Error
            SpeechHealthIcon.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                Microsoft.UI.Colors.Red);
        }
        else // Initializing
        {
            SpeechHealthIcon.Glyph = "\uE946"; // Sync
            SpeechHealthIcon.Foreground = Microsoft.UI.Xaml.Application.Current.Resources["SystemFillColorCautionBrush"] as Microsoft.UI.Xaml.Media.Brush;
        }
    }

    private void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        // Check if Ctrl key is pressed
        var ctrlState = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control);
        var isCtrlPressed = (ctrlState & Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down;

        if (!isCtrlPressed) return;

        bool handled = false;

        switch (e.Key)
        {
            case Windows.System.VirtualKey.N:
                if (ViewModel.NewSolutionCommand.CanExecute(null))
                {
                    ViewModel.NewSolutionCommand.Execute(null);
                    handled = true;
                }
                break;

            case Windows.System.VirtualKey.O:
                if (ViewModel.LoadSolutionCommand.CanExecute(null))
                {
                    ViewModel.LoadSolutionCommand.Execute(null);
                    handled = true;
                }
                break;

            case Windows.System.VirtualKey.Z:
                // Undo removed - will be reimplemented later
                handled = true;
                break;

            case Windows.System.VirtualKey.Y:
                // Redo removed - will be reimplemented later
                handled = true;
                break;

            case Windows.System.VirtualKey.S:
                if (ViewModel.SaveSolutionCommand.CanExecute(null))
                {
                    ViewModel.SaveSolutionCommand.Execute(null);
                    handled = true;
                }
                break;
        }

        e.Handled = handled;
    }

    private void NavigationView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.InvokedItemContainer?.Tag is not string tag) return;

        switch (tag)
        {
            case "overview":
                // Navigate to OverviewPage (Lap Counter Dashboard)
                NavigateToOverview();
                break;

            case "editor":
                // Navigate to EditorPage (using Singleton ViewModel)
                try
                {
                    System.Diagnostics.Debug.WriteLine($"🔍 Attempting to navigate to EditorPage...");
                    System.Diagnostics.Debug.WriteLine($"   Solution: {ViewModel.Solution != null}");
                    System.Diagnostics.Debug.WriteLine($"   Projects: {ViewModel.Solution?.Projects.Count ?? 0}");
                    
                    var editorViewModel = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<SharedUI.ViewModel.EditorPageViewModel>(
                        ((App)Microsoft.UI.Xaml.Application.Current).Services);
                    
                    System.Diagnostics.Debug.WriteLine($"🔄 Navigating to EditorPage - refreshing data first");
                    System.Diagnostics.Debug.WriteLine($"   Solution has {ViewModel.Solution?.Projects.Count ?? 0} projects");
                    if (ViewModel.Solution?.Projects.Count > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"   First project has {ViewModel.Solution.Projects[0].Journeys.Count} journeys");
                    }
                    
                    // Always refresh EditorPageViewModel before navigating
                    editorViewModel.Refresh();
                    
                    var editorPage = new EditorPage(editorViewModel);
                    
                    System.Diagnostics.Debug.WriteLine($"✅ Navigating to EditorPage (Singleton ViewModel)");
                    
                    ContentFrame.Content = editorPage;
                    ContentFrame.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                    LegacyContent.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
                    
                    System.Diagnostics.Debug.WriteLine($"✅ Navigation to EditorPage complete");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Failed to navigate to EditorPage:");
                    System.Diagnostics.Debug.WriteLine($"   Exception Type: {ex.GetType().Name}");
                    System.Diagnostics.Debug.WriteLine($"   Message: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"   StackTrace: {ex.StackTrace}");
                    if (ex.InnerException != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"   Inner Exception: {ex.InnerException.Message}");
                    }
                    
                    // Fallback: Show legacy content
                    ContentFrame.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
                    LegacyContent.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                    
                    // Show error dialog to user
                    _ = ShowErrorDialogAsync("Navigation Error", $"Failed to open Editor: {ex.Message}");
                }
                break;

            case "configuration":
                // Navigate to ProjectConfigurationPage
                try
                {
                    var project = ViewModel.Solution.Projects[0];
                    var preferencesService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<SharedUI.Service.IPreferencesService>(
                        ((App)Microsoft.UI.Xaml.Application.Current).Services);
                    var configViewModel = new SharedUI.ViewModel.ProjectConfigurationPageViewModel(project, preferencesService, ViewModel);
                    
                    System.Diagnostics.Debug.WriteLine($"✅ Navigating to ProjectConfigurationPage with {project.Journeys.Count} journeys, {project.Workflows.Count} workflows, {project.Trains.Count} trains");
                    
                    ContentFrame.Navigate(typeof(ProjectConfigurationPage), configViewModel);
                    ContentFrame.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                    LegacyContent.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Failed to navigate to ProjectConfigurationPage: {ex.Message}");
                    
                    // Fallback: Show legacy content
                    ContentFrame.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
                    LegacyContent.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                    
                    _ = ShowErrorDialogAsync("Navigation Error", $"Failed to open Project Configuration: {ex.Message}");
                }
                break;

            case "explorer":
                // Navigate to ExplorerPage
                try
                {
                    var explorerPage = new ExplorerPage(ViewModel);
                    ContentFrame.Content = explorerPage;
                    ContentFrame.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                    LegacyContent.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
                    
                    System.Diagnostics.Debug.WriteLine($"✅ Navigated to ExplorerPage with {ViewModel.TreeNodes.Count} root nodes");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Failed to navigate to ExplorerPage: {ex.Message}");
                    
                    // Fallback: Show legacy content
                    ContentFrame.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
                    LegacyContent.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                    
                    _ = ShowErrorDialogAsync("Navigation Error", $"Failed to open Explorer: {ex.Message}");
                }
                break;
        }
    }

    private async Task ShowNoSolutionDialogAsync()
    {
        var dialog = new ContentDialog
        {
            Title = "No Solution Loaded",
            Content = "Please load a solution first before accessing Project Configuration.",
            CloseButtonText = "OK",
            XamlRoot = Content.XamlRoot
        };
        
        await dialog.ShowAsync();
    }

    private async Task ShowErrorDialogAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = Content.XamlRoot
        };
        
        await dialog.ShowAsync();
    }

    // Context Menu Handlers
    private void ContextMenu_Opening(object sender, object e)
    {
        if (sender is not MenuFlyout menuFlyout) return;
        
        // Get the TreeViewItem from the MenuFlyout's target
        var target = menuFlyout.Target as Microsoft.UI.Xaml.FrameworkElement;
        var treeViewItem = FindParent<TreeViewItem>(target);
        var node = treeViewItem?.Tag as TreeNodeViewModel;
        
        if (node == null) return;

        // Hide all menu items first
        foreach (var item in menuFlyout.Items)
        {
            if (item is MenuFlyoutItem menuItem)
            {
                menuItem.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
            }
        }

        // Show relevant menu items based on node type
        var dataType = node.DataType;
        
        if (dataType == typeof(Journey))
        {
            FindMenuItemByName(menuFlyout, "AddStationMenuItem").Visibility = Microsoft.UI.Xaml.Visibility.Visible;
            FindMenuItemByName(menuFlyout, "DeleteJourneyMenuItem").Visibility = Microsoft.UI.Xaml.Visibility.Visible;
        }
        else if (dataType == typeof(Workflow))
        {
            FindMenuItemByName(menuFlyout, "AddActionMenuItem").Visibility = Microsoft.UI.Xaml.Visibility.Visible;
            FindMenuItemByName(menuFlyout, "DeleteWorkflowMenuItem").Visibility = Microsoft.UI.Xaml.Visibility.Visible;
        }
        else if (dataType == typeof(Train))
        {
            FindMenuItemByName(menuFlyout, "AddLocomotiveMenuItem").Visibility = Microsoft.UI.Xaml.Visibility.Visible;
            FindMenuItemByName(menuFlyout, "AddWagonMenuItem").Visibility = Microsoft.UI.Xaml.Visibility.Visible;
            FindMenuItemByName(menuFlyout, "DeleteTrainMenuItem").Visibility = Microsoft.UI.Xaml.Visibility.Visible;
        }
        else if (dataType == typeof(Station))
        {
            FindMenuItemByName(menuFlyout, "MoveStationUpMenuItem").Visibility = Microsoft.UI.Xaml.Visibility.Visible;
            FindMenuItemByName(menuFlyout, "MoveStationDownMenuItem").Visibility = Microsoft.UI.Xaml.Visibility.Visible;
            FindMenuItemByName(menuFlyout, "DeleteStationMenuItem").Visibility = Microsoft.UI.Xaml.Visibility.Visible;
        }
    }

    private MenuFlyoutItem FindMenuItemByName(MenuFlyout menuFlyout, string name)
    {
        foreach (var item in menuFlyout.Items)
        {
            if (item is MenuFlyoutItem menuItem && menuItem.Name == name)
            {
                return menuItem;
            }
        }
        return new MenuFlyoutItem(); // Return dummy to avoid null checks
    }

    private static T? FindParent<T>(Microsoft.UI.Xaml.DependencyObject? child) where T : Microsoft.UI.Xaml.DependencyObject
    {
        if (child == null) return null;
        
        var parent = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(child);
        
        while (parent != null)
        {
            if (parent is T typedParent)
                return typedParent;
                
            parent = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(parent);
        }
        
        return null;
    }

    private void AddStation_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (ViewModel.CurrentSelectedNode?.DataContext is not Journey)
            return;

        System.Diagnostics.Debug.WriteLine("Add Station - Navigate to Editor page");
    }

    private void DeleteJourney_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (ViewModel.CurrentSelectedNode?.DataContext is not Journey)
            return;

        System.Diagnostics.Debug.WriteLine("Delete Journey - Will be implemented in Editor page");
    }

    private void AddAction_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (ViewModel.CurrentSelectedNode?.DataContext is not Workflow)
            return;

        System.Diagnostics.Debug.WriteLine("Add Action - Navigate to Editor page");
    }

    private void DeleteWorkflow_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (ViewModel.CurrentSelectedNode?.DataContext is not Workflow)
            return;

        System.Diagnostics.Debug.WriteLine("Delete Workflow - Will be implemented in Editor page");
    }

    private void AddLocomotive_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (ViewModel.CurrentSelectedNode?.DataContext is not Train)
            return;

        System.Diagnostics.Debug.WriteLine("Add Locomotive - Navigate to Editor page");
    }

    private void AddWagon_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (ViewModel.CurrentSelectedNode?.DataContext is not Train)
            return;

        System.Diagnostics.Debug.WriteLine("Add Wagon - Navigate to Editor page");
    }

    private void DeleteTrain_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (ViewModel.CurrentSelectedNode?.DataContext is not Train)
            return;

        System.Diagnostics.Debug.WriteLine("Delete Train - Will be implemented in Editor page");
    }

    private void DeleteStation_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (ViewModel.CurrentSelectedNode?.DataContext is not Station station)
            return;

        // Find parent Journey in Solution
        if (ViewModel.Solution?.Projects.Count > 0)
        {
            var project = ViewModel.Solution.Projects[0];
            foreach (var journey in project.Journeys)
            {
                if (journey.Stations.Contains(station))
                {
                    journey.Stations.Remove(station);
                    ViewModel.RefreshTreeView();
                    break;
                }
            }
        }
    }

    private async void TrackPower_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is Microsoft.UI.Xaml.Controls.AppBarToggleButton toggleButton)
        {
            // Execute the command with the new state
            if (CounterViewModel.SetTrackPowerCommand.CanExecute(toggleButton.IsChecked))
            {
                await CounterViewModel.SetTrackPowerCommand.ExecuteAsync(toggleButton.IsChecked);
            }
        }
    }
}