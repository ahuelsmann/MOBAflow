using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Moba.SharedUI.ViewModel;
using MainWindowViewModel = Moba.SharedUI.ViewModel.WinUI.MainWindowViewModel;

namespace Moba.WinUI.View;

public sealed partial class ExplorerPage : Page
{
    public MainWindowViewModel ViewModel { get; }

    public ExplorerPage(MainWindowViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }

    private void SolutionTreeView_SelectionChanged(TreeView sender, TreeViewSelectionChangedEventArgs args)
    {
        if (args.AddedItems.Count > 0 && args.AddedItems[0] is TreeViewItem item && item.Tag is TreeNodeViewModel node)
        {
            ViewModel.OnNodeSelected(node);
        }
    }
}
