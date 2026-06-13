// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.MAUI.View;

using SharedUI.Interface;

/// <summary>
/// Splash page shown during app startup.
/// Displays logo and "MOBAsmart" text, then navigates to main page.
/// </summary>
public partial class SplashPage
{
    private readonly ISettingsService _settingsService;

    public SplashPage(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            // Ensure settings are loaded before the Shell pages and MauiViewModel are created.
            // This fixes Z21 connection using wrong/default IP when settings load was still in progress.
            if (Application.Current is App app)
            {
                await _settingsService.LoadSettingsAsync().ConfigureAwait(true);
                var settings = _settingsService.GetSettings();
                app.ApplyTheme(settings.Application.IsDarkMode, settings.Application.UseSystemTheme);
            }

            // Navigate to the Shell root using the new Windows API.
            var windows = Application.Current?.Windows;
            var window = windows != null && windows.Count > 0 ? windows[0] : null;
            if (window is not null)
            {
                window.Page = App.CreateAppShell();
            }
        }
        catch (Exception)
        {
            // Keep splash page visible when initialization fails.
        }
    }
}
