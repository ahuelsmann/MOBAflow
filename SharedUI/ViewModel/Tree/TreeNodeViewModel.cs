namespace Moba.SharedUI.ViewModel.Tree;

using System.Collections.ObjectModel;

public class TreeNodeViewModel
{
    public TreeNodeViewModel(string name, object? tag = null)
    {
        Name = name;
        Tag = tag;
    }

    public string Name { get; set; }
    public object? Tag { get; set; }
    public ObservableCollection<TreeNodeViewModel> Children { get; } = new();
}