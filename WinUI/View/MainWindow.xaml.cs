// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

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
    public TrackPlanEditorViewModel TrackPlanEditorViewModel { get; }

    private readonly NavigationService _navigationService;
    private readonly HealthCheckService _healthCheckService;
    private readonly IUiDispatcher _uiDispatcher;
    #endregion

    public MainWindow(
        MainWindowViewModel viewModel,
        TrackPlanEditorViewModel trackPlanEditorViewModel,
        NavigationService navigationService,
        HealthCheckService healthCheckService,
        IUiDispatcher uiDispatcher,
        IIoService ioService)
    {
        ViewModel = viewModel;
        TrackPlanEditorViewModel = trackPlanEditorViewModel;
        _navigationService = navigationService;
        _healthCheckService = healthCheckService;
        _uiDispatcher = uiDispatcher;

        InitializeComponent();

        // Set DataContext for Binding (needed for NavigationView.MenuItems which don't support x:Bind)
        RootGrid.DataContext = this;

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
        if (e.PropertyName == nameof(MainWindowViewModel.IsConnected))
        {
            Z21StatusIcon.Glyph = ViewModel.IsConnected ? "\uE8EB" : "\uF384";
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

    private void TrackPower_Toggled(object sender, RoutedEventArgs e)
    {
        _ = e; // Suppress unused parameter warning
        if (sender is not ToggleSwitch toggleSwitch) return;

        // Execute track power command
        if (ViewModel.SetTrackPowerCommand.CanExecute(toggleSwitch.IsOn))
        {
            ViewModel.SetTrackPowerCommand.Execute(toggleSwitch.IsOn);
        }
    }
    #endregion
}