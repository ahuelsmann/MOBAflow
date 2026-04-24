namespace Moba.WinUI.View;

using Microsoft.UI.Xaml;

using ViewModel;

internal sealed partial class DisplayPage
{
    public DisplayPageViewModel ViewModel { get; }

    public DisplayPage(DisplayPageViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ViewModel = viewModel;
        InitializeComponent();
        Unloaded += OnUnloaded;
    }

    private async void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;

        Unloaded -= OnUnloaded;
        await ViewModel.ShutdownAsync();
    }
}
