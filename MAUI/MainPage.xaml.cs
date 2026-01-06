// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI;

using Common.Configuration;
using SharedUI.Interface;
using SharedUI.ViewModel;

// ReSharper disable once PartialTypeWithSinglePart
public partial class MainPage
{
    public MauiViewModel ViewModel { get; }
    private readonly ISettingsService _settingsService;
    private readonly AppSettings _settings;
    private CancellationTokenSource? _pulseAnimationCts;

    public MainPage(MauiViewModel viewModel, ISettingsService settingsService, AppSettings settings)
    {
        ViewModel = viewModel;
        _settingsService = settingsService;
        _settings = settings;
        BindingContext = ViewModel;
        InitializeComponent();

        // Set initial theme switch state based on saved preference
        // Switch ON = Light theme, Switch OFF = Dark theme
        var savedTheme = _settings.Application.Theme;
        var isLightTheme = savedTheme == "Light" || 
                           (savedTheme == "System" && Application.Current?.RequestedTheme == AppTheme.Light);
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

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
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

    private async void TrackPowerSwitch_Toggled(object sender, ToggledEventArgs e)
    {
        _ = sender; // Suppress unused parameter warning
        
        // Haptic feedback for track power toggle
        PerformHapticFeedback();
        
        await ViewModel.SetTrackPowerCommand.ExecuteAsync(e.Value);
    }

    private async void ConnectionSwitch_Toggled(object sender, ToggledEventArgs e)
    {
        _ = sender; // Suppress unused parameter warning
        
        // Haptic feedback for connection toggle
        PerformHapticFeedback();
        
        if (e.Value)
            await ViewModel.ConnectCommand.ExecuteAsync(null);
        else
            await ViewModel.DisconnectCommand.ExecuteAsync(null);
    }

    private async void ThemeSwitch_Toggled(object sender, ToggledEventArgs e)
    {
        _ = sender; // Suppress unused parameter warning
        if (Application.Current is not App app)
            return;

        // Haptic feedback for theme switch
        PerformHapticFeedback();

        // Switch ON = Light, Switch OFF = Dark
        var themePreference = e.Value ? "Light" : "Dark";
        
        // Apply theme immediately
        app.ApplyTheme(themePreference);
        
        // Update the theme icon
        UpdateThemeIcon(e.Value);
        
        // Save preference to settings
        _settings.Application.Theme = themePreference;
        await _settingsService.SaveSettingsAsync(_settings);
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
            HapticFeedback.Perform(HapticFeedbackType.Click);
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
        
        _ = Task.Run(async () =>
        {
            while (!_pulseAnimationCts.Token.IsCancellationRequested)
            {
                try
                {
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        // Pulse: Scale up
                        await ConnectionIndicator.ScaleTo(1.3, 500, Easing.SinInOut);
                        // Pulse: Scale down
                        await ConnectionIndicator.ScaleTo(1.0, 500, Easing.SinInOut);
                    });
                    
                    // Pause between pulses
                    await Task.Delay(1500, _pulseAnimationCts.Token);
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
        }, _pulseAnimationCts.Token);
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
}







