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

    private readonly ISettingsService _settingsService;

    private readonly AppSettings _settings;

    private readonly MauiViewModel _viewModel;

    private CancellationTokenSource? _pulseAnimationCts;

    private Task? _viewModelInitializationTask;

    private bool _isViewModelHooked;

    private bool _isSyncingMobaflowSwitch;



    public CounterPage(

        MauiViewModel viewModel,

        ISettingsService settingsService,

        AppSettings settings)

    {

        _viewModel = viewModel;

        _settingsService = settingsService;

        _settings = settings;

        InitializeComponent();



        BindingContext = _viewModel;

        ConnectionHeader.BindingContext = _viewModel;



        ConnectionHeader.ThemeSwitchToggled += (_, e) => ThemeSwitch_Toggled(ConnectionHeader.ThemeSwitchControl, e);

        ConnectionHeader.TrackPowerSwitchToggled += (_, e) => TrackPowerSwitch_Toggled(ConnectionHeader.TrackPowerSwitchControl, e);

        ConnectionHeader.MobaflowSwitchToggled += (_, e) => MobaflowSwitch_Toggled(ConnectionHeader.MobaflowSwitchControl, e);

        StatisticsSection.ResetCountersClicked += (_, _) => ResetCountersButton_Clicked(StatisticsSection, EventArgs.Empty);



        Loaded += OnCounterPageLoaded;

    }



    private void OnCounterPageLoaded(object? sender, EventArgs e)

    {

        EnsureViewModelHooked();

    }



    public void ActivateTab()

    {

        EnsureViewModelHooked();



        Dispatcher.DispatchAsync(async () =>

        {

            _viewModelInitializationTask ??= _viewModel.InitializeAsync();

            await _viewModelInitializationTask.ConfigureAwait(false);

        });

    }



    public void DeactivateTab()

    {

        StopPulseAnimation();

        if (_isViewModelHooked)

        {

            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;

            _isViewModelHooked = false;

        }

    }



    private void EnsureViewModelHooked()

    {

        if (_isViewModelHooked)

        {

            return;

        }



        var isDarkMode = _settings.Application.IsDarkMode;

        var useSystemTheme = _settings.Application.UseSystemTheme;

        var isLightTheme = useSystemTheme

            ? Application.Current?.RequestedTheme == AppTheme.Light

            : !isDarkMode;



        ConnectionHeader.ThemeSwitchControl.IsToggled = isLightTheme;

        UpdateThemeIcon(isLightTheme);

        SyncMobaflowSwitch();

        _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        _isViewModelHooked = true;



        if (_viewModel.IsConnected)

        {

            StartPulseAnimation();

        }

    }



    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)

    {

        if (e.PropertyName == nameof(MauiViewModel.IsConnected))

        {

            if (_viewModel.IsConnected)

            {

                StartPulseAnimation();

            }

            else

            {

                StopPulseAnimation();

            }

        }

        else if (e.PropertyName == nameof(MauiViewModel.IsMobaflowConnectionEnabled))

        {

            SyncMobaflowSwitch();

        }

    }



    private void SyncMobaflowSwitch()

    {

        _isSyncingMobaflowSwitch = true;

        try

        {

            ConnectionHeader.MobaflowSwitchControl.IsToggled = _viewModel.IsMobaflowConnectionEnabled;

        }

        finally

        {

            _isSyncingMobaflowSwitch = false;

        }

    }



    private void TrackPowerSwitch_Toggled(object sender, ToggledEventArgs e)

    {

        _ = sender;

        HandleTrackPowerSwitchToggledAsync(e.Value).Observe();

    }



    private async Task HandleTrackPowerSwitchToggledAsync(bool isTrackPowerOn)

    {

        if (isTrackPowerOn == _viewModel.IsTrackPowerOn)

        {

            return;

        }



        PerformHapticFeedback();

        await _viewModel.SetTrackPowerCommand.ExecuteAsync(isTrackPowerOn);

    }



    private void MobaflowSwitch_Toggled(object sender, ToggledEventArgs e)

    {

        _ = sender;

        if (_isSyncingMobaflowSwitch)

        {

            return;

        }

        HandleMobaflowSwitchToggledAsync(e.Value).Observe();

    }



    private async Task HandleMobaflowSwitchToggledAsync(bool isEnabled)

    {

        PerformHapticFeedback();

        if (isEnabled != _viewModel.IsMobaflowConnectionEnabled)

        {

            await _viewModel.SetMobaflowConnectionCommand.ExecuteAsync(isEnabled);

            return;

        }

        if (isEnabled)

        {

            await _viewModel.RetryMobaflowConnectionAsync();

        }

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



        var shouldReset = await FindHostPage().DisplayAlertAsync(

            "Reset lap counters",

            "Do you really want to reset all lap counters to zero?",

            "Reset",

            "Cancel");



        if (!shouldReset)

        {

            return;

        }



        _viewModel.ResetCountersCommand.Execute(null);

    }



    private Page FindHostPage()

    {

        Element? element = this;

        while (element is not null)

        {

            if (element is Page page)

            {

                return page;

            }



            element = element.Parent;

        }



        return Application.Current!.Windows[0].Page!;

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


