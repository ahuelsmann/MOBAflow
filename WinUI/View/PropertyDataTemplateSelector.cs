namespace Moba.WinUI.View;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using SharedUI.ViewModel;

/// <summary>
/// Wählt das passende DataTemplate basierend auf dem Property-Typ
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