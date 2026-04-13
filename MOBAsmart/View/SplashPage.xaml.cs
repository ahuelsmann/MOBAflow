// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.MAUI.View;

using SharedUI.Interface;
using Microsoft.Extensions.Logging;

/// <summary>
/// Splash page shown during app startup.
/// Displays logo and "MOBAsmart" text, then navigates to main page.
/// </summary>
public partial class SplashPage
{
    private readonly ISettingsService _settingsService;
    private readonly ILogger<SplashPage> _logger;

    public SplashPage(ISettingsService settingsService, ILogger<SplashPage> logger)
    {
        _settingsService = settingsService;
        _logger = logger;
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            // Ensure settings are loaded before MainPage (and MauiViewModel) are created.
            // This fixes Z21 connection using wrong/default IP when settings load was still in progress.
            if (Application.Current is App app)
            {
                await _settingsService.LoadSettingsAsync().ConfigureAwait(true);
                var settings = _settingsService.GetSettings();
                app.ApplyTheme(settings.Application.IsDarkMode, settings.Application.UseSystemTheme);
            }

            // Show splash for a short time, then navigate to main page
            await Task.Delay(1500).ConfigureAwait(true);

            // Navigate to main page using the new Windows API (MainPage is deprecated)
            var window = Application.Current?.Windows.FirstOrDefault();
            if (window is not null)
            {
                window.Page = App.CreateMainPage();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SplashPage initialization failed");
        }
    }
}
