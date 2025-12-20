// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

using Service;

using SharedUI.Interface;
using SharedUI.ViewModel;

using System.ComponentModel;
using System.Diagnostics;

using MainWindowViewModel = SharedUI.ViewModel.MainWindowViewModel;

public sealed partial class MainWindow
{
    #region Fields
    public MainWindowViewModel ViewModel { get; }
    public CounterViewModel CounterViewModel { get; }
    public TrackPlanEditorViewModel TrackPlanEditorViewModel { get; }

    private readonly NavigationService _navigationService;
    private readonly HealthCheckService _healthCheckService;
    private readonly IUiDispatcher _uiDispatcher;
    #endregion

    public MainWindow(
        MainWindowViewModel viewModel,
        CounterViewModel counterViewModel,
        TrackPlanEditorViewModel trackPlanEditorViewModel,
        NavigationService navigationService,
        HealthCheckService healthCheckService,
        IUiDispatcher uiDispatcher,
        IIoService ioService)
    {
        ViewModel = viewModel;
        CounterViewModel = counterViewModel;
        TrackPlanEditorViewModel = trackPlanEditorViewModel;
        _navigationService = navigationService;
        _healthCheckService = healthCheckService;
        _uiDispatcher = uiDispatcher;

        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        // Maximize window on startup
        var appWindow = AppWindow;
        if (appWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.Maximize();
        }

        // Initialize IoService with WindowId (required before any file operations)
        if (ioService is IoService winUiIoService)
        {
            winUiIoService.SetWindowId(AppWindow.Id, Content.XamlRoot);
            Debug.WriteLine("✅ IoService initialized with WindowId");
        }

        // Initialize NavigationService with ContentFrame
        _navigationService.Initialize(ContentFrame);

        // Set first nav item as selected (Overview)
        MainNavigation.SelectedItem = MainNavigation.MenuItems[0];

        // Subscribe to ViewModel events
        ViewModel.ExitApplicationRequested += OnExitApplicationRequested;
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        Closed += MainWindow_Closed;

        // Apply initial theme
        ApplyTheme(ViewModel.IsDarkMode);

        // Subscribe to health check events and start monitoring
        _healthCheckService.HealthStatusChanged += OnHealthStatusChanged;
        _healthCheckService.StartPeriodicChecks();

        // Initial health status
        ViewModel.UpdateHealthStatus(_healthCheckService.SpeechServiceStatus);

        // Navigate to Overview page on startup
        _navigationService.NavigateToOverview();
    }

    #region Event Handlers
    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
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
        _healthCheckService.StopPeriodicChecks();
        ViewModel.OnWindowClosing();
    }

    private static void OnExitApplicationRequested(object? sender, EventArgs e)
    {
        Application.Current.Exit();
    }

    private void OnHealthStatusChanged(object? sender, HealthStatusChangedEventArgs e)
    {
        // Update ViewModel on UI thread
        _uiDispatcher.InvokeOnUi(() =>
        {
            ViewModel.UpdateHealthStatus(e.StatusMessage);
        });
    }

    private void NavigationView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.InvokedItemContainer?.Tag is string tag)
        {
            _navigationService.NavigateToPage(tag);
        }
    }

    // Minimal event handler - delegates to ViewModel Command
    private void TrackPower_Click(object sender, RoutedEventArgs e)
    {
        if (sender is AppBarToggleButton toggleButton)
        {
            // Simply execute command with current state - no business logic here
            CounterViewModel.SetTrackPowerCommand.Execute(toggleButton.IsChecked);
        }
    }

    private void ToggleSwitch_Toggled(object sender, RoutedEventArgs e)
    {
        ToggleSwitch toggleSwitch = sender as ToggleSwitch;
        if (toggleSwitch != null)
        {
            if (toggleSwitch.IsOn == true)
            {
                CounterViewModel.ConnectCommand.Execute(toggleSwitch.IsOn);
            }
            else
            {
                CounterViewModel.DisconnectCommand.Execute(toggleSwitch.IsOn);
            }
        }
    }
    #endregion
}
