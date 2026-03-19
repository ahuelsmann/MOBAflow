// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Common.Navigation;
using SharedUI.ViewModel;

internal sealed partial class LocomotivesPage
{
    public MainWindowViewModel ViewModel { get; }

    public LocomotivesPage(MainWindowViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }
}
