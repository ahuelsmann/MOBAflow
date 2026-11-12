// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.WinUI.Controls.SignalBox;

using SharedUI.Appearance;

using Domain;

using Moba.WinUI.Controls;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

using SharedUI.Interface;
using SharedUI.ViewModel;

using System.ComponentModel;

public sealed partial class SignalBoxPropertiesControl
{
    private SignalBoxPropertiesViewModel? _editorViewModel;
    private bool _isUpdatingControls;

    public SignalBoxPropertiesViewModel? EditorViewModel
    {
        get => _editorViewModel;
        set
        {
            if (ReferenceEquals(_editorViewModel, value))
            {
                return;
            }

            if (_editorViewModel is not null)
            {
                _editorViewModel.PropertyChanged -= OnEditorViewModelPropertyChanged;
            }

            _editorViewModel = value;
            if (_editorViewModel is not null)
            {
                _editorViewModel.PropertyChanged += OnEditorViewModelPropertyChanged;
            }

            UpdatePropertiesPanel();
        }
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
            SetSelectionVisibility(this, hasSelection: false);
            return;
        }

        SetSelectionVisibility(this, hasSelection: true);
        RunWhileUpdatingControls(() =>
            UpdateSelectedElementProperties(this, viewModel, selectedElement));
    }

    private static void SetSelectionVisibility(
        SignalBoxPropertiesControl control,
        bool hasSelection)
    {
        control.NoSelectionInfo.Visibility = hasSelection
            ? Visibility.Collapsed
            : Visibility.Visible;
        control.ElementPropertiesPanel.Visibility = hasSelection
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private static void UpdateSelectedElementProperties(
        SignalBoxPropertiesControl control,
        SignalBoxPropertiesViewModel viewModel,
        SbElement selectedElement)
    {
        UpdateGeneralProperties(control, viewModel);

        if (selectedElement is SbSignal signal)
        {
            UpdateSignalProperties(control, viewModel, signal);
        }

        if (selectedElement is SbSwitch)
        {
            control.UpdateSwitchButtons();
        }
    }

    private static void UpdateGeneralProperties(
        SignalBoxPropertiesControl control,
        SignalBoxPropertiesViewModel viewModel)
    {
        control.ElementNameBox.Text = viewModel.ElementName;
        control.AddressPanel.Visibility = ToVisibility(viewModel.IsAddressVisible);
        control.ElementAddressBox.Header = viewModel.AddressHeader;
        control.ElementAddressBox.Value = viewModel.ElementAddress ?? double.NaN;
        control.SignalAspectPanel.Visibility = ToVisibility(viewModel.IsSignalSelected);
        control.MultiplexConfigPanel.Visibility = ToVisibility(viewModel.IsSignalSelected);
        control.SwitchPositionPanel.Visibility = ToVisibility(viewModel.IsSwitchSelected);
    }

    private static void UpdateSignalProperties(
        SignalBoxPropertiesControl control,
        SignalBoxPropertiesViewModel viewModel,
        SbSignal signal)
    {
        control.UpdateMultiplexerOptions(viewModel);
        UpdateSignalArticleOptions(control, viewModel);
        control.BaseAddressBox.Value = viewModel.BaseAddress is { } baseAddress
            ? baseAddress
            : double.NaN;
        control.SpeedIndicatorConfigPanel.Visibility = ToVisibility(viewModel.IsSpeedIndicatorVisible);
        control.TopSpeedIndicatorBox.Value = viewModel.TopSpeedIndicator is { } topSpeed
            ? topSpeed
            : double.NaN;
        control.BottomSpeedIndicatorBox.Value = viewModel.BottomSpeedIndicator is { } bottomSpeed
            ? bottomSpeed
            : double.NaN;
        ApplySupportedAspectVisibility(control, viewModel);
        control.UpdateAspectButtons();
        control.UpdateAspectPresentation(signal);
    }

    private void UpdateMultiplexerOptions(SignalBoxPropertiesViewModel viewModel)
    {
        ApplyOptions(
            MultiplexerComboBox,
            viewModel.MultiplexerOptions.Select(option => (option.ArticleNumber, option.DisplayName)),
            viewModel.SelectedMultiplexerArticleNumber);
    }

    private static void UpdateSignalArticleOptions(
        SignalBoxPropertiesControl control,
        SignalBoxPropertiesViewModel viewModel)
    {
        PopulateSignalArticleOptions(
            control.MainSignalComboBox,
            viewModel.MainSignalOptions,
            viewModel.SelectedMainSignalArticleNumber);
        PopulateSignalArticleOptions(
            control.DistantSignalComboBox,
            viewModel.DistantSignalOptions,
            viewModel.SelectedDistantSignalArticleNumber);
    }

    private static void PopulateSignalArticleOptions(
        ComboBox comboBox,
        IReadOnlyList<SignalArticleOption> options,
        string? selectedArticle)
    {
        ApplyOptions(
            comboBox,
            options.Select(option => (option.ArticleNumber, option.DisplayName)),
            selectedArticle);
    }

    // A selection publishes a change that refreshes this panel while the ComboBox is still inside its own
    // SelectionChanged. WinUI rejects clearing its items there (COMException 0x8000FFFF, #402), so the items are only
    // rebuilt when the options actually differ; otherwise only the selection is synchronized.
    private static void ApplyOptions(
        ComboBox comboBox,
        IEnumerable<(string ArticleNumber, string DisplayName)> options,
        string? selectedArticle)
    {
        var wanted = options.ToArray();
        if (!HasItems(comboBox, wanted))
        {
            comboBox.Items.Clear();
            foreach (var (articleNumber, displayName) in wanted)
            {
                comboBox.Items.Add(new ComboBoxItem
                {
                    Content = displayName,
                    Tag = articleNumber
                });
            }
        }

        var selectedItem = FindOption(comboBox, selectedArticle);
        if (!ReferenceEquals(comboBox.SelectedItem, selectedItem))
        {
            comboBox.SelectedItem = selectedItem;
        }
    }

    private static bool HasItems(ComboBox comboBox, (string ArticleNumber, string DisplayName)[] options)
    {
        if (comboBox.Items.Count != options.Length)
        {
            return false;
        }

        for (var index = 0; index < options.Length; index++)
        {
            if (comboBox.Items[index] is not ComboBoxItem item ||
                !string.Equals(item.Tag?.ToString(), options[index].ArticleNumber, StringComparison.Ordinal) ||
                !string.Equals(item.Content?.ToString(), options[index].DisplayName, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
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

    private static void ApplySupportedAspectVisibility(
        SignalBoxPropertiesControl control,
        SignalBoxPropertiesViewModel viewModel)
    {
        control.AspectHp0Button.Visibility = ToVisibility(viewModel.IsAspectAvailable(SignalAspect.Hp0));
        control.AspectKs1Button.Visibility = ToVisibility(viewModel.IsAspectAvailable(SignalAspect.Ks1));
        control.AspectKs2Button.Visibility = ToVisibility(viewModel.IsAspectAvailable(SignalAspect.Ks2));
        control.AspectKs1BlinkButton.Visibility = ToVisibility(viewModel.IsAspectAvailable(SignalAspect.Ks1Blink));
        control.AspectKennlichtButton.Visibility = ToVisibility(viewModel.IsAspectAvailable(SignalAspect.Kennlicht));
        control.AspectDunkelButton.Visibility = ToVisibility(viewModel.IsAspectAvailable(SignalAspect.Dunkel));
        control.AspectRa12Button.Visibility = ToVisibility(viewModel.IsAspectAvailable(SignalAspect.Ra12));
        control.AspectZs1Button.Visibility = ToVisibility(viewModel.IsAspectAvailable(SignalAspect.Zs1));
        control.AspectZs7Button.Visibility = ToVisibility(viewModel.IsAspectAvailable(SignalAspect.Zs7));
    }

    private static Visibility ToVisibility(bool isVisible) =>
        isVisible ? Visibility.Visible : Visibility.Collapsed;

    private void UpdateAspectButtons()
    {
        if (EditorViewModel?.SelectedElement is not SbSignal signal)
        {
            return;
        }

        // The checked tile is the current aspect; the system style draws it in accent or High Contrast highlight.
        foreach (var (button, aspect) in EnumerateAspectButtons(this))
        {
            button.IsChecked = signal.SignalAspect == aspect;
        }
    }

    private static IEnumerable<(ToggleButton Button, SignalAspect Aspect)> EnumerateAspectButtons(
        SignalBoxPropertiesControl control)
    {
        yield return (control.AspectHp0Button, SignalAspect.Hp0);
        yield return (control.AspectKs1Button, SignalAspect.Ks1);
        yield return (control.AspectKs2Button, SignalAspect.Ks2);
        yield return (control.AspectKs1BlinkButton, SignalAspect.Ks1Blink);
        yield return (control.AspectKennlichtButton, SignalAspect.Kennlicht);
        yield return (control.AspectDunkelButton, SignalAspect.Dunkel);
        yield return (control.AspectRa12Button, SignalAspect.Ra12);
        yield return (control.AspectZs1Button, SignalAspect.Zs1);
        yield return (control.AspectZs7Button, SignalAspect.Zs7);
    }

    private void UpdateAspectPresentation(SbSignal? signal)
    {
        var isSpeedIndicatorSignal = KsSignalAspectNames.Is4046Signal(signal?.MainSignalArticleNumber);
        var signalArticleNumber = KsSignalAspectNames.ResolvePreviewSignalArticleNumber(signal?.MainSignalArticleNumber);

        ApplyAspectPreviewSignals(signalArticleNumber, signal);
        ApplyAspectTexts(isSpeedIndicatorSignal);
    }

    private void ApplyAspectPreviewSignals(
        string signalArticleNumber,
        SbSignal? selectedSignal)
    {
        var topSpeedIndicator = selectedSignal?.TopSpeedIndicator ?? string.Empty;
        var bottomSpeedIndicator = selectedSignal?.BottomSpeedIndicator ?? string.Empty;

        foreach (var (screen, aspect) in EnumerateAspectSignals(this))
        {
            screen.ApplyVisualState(
                signalArticleNumber,
                topSpeedIndicator,
                bottomSpeedIndicator,
                KsSignalAspectNames.ToAspectName(aspect));
        }
    }

    private static IEnumerable<(KsSignalScreen Screen, SignalAspect Aspect)> EnumerateAspectSignals(
        SignalBoxPropertiesControl control)
    {
        yield return (control.AspectHp0Signal, SignalAspect.Hp0);
        yield return (control.AspectKs1Signal, SignalAspect.Ks1);
        yield return (control.AspectKs2Signal, SignalAspect.Ks2);
        yield return (control.AspectKs1BlinkSignal, SignalAspect.Ks1Blink);
        yield return (control.AspectKennlichtSignal, SignalAspect.Kennlicht);
        yield return (control.AspectDunkelSignal, SignalAspect.Dunkel);
        yield return (control.AspectRa12Signal, SignalAspect.Ra12);
        yield return (control.AspectZs1Signal, SignalAspect.Zs1);
        yield return (control.AspectZs7Signal, SignalAspect.Zs7);
    }

    private void ApplyAspectTexts(bool isSpeedIndicatorSignal)
    {
        foreach (var (label, aspect) in EnumerateAspectLabels(this))
            label.Text = KsSignalAspectNames.GetAspectLabel(aspect, isSpeedIndicatorSignal);

        // The tile shows only the short label; the tooltip explains it and the accessible name carries both.
        foreach (var (button, aspect) in EnumerateAspectButtons(this))
        {
            ToolTipService.SetToolTip(button, KsSignalAspectNames.GetAspectDescription(aspect, isSpeedIndicatorSignal));
            AutomationProperties.SetName(
                button,
                KsSignalAspectNames.GetAspectAccessibleName(aspect, isSpeedIndicatorSignal));
        }
    }

    private static IEnumerable<(TextBlock Label, SignalAspect Aspect)> EnumerateAspectLabels(
        SignalBoxPropertiesControl control)
    {
        yield return (control.AspectHp0Label, SignalAspect.Hp0);
        yield return (control.AspectKs1Label, SignalAspect.Ks1);
        yield return (control.AspectKs2Label, SignalAspect.Ks2);
        yield return (control.AspectKs1BlinkLabel, SignalAspect.Ks1Blink);
        yield return (control.AspectKennlichtLabel, SignalAspect.Kennlicht);
        yield return (control.AspectDunkelLabel, SignalAspect.Dunkel);
        yield return (control.AspectRa12Label, SignalAspect.Ra12);
        yield return (control.AspectZs1Label, SignalAspect.Zs1);
        yield return (control.AspectZs7Label, SignalAspect.Zs7);
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

    private void OnAspectClicked(object sender, RoutedEventArgs args)
    {
        _ = args;
        if (sender is ToggleButton { Tag: string aspectName } &&
            Enum.TryParse<SignalAspect>(aspectName, out var aspect))
        {
            EditorViewModel?.SetSignalAspectCommand.Execute(aspect);
        }

        // The clicked tile has checked itself; the signal's aspect decides which tile stays checked.
        UpdateAspectButtons();
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
