// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using Service;

using SharedUI.Interface;
using SharedUI.ViewModel;

using System;

using MainWindowViewModel = SharedUI.ViewModel.MainWindowViewModel;

public sealed partial class MainWindow
{
    public MainWindowViewModel ViewModel { get; }
    public CounterViewModel CounterViewModel { get; }

#pragma warning disable CS8618 // Field is initialized in constructor via DI
    private readonly HealthCheckService _healthCheckService;
#pragma warning restore CS8618
    private readonly IUiDispatcher _uiDispatcher;
    private readonly IServiceProvider _serviceProvider;

    public MainWindow(
        MainWindowViewModel viewModel,
        CounterViewModel counterViewModel,
        HealthCheckService healthCheckService,
        IUiDispatcher uiDispatcher,
        IServiceProvider serviceProvider)
    {
        ViewModel = viewModel;
        CounterViewModel = counterViewModel;

        _healthCheckService = healthCheckService;
        _uiDispatcher = uiDispatcher;
        _serviceProvider = serviceProvider;

        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        // Initialize IoService with WindowId (required before any file operations)
        var ioService = ServiceProviderServiceExtensions.GetRequiredService<IIoService>(
            ((App)Application.Current).Services);

        if (ioService is IoService winUiIoService)
        {
            winUiIoService.SetWindowId(this.AppWindow.Id, this.Content.XamlRoot);
            System.Diagnostics.Debug.WriteLine(" IoService initialized with WindowId");
        }

        // Set first nav item as selected (Overview)
        MainNavigation.SelectedItem = MainNavigation.MenuItems[0]; // Overview

        ViewModel.ExitApplicationRequested += OnExitApplicationRequested;
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        Closed += MainWindow_Closed;

        // Apply initial theme
        ApplyTheme(ViewModel.IsDarkMode);

        // Subscribe to health check events
        _healthCheckService.HealthStatusChanged += OnHealthStatusChanged;
        _healthCheckService.StartPeriodicChecks();

        // Initial status display
        UpdateHealthStatus(_healthCheckService.SpeechServiceStatus);

        // Navigate to Overview page on startup
        NavigateToOverview();
    }

    private void NavigateToOverview()
    {
        try
        {
            // ✅ DI: GetRequiredService resolves dependencies automatically
            var overviewPage = _serviceProvider.GetRequiredService<OverviewPage>();
            ContentFrame.Content = overviewPage;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Navigation to Overview failed: {ex.Message}");
        }
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.IsZ21Connected))
        {
            Z21StatusIcon.Glyph = ViewModel.IsZ21Connected ? "\uE8EB" : "\uF384";
        }
        else if (e.PropertyName == nameof(MainWindowViewModel.IsDarkMode))
        {
            ApplyTheme(ViewModel.IsDarkMode);
        }
    }

    private void ApplyTheme(bool isDarkMode)
    {
        RootGrid.RequestedTheme = isDarkMode ? ElementTheme.Dark : ElementTheme.Light;
    }

    private void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        _healthCheckService?.StopPeriodicChecks();
        ViewModel.OnWindowClosing();
    }

    private static void OnExitApplicationRequested(object? sender, EventArgs e)
    {
        Application.Current.Exit();
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
        ToolTipService.SetToolTip(SpeechHealthPanel, statusMessage);

        // Update icon and color based on health status
        if (statusMessage.Contains("Ready"))
        {
            SpeechHealthIcon.Glyph = "\uE930"; // Checkmark circle
            SpeechHealthIcon.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                Microsoft.UI.Colors.Green);
        }
        else if ( statusMessage.Contains("Not Configured"))
        {
            SpeechHealthIcon.Glyph = "\uE7BA"; // Warning
            SpeechHealthIcon.Foreground = Application.Current.Resources["SystemFillColorCautionBrush"] as Microsoft.UI.Xaml.Media.Brush;
        }
        else if (statusMessage.Contains("Failed"))
        {
            SpeechHealthIcon.Glyph = "\uE711"; // Error
            SpeechHealthIcon.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                Microsoft.UI.Colors.Red);
        }
        else // Initializing
        {
            SpeechHealthIcon.Glyph = "\uE946"; // Sync
            SpeechHealthIcon.Foreground = Application.Current.Resources["SystemFillColorCautionBrush"] as Microsoft.UI.Xaml.Media.Brush;
        }
    }

    private void NavigationView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.InvokedItemContainer?.Tag is not string tag) return;

        switch (tag)
        {
            case "overview":
                NavigateToOverview();
                break;

            case "solution":
                var solutionPage = _serviceProvider.GetRequiredService<SolutionPage>();
                ContentFrame.Content = solutionPage;
                break;

            case "journeys":
                var journeysPage = _serviceProvider.GetRequiredService<JourneysPage>();
                ContentFrame.Content = journeysPage;
                break;

            case "workflows":
                var workflowsPage = _serviceProvider.GetRequiredService<WorkflowsPage>();
                ContentFrame.Content = workflowsPage;
                break;

            case "settings":
                var settingsPage = _serviceProvider.GetRequiredService<SettingsPage>();
                ContentFrame.Content = settingsPage;
                break;
        }
    }

    // Minimal event handler - delegates to ViewModel Command (XAML limitation for AppBarToggleButton)
    private void TrackPower_Click(object sender, RoutedEventArgs e)
    {
        if (sender is AppBarToggleButton toggleButton)
        {
            // Simply execute command with current state - no business logic here
            CounterViewModel.SetTrackPowerCommand.Execute(toggleButton.IsChecked);
        }
    }
}
