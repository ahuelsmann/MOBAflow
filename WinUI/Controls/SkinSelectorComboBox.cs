// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Controls;

using Common.Configuration;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using SharedUI.Interface;

using AppTheme = Service.ApplicationTheme;

/// <summary>
/// ComboBox for selecting application skin/theme on individual pages.
/// Persists selection to AppSettings and triggers page reload for theme application.
/// </summary>
public sealed class SkinSelectorComboBox : ComboBox
{
    private readonly Service.IThemeProvider _themeProvider;
    private readonly AppSettings _settings;
    private readonly ISettingsService? _settingsService;
    private bool _isInitializing = true;

    /// <summary>
    /// Event raised when skin changes and page should reload.
    /// </summary>
    public event EventHandler<AppTheme>? SkinChanged;

    public SkinSelectorComboBox(Service.IThemeProvider themeProvider, AppSettings settings, ISettingsService? settingsService = null)
    {
        ArgumentNullException.ThrowIfNull(themeProvider);
        ArgumentNullException.ThrowIfNull(settings);

        _themeProvider = themeProvider;
        _settings = settings;
        _settingsService = settingsService;

        // ComboBox styling
        Width = 140;
        Height = 32;
        VerticalAlignment = VerticalAlignment.Center;

        // Add theme items
        Items.Add(new ComboBoxItem { Content = "Modern", Tag = AppTheme.Modern });
        Items.Add(new ComboBoxItem { Content = "Classic", Tag = AppTheme.Classic });
        Items.Add(new ComboBoxItem { Content = "Dark", Tag = AppTheme.Dark });
        Items.Add(new ComboBoxItem { Content = "ESU CabControl", Tag = AppTheme.EsuCabControl });
        Items.Add(new ComboBoxItem { Content = "Roco Z21", Tag = AppTheme.RocoZ21 });
        Items.Add(new ComboBoxItem { Content = "Märklin CS", Tag = AppTheme.MaerklinCS });

        // Set current selection
        SelectCurrentTheme();

        // Handle selection changed
        SelectionChanged += OnSelectionChanged;

        _isInitializing = false;
    }

    private void SelectCurrentTheme()
    {
        var currentTheme = _themeProvider.CurrentTheme;
        foreach (ComboBoxItem item in Items)
        {
            if (item.Tag is AppTheme theme && theme == currentTheme)
            {
                SelectedItem = item;
                break;
            }
        }
    }

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing || SelectedItem is not ComboBoxItem selectedItem)
            return;

        if (selectedItem.Tag is not AppTheme newTheme)
            return;

        if (newTheme == _themeProvider.CurrentTheme)
            return;

        // Apply theme
        _themeProvider.SetTheme(newTheme);

        // Persist to settings
        _settings.Application.SelectedSkin = newTheme.ToString();
        if (_settingsService != null)
        {
            _ = _settingsService.SaveSettingsAsync(_settings);
        }

        // Raise event for page to handle (e.g., reload)
        SkinChanged?.Invoke(this, newTheme);
    }

    /// <summary>
    /// Creates a header panel with label and the ComboBox.
    /// </summary>
    public static StackPanel CreateWithLabel(Service.IThemeProvider themeProvider, AppSettings settings, ISettingsService? settingsService, out SkinSelectorComboBox comboBox)
    {
        comboBox = new SkinSelectorComboBox(themeProvider, settings, settingsService);

        var panel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            VerticalAlignment = VerticalAlignment.Center
        };

        panel.Children.Add(new TextBlock
        {
            Text = "Skin:",
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 12
        });

        panel.Children.Add(comboBox);

        return panel;
    }
}
