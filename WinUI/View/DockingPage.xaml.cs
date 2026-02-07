// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.View;

using Microsoft.UI.Xaml.Controls;
using Moba.WinUI.ViewModel;

public sealed partial class DockingPage : Page
{
    public DockingPage(DockingPageViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
