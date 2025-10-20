using CommunityToolkit.Mvvm.ComponentModel;

using System.Reflection;

namespace Moba.SharedUI.ViewModel;

public partial class PropertyViewModel : ObservableObject
{
    private readonly PropertyInfo _propertyInfo;
    private readonly object _target;

    public PropertyViewModel(PropertyInfo propertyInfo, object target)
    {
        _propertyInfo = propertyInfo;
        _target = target;
        Name = propertyInfo.Name;
    }

    [ObservableProperty]
    private string name = string.Empty;

    public string? Value
    {
        get => _propertyInfo.GetValue(_target)?.ToString();
        set
        {
            if (_propertyInfo.CanWrite && value != null)
            {
                var currentValue = _propertyInfo.GetValue(_target)?.ToString();
                if (currentValue == value)
                    return;

                try
                {
                    if (_propertyInfo.PropertyType == typeof(string))
                    {
                        _propertyInfo.SetValue(_target, value);
                    }
                    else
                    {
                        var convertedValue = Convert.ChangeType(value, _propertyInfo.PropertyType);
                        _propertyInfo.SetValue(_target, convertedValue);
                    }
                    OnPropertyChanged();
                }
                catch
                {
                    // ignored
                }
            }
        }
    }
}