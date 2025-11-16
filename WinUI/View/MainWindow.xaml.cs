namespace Moba.WinUI.View;

using Microsoft.UI.Xaml.Controls;
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
}