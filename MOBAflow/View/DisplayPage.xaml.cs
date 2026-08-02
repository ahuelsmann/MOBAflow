namespace Moba.WinUI.View;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Moba.SharedUI.ViewModel;

/// <summary>
/// Displays live diagnostics and capability-gated commands for one configured ESP32 display.
/// </summary>
public sealed partial class DisplayPage : Page
{
    public DisplayViewModel ViewModel { get; }

    public DisplayPage(DisplayViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;
        ViewModel.SynchronizeConfiguration();
    }
}
