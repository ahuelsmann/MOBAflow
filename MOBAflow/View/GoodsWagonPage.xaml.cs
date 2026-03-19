namespace Moba.WinUI.View;

using Common.Navigation;
using SharedUI.ViewModel;

internal sealed partial class GoodsWagonPage
{
    public MainWindowViewModel ViewModel { get; }

    public GoodsWagonPage(MainWindowViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }
}
