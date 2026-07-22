// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.View;

using SharedUI.ViewModel;

// ReSharper disable once PartialTypeWithSinglePart
internal sealed partial class RecorderPage
{
    public RecorderPage(RecorderPageViewModel viewModel)
    {
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        InitializeComponent();
    }

    public RecorderPageViewModel ViewModel { get; }
}