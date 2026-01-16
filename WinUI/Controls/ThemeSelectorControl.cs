// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Controls;

using Common.Configuration;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

using Moba.WinUI.Service;

using Service;

using SharedUI.Interface;

/// <summary>
/// Theme selector control for application-wide theme switching.
/// Displays available themes, allows user selection, and persists to AppSettings.
/// </summary>
public sealed class ThemeSelectorControl : StackPanel
{
    private readonly IThemeProvider _themeProvider;
    private readonly AppSettings _settings;
    private readonly ISettingsService? _settingsService;
    private readonly List<Button> _themeButtons = [];

    public ThemeSelectorControl(IThemeProvider themeProvider, AppSettings settings, ISettingsService? settingsService = null)
    {
        ArgumentNullException.ThrowIfNull(themeProvider);
        ArgumentNullException.ThrowIfNull(settings);

        _themeProvider = themeProvider;
        _settings = settings;
        _settingsService = settingsService;

        Spacing = 12;
        Padding = new Thickness(0);

        // Title
        Children.Add(new TextBlock
        {
            Text = "Application Theme",
            FontSize = 14,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        });

        // Description
        Children.Add(new TextBlock
        {
            Text = "Choose a theme for a fresh appearance",
            FontSize = 11,
            Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
            TextWrapping = TextWrapping.Wrap
        });

        // Theme buttons - Row 1 (Basic themes)
        var buttonPanel1 = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        var basicThemes = new[]
        {
            ("Modern", Service.ApplicationTheme.Modern, "Modern: Blue accents, Fluent Design"),
            ("Klassisch", Service.ApplicationTheme.Classic, "Klassisch: Maerklin green accents"),
            ("Dark", Service.ApplicationTheme.Dark, "Dark: Violet accents, night-friendly")
        };

        foreach (var (name, theme, description) in basicThemes)
        {
            buttonPanel1.Children.Add(CreateThemeButton(name, theme, description));
        }
        Children.Add(buttonPanel1);

        // Theme buttons - Row 2 (Hardware-inspired themes)
        var buttonPanel2 = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(0, 8, 0, 0) };
        var hardwareThemes = new[]
        {
            ("ESU", Service.ApplicationTheme.EsuCabControl, "ESU CabControl: Orange/Amber, dark"),
            ("Z21", Service.ApplicationTheme.RocoZ21, "Roco Z21: Orange/Black, minimal"),
            ("CS2/3", Service.ApplicationTheme.MaerklinCS, "Maerklin CS: Red/Grey, classic")
        };

        foreach (var (name, theme, description) in hardwareThemes)
        {
            buttonPanel2.Children.Add(CreateThemeButton(name, theme, description));
        }
        Children.Add(buttonPanel2);

        // Subscribe to theme changes for button state updates
        _themeProvider.ThemeChanged += OnThemeChanged;
    }

    private Button CreateThemeButton(string name, Service.ApplicationTheme theme, string description)
    {
        var button = new Button
        {
            Content = name,
            Tag = theme,
            MinWidth = 80,
            Height = 36
        };

        button.Click += OnThemeButtonClick;
        _themeButtons.Add(button);

        UpdateButtonState(button, theme);
        ToolTipService.SetToolTip(button, description);
        return button;
    }

    private void OnThemeButtonClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not Service.ApplicationTheme theme)
            return;

        // Apply theme
        _themeProvider.SetTheme(theme);

        // Persist to settings
        _settings.Application.SelectedSkin = theme.ToString();
        if (_settingsService != null)
        {
            _ = _settingsService.SaveSettingsAsync(_settings);
        }
    }

    private void OnThemeChanged(object? sender, ThemeChangedEventArgs e)
    {
        // Update all button states when theme changes
        foreach (var button in _themeButtons)
        {
            if (button.Tag is Service.ApplicationTheme theme)
            {
                UpdateButtonState(button, theme);
            }
        }
    }

    private void UpdateButtonState(Button button, Service.ApplicationTheme theme)
    {
        var isSelected = _themeProvider.CurrentTheme == theme;
        button.IsEnabled = !isSelected;
        button.Opacity = isSelected ? 0.6 : 1.0;
    }
}
