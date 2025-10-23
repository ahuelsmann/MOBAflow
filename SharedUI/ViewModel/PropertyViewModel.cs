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

        // Underlying Type für Nullable<T> ermitteln
        var underlyingType = Nullable.GetUnderlyingType(propertyInfo.PropertyType) ?? propertyInfo.PropertyType;

        // Property-Typ prüfen
        IsBoolean = underlyingType == typeof(bool);
        IsEnum = underlyingType.IsEnum;

        // Enum-Werte sammeln
        if (IsEnum)
        {
            EnumValues = Enum.GetValues(underlyingType).Cast<object>().ToList();
        }
    }

    [ObservableProperty]
    private string name = string.Empty;

    /// <summary>
    /// Gibt an, ob die Property vom Typ Boolean ist
    /// </summary>
    public bool IsBoolean { get; }

    /// <summary>
    /// Gibt an, ob die Property vom Typ Enum ist
    /// </summary>
    public bool IsEnum { get; }

    /// <summary>
    /// Gibt an, ob die Property eine Objektreferenz ist (z.B. Workflow, Train, etc.)
    /// Wird true, wenn ReferenceValues gesetzt wurde
    /// </summary>
    public bool IsReference => ReferenceValues != null && ReferenceValues.Any();

    /// <summary>
    /// Liefert alle möglichen Enum-Werte für eine ComboBox
    /// </summary>
    public IEnumerable<object>? EnumValues { get; }

    /// <summary>
    /// Liefert alle verfügbaren Objekte für Referenz-Properties (z.B. alle Workflows)
    /// Wird von außen gesetzt (z.B. vom ViewModel)
    /// </summary>
    public IEnumerable<object>? ReferenceValues { get; set; }

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
                        // Versuch die Konvertierung
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
                        // Komplexe Typen werden ignoriert
                    }
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(BoolValue));
                    OnPropertyChanged(nameof(EnumValue));
                    OnPropertyChanged(nameof(ReferenceValue));
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

    /// <summary>
    /// Boolean-spezifische Property für Two-Way-Binding mit CheckBox
    /// </summary>
    public bool BoolValue
    {
        get => _propertyInfo.GetValue(_target) is bool b && b;
        set
        {
            if (_propertyInfo.CanWrite)
            {
                var currentValue = _propertyInfo.GetValue(_target);
                if (currentValue is bool current && current == value)
                    return;

                _propertyInfo.SetValue(_target, value);
                OnPropertyChanged();
                OnPropertyChanged(nameof(Value));
            }
        }
    }

    /// <summary>
    /// Enum-spezifische Property für Two-Way-Binding mit ComboBox
    /// </summary>
    public object? EnumValue
    {
        get => _propertyInfo.GetValue(_target);
        set
        {
            if (_propertyInfo.CanWrite && value != null)
            {
                var currentValue = _propertyInfo.GetValue(_target);
                if (Equals(currentValue, value))
                    return;

                _propertyInfo.SetValue(_target, value);
                OnPropertyChanged();
                OnPropertyChanged(nameof(Value));
            }
        }
    }

    /// <summary>
    /// Referenz-spezifische Property für Two-Way-Binding mit ComboBox
    /// (z.B. Workflow-Auswahl)
    /// </summary>
    public object? ReferenceValue
    {
        get
        {
            var value = _propertyInfo.GetValue(_target);

            // ✅ Debug: Log was wir zurückgeben
            if (value != null)
            {
                System.Diagnostics.Debug.WriteLine($"ReferenceValue GET: {value.GetType().Name} - {value}");

                // Prüfe ob Workflow
                if (value is Backend.Model.Workflow workflow)
                {
                    System.Diagnostics.Debug.WriteLine($"Workflow: Id={workflow.Id}, Name={workflow.Name}");
                }
            }

            return value;
        }
        set
        {
            if (_propertyInfo.CanWrite)
            {
                var currentValue = _propertyInfo.GetValue(_target);

                // ✅ Debug: Log was gesetzt wird
                System.Diagnostics.Debug.WriteLine($"ReferenceValue SET: {value?.GetType().Name ?? "null"}");
                if (value is Backend.Model.Workflow workflow)
                {
                    System.Diagnostics.Debug.WriteLine($"  Workflow: Id={workflow.Id}, Name={workflow.Name}");
                }

                if (Equals(currentValue, value))
                    return;

                _propertyInfo.SetValue(_target, value);
                OnPropertyChanged();
                OnPropertyChanged(nameof(Value));
                OnPropertyChanged(nameof(ReferenceValueName));
            }
        }
    }

    /// <summary>
    /// Name des referenzierten Objekts (z.B. Workflow.Name) für SelectedValue-Binding
    /// </summary>
    public string? ReferenceValueName
    {
        get
        {
            var value = _propertyInfo.GetValue(_target);
            if (value == null)
                return null;

            // Versuche "Name" Property zu lesen
            var nameProp = value.GetType().GetProperty("Name");
            var name = nameProp?.GetValue(value)?.ToString();

            // ✅ Debug
            System.Diagnostics.Debug.WriteLine($"ReferenceValueName GET: {name}");

            return name;
        }
        set
        {
            System.Diagnostics.Debug.WriteLine($"ReferenceValueName SET: {value}");

            if (_propertyInfo.CanWrite && ReferenceValues != null)
            {
                // Finde das Objekt mit dem passenden Namen
                var matchingItem = ReferenceValues.FirstOrDefault(item =>
               {
                   if (item == null)
                       return string.IsNullOrEmpty(value);

                   var nameProp = item.GetType().GetProperty("Name");
                   var itemName = nameProp?.GetValue(item)?.ToString();
                   return itemName == value;
               });

                System.Diagnostics.Debug.WriteLine($"  Matching Item: {matchingItem?.GetType().Name ?? "null"}");

                if (matchingItem != ReferenceValue)
                {
                    _propertyInfo.SetValue(_target, matchingItem);
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ReferenceValue));
                    OnPropertyChanged(nameof(Value));
                }
            }
        }
    }
}