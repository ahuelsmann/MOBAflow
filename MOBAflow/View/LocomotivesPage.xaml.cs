// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Microsoft.UI.Xaml;
using Moba.SharedUI.ViewModel;

internal sealed partial class LocomotivesPage
{
    public MainWindowViewModel ViewModel { get; }

    private GridLength _listExpandedWidth = new(1, GridUnitType.Star);
    private GridLength _propertiesExpandedWidth = new(3.5, GridUnitType.Star);

    public LocomotivesPage(MainWindowViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();

        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.IsLocomotivesListExpanded))
        {
            if (!ViewModel.IsLocomotivesListExpanded)
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
        else if (e.PropertyName == nameof(ViewModel.IsLocomotivesPropertiesExpanded))
        {
            if (!ViewModel.IsLocomotivesPropertiesExpanded)
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
