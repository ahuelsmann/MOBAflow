// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Controls;

using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using System.Globalization;

using Windows.UI;

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

    public string? SelectedColorHex { get; private set; }

    /// <summary>
    /// True when the user explicitly cleared glyph and color via "Auswahl loeschen".
    /// Distinguishes intentional clear on Ok from Cancel (both leave null selections).
    /// </summary>
    public bool IsSelectionCleared { get; private set; }

    private bool _suppressColorSelectionUpdate;

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

    public void SetInitialColor(string colorHex)
    {
        if (!TryParseHexColor(colorHex, out var color))
            return;

        FunctionColorPicker.Color = color;
        SelectedColorHex = colorHex;
    }

    private static bool TryParseHexColor(string? value, out Color color)
    {
        color = default;
        if (string.IsNullOrWhiteSpace(value) || value.Length != 7 || value[0] != '#')
            return false;

        if (!byte.TryParse(value.AsSpan(1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var r) ||
            !byte.TryParse(value.AsSpan(3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var g) ||
            !byte.TryParse(value.AsSpan(5, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var b))
            return false;

        color = Color.FromArgb(255, r, g, b);
        return true;
    }

    private static string ToHexColor(Color color) =>
        string.Create(CultureInfo.InvariantCulture, $"#{color.R:X2}{color.G:X2}{color.B:X2}");

    private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        SelectedGlyph = null;
        SelectedColorHex = null;
    }

    private void SymbolButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string fileName)
        {
            SelectedGlyph = fileName;
            IsSelectionCleared = false;
        }
    }

    private void FunctionColorPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
    {
        if (_suppressColorSelectionUpdate)
            return;

        SelectedColorHex = ToHexColor(args.NewColor);
        IsSelectionCleared = false;
    }

    private void ClearSelectionButton_Click(object sender, RoutedEventArgs e)
    {
        SelectedGlyph = null;
        IsSelectionCleared = true;

        // Reset the color picker visually without persisting black (#000000).
        _suppressColorSelectionUpdate = true;
        try
        {
            FunctionColorPicker.Color = Colors.Black;
        }
        finally
        {
            _suppressColorSelectionUpdate = false;
        }

        SelectedColorHex = null;
    }
}
