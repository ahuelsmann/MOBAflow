// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using System.Reflection;
using System.Runtime.InteropServices;

using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;

using Windows.ApplicationModel;
using Windows.Graphics;

public sealed partial class SplashWindow : Window
{
    private const int Width = 520;
    private const int Height = 320;
    private bool _isPrepared;

    public SplashWindow()
    {
        InitializeComponent();
        VersionTextBlock.Text = GetVersionText();
    }

    public void PrepareForDisplay()
    {
        if (_isPrepared)
        {
            return;
        }

        _isPrepared = true;

        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "mobaflow-icon.ico");
        if (File.Exists(iconPath))
        {
            AppWindow.SetIcon(iconPath);
        }

        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
            presenter.IsResizable = false;
        }

        CenterOnPrimaryDisplay();
    }

    private void CenterOnPrimaryDisplay()
    {
        var displayArea = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary);
        var workArea = displayArea.WorkArea;
        var x = workArea.X + (workArea.Width - Width) / 2;
        var y = workArea.Y + (workArea.Height - Height) / 2;

        AppWindow.MoveAndResize(new RectInt32(x, y, Width, Height));
    }

    private static string GetVersionText()
    {
        var version = GetPackageVersionText() ?? GetAssemblyVersionText();
        return $"Version {version} - (c) 2026 Andreas Huelsmann";
    }

    private static string? GetPackageVersionText()
    {
        try
        {
            var packageVersion = Package.Current.Id.Version;
            return $"{packageVersion.Major}.{packageVersion.Minor}.{packageVersion.Build}";
        }
        catch (InvalidOperationException)
        {
            return null;
        }
        catch (COMException)
        {
            return null;
        }
    }

    private static string GetAssemblyVersionText()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        return version is null
            ? "1.0.0"
            : $"{version.Major}.{version.Minor}.{version.Build}";
    }
}
