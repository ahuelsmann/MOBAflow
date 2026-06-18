// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Moba.WinUI.View;

using Microsoft.UI.Xaml.Controls;
using Moba.SharedUI.ViewModel;
using System;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class DisplayPage : Page
{
    public DisplayViewModel ViewModel { get; }

    public DisplayPage(DisplayViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }

    private void OnDisplaySelectionChanged(object sender, SelectorBarSelectionChangedEventArgs args)
    {
        if (sender is not SelectorBar selectorBar ||
            selectorBar.SelectedItem?.Tag is not string tag ||
            !Enum.TryParse<DisplayConfigurationKind>(tag, out var kind))
        {
            return;
        }

        ViewModel.SelectConfiguration(kind);
    }
}