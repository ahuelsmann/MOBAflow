// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
using CommunityToolkit.Mvvm.ComponentModel;

using Moba.Domain;

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
    /// Event is triggered when a property value has changed.
    /// Used to notify TreeNodes about changes.
    /// </summary>
    public event EventHandler? ValueChanged;

    public PropertyViewModel(PropertyInfo propertyInfo, object target)
    {
        _propertyInfo = propertyInfo;
        _target = target;

        // Read Display attribute if available
        var displayAttr = propertyInfo.GetCustomAttribute<DisplayAttribute>();
        Name = displayAttr?.Name ?? propertyInfo.Name;

        // Determine underlying type for Nullable<T>
        var underlyingType = Nullable.GetUnderlyingType(propertyInfo.PropertyType) ?? propertyInfo.PropertyType;

        // Check property type
        IsBoolean = underlyingType == typeof(bool);
        IsEnum = underlyingType.IsEnum;

        // Collect enum values
        if (IsEnum)
        {
            EnumValues = System.Enum.GetValues(underlyingType).Cast<object>().ToList();
        }

        // If the target implements INotifyPropertyChanged, register event handler
        if (_target is INotifyPropertyChanged notifyTarget)
        {
            notifyTarget.PropertyChanged += OnTargetPropertyChanged;
        }
    }

    // Called when a property on the target object changes - updates UI bindings automatically
    private void OnTargetPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Performance: Only react if it's our property OR if PropertyName is null (all properties)
        if (e.PropertyName == null || e.PropertyName == _propertyInfo.Name)
        {
            // Update UI bindings - only Value, the rest is updated automatically through binding
            OnPropertyChanged(nameof(Value));

            // Only update specific properties when necessary
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
    /// Indicates whether the property is of type Boolean
    /// </summary>
    public bool IsBoolean { get; }

    /// <summary>
    /// Indicates whether the property is of type Enum
    /// </summary>
    public bool IsEnum { get; }

    /// <summary>
    /// Indicates whether the property is an object reference (e.g., Workflow, Train, etc.)
    /// Returns true when ReferenceValues has been set
    /// </summary>
    public bool IsReference => ReferenceValues != null && ReferenceValues.Any();

    /// <summary>
    /// Provides all possible enum values for a ComboBox
    /// </summary>
    public IEnumerable<object>? EnumValues { get; }

    /// <summary>
    /// Provides all available objects for reference properties (e.g., all Workflows)
    /// Set from outside (e.g., by the ViewModel)
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
                        // Try conversion
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
                        // Complex types are ignored
                    }
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(BoolValue));
                    OnPropertyChanged(nameof(EnumValue));
                    OnPropertyChanged(nameof(ReferenceValue));
                }
                catch
                {
                    // On conversion errors, temporarily store the entered value
                    _pendingValue = value;
                    OnPropertyChanged();
                }
            }
        }
    }

    /// <summary>
    /// Boolean-specific property for two-way binding with CheckBox
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
    /// Enum-specific property for two-way binding with ComboBox
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
    /// Reference-specific property for two-way binding with ComboBox (e.g., workflow selection)
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
                if (value is Workflow workflow)
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
                if (value is Workflow workflow)
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

    /// <summary>
    /// Releases managed and unmanaged resources.
    /// </summary>
    /// <param name="disposing">True to release managed resources; otherwise, false.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing && _target is INotifyPropertyChanged notifyTarget)
            {
                notifyTarget.PropertyChanged -= OnTargetPropertyChanged;
            }

            // Unmanaged resources would be released here (none present)

            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
