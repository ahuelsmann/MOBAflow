// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Service;

using Common.Configuration;
using Microsoft.UI.Xaml;

/// <summary>
/// WinUI-specific implementation of skin provider.
/// Manages ResourceDictionary switching and skin events.
/// Uses programmatically-built skins to avoid XAML parsing issues in WinUI 3.
/// </summary>
public class SkinProvider : ISkinProvider
{
    private AppSkin _currentSkin = AppSkin.Blue;
    private bool _isDarkMode;

    /// <summary>
    /// Initializes the skin provider with saved settings.
    /// </summary>
    public void Initialize(AppSettings settings)
    {
        if (!string.IsNullOrEmpty(settings.Application.SelectedSkin))
        {
            // Support both old enum names and new names for backwards compatibility
            var skinName = settings.Application.SelectedSkin;
            skinName = MapLegacySkinName(skinName);

            if (Enum.TryParse<AppSkin>(skinName, out var savedSkin))
            {
                SetSkin(savedSkin);
            }
        }
    }

    /// <summary>
    /// Maps legacy skin names (from old ApplicationTheme enum) to new AppSkin names.
    /// </summary>
    private static string MapLegacySkinName(string legacyName)
    {
        return legacyName switch
        {
            "Original" => nameof(AppSkin.System),
            "Modern" => nameof(AppSkin.Blue),
            "Classic" => nameof(AppSkin.Green),
            "Dark" => nameof(AppSkin.Violet),
            "EsuCabControl" => nameof(AppSkin.Orange),
            "RocoZ21" => nameof(AppSkin.DarkOrange),
            "MaerklinCS" => nameof(AppSkin.Red),
            _ => legacyName
        };
    }

    public AppSkin CurrentSkin
    {
        get => _currentSkin;
    }

    public bool IsDarkMode
    {
        get => _isDarkMode;
        set
        {
            if (_isDarkMode != value)
            {
                _isDarkMode = value;
                DarkModeChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public event EventHandler<SkinChangedEventArgs>? SkinChanged;
    public event EventHandler? DarkModeChanged;

    /// <summary>
    /// Sets the application skin by switching ResourceDictionaries.
    /// Uses programmatically-built skins for WinUI 3 compatibility.
    /// </summary>
    public void SetSkin(AppSkin skin)
    {
        if (_currentSkin == skin)
            return;

        var oldSkin = _currentSkin;
        _currentSkin = skin;

        // Remove old skin resource dictionary
        var oldDict = FindSkinDictionary();
        if (oldDict != null)
        {
            Application.Current.Resources.MergedDictionaries.Remove(oldDict);
        }

        // Build and add new skin resource dictionary
        var newDict = skin switch
        {
            AppSkin.Blue => SkinResourceBuilder.BuildBlueSkin(),
            AppSkin.Green => SkinResourceBuilder.BuildGreenSkin(),
            AppSkin.Violet => SkinResourceBuilder.BuildVioletSkin(),
            AppSkin.Orange => SkinResourceBuilder.BuildOrangeSkin(),
            AppSkin.DarkOrange => SkinResourceBuilder.BuildDarkOrangeSkin(),
            AppSkin.Red => SkinResourceBuilder.BuildRedSkin(),
            AppSkin.System => SkinResourceBuilder.BuildSystemSkin(),
            _ => SkinResourceBuilder.BuildBlueSkin()
        };

        Application.Current.Resources.MergedDictionaries.Add(newDict);

        // Raise event
        SkinChanged?.Invoke(this, new SkinChangedEventArgs(oldSkin, skin));
    }

    /// <summary>
    /// Gets the ResourceDictionary URI for a given skin (not used with C# builder, but kept for compatibility).
    /// </summary>
    public Uri GetSkinResourceUri(AppSkin skin)
    {
        return skin switch
        {
            AppSkin.Green => new Uri("ms-appx:///WinUI/Resources/Skins/SkinGreen.xaml"),
            AppSkin.Blue => new Uri("ms-appx:///WinUI/Resources/Skins/SkinBlue.xaml"),
            AppSkin.Violet => new Uri("ms-appx:///WinUI/Resources/Skins/SkinViolet.xaml"),
            _ => new Uri("ms-appx:///WinUI/Resources/Skins/SkinBlue.xaml")
        };
    }

    /// <summary>
    /// Finds and returns the skin ResourceDictionary.
    /// </summary>
    private ResourceDictionary? FindSkinDictionary()
    {
        // Try to find by checking if it was created by our builder
        // Since we can't directly identify the source, we find dicts with our custom skin colors
        foreach (var dict in Application.Current.Resources.MergedDictionaries.ToList())
        {
            if (dict.ContainsKey("ThemeAccentColor"))
            {
                return dict;
            }
        }
        return null;
    }
}
