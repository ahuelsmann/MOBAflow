// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Controls;

using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.UI;

/// <summary>
/// One symbol entry shown in the picker.
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
/// Window for selecting a PNG symbol from MOBAflow/Assets/FunctionSymbols for a function button (Train Control).
/// Enumerates the deployed FunctionSymbols catalog at runtime.
/// </summary>
internal sealed partial class FunctionSymbolPickerWindow : Window
{
    /// <summary>
    /// After closing: selected PNG asset filename (e.g. "headlight.png") or null on cancel.
    /// </summary>
    public string? SelectedGlyph { get; private set; }

    public string? SelectedColorHex { get; private set; }

    /// <summary>
    /// True when the user explicitly cleared glyph and color via "Auswahl loeschen".
    /// Distinguishes intentional clear on Ok from Cancel (both leave null selections).
    /// </summary>
    public bool IsSelectionCleared { get; private set; }

    public bool IsConfirmed { get; private set; }

    public ElementTheme SelectedTheme
    {
        get => RootGrid.RequestedTheme;
        set => RootGrid.RequestedTheme = value;
    }

    private readonly TaskCompletionSource<bool> _tcs = new();
    private bool _suppressColorSelectionUpdate;

    private const int DefaultWindowWidth = 750;
    private const int DefaultWindowHeight = 600;
    private const int MinimumWindowWidth = 700;
    private const int MinimumWindowHeight = 550;
    private const int WM_GETMINMAXINFO = 0x0024;
    private delegate IntPtr SubclassProcDelegate(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, UIntPtr uIdSubclass, IntPtr dwRefData);
    private SubclassProcDelegate? _subclassDelegate;
    private static readonly Lazy<IReadOnlyList<FunctionSymbolItem>> Symbols = new(LoadSymbols);

    [DllImport("comctl32.dll", SetLastError = true)]
    private static extern bool SetWindowSubclass(IntPtr hWnd, SubclassProcDelegate pfnSubclass, UIntPtr uIdSubclass, IntPtr dwRefData);

    [DllImport("comctl32.dll", SetLastError = true)]
    private static extern IntPtr DefSubclassProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hwnd);

    [StructLayout(LayoutKind.Sequential)]
    private struct MINMAXINFO
    {
        public int ptReservedX, ptReservedY;
        public int ptMaxSizeX, ptMaxSizeY;
        public int ptMaxPositionX, ptMaxPositionY;
        public int ptMinTrackSizeX, ptMinTrackSizeY;
        public int ptMaxTrackSizeX, ptMaxTrackSizeY;
    }

    /// <summary>
    /// PNG filenames that are not function-button symbols and must be excluded from the library.
    /// Compared case-insensitively.
    /// </summary>
    private static readonly HashSet<string> ExcludedAssets = new(StringComparer.OrdinalIgnoreCase)
    {
        "door_close.png",
        "door_open.png",
        "door_blocked.png",
        "mobaflow-icon.png"
    };

    /// <summary>
    /// Loads PNG asset filenames from the deployed FunctionSymbols catalog (dark/32 preview size).
    /// New exports from Figma are picked up on the next build (csproj globs FunctionSymbols PNGs).
    /// </summary>
    private static IReadOnlyList<FunctionSymbolItem> LoadSymbols()
    {
        try
        {
            var catalogDir = Path.Combine(AppContext.BaseDirectory, "Assets", "FunctionSymbols", "dark", "32");
            if (!Directory.Exists(catalogDir))
                return Array.Empty<FunctionSymbolItem>();

            var culture = CultureInfo.GetCultureInfo("de-DE");
            return Directory.EnumerateFiles(catalogDir, "*.png", SearchOption.TopDirectoryOnly)
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

    public FunctionSymbolPickerWindow()
    {
        InitializeComponent();
        SymbolsItemsControl.ItemsSource = Symbols.Value;

        Title = "Symbol für Funktionstaste auswählen";

        // Setup custom window properties
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.PreferredMinimumWidth = MinimumWindowWidth;
            presenter.PreferredMinimumHeight = MinimumWindowHeight;
            presenter.IsMaximizable = true;
            presenter.IsMinimizable = true;
            presenter.IsResizable = true;
        }

        // Center on parent window (MainWindow)
        if (App.MainWindow != null)
        {
            var parentAppWindow = App.MainWindow.AppWindow;
            if (parentAppWindow != null)
            {
                var parentPos = parentAppWindow.Position;
                var parentSize = parentAppWindow.Size;

                int width = DefaultWindowWidth;
                int height = DefaultWindowHeight;

                int x = parentPos.X + (parentSize.Width - width) / 2;
                int y = parentPos.Y + (parentSize.Height - height) / 2;

                AppWindow.MoveAndResize(new Windows.Graphics.RectInt32(x, y, width, height));
            }
        }
        else
        {
            AppWindow.Resize(new Windows.Graphics.SizeInt32(DefaultWindowWidth, DefaultWindowHeight));
        }

        Closed += (s, e) => _tcs.TrySetResult(false);

        _subclassDelegate = new SubclassProcDelegate(WindowSubclassProc);
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        SetWindowSubclass(hwnd, _subclassDelegate, 1, IntPtr.Zero);
    }

    private IntPtr WindowSubclassProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, UIntPtr uIdSubclass, IntPtr dwRefData)
    {
        if (uMsg == WM_GETMINMAXINFO)
        {
            var dpi = GetDpiForWindow(hWnd);
            float scalingFactor = dpi / 96f;

            var minMaxInfo = Marshal.PtrToStructure<MINMAXINFO>(lParam);
            minMaxInfo.ptMinTrackSizeX = (int)(MinimumWindowWidth * scalingFactor);
            minMaxInfo.ptMinTrackSizeY = (int)(MinimumWindowHeight * scalingFactor);
            Marshal.StructureToPtr(minMaxInfo, lParam, true);

            return IntPtr.Zero;
        }

        return DefSubclassProc(hWnd, uMsg, wParam, lParam);
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

        IsConfirmed = true;
        _tcs.TrySetResult(true);
        Close();
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        IsConfirmed = true;
        _tcs.TrySetResult(true);
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        IsConfirmed = false;
        SelectedGlyph = null;
        SelectedColorHex = null;
        _tcs.TrySetResult(false);
        Close();
    }

    public Task<bool> ShowDialogAsync()
    {
        Activate();
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.Maximize();
        }

        return _tcs.Task;
    }
}
