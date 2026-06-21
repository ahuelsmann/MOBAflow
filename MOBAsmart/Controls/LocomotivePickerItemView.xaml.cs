// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI.Controls;

using SharedUI.ViewModel;

using System.Windows.Input;

public partial class LocomotivePickerItemView
{
    public static readonly BindableProperty LocomotiveProperty = BindableProperty.Create(
        nameof(Locomotive),
        typeof(LocomotiveViewModel),
        typeof(LocomotivePickerItemView));

    public static readonly BindableProperty IsSelectedProperty = BindableProperty.Create(
        nameof(IsSelected),
        typeof(bool),
        typeof(LocomotivePickerItemView));

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
}
