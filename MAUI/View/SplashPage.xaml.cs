// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.MAUI.View;

/// <summary>
/// Splash page shown during app startup.
/// Displays logo and "MOBAsmart" text, then navigates to main page.
/// </summary>
public partial class SplashPage : ContentPage
{
    public SplashPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Show splash for a short time, then navigate to main page
        await Task.Delay(1500);

        // Navigate to main page
        if (Application.Current?.MainPage is not null)
        {
            Application.Current.MainPage = App.CreateMainPage();
        }
    }
}
