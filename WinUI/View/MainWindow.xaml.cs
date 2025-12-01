// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

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
        _healthCheckService.HealthStatusChanged += OnHealthStatusChanged;
        _healthCheckService.StartPeriodicChecks();

        // Initial status display
        UpdateHealthStatus(_healthCheckService.SpeechServiceStatus);

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
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Failed to navigate to Overview: {ex.Message}");
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

    // TreeView event handlers removed - no longer needed

    // TreeView drag & drop handlers removed - reordering now handled in EditorPage

    // Station move handlers removed - now handled in EditorPage

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
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Failed to navigate to ProjectConfigurationPage: {ex.Message}");

                    _ = ShowErrorDialogAsync("Navigation Error", $"Failed to open Project Configuration: {ex.Message}");
                }
                break;

            case "settings":
                // Navigate to SettingsPage (application-wide settings)
                try
                {
                    var settingsViewModel = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<SharedUI.ViewModel.SettingsPageViewModel>(
                        ((App)Microsoft.UI.Xaml.Application.Current).Services);

                    var settingsPage = new SettingsPage(settingsViewModel);
                    ContentFrame.Content = settingsPage;

                    System.Diagnostics.Debug.WriteLine($"✅ Navigated to SettingsPage");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Failed to navigate to SettingsPage: {ex.Message}");
                    _ = ShowErrorDialogAsync("Navigation Error", $"Failed to open Settings: {ex.Message}");
                }
                break;

                // Explorer removed - functionality moved to EditorPage
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

    // Context menu handlers removed - editing now done in EditorPage

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