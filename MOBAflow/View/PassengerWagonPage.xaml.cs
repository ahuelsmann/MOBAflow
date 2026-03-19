namespace Moba.WinUI.View;

using Common.Navigation;
using SharedUI.ViewModel;

internal sealed partial class PassengerWagonPage
{
    public MainWindowViewModel ViewModel { get; }

    public PassengerWagonPage(MainWindowViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }
}
