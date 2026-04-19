// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Common.Configuration;
using Common.Extension;
using Common.Navigation;

using Microsoft.Extensions.Logging;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using Service;

using SharedUI.Interface;

using System.ComponentModel;
using System.Reflection;

using MainWindowViewModel = SharedUI.ViewModel.MainWindowViewModel;

public sealed partial class MainWindow
{
    #region Fields
    public MainWindowViewModel ViewModel { get; }

    private readonly NavigationService _navigationService;
    private HealthCheckService? _healthCheckService;
    private readonly RestApiStatusService _restApiStatusService;
    private readonly RestApiProcessService _restApiProcessService;
    private readonly IUiDispatcher _uiDispatcher;
    private readonly List<PageMetadata> _pages;
    private readonly NavigationItemFactory _navigationItemFactory;
    private readonly ISkinProvider _skinProvider;
    private readonly ILogger<MainWindow> _logger;
    private bool _isClosing;
    private bool _isShutdownInProgress;

    /// <summary>
    /// Application version string for display in TitleBar.
    /// </summary>
    public string AppVersion { get; } = GetAppVersion();
    #endregion

    private static string GetAppVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var infoVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;

        if (!string.IsNullOrWhiteSpace(infoVersion))
        {
            // Nur den SemVer-Teil ohne Build-Metadaten anzeigen
            var semVer = infoVersion.Split('+')[0];
            return $"v{semVer}";
        }

