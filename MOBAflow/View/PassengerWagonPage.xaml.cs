namespace Moba.WinUI.View;

using Common.Navigation;
using SharedUI.ViewModel;

[NavigationItem(
    Tag = "passengerwagons",
    Title = "Passenger Wagons",
    Icon = "\uE7C0",
    Category = NavigationCategory.Solution,
    Order = 26,
    FeatureToggleKey = "IsPassengerWagonsPageAvailable",
    BadgeLabelKey = "PassengerWagonsPageLabel")]
internal sealed partial class PassengerWagonPage
{
    public MainWindowViewModel ViewModel { get; }

    public PassengerWagonPage(MainWindowViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }
}
