// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Controls;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
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
        if (_propertiesPanel == null || SelectedObject == null)
            return;

        _propertiesPanel.Children.Clear();

        var properties = SelectedObject.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite)
            .OrderBy(p => p.Name);

        foreach (var property in properties)
        {
            // Skip collections
            if (property.PropertyType.IsGenericType && 
                property.PropertyType.GetGenericTypeDefinition() == typeof(System.Collections.Generic.List<>))
                continue;

            var propertyItem = CreatePropertyItem(property);
            if (propertyItem != null)
            {
                _propertiesPanel.Children.Add(propertyItem);
            }
        }
    }

    private FrameworkElement? CreatePropertyItem(PropertyInfo property)
    {
        var container = new StackPanel
        {
            Spacing = 4,
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
                textBox.MinHeight = 60;
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
        // In production, use Binding with IValueConverter
        
        var currentValue = sourceProperty.GetValue(SelectedObject);
        if (currentValue != null && toTarget != null)
        {
            currentValue = toTarget(currentValue);
        }
        target.SetValue(targetProperty, currentValue);

        // Subscribe to changes
        if (target is TextBox textBox)
        {
            textBox.TextChanged += (s, e) =>
            {
                object newValue = textBox.Text;
                if (toSource != null)
                    newValue = toSource(newValue);
                sourceProperty.SetValue(SelectedObject, newValue);
            };
        }
        else if (target is NumberBox numberBox)
        {
            numberBox.ValueChanged += (s, e) =>
            {
                object newValue = numberBox.Value;
                if (toSource != null)
                    newValue = toSource(newValue);
                sourceProperty.SetValue(SelectedObject, newValue);
            };
        }
        else if (target is CheckBox checkBox)
        {
            checkBox.Checked += (s, e) => sourceProperty.SetValue(SelectedObject, true);
            checkBox.Unchecked += (s, e) => sourceProperty.SetValue(SelectedObject, false);
        }
        else if (target is ComboBox comboBox)
        {
            comboBox.SelectionChanged += (s, e) =>
            {
                if (comboBox.SelectedItem != null && toSource != null)
                {
                    var newValue = toSource(comboBox.SelectedItem);
                    sourceProperty.SetValue(SelectedObject, newValue);
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
