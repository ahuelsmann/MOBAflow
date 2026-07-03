// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.MAUI.Controls;

using System.Windows.Input;

/// <summary>
/// Themed +/- stepper control using Border instead of Button to avoid Android Material overrides.
/// </summary>
public partial class CounterStepperButton
{
    public static readonly BindableProperty CommandProperty = BindableProperty.Create(
        nameof(Command),
        typeof(ICommand),
        typeof(CounterStepperButton),
        propertyChanged: OnCommandChanged);

    public static readonly BindableProperty SymbolProperty = BindableProperty.Create(
        nameof(Symbol),
        typeof(string),
        typeof(CounterStepperButton),
        "+");

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public string Symbol
    {
        get => (string)GetValue(SymbolProperty);
        set => SetValue(SymbolProperty, value);
    }

    public CounterStepperButton()
    {
        InitializeComponent();
    }

    protected override void OnParentSet()
    {
        base.OnParentSet();
        UpdateEnabledVisual();
    }

    private static void OnCommandChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is not CounterStepperButton stepper)
        {
            return;
        }

        if (oldValue is ICommand oldCommand)
        {
            oldCommand.CanExecuteChanged -= stepper.OnCanExecuteChanged;
        }

        if (newValue is ICommand newCommand)
        {
            newCommand.CanExecuteChanged += stepper.OnCanExecuteChanged;
        }

        stepper.UpdateEnabledVisual();
    }

    private void OnCanExecuteChanged(object? sender, EventArgs e)
    {
        UpdateEnabledVisual();
    }

    private void UpdateEnabledVisual()
    {
        var canExecute = Command?.CanExecute(null) ?? false;
        StepperBorder.Opacity = canExecute ? 1.0 : 0.75;
        StepperBorder.StrokeThickness = canExecute ? 0 : 1;
        SymbolLabel.SetDynamicResource(Label.TextColorProperty, canExecute ? "TextPrimary" : "TextSecondary");
    }
}
