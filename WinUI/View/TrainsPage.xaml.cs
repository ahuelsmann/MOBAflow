// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using SharedUI.ViewModel;

public sealed partial class TrainsPage
{
    public MainWindowViewModel ViewModel { get; }

    public TrainsPage(MainWindowViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }
}