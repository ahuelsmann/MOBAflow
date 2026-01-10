// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using Service;

using SharedUI.Interface;
using SharedUI.ViewModel;

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

using MainWindowViewModel = SharedUI.ViewModel.MainWindowViewModel;

public sealed partial class MainWindow
{
    #region Fields
    public MainWindowViewModel ViewModel { get; }
    public TrackPlanEditorViewModel TrackPlanEditorViewModel { get; }

    private readonly NavigationService _navigationService;
    private readonly HealthCheckService _healthCheckService;
    private readonly IUiDispatcher _uiDispatcher;
    private readonly NavigationRegistry _navigationRegistry;
    
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
        TrackPlanEditorViewModel trackPlanEditorViewModel,
        NavigationService navigationService,
        HealthCheckService healthCheckService,
        IUiDispatcher uiDispatcher,
            IIoService ioService,
            NavigationRegistry navigationRegistry)
        {
            ViewModel = viewModel;
            TrackPlanEditorViewModel = trackPlanEditorViewModel;
            _navigationService = navigationService;
            _healthCheckService = healthCheckService;
            _uiDispatcher = uiDispatcher;
            _navigationRegistry = navigationRegistry;

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
            Debug.WriteLine("ÃƒÂ¢Ã…â€œÃ¢â‚¬Â¦ IoService initialized with WindowId");
        }

        // Add plugin navigation items before initialization selects overview
        AddPluginNavigationItems();

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

    private void AddPluginNavigationItems()
    {
        // Find Help item - plugins should be inserted BEFORE Help/Info/Settings group
        var helpItem = MainNavigation.MenuItems.OfType<NavigationViewItem>()
            .FirstOrDefault(item => (item.Tag as string) == "help");
        
        if (helpItem is null)
        {
            return; // No help page found, cannot determine insertion point
        }

        var pluginPages = _navigationRegistry.Pages
            .Where(p => !string.Equals(p.Source, "Shell", StringComparison.OrdinalIgnoreCase))
            .ToList();
        
        if (pluginPages.Count == 0)
        {
            return; // No plugins to add
        }

        // Find the separator BEFORE Help (this is where we insert plugins)
        var helpIndex = MainNavigation.MenuItems.IndexOf(helpItem);
        var separatorBeforeHelpIndex = helpIndex > 0 && MainNavigation.MenuItems[helpIndex - 1] is NavigationViewItemSeparator
            ? helpIndex - 1
            : -1;

        // Insert position: before the separator that precedes Help (or before Help if no separator)
        var insertIndex = separatorBeforeHelpIndex != -1 ? separatorBeforeHelpIndex : helpIndex;

        // Add separator BEFORE plugins (between Monitor and first plugin)
        MainNavigation.MenuItems.Insert(insertIndex, new NavigationViewItemSeparator());
        insertIndex++;

        // Insert plugin pages
        foreach (var page in pluginPages)
        {
            var navItem = new NavigationViewItem
            {
                Content = page.Title,
                Tag = page.Tag
            };

            if (!string.IsNullOrWhiteSpace(page.IconGlyph))
            {
                navItem.Icon = new FontIcon { Glyph = page.IconGlyph };
            }

            MainNavigation.MenuItems.Insert(insertIndex, navItem);
            insertIndex++;
        }

        // The existing separator before Help will now be AFTER plugins
        // No need to add another separator - the one before Help already exists
    }

    /// <summary>
    /// Initializes navigation asynchronously and navigates to Overview page.
    /// </summary>
    private async Task InitializeNavigationAsync()
    {
        await _navigationService.InitializeAsync(ContentFrame);
        await _navigationService.NavigateToOverviewAsync();
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
        _uiDispatcher.InvokeOnUi(() =>
        {
            ViewModel.UpdateHealthStatus(e.StatusMessage);
        });
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
    #endregion
}