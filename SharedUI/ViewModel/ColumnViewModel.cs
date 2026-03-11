using Moba.Common.Configuration;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SharedUI.ViewModel;

/// <summary>
/// ViewModel for managing the state of a single column in a grid layout.
/// Supports MVVM binding for width, expanded state, and visual state management.
/// </summary>
public class ColumnViewModel : ObservableObject
{
    private bool _isExpanded;
    private double _width;

    /// <summary>
    /// Gets the name of the column.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the minimum width the column can have when expanded.
    /// </summary>
    public double MinWidth { get; }

    /// <summary>
    /// Gets the maximum width the column can have when expanded.
    /// NaN means no maximum limit.
    /// </summary>
    public double MaxWidth { get; }

    /// <summary>
    /// Gets the default width for the column when no persisted value exists.
    /// </summary>
    public double DefaultWidth { get; }

    /// <summary>
    /// Gets or sets whether the column is expanded (visible) or collapsed (width = 0).
    /// </summary>
    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (SetProperty(ref _isExpanded, value))
            {
                OnPropertyChanged(nameof(Width));
                OnPropertyChanged(nameof(IsContentVisible));
            }
        }
    }

    /// <summary>
    /// Gets the width of the column when expanded.
    /// When collapsed, returns 0.
    /// </summary>
    public double Width
    {
        get => _isExpanded ? _width : 0;
        set
        {
            if (value < 0) value = 0;
            if (MaxWidth > 0 && !double.IsNaN(MaxWidth) && value > MaxWidth) value = MaxWidth;
            if (value < MinWidth) value = MinWidth;
            
            if (SetProperty(ref _width, value))
            {
                OnPropertyChanged(nameof(IsExpanded));
            }
        }
    }

    /// <summary>
    /// Gets whether the column content should be visible.
    /// </summary>
    public bool IsContentVisible => _isExpanded;

    public ColumnViewModel(string name, double defaultWidth, double minWidth = 200, double maxWidth = double.NaN)
    {
        Name = name;
        DefaultWidth = defaultWidth;
        MinWidth = minWidth;
        MaxWidth = maxWidth;
        _width = defaultWidth;
        _isExpanded = true;
    }

    /// <summary>
    /// Applies the column state to a column definition.
    /// This method should be called by platform-specific implementations.
    /// </summary>
    /// <param name="columnDefinition">The column definition to apply the state to.</param>
    public void ApplyToColumnDefinition(object columnDefinition)
    {
        // Platform-specific implementations should override this method
        // or handle the columnDefinition appropriately
        if (columnDefinition != null)
        {
            // Set width based on expanded state
            var width = IsExpanded ? _width : 0;
            
            // Use reflection to set Width property if available
            var widthProperty = columnDefinition.GetType().GetProperty("Width");
            if (widthProperty != null)
            {
                // Try to create GridLength if the type exists
                var gridLengthType = Type.GetType("Microsoft.UI.Xaml.GridLength");
                if (gridLengthType != null)
                {
                    var gridLength = Activator.CreateInstance(gridLengthType, width);
                    widthProperty.SetValue(columnDefinition, gridLength);
                }
            }
        }
    }

    /// <summary>
    /// Updates the column state from a column definition.
    /// This method should be called by platform-specific implementations.
    /// </summary>
    /// <param name="columnDefinition">The column definition to read the state from.</param>
    public void UpdateFromColumnDefinition(object columnDefinition)
    {
        if (columnDefinition != null)
        {
            // Use reflection to get Width property
            var widthProperty = columnDefinition.GetType().GetProperty("Width");
            if (widthProperty != null)
            {
                var widthValue = widthProperty.GetValue(columnDefinition);
                if (widthValue != null)
                {
                    // Try to get the Value property from GridLength
                    var valueProperty = widthValue.GetType().GetProperty("Value");
                    if (valueProperty != null)
                    {
                        var width = (double)valueProperty.GetValue(widthValue);
                        if (width > 0)
                        {
                            _width = width;
                            _isExpanded = true;
                        }
                        else
                        {
                            _isExpanded = false;
                        }
                    }
                }
            }
        }
        OnPropertyChanged(nameof(Width));
        OnPropertyChanged(nameof(IsExpanded));
    }

    /// <summary>
    /// Creates a ColumnState object for persistence.
    /// </summary>
    /// <returns>A ColumnState representing the current state.</returns>
    public ColumnState ToColumnState()
    {
        return new ColumnState
        {
            Width = _width,
            IsExpanded = _isExpanded,
            MinWidth = MinWidth,
            MaxWidth = MaxWidth,
            DefaultWidth = DefaultWidth
        };
    }

    /// <summary>
    /// Updates the column state from a ColumnState.
    /// </summary>
    /// <param name="state">The ColumnState to apply.</param>
    public void FromColumnState(ColumnState state)
    {
        _width = state.Width;
        _isExpanded = state.IsExpanded;
        OnPropertyChanged(nameof(Width));
        OnPropertyChanged(nameof(IsExpanded));
        OnPropertyChanged(nameof(IsContentVisible));
    }
}
