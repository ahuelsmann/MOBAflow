// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Controls;

using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

/// <summary>
/// One symbol entry shown in the picker. Exposes both the bare filename (for persistence/tooltip)
/// and a packaged <see cref="Uri"/> bound to a monochrome BitmapIcon so the icon adopts the
/// active theme's text color.
/// </summary>
internal sealed class FunctionSymbolItem
{
    public FunctionSymbolItem(string fileName)
    {
        FileName = fileName;
        DisplayName = ToDisplayName(fileName);
    }

    public string FileName { get; }
    public string DisplayName { get; }

    private static string ToDisplayName(string fileName)
    {
        var withoutExt = Path.GetFileNameWithoutExtension(fileName);
        return withoutExt.Replace('_', ' ').Trim();
    }
}

/// <summary>
/// Dialog for selecting an SVG symbol from MOBAflow/Assets for a function button (Train Control).
/// Enumerates the deployed Assets folder at runtime so newly added or renamed SVGs are picked up
/// automatically on the next build.
/// </summary>
internal sealed partial class FunctionSymbolPickerDialog
{
    /// <summary>
    /// After closing: selected SVG asset filename (e.g. "scheinwerfer.svg") or null on cancel.
    /// </summary>
    public string? SelectedGlyph { get; private set; }

    /// <summary>
    /// SVG filenames that are not function-button symbols and must be excluded from the library.
    /// Compared case-insensitively.
    /// </summary>
    private static readonly HashSet<string> ExcludedAssets = new(StringComparer.OrdinalIgnoreCase)
    {
        "DoorClose.svg",
        "DoorOpen.svg",
        "IsDoorBlocked.svg",
        "mobaflow-icon.svg"
    };

    /// <summary>
    /// Loads the SVG asset filenames from the deployed Assets folder. Newly added or renamed
    /// SVG files are picked up automatically on the next build (csproj globs Assets\*.svg).
    /// </summary>
    private static IReadOnlyList<FunctionSymbolItem> LoadSymbols()
    {
        try
        {
            var assetsDir = Path.Combine(AppContext.BaseDirectory, "Assets");
            if (!Directory.Exists(assetsDir))
                return Array.Empty<FunctionSymbolItem>();

            var culture = CultureInfo.GetCultureInfo("de-DE");
            return Directory.EnumerateFiles(assetsDir, "*.svg", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName)
                .Where(name => !string.IsNullOrEmpty(name) && !ExcludedAssets.Contains(name))
                .OrderBy(name => name, StringComparer.Create(culture, ignoreCase: true))
                .Select(name => new FunctionSymbolItem(name!))
                .ToList();
        }
        catch
        {
            return Array.Empty<FunctionSymbolItem>();
        }
    }

    public FunctionSymbolPickerDialog()
    {
        InitializeComponent();
        SymbolsItemsControl.ItemsSource = LoadSymbols();
    }

    private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        SelectedGlyph = null;
    }

    private void SymbolButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string fileName)
        {
            SelectedGlyph = fileName;
            Hide();
        }
    }
}