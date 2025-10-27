using CommunityToolkit.Mvvm.ComponentModel;

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Moba.SharedUI.ViewModel;

public partial class PropertyViewModel : ObservableObject, IDisposable
{
    private readonly PropertyInfo _propertyInfo;
    private readonly object _target;
    private string? _pendingValue;
    private bool _disposed;

    /// <summary>
    /// Event wird ausgelöst, wenn ein Property-Wert geändert wurde.
    /// Wird verwendet, um TreeNodes über Änderungen zu informieren.
    /// </summary>
    public event EventHandler? ValueChanged;

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

        // Wenn das Target INotifyPropertyChanged implementiert, registriere Event-Handler
        if (_target is INotifyPropertyChanged notifyTarget)
        {
            notifyTarget.PropertyChanged += OnTargetPropertyChanged;
        }
    }

    /// <summary>
    /// Wird aufgerufen, wenn sich eine Property am Target-Objekt ändert.
    /// Aktualisiert die UI-Bindings automatisch.
    /// </summary>
    private void OnTargetPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // ✅ Performance: Nur reagieren, wenn es unsere Property ist ODER wenn PropertyName null ist (alle Properties)
        if (e.PropertyName == null || e.PropertyName == _propertyInfo.Name)
        {
            // UI-Bindings aktualisieren - nur Value, der Rest wird durch Binding automatisch aktualisiert
            OnPropertyChanged(nameof(Value));

            // Nur spezifische Properties aktualisieren, wenn nötig
            if (IsBoolean)
                OnPropertyChanged(nameof(BoolValue));
            if (IsEnum)
                OnPropertyChanged(nameof(EnumValue));
            if (IsReference)
            {
                OnPropertyChanged(nameof(ReferenceValue));
                OnPropertyChanged(nameof(ReferenceValueName));
            }
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
    public IEnumerable<object?>? ReferenceValues { get; set; }

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
                        ValueChanged?.Invoke(this, EventArgs.Empty);
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
                            ValueChanged?.Invoke(this, EventArgs.Empty);
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
                            ValueChanged?.Invoke(this, EventArgs.Empty);
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
                ValueChanged?.Invoke(this, EventArgs.Empty);
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
                ValueChanged?.Invoke(this, EventArgs.Empty);
                OnPropertyChanged();
                OnPropertyChanged(nameof(Value));
            }
        }
    }

    /// <summary>
    /// Reference-specific property for two-way binding with ComboBox
    /// (e.g., workflow selection)
    /// </summary>
    public object? ReferenceValue
    {
        get
        {
            var value = _propertyInfo.GetValue(_target);

            // Debug: Log what we're returning
            if (value != null)
            {
                System.Diagnostics.Debug.WriteLine($"ReferenceValue GET: {value.GetType().Name} - {value}");

                // Check if Workflow
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

                // Debug: Log what is being set
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
    /// Name of the referenced object (e.g., Workflow.Name) for SelectedValue binding
    /// </summary>
    public string? ReferenceValueName
    {
        get
        {
            var value = _propertyInfo.GetValue(_target);
            if (value == null)
                return null;

            // Try to read "Name" property
            var nameProp = value.GetType().GetProperty("Name");
            var valueName = nameProp?.GetValue(value)?.ToString();

            // Debug
            System.Diagnostics.Debug.WriteLine($"ReferenceValueName GET: {valueName}");

            return valueName;
        }
        set
        {
            System.Diagnostics.Debug.WriteLine($"ReferenceValueName SET: {value}");

            if (_propertyInfo.CanWrite && ReferenceValues != null)
            {
                // Find object with matching name
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

    //public void Refresh()
    //{
    //    // ✅ Performance: Nur notwendige Properties aktualisieren
    //    OnPropertyChanged(nameof(Value));

    //    if (IsBoolean)
    //        OnPropertyChanged(nameof(BoolValue));
    //    if (IsEnum)
    //        OnPropertyChanged(nameof(EnumValue));
    //    if (IsReference)
    //    {
    //        OnPropertyChanged(nameof(ReferenceValue));
    //        OnPropertyChanged(nameof(ReferenceValueName));
    //    }
    //}

    public void Dispose()
    {
        if (_disposed)
            return;

        // Event-Handler entfernen
        if (_target is INotifyPropertyChanged notifyTarget)
        {
            notifyTarget.PropertyChanged -= OnTargetPropertyChanged;
        }

        _disposed = true;
    }
}