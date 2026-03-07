// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.MAUI.View;

using Microsoft.Extensions.DependencyInjection;
using SharedUI.Interface;

/// <summary>
/// Splash page shown during app startup.
/// Displays logo and "MOBAsmart" text, then navigates to main page.
/// </summary>
public partial class SplashPage
{
    public SplashPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Ensure settings are loaded before MainPage (and MauiViewModel) are created.
        // This fixes Z21 connection using wrong/default IP when settings load was still in progress.
        if (Application.Current is App app)
        {
            var settingsService = app.Services.GetRequiredService<ISettingsService>();
            await settingsService.LoadSettingsAsync().ConfigureAwait(true);
            var settings = settingsService.GetSettings();
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
}
