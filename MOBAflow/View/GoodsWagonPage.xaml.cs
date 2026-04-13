namespace Moba.WinUI.View;

using Microsoft.UI.Xaml;

using Moba.SharedUI.ViewModel;

internal sealed partial class GoodsWagonPage
{
    public MainWindowViewModel ViewModel { get; }

    private GridLength _listExpandedWidth = new(1, GridUnitType.Star);
    private GridLength _propertiesExpandedWidth = new(3.5, GridUnitType.Star);

    public GoodsWagonPage(MainWindowViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();

        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        Unloaded += OnPageUnloaded;
    }

    private void OnPageUnloaded(object sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;
        ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
        Unloaded -= OnPageUnloaded;
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.IsGoodsWagonListExpanded))
        {
            if (!ViewModel.IsGoodsWagonListExpanded)
            {
                if (!ColList.Width.IsAuto)
                {
                    _listExpandedWidth = ColList.Width;
                }
                ColList.Width = GridLength.Auto;
            }
            else
            {
                ColList.Width = _listExpandedWidth;
            }
        }
        else if (e.PropertyName == nameof(ViewModel.IsGoodsWagonPropertiesExpanded))
        {
            if (!ViewModel.IsGoodsWagonPropertiesExpanded)
            {
                if (!ColProperties.Width.IsAuto)
                {
                    _propertiesExpandedWidth = ColProperties.Width;
                }
                ColProperties.Width = GridLength.Auto;
            }
            else
            {
                ColProperties.Width = _propertiesExpandedWidth;
            }
        }
    }
}
