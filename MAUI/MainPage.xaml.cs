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
    }

    private async void TrackPowerSwitch_Toggled(object sender, ToggledEventArgs e)
    {
        _ = sender; // Suppress unused parameter warning
        await ViewModel.SetTrackPowerCommand.ExecuteAsync(e.Value);
    }

    private async void ConnectionSwitch_Toggled(object sender, ToggledEventArgs e)
    {
        _ = sender; // Suppress unused parameter warning
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
}







