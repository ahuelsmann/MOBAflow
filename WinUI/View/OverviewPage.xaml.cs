namespace Moba.WinUI.View;

using Microsoft.UI.Xaml.Controls;
using Moba.SharedUI.ViewModel;

/// <summary>
/// Overview page showing the Lap Counter Dashboard with Z21 connection and track statistics.
/// This page provides the same functionality as MAUI MainPage and WebApp Counter page.
/// </summary>
public sealed partial class OverviewPage : Page
{
    public CounterViewModel ViewModel { get; }

    public OverviewPage(CounterViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }
}
