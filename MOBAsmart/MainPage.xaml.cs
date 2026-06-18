// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI;

using Common.Configuration;

using Common.Extension;

using SharedUI.Interface;
using SharedUI.ViewModel;

using System.ComponentModel;

// ReSharper disable once PartialTypeWithSinglePart
public partial class CounterPage
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ISettingsService _settingsService;
    private readonly AppSettings _settings;
    private MauiViewModel? _viewModel;
    private CancellationTokenSource? _pulseAnimationCts;
    private Task? _viewModelInitializationTask;
    private bool _isViewModelHooked;

    public MauiViewModel ViewModel => _viewModel ??= _serviceProvider.GetRequiredService<MauiViewModel>();

    public CounterPage(
        IServiceProvider serviceProvider,
        ISettingsService settingsService,
        AppSettings settings)
    {
        _serviceProvider = serviceProvider;
        _settingsService = settingsService;
        _settings = settings;
        InitializeComponent();

        ConnectionHeader.ThemeSwitchToggled += (_, e) => ThemeSwitch_Toggled(ConnectionHeader.ThemeSwitchControl, e);
        ConnectionHeader.TrackPowerSwitchToggled += (_, e) => TrackPowerSwitch_Toggled(ConnectionHeader.TrackPowerSwitchControl, e);
        StatisticsSection.ResetCountersClicked += (_, _) => ResetCountersButton_Clicked(StatisticsSection, EventArgs.Empty);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ActivateTab();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        StopPulseAnimation();
        if (_isViewModelHooked)
        {
            ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _isViewModelHooked = false;
        }
    }

    public void ActivateTab()
    {
        EnsureViewModelBound();

        _viewModelInitializationTask ??= ViewModel.InitializeAsync();
        ViewModel.ResumeHeavyUpdates();
    }

    public void DeactivateTab()
    {
        StopPulseAnimation();
    }

    private void EnsureViewModelBound()
    {
        if (_viewModel != null)
        {
            return;
        }

        _viewModel = _serviceProvider.GetRequiredService<MauiViewModel>();
        BindingContext = _viewModel;
        ConnectionHeader.BindingContext = _viewModel;
        StatisticsSection.BindingContext = _viewModel;

        var isDarkMode = _settings.Application.IsDarkMode;
        var useSystemTheme = _settings.Application.UseSystemTheme;
        var isLightTheme = useSystemTheme
            ? Application.Current?.RequestedTheme == AppTheme.Light
            : !isDarkMode;

        ConnectionHeader.ThemeSwitchControl.IsToggled = isLightTheme;
        UpdateThemeIcon(isLightTheme);

        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        _isViewModelHooked = true;

        if (ViewModel.IsConnected)
        {
            StartPulseAnimation();
        }
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
        _ = sender;
        HandleTrackPowerSwitchToggledAsync(e.Value).Observe();
    }

    private async Task HandleTrackPowerSwitchToggledAsync(bool isTrackPowerOn)
    {
        if (isTrackPowerOn == ViewModel.IsTrackPowerOn)
        {
            return;
        }

        PerformHapticFeedback();
        await ViewModel.SetTrackPowerCommand.ExecuteAsync(isTrackPowerOn);
    }

    private void ThemeSwitch_Toggled(object sender, ToggledEventArgs e)
    {
        _ = sender;
        HandleThemeSwitchToggledAsync(e.Value).Observe();
    }

    private async Task HandleThemeSwitchToggledAsync(bool isLightTheme)
    {
        if (Application.Current is not App app)
        {
            return;
        }

        PerformHapticFeedback();

        var isDarkMode = !isLightTheme;
        _settings.Application.UseSystemTheme = false;
        _settings.Application.IsDarkMode = isDarkMode;

        app.ApplyTheme(isDarkMode, useSystemTheme: false);
        UpdateThemeIcon(isLightTheme);
        await _settingsService.SaveSettingsAsync(_settings);
    }

    private void ResetCountersButton_Clicked(object sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        HandleResetCountersButtonClickedAsync().Observe();
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

    private void UpdateThemeIcon(bool isLightTheme)
    {
        ConnectionHeader.ThemeIconLabel.Text = isLightTheme ? "☀️" : "🌙";
    }

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

    private void StartPulseAnimation()
    {
        StopPulseAnimation();
        _pulseAnimationCts = new CancellationTokenSource();
        RunPulseAnimationAsync(_pulseAnimationCts.Token).Observe();
    }

    private void StopPulseAnimation()
    {
        _pulseAnimationCts?.Cancel();
        _pulseAnimationCts?.Dispose();
        _pulseAnimationCts = null;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            ConnectionHeader.ConnectionIndicatorBorder.Opacity = 1.0;
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
                    await ConnectionHeader.ConnectionIndicatorBorder.FadeToAsync(0.45, 600, Easing.SinInOut);
                    await ConnectionHeader.ConnectionIndicatorBorder.FadeToAsync(1.0, 600, Easing.SinInOut);
                });

                await Task.Delay(1500, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                break;
            }
        }
    }
}