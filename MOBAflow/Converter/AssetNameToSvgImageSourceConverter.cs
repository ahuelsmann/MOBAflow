// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Converter;

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

using Windows.Storage.Streams;

/// <summary>
/// Attached property that loads a monochrome SVG asset into an <see cref="Image"/>
/// and recolors it to match the current application theme (white in dark mode,
/// black in light mode).
/// <para>
/// WinUI 3's <see cref="SvgImageSource"/> does not evaluate the CSS <c>currentColor</c>
/// keyword, and unpackaged apps cannot use <c>ms-appx://</c> URIs. The behavior therefore
/// reads each SVG from the on-disk Assets folder, rewrites stroke/fill values to the
/// theme color, and feeds the result into <see cref="SvgImageSource"/> via an in-memory
/// stream (no disk writes — works in packaged MSIX where the install folder is read-only).
/// </para>
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

    private const string ColorValuePattern = @"currentColor|black|#000|#000000|rgb\(0,\s*0,\s*0\)";

    private static readonly Regex ColorReplacementQuoted = new(
        $@"(?<name>stroke|fill)=[""'](?<value>{ColorValuePattern})[""']",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex ColorReplacementUnquoted = new(
        $@"(?<name>stroke|fill)=(?<value>{ColorValuePattern})\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex ColorReplacementCss = new(
        $@"(?<name>stroke|fill)\s*:\s*(?<value>{ColorValuePattern})\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Dictionary<Image, string> _assetByImage = new();

    private static async void OnSvgAssetNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Image image)
            return;

        if (e.NewValue is not string assetName || string.IsNullOrWhiteSpace(assetName))
        {
            _assetByImage.Remove(image);
            image.Source = null;
            return;
        }

        _assetByImage[image] = assetName;

        // Subscribe once; the Tag flag avoids repeated subscriptions on rebind.
        if (image.Tag is not bool)
        {
            image.ActualThemeChanged += (s, _) =>
            {
                if (s is Image img && _assetByImage.TryGetValue(img, out var name))
                    _ = LoadSvgAsync(img, name);
            };
            image.Tag = true;
        }

        await LoadSvgAsync(image, assetName);
    }

    private static async Task LoadSvgAsync(Image image, string assetName)
    {
        var trimmed = assetName.Trim();
        try
        {
            var sourcePath = Path.Combine(AppContext.BaseDirectory, "Assets", trimmed);
            var svg = await File.ReadAllTextAsync(sourcePath);
            var color = GetThemeColor();

            var modified = ColorReplacementQuoted.Replace(svg, m => $"{m.Groups["name"].Value}=\"{color}\"");
            modified = ColorReplacementUnquoted.Replace(modified, m => $"{m.Groups["name"].Value}=\"{color}\"");
            modified = ColorReplacementCss.Replace(modified, m => $"{m.Groups["name"].Value}:{color}");

            // Feed the recolored SVG directly into SvgImageSource via an in-memory stream.
            // Avoids writing to AppContext.BaseDirectory (read-only in packaged MSIX) and
            // avoids file:// URI sandboxing issues.
            using var stream = new InMemoryRandomAccessStream();
            using (var writer = new DataWriter(stream))
            {
                writer.WriteBytes(Encoding.UTF8.GetBytes(modified));
                await writer.StoreAsync();
                await writer.FlushAsync();
                writer.DetachStream();
            }
            stream.Seek(0);

            var svgSource = new SvgImageSource();
            await svgSource.SetSourceAsync(stream);
            image.Source = svgSource;
        }
        catch
        {
            // Fallback: show the raw (uncolored) SVG so the icon is at least visible.
            try
            {
                image.Source = new SvgImageSource(new Uri($"ms-appx:///Assets/{trimmed}"));
            }
            catch
            {
                image.Source = null;
            }
        }
    }

    private static string GetThemeColor()
    {
        var vm = (Application.Current as Moba.WinUI.App)?.Services
            ?.GetService(typeof(Moba.SharedUI.ViewModel.MainWindowViewModel))
            as Moba.SharedUI.ViewModel.MainWindowViewModel;

        // Default to dark (white icons) when not resolvable: matches AppSettings default IsDarkMode=true.
        return vm is null || vm.IsDarkMode ? "#FFFFFF" : "#000000";
    }
}