using CommunityToolkit.Mvvm.ComponentModel;

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Moba.SharedUI.ViewModel;

public partial class PropertyViewModel : ObservableObject
{
    private readonly PropertyInfo _propertyInfo;
    private readonly object _target;
    private string? _pendingValue;

    public PropertyViewModel(PropertyInfo propertyInfo, object target)
    {
        _propertyInfo = propertyInfo;
        _target = target;

        // Display-Attribut auslesen, falls vorhanden
        var displayAttr = propertyInfo.GetCustomAttribute<DisplayAttribute>();
        Name = displayAttr?.Name ?? propertyInfo.Name;
    }

    [ObservableProperty]
    private string name = string.Empty;

    public string? Value
    {
        get => _pendingValue ?? _propertyInfo.GetValue(_target)?.ToString();
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
                        _pendingValue = null;
                    }
                    else
                    {
                        var converter = TypeDescriptor.GetConverter(_propertyInfo.PropertyType);
                        if (converter.CanConvertFrom(typeof(string)))
                        {
                            var convertedValue = converter.ConvertFromString(value);
                            _propertyInfo.SetValue(_target, convertedValue);
                            _pendingValue = null;
                        }
                        else if (_propertyInfo.PropertyType.IsPrimitive ||
                                 _propertyInfo.PropertyType.IsEnum ||
                                 _propertyInfo.PropertyType == typeof(decimal) ||
                                 _propertyInfo.PropertyType == typeof(DateTime) ||
                                 _propertyInfo.PropertyType == typeof(DateTimeOffset) ||
                                 _propertyInfo.PropertyType == typeof(TimeSpan) ||
                                 _propertyInfo.PropertyType == typeof(Guid))
                        {
                            var convertedValue = Convert.ChangeType(value, _propertyInfo.PropertyType);
                            _propertyInfo.SetValue(_target, convertedValue);
                            _pendingValue = null;
                        }
                        // Komplexe Typen werden ignoriert, da sie nicht aus Strings konvertiert werden können
                    }
                    OnPropertyChanged();
                }
                catch
                {
                    // Bei Konvertierungsfehlern den eingegebenen Wert temporär speichern
                    _pendingValue = value;
                    OnPropertyChanged();
                }
            }
        }
    }
}