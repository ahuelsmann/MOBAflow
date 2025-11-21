// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.View;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using SharedUI.ViewModel;

/// <summary>
/// Provides the DataTemplate that matches the property type.
/// </summary>
public class PropertyDataTemplateSelector : DataTemplateSelector
{
    public DataTemplate? TextBoxTemplate { get; set; }
    public DataTemplate? CheckBoxTemplate { get; set; }
    public DataTemplate? ComboBoxTemplate { get; set; }
    public DataTemplate? ReferenceComboBoxTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        if (item is PropertyViewModel propertyViewModel)
        {
            // Objektreferenz (z.B. Workflow) → ComboBox mit Objekten
            if (propertyViewModel.IsReference)
            {
                return ReferenceComboBoxTemplate ?? ComboBoxTemplate ?? TextBoxTemplate ?? base.SelectTemplateCore(item, container);
            }

            // Enum → ComboBox
            if (propertyViewModel.IsEnum)
            {
                return ComboBoxTemplate ?? TextBoxTemplate ?? base.SelectTemplateCore(item, container);
            }

            // Boolean → CheckBox
            if (propertyViewModel.IsBoolean)
            {
                return CheckBoxTemplate ?? TextBoxTemplate ?? base.SelectTemplateCore(item, container);
            }
        }

        // Fallback → TextBox
        return TextBoxTemplate ?? base.SelectTemplateCore(item, container);
    }
}