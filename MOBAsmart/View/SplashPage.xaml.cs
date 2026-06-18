// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.MAUI.View;



using SharedUI.Interface;



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

            await _settingsService.LoadSettingsAsync().ConfigureAwait(false);

            var settings = _settingsService.GetSettings();



            await MainThread.InvokeOnMainThreadAsync(async () =>

            {

                StatusLabel.Text = "Loading UI...";

                if (Application.Current is App app)

                {

                    app.ApplyTheme(settings.Application.IsDarkMode, settings.Application.UseSystemTheme);

                }



                // Yield one UI frame so the splash status text is painted before shell construction.

                await Task.Yield();



                var window = Application.Current?.Windows.FirstOrDefault();

                if (window is null)

                {

                    return;

                }



                // AppTabHostPage only: skip AppShell + CounterPage until the first tab is shown.

                window.Page = App.CreateMainPage();

            });

        }

        catch (Exception ex)

        {

            System.Diagnostics.Debug.WriteLine($"[Splash] Startup failed: {ex}");

            await MainThread.InvokeOnMainThreadAsync(() =>

            {

                StatusLabel.Text = "Startup failed";

            });

        }

    }

}


