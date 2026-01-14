// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Microsoft.UI.Xaml.Controls;

using SharedUI.ViewModel;

public sealed partial class TrainControlPage
{
    public TrainControlViewModel ViewModel { get; }

    public TrainControlPage(TrainControlViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }
}