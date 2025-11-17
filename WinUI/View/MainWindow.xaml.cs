namespace Moba.WinUI.View;

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Moba.SharedUI.ViewModel.WinUI;
using Moba.WinUI.Service;

public sealed partial class MainWindow
{
    public MainWindowViewModel ViewModel { get; }
    private readonly HealthCheckService? _healthCheckService;

    public MainWindow(MainWindowViewModel viewModel, HealthCheckService healthCheckService)
    {
        ViewModel = viewModel;
        _healthCheckService = healthCheckService;

        InitializeComponent();

        ViewModel.ExitApplicationRequested += OnExitApplicationRequested;
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        Closed += MainWindow_Closed;

        // Subscribe to health check events
        if (_healthCheckService != null)
        {
            _healthCheckService.HealthStatusChanged += OnHealthStatusChanged;
            _healthCheckService.StartPeriodicChecks();
            
            // Initial status display
            UpdateHealthStatus(_healthCheckService.SpeechServiceStatus, _healthCheckService.IsSpeechServiceHealthy);
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
        // Find which Journey was reordered and trigger save
        
        if (args.Items.Count > 0 && args.Items[0] is SharedUI.ViewModel.TreeNodeViewModel draggedNode)
        {
            // If a Station was dragged, find its parent Journey
            if (draggedNode.DataContext is SharedUI.ViewModel.StationViewModel)
            {
                var journeyNode = FindParentJourneyNode(draggedNode);
                if (journeyNode?.DataContext is SharedUI.ViewModel.JourneyViewModel journeyVM)
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

    private void MoveStationUp_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (ViewModel.CurrentSelectedNode?.DataContext is not SharedUI.ViewModel.StationViewModel stationVM)
            return;

        // Find parent JourneyViewModel
        var journeyNode = FindParentJourneyNode(ViewModel.CurrentSelectedNode);
        if (journeyNode?.DataContext is not SharedUI.ViewModel.JourneyViewModel journeyVM)
            return;

        var currentIndex = journeyVM.Stations.IndexOf(stationVM);
        if (currentIndex > 0)
        {
            // Move in ViewModel
            journeyVM.Stations.Move(currentIndex, currentIndex - 1);

            // Trigger reorder logic
            journeyVM.StationsReorderedCommand.Execute(null);
        }
    }

    private void MoveStationDown_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (ViewModel.CurrentSelectedNode?.DataContext is not SharedUI.ViewModel.StationViewModel stationVM)
            return;

        // Find parent JourneyViewModel
        var journeyNode = FindParentJourneyNode(ViewModel.CurrentSelectedNode);
        if (journeyNode?.DataContext is not SharedUI.ViewModel.JourneyViewModel journeyVM)
            return;

        var currentIndex = journeyVM.Stations.IndexOf(stationVM);
        if (currentIndex < journeyVM.Stations.Count - 1)
        {
            // Move in ViewModel
            journeyVM.Stations.Move(currentIndex, currentIndex + 1);

            // Trigger reorder logic
            journeyVM.StationsReorderedCommand.Execute(null);
        }
    }

    private SharedUI.ViewModel.TreeNodeViewModel? FindParentJourneyNode(SharedUI.ViewModel.TreeNodeViewModel? node)
    {
        if (node == null) return null;

        // Search in entire tree for parent
        return FindJourneyNodeRecursive(ViewModel.TreeNodes, node);
    }

    private SharedUI.ViewModel.TreeNodeViewModel? FindJourneyNodeRecursive(
        System.Collections.ObjectModel.ObservableCollection<SharedUI.ViewModel.TreeNodeViewModel> nodes, 
        SharedUI.ViewModel.TreeNodeViewModel targetNode)
    {
        foreach (var node in nodes)
        {
            // Check if this node's children contain the target
            if (node.Children.Contains(targetNode) && node.DataContext is SharedUI.ViewModel.JourneyViewModel)
            {
                return node;
            }

            // Recurse into children
            var result = FindJourneyNodeRecursive(node.Children, targetNode);
            if (result != null) return result;
        }

        return null;
    }

    private void OnHealthStatusChanged(object? sender, HealthStatusChangedEventArgs e)
    {
        // Update UI on dispatcher thread
        DispatcherQueue.TryEnqueue(() =>
        {
            UpdateHealthStatus(e.StatusMessage, e.IsHealthy);
        });
    }

    private void UpdateHealthStatus(string statusMessage, bool isHealthy)
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
}