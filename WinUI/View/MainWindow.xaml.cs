// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Common.Configuration;

using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using Service;

using SharedUI.Interface;

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

using MainWindowViewModel = SharedUI.ViewModel.MainWindowViewModel;

public sealed partial class MainWindow
{
    #region Fields
    public MainWindowViewModel ViewModel { get; }

    private readonly NavigationService _navigationService;
    private readonly HealthCheckService _healthCheckService;
    private readonly IUiDispatcher _uiDispatcher;
    private readonly NavigationRegistry _navigationRegistry;
    private readonly NavigationItemFactory _navigationItemFactory;
    private readonly ISkinProvider _skinProvider;

    /// <summary>
    /// Application version string for display in TitleBar.
    /// </summary>
    public string AppVersion { get; } = GetAppVersion();
    #endregion

    private static string GetAppVersion()
    {
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        return version is not null ? $"v{version.Major}.{version.Minor}.{version.Build}" : "";
    }

    public MainWindow(
        MainWindowViewModel viewModel,
        NavigationService navigationService,
        HealthCheckService healthCheckService,
        IUiDispatcher uiDispatcher,
        IIoService ioService,
        NavigationRegistry navigationRegistry,
        AppSettings appSettings,
        ISkinProvider skinProvider)
    {
        ViewModel = viewModel;
        _navigationService = navigationService;
        _healthCheckService = healthCheckService;
        _uiDispatcher = uiDispatcher;
        _navigationRegistry = navigationRegistry;
        _navigationItemFactory = new NavigationItemFactory(appSettings);
        _skinProvider = skinProvider;

        InitializeComponent();

        // Set DataContext for Binding (needed for NavigationView.MenuItems which don't support x:Bind)
        RootGrid.DataContext = this;

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        // Set TitleBar subtitle with version
        AppTitleBar.Subtitle = $"flow  {AppVersion}";

        // Set taskbar/window icon
        var iconPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "mobaflow-icon.ico");
        if (System.IO.File.Exists(iconPath))
        {
            AppWindow.SetIcon(iconPath);
        }

        // Maximize window on startup and set minimum size
        var appWindow = AppWindow;
        if (appWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.PreferredMinimumWidth = 1024;
            presenter.PreferredMinimumHeight = 768;
            presenter.Maximize();
        }

        // Initialize IoService with WindowId (required before any file operations)
        if (ioService is IoService winUiIoService)
        {
            winUiIoService.SetWindowId(AppWindow.Id, Content.XamlRoot);
            Debug.WriteLine("[OK] IoService initialized with WindowId");
        }

        // Build navigation items from registry (replaces hardcoded XAML items)
        BuildNavigationFromRegistry();

        // Initialize NavigationService with ContentFrame (async initialization)
        _ = InitializeNavigationAsync();

        // Set first nav item as selected (Overview)
        MainNavigation.SelectedItem = MainNavigation.MenuItems.FirstOrDefault();

        // Subscribe to ViewModel events
        ViewModel.ExitApplicationRequested += OnExitApplicationRequested;
        ViewModel.NavigationRequested += OnNavigationRequested;
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        Closed += MainWindow_Closed;

        // Apply initial theme
        ApplyTheme(ViewModel.IsDarkMode);

        // Subscribe to health check events and start monitoring
        _healthCheckService.HealthStatusChanged += OnHealthStatusChanged;
        _healthCheckService.StartPeriodicChecks();

        // Initial health status
        ViewModel.UpdateHealthStatus(_healthCheckService.SpeechServiceStatus);
    }

    /// <summary>
    /// Builds navigation items dynamically from NavigationRegistry.
    /// Groups pages by Category with separators between groups.
    /// </summary>
    private void BuildNavigationFromRegistry()
    {
        // Clear existing items (remove hardcoded XAML items)
        MainNavigation.MenuItems.Clear();

        NavigationCategory? lastCategory = null;

        foreach (var page in _navigationRegistry.Pages)
        {
            // Add separator between categories
            if (lastCategory.HasValue && page.Category != lastCategory.Value)
            {
                MainNavigation.MenuItems.Add(_navigationItemFactory.CreateSeparator());
            }

            // Create and add navigation item
            var navItem = _navigationItemFactory.CreateItem(page);
            MainNavigation.MenuItems.Add(navItem);

            lastCategory = page.Category;
        }

        Debug.WriteLine($"[NAV] Built {MainNavigation.MenuItems.Count} navigation items from registry");
    }

    /// <summary>
    /// Initializes navigation asynchronously and navigates to Overview page.
    /// </summary>
    private async Task InitializeNavigationAsync()
    {
        await _navigationService.InitializeAsync(ContentFrame);
        await _navigationService.NavigateToOverviewAsync();

        // Set initial focus to Transaction Code TextBox for quick keyboard navigation
        TransactionCodeTextBox.Focus(FocusState.Programmatic);
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
        _skinProvider.IsDarkMode = isDarkMode;
    }

    private async void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        _healthCheckService.StopPeriodicChecks();

        // Auto-save solution before closing to prevent data loss
        await ViewModel.SaveSolutionInternalAsync();

        ViewModel.OnWindowClosing();
    }

    private static void OnExitApplicationRequested(object? sender, EventArgs e)
    {
        Application.Current.Exit();
    }

    private async void OnNavigationRequested(object? sender, string tag)
    {
        _ = sender; // Suppress unused parameter warning
        await _navigationService.NavigateToPageAsync(tag);

        // Update NavigationView selection to match the navigated page
        var navItem = MainNavigation.MenuItems.OfType<NavigationViewItem>()
            .FirstOrDefault(item => string.Equals(item.Tag as string, tag, StringComparison.OrdinalIgnoreCase));
        if (navItem != null)
        {
            MainNavigation.SelectedItem = navItem;
        }
    }

    private void OnHealthStatusChanged(object? sender, HealthStatusChangedEventArgs e)
    {
        // Update ViewModel on UI thread
        _uiDispatcher.InvokeOnUi(() => ViewModel.UpdateHealthStatus(e.StatusMessage));
    }

    private async void NavigationView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        _ = sender; // Suppress unused parameter warning
        if (args.InvokedItemContainer?.Tag is string tag)
        {
            await _navigationService.NavigateToPageAsync(tag);
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

    private void ThemeToggleSwitch_Toggled(object sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;
        ApplyTheme(ViewModel.IsDarkMode);
    }

    private void TransactionCodeTextBox_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        _ = sender; // Suppress unused parameter warning
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            ViewModel.ExecuteTransactionCodeCommand.Execute(null);
            e.Handled = true;
        }
    }
    #endregion
}