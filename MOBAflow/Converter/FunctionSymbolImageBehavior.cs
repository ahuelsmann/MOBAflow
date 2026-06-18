// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Converter;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Collections.Concurrent;
using System.IO;

/// <summary>
/// Loads pre-rendered function symbol PNGs for a stored asset filename.
/// </summary>
public static class FunctionSymbolImageBehavior
{
    private static readonly ConcurrentDictionary<string, BitmapImage> ImageCache = new();

    public static readonly DependencyProperty AssetNameProperty = DependencyProperty.RegisterAttached(
        "AssetName",
        typeof(string),
        typeof(FunctionSymbolImageBehavior),
        new PropertyMetadata(null, OnSymbolPropertyChanged));

    public static readonly DependencyProperty RenderSizeProperty = DependencyProperty.RegisterAttached(
        "RenderSize",
        typeof(int),
        typeof(FunctionSymbolImageBehavior),
        new PropertyMetadata(32, OnSymbolPropertyChanged));

    public static string? GetAssetName(DependencyObject obj) => (string?)obj.GetValue(AssetNameProperty);
    public static void SetAssetName(DependencyObject obj, string? value) => obj.SetValue(AssetNameProperty, value);

    public static int GetRenderSize(DependencyObject obj) => (int)obj.GetValue(RenderSizeProperty);
    public static void SetRenderSize(DependencyObject obj, int value) => obj.SetValue(RenderSizeProperty, value);

    private static void OnSymbolPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Image image)
            return;

        if (image.Tag is not bool)
        {
            image.ActualThemeChanged += (s, _) =>
            {
                if (s is Image img)
                    UpdateSource(img);
            };
            image.Tag = true;
        }

        UpdateSource(image);
    }

    private static void UpdateSource(Image image)
    {
        var assetName = GetAssetName(image);
        if (string.IsNullOrWhiteSpace(assetName))
        {
            image.Source = null;
            return;
        }

        var renderSize = GetRenderSize(image);
        if (renderSize <= 0)
        {
            image.Source = null;
            return;
        }

        var pngPath = ResolvePngPath(assetName, image.ActualTheme, renderSize);
        image.Source = TryGetCachedImage(pngPath);
    }

    private static BitmapImage? TryGetCachedImage(string pngPath)
    {
        if (!File.Exists(pngPath))
        {
            return null;
        }

        return ImageCache.GetOrAdd(pngPath, static path => new BitmapImage(new Uri(path)));
    }

    private static string ResolvePngPath(string assetName, ElementTheme actualTheme, int renderSize)
    {
        var themeFolder = actualTheme == ElementTheme.Light ? "light" : "dark";
        var fileName = Path.GetFileName(assetName.Trim());
        return Path.Combine(AppContext.BaseDirectory, "Assets", "FunctionSymbols", themeFolder, renderSize.ToString(), fileName);
    }
}