        var version = assembly.GetName().Version;
        return version is not null ? $"v{version.Major}.{version.Minor}.{version.Build}" : "v0.0.0";
    }

    public MainWindow(
        MainWindowViewModel viewModel,
        NavigationService navigationService,
        IUiDispatcher uiDispatcher,
        IIoService ioService,
        List<PageMetadata> pages,
        AppSettings appSettings,
        ISkinProvider skinProvider,
        RestApiStatusService restApiStatusService,
        RestApiProcessService restApiProcessService,
        ILogger<MainWindow> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
        try
        {
            ViewModel = viewModel;
            _navigationService = navigationService;
            _uiDispatcher = uiDispatcher;
            _restApiStatusService = restApiStatusService;
            _restApiProcessService = restApiProcessService;
            _pages = pages;
            _navigationItemFactory = new NavigationItemFactory(appSettings);
            _skinProvider = skinProvider;

            InitializeComponent();

            ConfigureWindowChrome();
            ConfigureWindowIconAndSizing();
            InitializeIoService(ioService);
            InitializeNavigation();
            SubscribeWindowEvents();
            ApplyTheme(ViewModel.IsDarkMode);

            _restApiStatusService.Start();
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "MainWindow constructor failed");
            throw;
        }
    }

    private void ConfigureWindowChrome()
    {
        // Set DataContext for Binding (needed for NavigationView.MenuItems which don't support x:Bind)
        RootGrid.DataContext = this;
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        AppTitleBar.Subtitle = $"flow  {AppVersion}";
    }

    private void ConfigureWindowIconAndSizing()
    {
        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "mobaflow-icon.ico");
        if (File.Exists(iconPath))
        {
            AppWindow.SetIcon(iconPath);
        }

        var appWindow = AppWindow;
        if (appWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.PreferredMinimumWidth = 1024;
            presenter.PreferredMinimumHeight = 768;
            presenter.Maximize();
        }
    }

    private void InitializeIoService(IIoService ioService)
    {
        if (ioService is not IoService winUiIoService)
        {
            return;
        }

        winUiIoService.SetWindowId(AppWindow.Id, Content.XamlRoot);
    }

    private void InitializeNavigation()
    {
        BuildNavigationFromRegistry();
        InitializeNavigationAsync().Observe(ex => _logger.LogWarning(ex, "Initialize navigation failed"));
        MainNavigation.SelectedItem = MainNavigation.MenuItems.FirstOrDefault();
    }

    private void SubscribeWindowEvents()
    {
        ViewModel.ExitApplicationRequested += OnExitApplicationRequested;
        ViewModel.NavigationRequested += OnNavigationRequested;
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        _navigationService.Navigated += OnNavigationServiceNavigated;
        Closed += MainWindow_Closed;

        if (AppWindow is not null)
        {
            AppWindow.Closing += OnAppWindowClosing;
        }
    }

    /// <summary>
    /// Initializes health checks after the window is visible.
    /// </summary>
    public void InitializeHealthChecks(HealthCheckService healthCheckService)
    {
        ArgumentNullException.ThrowIfNull(healthCheckService);

        if (_healthCheckService != null)
        {
            return;
        }

        _healthCheckService = healthCheckService;
        _healthCheckService.HealthStatusChanged += OnHealthStatusChanged;
        _healthCheckService.StartPeriodicChecks();

        _uiDispatcher.InvokeOnUi(() =>
        {
            ViewModel.UpdateHealthStatus(_healthCheckService.SpeechServiceStatus);
            ViewModel.UpdateVisionHealthStatus(_healthCheckService.VisionServiceStatus);
        });
    }

    /// <summary>
    /// Builds navigation items dynamically from discovered pages.
    /// Groups pages by Category with separators between groups.
    /// </summary>
    private void BuildNavigationFromRegistry()
    {
        // Clear existing items (remove hardcoded XAML items)
        MainNavigation.MenuItems.Clear();

        NavigationCategory? lastCategory = null;

        foreach (var page in _pages)
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
    }

    /// <summary>
    /// Initializes navigation asynchronously and navigates to Overview page.
    /// </summary>
    private async Task InitializeNavigationAsync()
    {
        await _navigationService.InitializeAsync(ContentFrame);
        await _navigationService.NavigateToOverviewAsync();
        ViewModel.UpdateActivePhotoAssignmentPageTag(_navigationService.CurrentPageTag);
    }

    #region Event Handlers
    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_isClosing)
        {
            return;
        }

        if (e.PropertyName == nameof(MainWindowViewModel.IsConnected))
        {
            if (Z21StatusIcon.XamlRoot is null)
            {
                return;
            }

            Z21StatusIcon.Glyph = ViewModel.IsConnected ? "\uE8EB" : "\uF384";
        }
        else if (e.PropertyName == nameof(MainWindowViewModel.IsDarkMode))
        {
            ApplyTheme(ViewModel.IsDarkMode);
        }
    }

    private void ApplyTheme(bool isDarkMode)
    {
        if (_isClosing)
        {
            return;
        }

        // Do not gate on XamlRoot: during the window constructor it is often still null, which
        // previously skipped syncing ISkinProvider — pages (TrainControl, SignalBox) then forced
        // ElementTheme.Light while the shell appeared dark.
        RootGrid.RequestedTheme = isDarkMode ? ElementTheme.Dark : ElementTheme.Light;
        _skinProvider.IsDarkMode = isDarkMode;
    }

    private void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        _ = sender;
        _ = args;
        HandleMainWindowClosedAsync().Observe(ex => _logger.LogWarning(ex, "Main window closed handler failed"));
    }

    private async Task HandleMainWindowClosedAsync()
    {
        if (_isClosing)
        {
            return;
        }

        _isClosing = true;
        UnsubscribeWindowEvents();

        // 1) Stop status polling and cancel any in-flight HTTP requests
        _restApiStatusService.Stop();

        // 2) Disconnect PhotoHub (SignalR) and dispose status service BEFORE stopping the RestApi process,
        //    so SignalR shuts down cleanly and does not start reconnect timers after the server is killed.
        try
        {
            _restApiStatusService.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "RestApiStatusService.Dispose failed");
        }

        // 3) Stop RestApi child process so it doesn't outlive the app
        try
        {
            _restApiProcessService.Stop();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "RestApiProcessService.Stop failed");
        }

        if (_healthCheckService != null)
        {
            _healthCheckService.HealthStatusChanged -= OnHealthStatusChanged;
            _healthCheckService.StopPeriodicChecks();
            try
            {
                _healthCheckService.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "HealthCheckService.Dispose failed");
            }
        }

        try
        {
            await ViewModel.PrepareForShutdownAsync();
            DetachWindowBindings();

            // Auto-save solution before closing to prevent data loss
            await ViewModel.SaveSolutionInternalAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Shutdown workflow failed");
        }

        // WinUI 3 does not exit the process when the window is closed; we must exit explicitly.
        Application.Current.Exit();
    }

    private void UnsubscribeWindowEvents()
    {
        ViewModel.ExitApplicationRequested -= OnExitApplicationRequested;
        ViewModel.NavigationRequested -= OnNavigationRequested;
        ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
        _navigationService.Navigated -= OnNavigationServiceNavigated;
        Closed -= MainWindow_Closed;
    }

    private void OnNavigationServiceNavigated(object? sender, SharedUI.Shell.NavigationEventArgs e)
    {
        _ = sender;
        ViewModel.UpdateActivePhotoAssignmentPageTag(e.PageTag);
    }

    private void DetachWindowBindings()
    {
        Bindings?.StopTracking();

        if (RootGrid is not null)
        {
            RootGrid.DataContext = null;
        }
    }

    private static void OnExitApplicationRequested(object? sender, EventArgs e)
    {
        Application.Current.Exit();
    }

    private void OnNavigationRequested(object? sender, string tag)
    {
        _ = sender; // Suppress unused parameter warning
        HandleNavigationRequestedAsync(tag).Observe(ex => _logger.LogWarning(ex, "Navigation request failed"));
    }

    private async Task HandleNavigationRequestedAsync(string tag)
    {
        try
        {
            await _navigationService.NavigateToPageAsync(tag);

            // Update NavigationView selection to match the navigated page
            var navItem = MainNavigation.MenuItems.OfType<NavigationViewItem>()
                .FirstOrDefault(item => string.Equals(item.Tag as string, tag, StringComparison.OrdinalIgnoreCase));
            if (navItem != null)
            {
                MainNavigation.SelectedItem = navItem;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Navigation request failed for {Tag}", tag);
        }
    }

    private void OnHealthStatusChanged(object? sender, HealthStatusChangedEventArgs e)
    {
        // Route to the right VM update method based on the reporting service.
        _uiDispatcher.InvokeOnUi(() =>
        {
            if (string.Equals(e.ServiceName, "AzureVision", StringComparison.Ordinal))
            {
                ViewModel.UpdateVisionHealthStatus(e.StatusMessage);
            }
            else
            {
                ViewModel.UpdateHealthStatus(e.StatusMessage);
            }
        });
    }

    private void NavigationView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        _ = sender; // Suppress unused parameter warning
        HandleNavigationViewItemInvokedAsync(args).Observe(ex => _logger.LogWarning(ex, "Navigation item invoke failed"));
    }

    private async Task HandleNavigationViewItemInvokedAsync(NavigationViewItemInvokedEventArgs args)
    {
        try
        {
            if (args.InvokedItemContainer?.Tag is string tag)
            {
                await _navigationService.NavigateToPageAsync(tag);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Navigation item invoke failed");
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

    // Ensure only one AppWindow.Closing subscription exists and it is inside the MainWindow constructor.
    private void OnAppWindowClosing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        if (_isShutdownInProgress)
        {
            return;
        }

        // Cancel the synchronous closing to allow async shutdown logic
        args.Cancel = true;
        _isShutdownInProgress = true;
        HandleAppWindowClosingAsync(sender).Observe(ex => _logger.LogWarning(ex, "App window closing failed"));
    }

    private async Task HandleAppWindowClosingAsync(AppWindow sender)
    {
        try
        {
            await ViewModel.PrepareForShutdownAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AppWindow closing workflow failed");
        }
        finally
        {
            sender.Closing -= OnAppWindowClosing;
            Application.Current.Exit();
        }
    }
    #endregion
}