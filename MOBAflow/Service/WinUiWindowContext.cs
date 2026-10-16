// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Service;

using Microsoft.UI;
using Microsoft.UI.Xaml;

/// <summary>
/// The main window handle and XAML root that WinUI pickers and dialogs need; the main window sets them
/// once after its content is created.
/// </summary>
internal sealed class WinUiWindowContext
{
    private WindowId? _windowId;
    private XamlRoot? _xamlRoot;

    /// <summary>Records the main window once its content exists.</summary>
    public void Initialize(WindowId windowId, XamlRoot? xamlRoot)
    {
        _windowId = windowId;
        _xamlRoot = xamlRoot;
    }

    /// <summary>The main window id; throws when the window has not been initialized.</summary>
    public WindowId RequireWindowId() =>
        _windowId ?? throw new InvalidOperationException("The main window must be initialized before file dialogs are used.");

    /// <summary>The XAML root for content dialogs; throws when the window has not been initialized.</summary>
    public XamlRoot? RequireXamlRoot()
    {
        _ = RequireWindowId();
        return _xamlRoot;
    }
}
