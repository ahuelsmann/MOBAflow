namespace Moba.WinUI.View;

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Moba.SharedUI.ViewModel;
using Moba.WinUI.Service;
using MainWindowViewModel = Moba.SharedUI.ViewModel.WinUI.MainWindowViewModel;

public sealed partial class MainWindow
{
    public MainWindowViewModel ViewModel { get; }
    private readonly HealthCheckService? _healthCheckService;

    public MainWindow(MainWindowViewModel viewModel, HealthCheckService healthCheckService)
    {
        ViewModel = viewModel;
        _healthCheckService = healthCheckService;

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
        
        if (args.Items.Count > 0 && 
            args.Items[0] is SharedUI.ViewModel.TreeNodeViewModel draggedNode &&
            draggedNode.DataContext is SharedUI.ViewModel.StationViewModel)
        {
            // If a Station was dragged, find its parent Journey
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
                }
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
        // Update UI on dispatcher thread
        DispatcherQueue.TryEnqueue(() =>
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
            case Windows.System.VirtualKey.Z:
                if (ViewModel.UndoCommand.CanExecute(null))
                {
                    ViewModel.UndoCommand.Execute(null);
                    handled = true;
                }
                break;

            case Windows.System.VirtualKey.Y:
                if (ViewModel.RedoCommand.CanExecute(null))
                {
                    ViewModel.RedoCommand.Execute(null);
                    handled = true;
                }
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

        // Debug: Log all navigation attempts
        System.Diagnostics.Debug.WriteLine($"NavigationView_ItemInvoked called with tag: {tag}");
        System.Diagnostics.Debug.WriteLine($"ViewModel.Solution is null: {ViewModel.Solution == null}");
        System.Diagnostics.Debug.WriteLine($"ViewModel.Solution.Projects.Count: {ViewModel.Solution?.Projects.Count ?? -1}");

        switch (tag)
        {
            case "overview":
                // Navigate to OverviewPage (Lap Counter Dashboard)
                NavigateToOverview();
                break;

            case "configuration":
                // Navigate to ProjectConfigurationPage
                if (ViewModel.Solution != null && ViewModel.Solution.Projects.Count > 0)
                {
                    var project = ViewModel.Solution.Projects[0];
                    var configViewModel = new SharedUI.ViewModel.ProjectConfigurationPageViewModel(project);
                    
                    // Debug output
                    System.Diagnostics.Debug.WriteLine($"✅ Navigating to ProjectConfigurationPage with {project.Journeys.Count} journeys, {project.Workflows.Count} workflows, {project.Trains.Count} trains");
                    
                    ContentFrame.Navigate(typeof(ProjectConfigurationPage), configViewModel);
                    ContentFrame.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                    LegacyContent.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
                }
                else
                {
                    // Debug: No solution loaded
                    System.Diagnostics.Debug.WriteLine($"❌ Cannot navigate: Solution={ViewModel.Solution != null}, Projects={ViewModel.Solution?.Projects.Count ?? 0}");
                    
                    // Show InfoBar or ContentDialog to inform user
                    _ = ShowNoSolutionDialogAsync();
                }
                break;

            case "explorer":
                // Show legacy content (TreeView + PropertyGrid)
                ContentFrame.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
                LegacyContent.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
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
}