// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Converter;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Windows.Storage.Streams;

/// <summary>
/// Converts an asset filename (e.g. "scheinwerfer.svg") to an <see cref="SvgImageSource"/>
/// referencing the packaged Assets folder. Empty/null input produces null so the bound
/// Image control stays blank.
/// </summary>
public sealed partial class AssetNameToSvgImageSourceConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is not string name || string.IsNullOrWhiteSpace(name))
            return null;

        // Return a placeholder - actual loading happens via SvgImageBehavior
        return new SvgImageSource(new Uri($"ms-appx:///Assets/{name.Trim()}"));
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotSupportedException();
}

/// <summary>
/// Attached property for Image controls that loads SVG assets asynchronously with currentColor replacement.
/// WinUI 3 requires async loading for proper SVG processing from streams.
/// </summary>
public static class SvgImageBehavior
{
    public static readonly DependencyProperty SvgAssetNameProperty = DependencyProperty.RegisterAttached(
        "SvgAssetName",
        typeof(string),
        typeof(SvgImageBehavior),
        new PropertyMetadata(null, OnSvgAssetNameChanged));

    public static string? GetSvgAssetName(DependencyObject obj) => (string?)obj.GetValue(SvgAssetNameProperty);
    public static void SetSvgAssetName(DependencyObject obj, string? value) => obj.SetValue(SvgAssetNameProperty, value);

    private static readonly Dictionary<Image, string> _loadingCache = new();

    private static async void OnSvgAssetNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Image image || e.NewValue is not string assetName || string.IsNullOrWhiteSpace(assetName))
        {
            if (d is Image img) img.Source = null;
            return;
        }

        // Cache the asset name for theme change handling
        _loadingCache[image] = assetName;

        // Subscribe to theme changes if not already subscribed
        if (image.Tag is not bool)
        {
            image.ActualThemeChanged += OnImageActualThemeChanged;
            image.Tag = true;
        }

        await LoadSvgAsync(image, assetName);
    }

    private static void OnImageActualThemeChanged(FrameworkElement sender, object args)
    {
        if (sender is Image image && _loadingCache.TryGetValue(image, out var assetName))
        {
            _ = LoadSvgAsync(image, assetName);
        }
    }

    private static async Task LoadSvgAsync(Image image, string assetName)
    {
        try
        {
            // Try to load from ms-appx first
            var uri = new Uri($"ms-appx:///Assets/{assetName.Trim()}");
            var file = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(uri);
            var buffer = await Windows.Storage.FileIO.ReadBufferAsync(file);

            // Read SVG content
            using var dataReader = Windows.Storage.Streams.DataReader.FromBuffer(buffer);
            var bytes = new byte[buffer.Length];
            dataReader.ReadBytes(bytes);
            var svgContent = Encoding.UTF8.GetString(bytes);

            // Get appropriate color based on current theme
            var themeColor = GetThemeColor(image);

            // Replace currentColor with theme-appropriate color
            var modifiedSvg = ReplaceCurrentColor(svgContent, themeColor);

            // Create stream for SvgImageSource
            using var stream = new InMemoryRandomAccessStream();
            using (var writer = new DataWriter(stream))
            {
                writer.WriteBytes(Encoding.UTF8.GetBytes(modifiedSvg));
                await writer.StoreAsync();
                await writer.FlushAsync();
                writer.DetachStream(); // Prevent DataWriter from closing the stream on dispose
            }
            stream.Seek(0);

            // Create and set SvgImageSource
            var svgSource = new SvgImageSource();
            await svgSource.SetSourceAsync(stream);
            image.Source = svgSource;
        }
        catch
        {
            // Fallback: try direct ms-appx URI
            try
            {
                image.Source = new SvgImageSource(new Uri($"ms-appx:///Assets/{assetName.Trim()}"));
            }
            catch
            {
                image.Source = null;
            }
        }
    }

    private static string GetThemeColor(Image image)
    {
        // Use the global application theme state from MainWindowViewModel as the most reliable source.
        // ContentDialogs and dynamically loaded DataTemplates often fail to inherit ActualTheme correctly
        // before they are fully materialized or when hosted in popup roots.
        if (Application.Current is Moba.WinUI.App app)
        {
            var vm = app.Services?.GetService(typeof(Moba.SharedUI.ViewModel.MainWindowViewModel)) as Moba.SharedUI.ViewModel.MainWindowViewModel;
            if (vm != null)
            {
                return vm.IsDarkMode ? "#FFFFFF" : "#000000";
            }
        }

        // Fallback (should normally not be reached)
        var theme = image.ActualTheme;
        if (theme == ElementTheme.Default && image.XamlRoot?.Content is FrameworkElement root)
            theme = root.ActualTheme;

        return theme == ElementTheme.Light ? "#000000" : "#FFFFFF";
    }

    private static string ReplaceCurrentColor(string svg, string color)
    {
        // Replace currentColor with the specified color
        // Handle both quoted and unquoted variants
        const string colorValuePattern = @"currentColor|black|#000|#000000|rgb\(0,\s*0,\s*0\)";

        var result = Regex.Replace(
            svg,
            $@"(?<name>stroke|fill)=[""'](?<value>{colorValuePattern})[""']",
            match => $"{match.Groups["name"].Value}=\"{color}\"",
            RegexOptions.IgnoreCase);

        result = Regex.Replace(
            result,
            $@"(?<name>stroke|fill)=(?<value>{colorValuePattern})\b",
            match => $"{match.Groups["name"].Value}=\"{color}\"",
            RegexOptions.IgnoreCase);

        result = Regex.Replace(
            result,
            $@"(?<name>stroke|fill)\s*:\s*(?<value>{colorValuePattern})\b",
            match => $"{match.Groups["name"].Value}:{color}",
            RegexOptions.IgnoreCase);

        return result;
    }
}