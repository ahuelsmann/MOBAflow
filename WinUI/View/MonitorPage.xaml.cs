// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Microsoft.UI.Xaml.Controls;
using SharedUI.ViewModel;

// ReSharper disable once PartialTypeWithSinglePart
public sealed partial class MonitorPage : Page
{
    public MonitorPageViewModel ViewModel { get; }

    public MonitorPage(MonitorPageViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }
}
