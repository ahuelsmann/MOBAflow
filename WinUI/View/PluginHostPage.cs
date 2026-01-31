// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.View;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

/// <summary>
/// Host page for plugin content.
/// This page is part of the WinUI project (not the plugin) so XamlTypeInfo can resolve it.
/// Plugin content is set dynamically via the SetPluginContent method.
/// </summary>
public sealed partial class PluginHostPage : Page
{
    public void SetPluginContent(UIElement content)
    {
        Content = content;
    }
}