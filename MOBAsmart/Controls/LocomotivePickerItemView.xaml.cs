// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI.Controls;

using SharedUI.ViewModel;

using System.Windows.Input;

/// <summary>
/// Tappable locomotive card. The whole card carries one TalkBack description with name, address and selection
/// state, so the screen reader does not read photo, name and address as three unrelated items.
/// </summary>
public partial class LocomotivePickerItemView
{
    public static readonly BindableProperty LocomotiveProperty = BindableProperty.Create(
        nameof(Locomotive),
        typeof(LocomotiveViewModel),
        typeof(LocomotivePickerItemView),
        propertyChanged: OnDescriptionSourceChanged);

    public static readonly BindableProperty IsSelectedProperty = BindableProperty.Create(
        nameof(IsSelected),
        typeof(bool),
        typeof(LocomotivePickerItemView),
        propertyChanged: OnDescriptionSourceChanged);

    public static readonly BindableProperty PhotoSourceProperty = BindableProperty.Create(
        nameof(PhotoSource),
        typeof(ImageSource),
        typeof(LocomotivePickerItemView));

    public static readonly BindableProperty SelectCommandProperty = BindableProperty.Create(
        nameof(SelectCommand),
        typeof(ICommand),
        typeof(LocomotivePickerItemView));

    public LocomotivePickerItemView()
    {
        InitializeComponent();
    }

    public LocomotiveViewModel? Locomotive
    {
        get => (LocomotiveViewModel?)GetValue(LocomotiveProperty);
        set => SetValue(LocomotiveProperty, value);
    }

    public bool IsSelected
    {
        get => (bool)GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    public ImageSource? PhotoSource
    {
        get => (ImageSource?)GetValue(PhotoSourceProperty);
        set => SetValue(PhotoSourceProperty, value);
    }

    public ICommand? SelectCommand
    {
        get => (ICommand?)GetValue(SelectCommandProperty);
        set => SetValue(SelectCommandProperty, value);
    }

    private static void OnDescriptionSourceChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is LocomotivePickerItemView item)
        {
            item.UpdateSemanticDescription();
        }
    }

    private void UpdateSemanticDescription()
    {
        var name = string.IsNullOrWhiteSpace(Locomotive?.Name) ? "Locomotive" : $"Locomotive {Locomotive.Name}";
        var address = Locomotive?.DigitalAddress is { } digitalAddress ? $", digital address {digitalAddress}" : string.Empty;
        var state = IsSelected ? ", selected" : string.Empty;
        SemanticProperties.SetDescription(this, $"{name}{address}{state}");
    }
}
