// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.Controls;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SharedUI.ViewModel;

internal sealed partial class RollingStockMaintenancePanel : UserControl
{
    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
        nameof(ViewModel),
        typeof(RollingStockMaintenanceViewModel),
        typeof(RollingStockMaintenancePanel),
        new PropertyMetadata(null));

    public RollingStockMaintenancePanel()
    {
        InitializeComponent();
    }

    public RollingStockMaintenanceViewModel? ViewModel
    {
        get => (RollingStockMaintenanceViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }
}
