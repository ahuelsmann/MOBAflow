// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Common.Configuration;
using Common.Extension;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Service;
using SharedUI.Interface;
using System.ComponentModel;
using System.Diagnostics;
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
    private readonly MainWindowNavigationBootstrapper _navigationBootstrapper;
    private readonly ILogger<MainWindow> _logger;
    private bool _isClosing;
    private bool _isShutdownInProgress;
    private bool _deferredUiStartupStarted;
    private bool _deferredUiStartupCompleted;

    // NEU: Window Activation Service für InputActivationListener
    private readonly WindowActivationService _windowActivationService;

    private const double Z21TrackPowerIconOpacityConnected = 1.0;
    private const double Z21TrackPowerIconOpacityDisconnected = 0.4;

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
        RestApiStatusService restApiStatusService,
        RestApiProcessService restApiProcessService,
        ILogger<WindowActivationService> windowActivationLogger,
        ILogger<MainWindow> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
        try
        {
            var timer = Stopwatch.StartNew();
            ViewModel = viewModel;
            _navigationService = navigationService;
            _uiDispatcher = uiDispatcher;
            _restApiStatusService = restApiStatusService;
            _restApiProcessService = restApiProcessService;
            _pages = pages;
            var navigationItemFactory = new NavigationItemFactory(appSettings);
            _navigationBootstrapper = new MainWindowNavigationBootstrapper(
                _navigationService,
                _pages,
                navigationItemFactory);

            InitializeComponent();
            _logger.LogInformation("[Startup] MainWindow XAML initialized in {ElapsedMs}ms", timer.ElapsedMilliseconds);

            // NEU: Window Activation Service initialisieren
            _windowActivationService = new WindowActivationService(AppWindow, windowActivationLogger);
            SubscribeWindowActivationEvents();

            ConfigureWindowChrome();
            ConfigureWindowIconAndSizing();
            InitializeIoService(ioService);
            SubscribeWindowEvents();
            ApplyTheme(ViewModel.IsDarkMode);
            ApplyZ21TrackPowerIconConnectedState();
            _logger.LogInformation("[Startup] MainWindow constructor completed in {ElapsedMs}ms", timer.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "MainWindow constructor failed");
            throw;
        }
    }

    public void ScheduleDeferredUiStartup()
    {
        if (_deferredUiStartupStarted)
        {
            return;
        }

        _deferredUiStartupStarted = true;
        if (!DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, RunDeferredUiStartup))
        {
            RunDeferredUiStartup();
        }
    }

    private void RunDeferredUiStartup()
    {
        var timer = Stopwatch.StartNew();
        InitializeNavigation();
        _restApiStatusService.Start();
        _deferredUiStartupCompleted = true;
        _logger.LogInformation("[Startup] Deferred MainWindow UI startup completed in {ElapsedMs}ms", timer.ElapsedMilliseconds);
    }

    private void ConfigureWindowChrome()
    {
        // Set DataContext for Binding (needed for NavigationView.MenuItems which don't support x:Bind)
        RootGrid.DataContext = this;
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        // Windows App SDK 2.0 TitleBar-Erweiterungen
        ConfigureTitleBarOptions();
    }

    private void ConfigureTitleBarOptions()
    {
        var titleBar = AppWindow.TitleBar;

        titleBar.PreferredHeightOption = TitleBarHeightOption.Standard;

        // Icon mit System-Menü anzeigen
        titleBar.IconShowOptions = IconShowOptions.ShowIconAndSystemMenu;

        // Hintergrundfarbe auf transparent für Mica-Effekt
        titleBar.BackgroundColor = Microsoft.UI.Colors.Transparent;
        titleBar.InactiveBackgroundColor = Microsoft.UI.Colors.Transparent;
        titleBar.ButtonBackgroundColor = Microsoft.UI.Colors.Transparent;
        titleBar.ButtonInactiveBackgroundColor = Microsoft.UI.Colors.Transparent;
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

    /// <summary>
    /// Updates track-power status icon opacity: full when Z21 is connected, dimmed when not.
    /// </summary>
    private void ApplyZ21TrackPowerIconConnectedState()
    {
        if (_isClosing)
        {
            return;
        }

        Z21StatusIcon.Opacity = ViewModel.IsConnected
            ? Z21TrackPowerIconOpacityConnected
            : Z21TrackPowerIconOpacityDisconnected;
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

    private void SubscribeWindowActivationEvents()
    {
        _windowActivationService.ActivationStateChanged += OnActivationStateChanged;
    }

    private void OnActivationStateChanged(object? sender, InputActivationStateChangedEventArgs e)
    {
        if (e.NewState == InputActivationState.Activated)
        {
            // Z21-Status refreshen wenn Fenster aktiv wird
            ViewModel?.RefreshZ21StatusCommand?.Execute(null);
            if (_deferredUiStartupCompleted)
            {
                _restApiStatusService?.ResumePolling();
            }
        }
        else if (e.NewState == InputActivationState.Deactivated)
        {
            // SignalR/Polling pausieren bei Inaktivität
            if (_deferredUiStartupCompleted)
            {
                _restApiStatusService?.PausePolling();
            }
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
        });
    }

    /// <summary>
    /// Builds navigation items dynamically from discovered pages.
    /// Groups pages by Category with separators between groups.
    /// </summary>
    private void BuildNavigationFromRegistry()
    {
        _navigationBootstrapper.BuildMenu(MainNavigation);
    }

    /// <summary>
    /// Initializes navigation asynchronously and navigates to Overview page.
    /// </summary>
    private async Task InitializeNavigationAsync()
    {
        await _navigationBootstrapper.InitializeAsync(
            ContentFrame,
            pageTag => ViewModel.UpdateActivePhotoAssignmentPageTag(pageTag));
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
            ApplyZ21TrackPowerIconConnectedState();
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

        // Do not gate on XamlRoot: during the window constructor it is often still null.
        RootGrid.RequestedTheme = isDarkMode ? ElementTheme.Dark : ElementTheme.Light;
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

        // NEU: Window Activation Service cleanup
        _windowActivationService?.Dispose();
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
        _uiDispatcher.InvokeOnUi(() =>
        {
            ViewModel.UpdateHealthStatus(e.StatusMessage);
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

        // Ignore programmatic UI synchronization updates from runtime snapshots.
        if (toggleSwitch.IsOn == ViewModel.IsTrackPowerOn)
        {
            return;
        }

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