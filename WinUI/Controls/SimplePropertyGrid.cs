// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Controls;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

/// <summary>
/// Simple dynamic PropertyGrid that automatically generates UI for object properties.
/// Supports basic types: string, int, bool, double, DateTime, Enum.
/// </summary>
[TemplatePart(Name = "PART_PropertiesPanel", Type = typeof(StackPanel))]
public sealed partial class SimplePropertyGrid : Control
{
    private StackPanel? _propertiesPanel;
    private object? _currentTargetObject; // The actual object being displayed (might be Model instead of ViewModel)

    public SimplePropertyGrid()
    {
        this.DefaultStyleKey = typeof(SimplePropertyGrid);
    }

    public static readonly DependencyProperty SelectedObjectProperty =
        DependencyProperty.Register(
            nameof(SelectedObject),
            typeof(object),
            typeof(SimplePropertyGrid),
            new PropertyMetadata(null, OnSelectedObjectChanged));

    public object? SelectedObject
    {
        get => GetValue(SelectedObjectProperty);
        set => SetValue(SelectedObjectProperty, value);
    }

    private static void OnSelectedObjectChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SimplePropertyGrid grid)
        {
            System.Diagnostics.Debug.WriteLine($"🔄 SimplePropertyGrid.SelectedObject changed: {e.OldValue?.GetType().Name} → {e.NewValue?.GetType().Name}");
            grid.RefreshProperties();
        }
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _propertiesPanel = GetTemplateChild("PART_PropertiesPanel") as StackPanel;
        RefreshProperties();
    }

    private void RefreshProperties()
    {
        System.Diagnostics.Debug.WriteLine($"🔍 SimplePropertyGrid.RefreshProperties called");
        System.Diagnostics.Debug.WriteLine($"   _propertiesPanel: {(_propertiesPanel != null ? "OK" : "NULL")}");
        System.Diagnostics.Debug.WriteLine($"   SelectedObject: {(SelectedObject != null ? SelectedObject.GetType().Name : "NULL")}");

        if (SelectedObject == null)
        {
            if (_propertiesPanel != null)
            {
                _propertiesPanel.Children.Clear();
            }
            return;
        }

        // If panel is not yet loaded, force template application
        if (_propertiesPanel == null)
        {
            ApplyTemplate();
            _propertiesPanel = GetTemplateChild("PART_PropertiesPanel") as StackPanel;
            
            if (_propertiesPanel == null)
            {
                System.Diagnostics.Debug.WriteLine("⚠️ PropertyGrid: Template not yet applied, will retry on next change");
                return;
            }
        }

        _propertiesPanel.Children.Clear();

        // Check if SelectedObject is a ViewModel wrapper (has a Model property)
        var modelProperty = SelectedObject.GetType().GetProperty("Model");
        _currentTargetObject = SelectedObject;
        
        if (modelProperty != null && modelProperty.CanRead)
        {
            var modelValue = modelProperty.GetValue(SelectedObject);
            if (modelValue != null)
            {
                System.Diagnostics.Debug.WriteLine($"   📦 Found Model property, displaying Model properties instead of ViewModel");
                _currentTargetObject = modelValue;
            }
        }

        var properties = _currentTargetObject.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && !IsCollectionType(p.PropertyType) && IsSupportedType(p.PropertyType))
            .OrderBy(p => p.Name);

        System.Diagnostics.Debug.WriteLine($"   Found {properties.Count()} properties total (from {_currentTargetObject.GetType().Name})");

        foreach (var property in properties)
        {
            System.Diagnostics.Debug.WriteLine($"   ✅ Creating UI for: {property.Name} ({property.PropertyType.Name})");

            var propertyItem = CreatePropertyItem(property);
            if (propertyItem != null)
            {
                _propertiesPanel.Children.Add(propertyItem);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"   ❌ Failed to create UI for: {property.Name}");
            }
        }

        System.Diagnostics.Debug.WriteLine($"   Total properties displayed: {_propertiesPanel.Children.Count}");
    }

    private bool IsCollectionType(Type type)
    {
        // Check if it's a generic collection
        if (type.IsGenericType)
        {
            var genericDef = type.GetGenericTypeDefinition();
            if (genericDef == typeof(List<>) || 
                genericDef == typeof(ObservableCollection<>) ||
                genericDef == typeof(IEnumerable<>) ||
                genericDef == typeof(ICollection<>) ||
                genericDef == typeof(IList<>))
            {
                return true;
            }
        }

        // Check if it implements IEnumerable (but not string)
        if (type != typeof(string) && typeof(System.Collections.IEnumerable).IsAssignableFrom(type))
        {
            return true;
        }

        return false;
    }

    private bool IsSupportedType(Type type)
    {
        // Handle Nullable<T>
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        // Supported types that can be edited in PropertyGrid
        return underlyingType == typeof(string) ||
               underlyingType == typeof(int) ||
               underlyingType == typeof(uint) ||
               underlyingType == typeof(double) ||
               underlyingType == typeof(float) ||
               underlyingType == typeof(bool) ||
               underlyingType == typeof(DateTime) ||
               underlyingType.IsEnum;
    }

    private FrameworkElement? CreatePropertyItem(PropertyInfo property)
    {
        var container = new StackPanel
        {
            Spacing = 2,
            Margin = new Thickness(0, 0, 0, 12)
        };

        // Label
        var label = new TextBlock
        {
            Text = GetDisplayName(property),
            Style = Application.Current.Resources["CaptionTextBlockStyle"] as Style,
            Foreground = Application.Current.Resources["TextFillColorSecondaryBrush"] as Microsoft.UI.Xaml.Media.Brush
        };
        container.Children.Add(label);

        // Editor based on type
        var editor = CreateEditor(property);
        if (editor != null)
        {
            container.Children.Add(editor);
        }

        return container;
    }

    private FrameworkElement? CreateEditor(PropertyInfo property)
    {
        var propertyType = property.PropertyType;

        // Handle Nullable<T>
        if (Nullable.GetUnderlyingType(propertyType) != null)
        {
            propertyType = Nullable.GetUnderlyingType(propertyType)!;
        }

        // String
        if (propertyType == typeof(string))
        {
            var textBox = new TextBox();
            BindProperty(textBox, TextBox.TextProperty, property);
            
            // Multi-line for Description properties
            if (property.Name.Contains("Description"))
            {
                textBox.AcceptsReturn = true;
                textBox.TextWrapping = TextWrapping.Wrap;
                textBox.MinHeight = 48;
            }
            
            return textBox;
        }

        // Int, UInt, Double
        if (propertyType == typeof(int) || propertyType == typeof(uint) || 
            propertyType == typeof(double) || propertyType == typeof(float))
        {
            var numberBox = new NumberBox
            {
                SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline,
                Minimum = 0
            };

            // Bind via Value (requires converter for uint)
            if (propertyType == typeof(int))
            {
                BindProperty(numberBox, NumberBox.ValueProperty, property, ConvertToDouble, ConvertFromDouble);
            }
            else if (propertyType == typeof(uint))
            {
                BindProperty(numberBox, NumberBox.ValueProperty, property, 
                    value => Convert.ToDouble(value), 
                    value => Convert.ToUInt32(value));
            }
            else
            {
                BindProperty(numberBox, NumberBox.ValueProperty, property);
            }

            return numberBox;
        }

        // Bool
        if (propertyType == typeof(bool))
        {
            var checkBox = new CheckBox
            {
                Content = property.Name
            };
            BindProperty(checkBox, CheckBox.IsCheckedProperty, property, 
                value => (bool)value, 
                value => value is true);
            return checkBox;
        }

        // Enum
        if (propertyType.IsEnum)
        {
            var comboBox = new ComboBox
            {
                ItemsSource = Enum.GetNames(propertyType),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            BindProperty(comboBox, ComboBox.SelectedItemProperty, property, 
                value => value.ToString(), 
                value => Enum.Parse(propertyType, value.ToString()!));
            return comboBox;
        }

        // DateTime
        if (propertyType == typeof(DateTime))
        {
            var datePicker = new DatePicker();
            // Note: Simplified - would need proper DateTimeOffset conversion
            return datePicker;
        }

        return null;
    }

    private void BindProperty(DependencyObject target, DependencyProperty targetProperty, PropertyInfo sourceProperty,
        Func<object, object>? toTarget = null, Func<object, object>? toSource = null)
    {
        // Simple two-way binding implementation
        
        var currentValue = sourceProperty.GetValue(_currentTargetObject);
        
        // Skip if value is null and target property doesn't support null
        if (currentValue == null)
        {
            System.Diagnostics.Debug.WriteLine($"   ⚠️ Property {sourceProperty.Name} is null, skipping binding");
            return;
        }
        
        try
        {
            if (toTarget != null)
            {
                currentValue = toTarget(currentValue);
            }
            
            target.SetValue(targetProperty, currentValue);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"   ❌ Failed to bind property {sourceProperty.Name}: {ex.Message}");
            return;
        }

        // Subscribe to changes
        if (target is TextBox textBox)
        {
            textBox.TextChanged += (s, e) =>
            {
                object newValue = textBox.Text;
                if (toSource != null)
                    newValue = toSource(newValue);
                sourceProperty.SetValue(_currentTargetObject, newValue);
            };
        }
        else if (target is NumberBox numberBox)
        {
            numberBox.ValueChanged += (s, e) =>
            {
                object newValue = numberBox.Value;
                if (toSource != null)
                    newValue = toSource(newValue);
                sourceProperty.SetValue(_currentTargetObject, newValue);
            };
        }
        else if (target is CheckBox checkBox)
        {
            checkBox.Checked += (s, e) => sourceProperty.SetValue(_currentTargetObject, true);
            checkBox.Unchecked += (s, e) => sourceProperty.SetValue(_currentTargetObject, false);
        }
        else if (target is ComboBox comboBox)
        {
            comboBox.SelectionChanged += (s, e) =>
            {
                if (comboBox.SelectedItem != null && toSource != null)
                {
                    var newValue = toSource(comboBox.SelectedItem);
                    sourceProperty.SetValue(_currentTargetObject, newValue);
                }
            };
        }
    }

    private string GetDisplayName(PropertyInfo property)
    {
        var displayAttr = property.GetCustomAttribute<DisplayNameAttribute>();
        if (displayAttr != null)
            return displayAttr.DisplayName;

        // Add spaces before capitals: "FirstName" -> "First Name"
        return System.Text.RegularExpressions.Regex.Replace(property.Name, "([a-z])([A-Z])", "$1 $2");
    }

    private object ConvertToDouble(object value) => Convert.ToDouble(value);
    private object ConvertFromDouble(object value) => Convert.ToInt32(value);
}