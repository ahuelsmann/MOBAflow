// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Controls.SignalBox;

using Common.Display;

using Domain;

using Moba.WinUI.Controls;
using Moba.WinUI.View;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

using SharedUI.Interface;
using SharedUI.ViewModel;

using System.ComponentModel;

public sealed partial class SignalBoxPropertiesControl
{
    private bool _isUpdatingControls;

    public static readonly DependencyProperty EditorViewModelProperty = DependencyProperty.Register(
        nameof(EditorViewModel),
        typeof(SignalBoxPropertiesViewModel),
        typeof(SignalBoxPropertiesControl),
        new PropertyMetadata(null, OnEditorViewModelChanged));

    public SignalBoxPropertiesViewModel? EditorViewModel
    {
        get => (SignalBoxPropertiesViewModel?)GetValue(EditorViewModelProperty);
        set => SetValue(EditorViewModelProperty, value);
    }

    public SignalBoxPropertiesControl()
    {
        InitializeComponent();
    }

    public void RefreshAspectDisplay()
    {
        UpdateAspectButtons();
        UpdateAspectPresentation(EditorViewModel?.SelectedElement as SbSignal);
    }

    private static void OnEditorViewModelChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs args)
    {
        if (dependencyObject is not SignalBoxPropertiesControl control)
        {
            return;
        }

        if (args.OldValue is SignalBoxPropertiesViewModel oldViewModel)
        {
            oldViewModel.PropertyChanged -= control.OnEditorViewModelPropertyChanged;
        }

        if (args.NewValue is SignalBoxPropertiesViewModel newViewModel)
        {
            newViewModel.PropertyChanged += control.OnEditorViewModelPropertyChanged;
        }

        control.UpdatePropertiesPanel();
    }

    private void OnEditorViewModelPropertyChanged(
        object? sender,
        PropertyChangedEventArgs args)
    {
        _ = sender;
        _ = args;
        UpdatePropertiesPanel();
    }

    private void UpdatePropertiesPanel()
    {
        var viewModel = EditorViewModel;
        var selectedElement = viewModel?.SelectedElement;
        if (viewModel == null || selectedElement == null)
        {
            NoSelectionInfo.Visibility = Visibility.Visible;
            ElementPropertiesPanel.Visibility = Visibility.Collapsed;
            return;
        }

        NoSelectionInfo.Visibility = Visibility.Collapsed;
        ElementPropertiesPanel.Visibility = Visibility.Visible;

        RunWhileUpdatingControls(() =>
        {
            ElementNameBox.Text = viewModel.ElementName;
            AddressPanel.Visibility = viewModel.IsAddressVisible
                ? Visibility.Visible
                : Visibility.Collapsed;
            ElementAddressBox.Header = viewModel.AddressHeader;
            ElementAddressBox.Value = viewModel.ElementAddress ?? double.NaN;

            SignalAspectPanel.Visibility = viewModel.IsSignalSelected
                ? Visibility.Visible
                : Visibility.Collapsed;
            MultiplexConfigPanel.Visibility = viewModel.IsSignalSelected
                ? Visibility.Visible
                : Visibility.Collapsed;
            SwitchPositionPanel.Visibility = viewModel.IsSwitchSelected
                ? Visibility.Visible
                : Visibility.Collapsed;

            if (selectedElement is SbSignal signal)
            {
                UpdateMultiplexerOptions(viewModel);
                UpdateSignalArticleOptions(viewModel);
                BaseAddressBox.Value = viewModel.BaseAddress is { } baseAddress
                    ? baseAddress
                    : double.NaN;
                SpeedIndicatorConfigPanel.Visibility = viewModel.IsSpeedIndicatorVisible
                    ? Visibility.Visible
                    : Visibility.Collapsed;
                TopSpeedIndicatorBox.Value = viewModel.TopSpeedIndicator is { } topSpeed
                    ? topSpeed
                    : double.NaN;
                BottomSpeedIndicatorBox.Value = viewModel.BottomSpeedIndicator is { } bottomSpeed
                    ? bottomSpeed
                    : double.NaN;
                ApplySupportedAspectVisibility(viewModel);
                UpdateAspectButtons();
                UpdateAspectPresentation(signal);
            }

            if (selectedElement is SbSwitch)
            {
                UpdateSwitchButtons();
            }
        });
    }

    private void UpdateMultiplexerOptions(SignalBoxPropertiesViewModel viewModel)
    {
        MultiplexerComboBox.Items.Clear();
        foreach (var option in viewModel.MultiplexerOptions)
        {
            MultiplexerComboBox.Items.Add(new ComboBoxItem
            {
                Content = option.DisplayName,
                Tag = option.ArticleNumber
            });
        }

        MultiplexerComboBox.SelectedItem = FindOption(
            MultiplexerComboBox,
            viewModel.SelectedMultiplexerArticleNumber);
    }

    private void UpdateSignalArticleOptions(SignalBoxPropertiesViewModel viewModel)
    {
        PopulateSignalArticleOptions(
            MainSignalComboBox,
            viewModel.MainSignalOptions,
            viewModel.SelectedMainSignalArticleNumber);
        PopulateSignalArticleOptions(
            DistantSignalComboBox,
            viewModel.DistantSignalOptions,
            viewModel.SelectedDistantSignalArticleNumber);
    }

    private static void PopulateSignalArticleOptions(
        ComboBox comboBox,
        IReadOnlyList<SignalArticleOption> options,
        string? selectedArticle)
    {
        comboBox.Items.Clear();
        foreach (var option in options)
        {
            comboBox.Items.Add(new ComboBoxItem
            {
                Content = option.DisplayName,
                Tag = option.ArticleNumber
            });
        }

        comboBox.SelectedItem = FindOption(comboBox, selectedArticle);
    }

    private static ComboBoxItem? FindOption(ComboBox comboBox, string? articleNumber)
    {
        if (string.IsNullOrWhiteSpace(articleNumber))
        {
            return null;
        }

        return comboBox.Items
            .OfType<ComboBoxItem>()
            .FirstOrDefault(item =>
                string.Equals(
                    item.Tag?.ToString(),
                    articleNumber,
                    StringComparison.Ordinal));
    }

    private void ApplySupportedAspectVisibility(SignalBoxPropertiesViewModel viewModel)
    {
        AspectHp0Button.Visibility = ToVisibility(viewModel.IsAspectAvailable(SignalAspect.Hp0));
        AspectKs1Button.Visibility = ToVisibility(viewModel.IsAspectAvailable(SignalAspect.Ks1));
        AspectKs2Button.Visibility = ToVisibility(viewModel.IsAspectAvailable(SignalAspect.Ks2));
        AspectKs1BlinkButton.Visibility = ToVisibility(viewModel.IsAspectAvailable(SignalAspect.Ks1Blink));
        AspectKennlichtButton.Visibility = ToVisibility(viewModel.IsAspectAvailable(SignalAspect.Kennlicht));
        AspectDunkelButton.Visibility = ToVisibility(viewModel.IsAspectAvailable(SignalAspect.Dunkel));
        AspectRa12Button.Visibility = ToVisibility(viewModel.IsAspectAvailable(SignalAspect.Ra12));
        AspectZs1Button.Visibility = ToVisibility(viewModel.IsAspectAvailable(SignalAspect.Zs1));
        AspectZs7Button.Visibility = ToVisibility(viewModel.IsAspectAvailable(SignalAspect.Zs7));
    }

    private static Visibility ToVisibility(bool isVisible) =>
        isVisible ? Visibility.Visible : Visibility.Collapsed;

    private void UpdateAspectButtons()
    {
        if (EditorViewModel?.SelectedElement is not SbSignal signal)
        {
            return;
        }

        var accentBrush = ThemeResourceResolver.ResolveBrush(
            this,
            "AccentFillColorDefaultBrush",
            Microsoft.UI.Colors.Blue);
        var normalBrush = ThemeResourceResolver.ResolveBrush(
            this,
            "SubtleFillColorSecondaryBrush",
            Microsoft.UI.Colors.Gray);

        foreach (var (button, aspect) in EnumerateAspectButtons())
        {
            button.Background = signal.SignalAspect == aspect
                ? accentBrush
                : normalBrush;
        }
    }

    private IEnumerable<(Border Button, SignalAspect Aspect)> EnumerateAspectButtons()
    {
        yield return (AspectHp0Button, SignalAspect.Hp0);
        yield return (AspectKs1Button, SignalAspect.Ks1);
        yield return (AspectKs2Button, SignalAspect.Ks2);
        yield return (AspectKs1BlinkButton, SignalAspect.Ks1Blink);
        yield return (AspectKennlichtButton, SignalAspect.Kennlicht);
        yield return (AspectDunkelButton, SignalAspect.Dunkel);
        yield return (AspectRa12Button, SignalAspect.Ra12);
        yield return (AspectZs1Button, SignalAspect.Zs1);
        yield return (AspectZs7Button, SignalAspect.Zs7);
    }

    private void UpdateAspectPresentation(SbSignal? signal)
    {
        var isSpeedIndicatorSignal = signal is not null &&
            string.Equals(
                signal.MainSignalArticleNumber,
                "4046",
                StringComparison.Ordinal);
        var signalArticleNumber = isSpeedIndicatorSignal ? "4046" : string.Empty;

        ApplyAspectPreviewSignals(signalArticleNumber, signal);
        ApplyAspectLabels(isSpeedIndicatorSignal);
        ApplyAspectTooltips(isSpeedIndicatorSignal);
    }

    private void ApplyAspectPreviewSignals(
        string signalArticleNumber,
        SbSignal? selectedSignal)
    {
        var topSpeedIndicator = selectedSignal?.TopSpeedIndicator ?? string.Empty;
        var bottomSpeedIndicator = selectedSignal?.BottomSpeedIndicator ?? string.Empty;

        foreach (var (screen, aspect) in EnumerateAspectSignals())
        {
            screen.ApplyVisualState(
                signalArticleNumber,
                topSpeedIndicator,
                bottomSpeedIndicator,
                KsSignalAspectNames.ToAspectName(aspect));
        }
    }

    private IEnumerable<(KsSignalScreen Screen, SignalAspect Aspect)> EnumerateAspectSignals()
    {
        yield return (AspectHp0Signal, SignalAspect.Hp0);
        yield return (AspectKs1Signal, SignalAspect.Ks1);
        yield return (AspectKs2Signal, SignalAspect.Ks2);
        yield return (AspectKs1BlinkSignal, SignalAspect.Ks1Blink);
        yield return (AspectKennlichtSignal, SignalAspect.Kennlicht);
        yield return (AspectDunkelSignal, SignalAspect.Dunkel);
        yield return (AspectRa12Signal, SignalAspect.Ra12);
        yield return (AspectZs1Signal, SignalAspect.Zs1);
        yield return (AspectZs7Signal, SignalAspect.Zs7);
    }

    private void ApplyAspectLabels(bool isSpeedIndicatorSignal)
    {
        AspectHp0Label.Text = KsSignalAspectNames.GetAspectLabel(
            SignalAspect.Hp0,
            isSpeedIndicatorSignal);
        AspectKs1Label.Text = KsSignalAspectNames.GetAspectLabel(
            SignalAspect.Ks1,
            isSpeedIndicatorSignal);
        AspectKs2Label.Text = KsSignalAspectNames.GetAspectLabel(
            SignalAspect.Ks2,
            isSpeedIndicatorSignal);
        AspectKs1BlinkLabel.Text = KsSignalAspectNames.GetAspectLabel(
            SignalAspect.Ks1Blink,
            isSpeedIndicatorSignal);
        AspectKennlichtLabel.Text = KsSignalAspectNames.GetAspectLabel(
            SignalAspect.Kennlicht,
            isSpeedIndicatorSignal);
        AspectDunkelLabel.Text = KsSignalAspectNames.GetAspectLabel(
            SignalAspect.Dunkel,
            isSpeedIndicatorSignal);
        AspectRa12Label.Text = KsSignalAspectNames.GetAspectLabel(
            SignalAspect.Ra12,
            isSpeedIndicatorSignal);
        AspectZs1Label.Text = KsSignalAspectNames.GetAspectLabel(
            SignalAspect.Zs1,
            isSpeedIndicatorSignal);
        AspectZs7Label.Text = KsSignalAspectNames.GetAspectLabel(
            SignalAspect.Zs7,
            isSpeedIndicatorSignal);
    }

    private void ApplyAspectTooltips(bool isSpeedIndicatorSignal)
    {
        ToolTipService.SetToolTip(AspectHp0Button, "Hp 0 - Stop");
        ToolTipService.SetToolTip(AspectKs1Button, "Ks 1 - Proceed");
        ToolTipService.SetToolTip(
            AspectKs2Button,
            isSpeedIndicatorSignal
                ? "Ks 2 with white marker light at the top left"
                : "Ks 2 - Expect stop");
        ToolTipService.SetToolTip(
            AspectKs1BlinkButton,
            isSpeedIndicatorSignal
                ? "Ks 2 with white marker light at the top left and top speed indicator"
                : "Ks 1 flashing - Proceed with speed pre-indicator");
        ToolTipService.SetToolTip(
            AspectKennlichtButton,
            isSpeedIndicatorSignal
                ? "Only white marker light at the top left"
                : "Marker light - Signal disabled for operations");
        ToolTipService.SetToolTip(
            AspectDunkelButton,
            isSpeedIndicatorSignal
                ? "Green flashing with white marker light at the top left and top/bottom speed indicators"
                : "Dark mode - Signal inactive");
        ToolTipService.SetToolTip(
            AspectRa12Button,
            isSpeedIndicatorSignal
                ? "Hp0 with white marker light at the bottom for shunting movements"
                : "Sh 1/Ra 12 - Shunting allowed");
        ToolTipService.SetToolTip(
            AspectZs1Button,
            isSpeedIndicatorSignal
                ? "Ks 1 with top speed indicator"
                : "Zs 1 - Substitute signal (white flashing)");
        ToolTipService.SetToolTip(
            AspectZs7Button,
            "Zs 7 - Caution signal (3x yellow)");
    }

    private void UpdateSwitchButtons()
    {
        if (EditorViewModel?.SelectedElement is not SbSwitch sw)
        {
            return;
        }

        ThirdSwitchColumn.Width = new GridLength(1, GridUnitType.Star);
        SwitchRightButton.Visibility = Visibility.Visible;
        SwitchLeftButton.Visibility = Visibility.Visible;

        var accentStyle = (Style)Application.Current.Resources["AccentButtonStyle"];
        var defaultStyle = (Style)Application.Current.Resources["DefaultButtonStyle"];
        ApplySwitchButtonStyle(
            SwitchStraightButton,
            sw.SwitchPosition == SwitchPosition.Straight,
            accentStyle,
            defaultStyle);
        ApplySwitchButtonStyle(
            SwitchLeftButton,
            sw.SwitchPosition == SwitchPosition.DivergingLeft,
            accentStyle,
            defaultStyle);
        ApplySwitchButtonStyle(
            SwitchRightButton,
            sw.SwitchPosition == SwitchPosition.DivergingRight,
            accentStyle,
            defaultStyle);
    }

    private static void ApplySwitchButtonStyle(
        Button button,
        bool isActive,
        Style accentStyle,
        Style defaultStyle)
    {
        button.Style = isActive ? accentStyle : defaultStyle;
    }

    private void OnElementNameChanged(object sender, TextChangedEventArgs args)
    {
        _ = sender;
        _ = args;
        if (!_isUpdatingControls)
        {
            EditorViewModel?.SetElementName(ElementNameBox.Text);
        }
    }

    private void OnRotateClicked(object sender, RoutedEventArgs args)
    {
        _ = args;
        if (TryGetButtonTag(sender, out var rotationTag) &&
            int.TryParse(rotationTag, out var rotation))
        {
            EditorViewModel?.RotateCommand.Execute(rotation);
        }
    }

    private void OnAspectClicked(object sender, PointerRoutedEventArgs args)
    {
        _ = args;
        if (sender is Border { Tag: string aspectName } &&
            Enum.TryParse<SignalAspect>(aspectName, out var aspect))
        {
            EditorViewModel?.SetSignalAspectCommand.Execute(aspect);
        }
    }

    private void OnSwitchPositionClicked(object sender, RoutedEventArgs args)
    {
        _ = args;
        if (TryGetButtonTag(sender, out var positionTag) &&
            Enum.TryParse<SwitchPosition>(positionTag, out var position))
        {
            EditorViewModel?.SetSwitchPositionCommand.Execute(position);
        }
    }

    private void OnAddressChanged(
        NumberBox sender,
        NumberBoxValueChangedEventArgs args)
    {
        _ = sender;
        if (!_isUpdatingControls && TryGetNumberBoxIntValue(args, out var value))
        {
            EditorViewModel?.SetElementAddress(value);
        }
    }

    private void OnDeleteElementClicked(object sender, RoutedEventArgs args)
    {
        _ = sender;
        _ = args;
        EditorViewModel?.DeleteSelectedElementCommand.Execute(null);
    }

    private void OnMultiplexerSelected(object sender, SelectionChangedEventArgs args)
    {
        _ = sender;
        _ = args;
        if (_isUpdatingControls)
        {
            return;
        }

        var articleNumber = (MultiplexerComboBox.SelectedItem as ComboBoxItem)?
            .Tag?
            .ToString();
        EditorViewModel?.SelectMultiplexer(articleNumber);
    }

    private void OnMainSignalSelected(object sender, SelectionChangedEventArgs args)
    {
        _ = sender;
        _ = args;
        if (!_isUpdatingControls &&
            MainSignalComboBox.SelectedItem is ComboBoxItem { Tag: string articleNumber })
        {
            EditorViewModel?.SelectMainSignalArticle(articleNumber);
        }
    }

    private void OnDistantSignalSelected(object sender, SelectionChangedEventArgs args)
    {
        _ = sender;
        _ = args;
        if (_isUpdatingControls)
        {
            return;
        }

        var articleNumber = (DistantSignalComboBox.SelectedItem as ComboBoxItem)?
            .Tag?
            .ToString();
        EditorViewModel?.SelectDistantSignalArticle(articleNumber);
    }

    private void OnBaseAddressChanged(
        NumberBox sender,
        NumberBoxValueChangedEventArgs args)
    {
        _ = sender;
        if (!_isUpdatingControls && TryGetNumberBoxIntValue(args, out var value))
        {
            EditorViewModel?.SetBaseAddress(value);
        }
    }

    private void OnTopSpeedIndicatorChanged(
        NumberBox sender,
        NumberBoxValueChangedEventArgs args)
    {
        _ = sender;
        if (!_isUpdatingControls)
        {
            EditorViewModel?.SetTopSpeedIndicator(ParseNullableNumber(args.NewValue));
        }
    }

    private void OnBottomSpeedIndicatorChanged(
        NumberBox sender,
        NumberBoxValueChangedEventArgs args)
    {
        _ = sender;
        if (!_isUpdatingControls)
        {
            EditorViewModel?.SetBottomSpeedIndicator(ParseNullableNumber(args.NewValue));
        }
    }

    private void RunWhileUpdatingControls(Action update)
    {
        _isUpdatingControls = true;
        try
        {
            update();
        }
        finally
        {
            _isUpdatingControls = false;
        }
    }

    private static bool TryGetButtonTag(object sender, out string tag)
    {
        if (sender is Button { Tag: string buttonTag })
        {
            tag = buttonTag;
            return true;
        }

        tag = string.Empty;
        return false;
    }

    private static bool TryGetNumberBoxIntValue(
        NumberBoxValueChangedEventArgs args,
        out int value)
    {
        if (double.IsNaN(args.NewValue))
        {
            value = 0;
            return false;
        }

        value = (int)args.NewValue;
        return true;
    }

    private static int? ParseNullableNumber(double value) =>
        double.IsNaN(value) ? null : (int)value;
}
