// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Common.Navigation;

/// <summary>
/// Rudimentary template page with a 2-column grid layout for reuse as a starting point.
/// </summary>
[NavigationItem(
    Tag = "template",
    Title = "Template",
    Icon = "\uE8A1",
    Category = NavigationCategory.Core,
    Order = 200)]
internal sealed partial class TemplatePage
{
    public TemplatePage()
    {
        InitializeComponent();
    }
}
