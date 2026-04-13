// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI;

using Common.Configuration;
using Moba.Common.Extension;

using Microsoft.Extensions.Logging;

using SharedUI.Interface;
using SharedUI.ViewModel;

using System.ComponentModel;

// ReSharper disable once PartialTypeWithSinglePart
public partial class MainPage
{
    public MauiViewModel ViewModel { get; }
    private readonly ISettingsService _settingsService;
    private readonly AppSettings _settings;
    private readonly ILogger<MainPage>? _logger;
    private CancellationTokenSource? _pulseAnimationCts;
    private Task? _viewModelInitializationTask;

    public MainPage(
        MauiViewModel viewModel,
        ISettingsService settingsService,
        AppSettings settings,
        ILogger<MainPage>? logger = null)
    {
        ViewModel = viewModel;
        _settingsService = settingsService;
        _settings = settings;
        _logger = logger;
        BindingContext = ViewModel;
        InitializeComponent();

        // Set initial theme switch state based on saved preference
        // Switch ON = Light theme, Switch OFF = Dark theme
        var isDarkMode = _settings.Application.IsDarkMode;
        var useSystemTheme = _settings.Application.UseSystemTheme;

        bool isLightTheme;
        if (useSystemTheme)
        {
            isLightTheme = Application.Current?.RequestedTheme == AppTheme.Light;
        }
        else
        {
            isLightTheme = !isDarkMode;
        }

        ThemeSwitch.IsToggled = isLightTheme;

        // Update theme icon based on current state
        UpdateThemeIcon(isLightTheme);

        // Subscribe to connection changes for pulse animation
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;

        // Start pulse animation if already connected
        if (ViewModel.IsConnected)
        {
            StartPulseAnimation();
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModelInitializationTask ??= ViewModel.InitializeAsync();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MauiViewModel.IsConnected))
        {
            if (ViewModel.IsConnected)
            {
                StartPulseAnimation();
            }
            else
            {
                StopPulseAnimation();
            }
        }
    }

    private void TrackPowerSwitch_Toggled(object sender, ToggledEventArgs e)
    {
        _ = sender; // Suppress unused parameter warning
        HandleTrackPowerSwitchToggledAsync(e.Value).Observe(
            ex => _logger?.LogWarning(ex, "Track power toggle failed"));
    }

    private async Task HandleTrackPowerSwitchToggledAsync(bool isTrackPowerOn)
    {
        // Ignore programmatic toggle updates from runtime snapshots.
        if (isTrackPowerOn == ViewModel.IsTrackPowerOn)
        {
            return;
        }

        // Haptic feedback for track power toggle
        PerformHapticFeedback();

        await ViewModel.SetTrackPowerCommand.ExecuteAsync(isTrackPowerOn);
    }

    private void ThemeSwitch_Toggled(object sender, ToggledEventArgs e)
    {
        _ = sender; // Suppress unused parameter warning
        HandleThemeSwitchToggledAsync(e.Value).Observe(
            ex => _logger?.LogWarning(ex, "Theme toggle failed"));
    }

    private async Task HandleThemeSwitchToggledAsync(bool isLightTheme)
    {
        if (Application.Current is not App app)
            return;

        // Haptic feedback for theme switch
        PerformHapticFeedback();

        // Switch ON = Light, Switch OFF = Dark
        var isDarkMode = !isLightTheme;

        // Disable UseSystemTheme when manually toggling
        _settings.Application.UseSystemTheme = false;
        _settings.Application.IsDarkMode = isDarkMode;

        // Apply theme immediately
        app.ApplyTheme(isDarkMode, useSystemTheme: false);

        // Update the theme icon
        UpdateThemeIcon(isLightTheme);

        // Save preference to settings
        await _settingsService.SaveSettingsAsync(_settings);
    }

    private void ResetCountersButton_Clicked(object sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        HandleResetCountersButtonClickedAsync().Observe(
            ex => _logger?.LogWarning(ex, "Reset counters failed"));
    }

    private async Task HandleResetCountersButtonClickedAsync()
    {

        PerformHapticFeedback();

        var shouldReset = await DisplayAlertAsync(
            "Reset lap counters",
            "Do you really want to reset all lap counters to zero?",
            "Reset",
            "Cancel");

        if (!shouldReset)
        {
            return;
        }

        ViewModel.ResetCountersCommand.Execute(null);
    }

    /// <summary>
    /// Updates the theme icon label to reflect current theme state.
    /// </summary>
    private void UpdateThemeIcon(bool isLightTheme)
    {
        // Update the icon - ☀️ for light, 🌙 for dark
        ThemeIcon.Text = isLightTheme ? "☀️" : "🌙";
    }

    /// <summary>
    /// Performs haptic feedback (vibration) on user interaction.
    /// </summary>
    private static void PerformHapticFeedback()
    {
        try
        {
            HapticFeedback.Perform();
        }
        catch
        {
            // Haptic feedback not available on all devices
        }
    }

    /// <summary>
    /// Starts a pulsing animation on the connection indicator when connected.
    /// </summary>
    private void StartPulseAnimation()
    {
        StopPulseAnimation();
        _pulseAnimationCts = new CancellationTokenSource();
        RunPulseAnimationAsync(_pulseAnimationCts.Token).Observe(
            ex => _logger?.LogWarning(ex, "Connection pulse animation failed"));
    }

    /// <summary>
    /// Stops the pulsing animation on the connection indicator.
    /// </summary>
    private void StopPulseAnimation()
    {
        _pulseAnimationCts?.Cancel();
        _pulseAnimationCts?.Dispose();
        _pulseAnimationCts = null;

        // Reset scale
        MainThread.BeginInvokeOnMainThread(() =>
        {
            ConnectionIndicator.Scale = 1.0;
        });
    }

    private async Task RunPulseAnimationAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    // Pulse: Scale up
                    await ConnectionIndicator.ScaleToAsync(1.3, 500, Easing.SinInOut);
                    // Pulse: Scale down
                    await ConnectionIndicator.ScaleToAsync(1.0, 500, Easing.SinInOut);
                });

                // Pause between pulses
                await Task.Delay(1500, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                // Animation failed, stop trying
                break;
            }
        }
    }

}







