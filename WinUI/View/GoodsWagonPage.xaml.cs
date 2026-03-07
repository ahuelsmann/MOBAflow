namespace Moba.WinUI.View;

using Common.Navigation;
using SharedUI.ViewModel;

[NavigationItem(
    Tag = "goodswagons",
    Title = "Goods Wagons",
    Icon = "\uE7C0",
    Category = NavigationCategory.Solution,
    Order = 27,
    FeatureToggleKey = "IsGoodsWagonsPageAvailable",
    BadgeLabelKey = "GoodsWagonsPageLabel")]
internal sealed partial class GoodsWagonPage
{
    public MainWindowViewModel ViewModel { get; }

    public GoodsWagonPage(MainWindowViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }
}
