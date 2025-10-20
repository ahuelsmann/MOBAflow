using CommunityToolkit.Mvvm.ComponentModel;

using System.Collections.ObjectModel;

namespace Moba.SharedUI.ViewModel;

public partial class TreeNodeViewModel : ObservableObject
{
    [ObservableProperty]
    private string displayName = string.Empty;

    [ObservableProperty]
    private string icon = "\uE8B7"; // Document icon

    [ObservableProperty]
    private bool isExpanded;

    [ObservableProperty]
    private ObservableCollection<TreeNodeViewModel> children = [];

    public object? DataContext { get; set; }
    public Type? DataType { get; set; }
}