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

    /// <summary>
    /// Aktualisiert den DisplayName basierend auf dem DataContext.
    /// Wird aufgerufen, wenn Properties im PropertyGrid geändert werden.
    /// </summary>
    public void RefreshDisplayName()
    {
        if (DataContext == null)
            return;

        // Versuche die "Name"-Property zu lesen
        var nameProp = DataContext.GetType().GetProperty("Name");
        if (nameProp != null)
        {
            var newName = nameProp.GetValue(DataContext)?.ToString();
            if (!string.IsNullOrEmpty(newName) && newName != DisplayName)
            {
                DisplayName = newName;
            }
        }
    }
}