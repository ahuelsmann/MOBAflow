// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using ViewModel;

internal sealed partial class MatrixPage
{
    public MatrixPageViewModel ViewModel { get; }

    public MatrixPage(MatrixPageViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }
}