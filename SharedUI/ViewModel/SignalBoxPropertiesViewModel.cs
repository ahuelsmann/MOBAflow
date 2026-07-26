// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Common.Multiplex;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Domain;

using Interface;

using System.Globalization;

/// <summary>
/// Owns signal-box property editing without depending on a UI framework.
/// </summary>
public sealed partial class SignalBoxPropertiesViewModel : ObservableObject
{
    private const int MaximumElementAddress = 2048;
    private const int MaximumMultiplexerAddress = 2044;
    private const string SpeedIndicatorSignalArticle = "4046";

    private readonly ISignalArticleCatalog _articleCatalog;
    private readonly IMultiplexerProvider _multiplexerProvider;

    public SignalBoxPropertiesViewModel(
        ISignalArticleCatalog articleCatalog,
        IMultiplexerProvider multiplexerProvider)
    {
        ArgumentNullException.ThrowIfNull(articleCatalog);
        ArgumentNullException.ThrowIfNull(multiplexerProvider);

        _articleCatalog = articleCatalog;
        _multiplexerProvider = multiplexerProvider;
        MultiplexerOptions = _multiplexerProvider
            .GetAllDefinitions()
            .Select(definition => new SignalBoxMultiplexerOption(
                definition.ArticleNumber,
                definition.DisplayName))
            .ToArray();
    }

    public event EventHandler<SignalBoxPropertyChangeEventArgs>? ElementChanged;

    public event EventHandler<SignalBoxElementEventArgs>? DeletionRequested;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelection))]
    [NotifyPropertyChangedFor(nameof(IsSignalSelected))]
    [NotifyPropertyChangedFor(nameof(IsSwitchSelected))]
    [NotifyPropertyChangedFor(nameof(IsAddressVisible))]
    [NotifyPropertyChangedFor(nameof(ElementName))]
    [NotifyPropertyChangedFor(nameof(ElementAddress))]
    [NotifyPropertyChangedFor(nameof(AddressHeader))]
    [NotifyPropertyChangedFor(nameof(SelectedMultiplexerArticleNumber))]
    [NotifyPropertyChangedFor(nameof(SelectedMainSignalArticleNumber))]
    [NotifyPropertyChangedFor(nameof(SelectedDistantSignalArticleNumber))]
    [NotifyPropertyChangedFor(nameof(BaseAddress))]
    [NotifyPropertyChangedFor(nameof(TopSpeedIndicator))]
    [NotifyPropertyChangedFor(nameof(BottomSpeedIndicator))]
    [NotifyPropertyChangedFor(nameof(IsSpeedIndicatorVisible))]
    public partial SbElement? SelectedElement { get; set; }

    public bool HasSelection => SelectedElement is not null;

    public bool IsSignalSelected => SelectedElement is SbSignal;

    public bool IsSwitchSelected => SelectedElement is SbSwitch;

    public bool IsAddressVisible => SelectedElement is SbSwitch or SbDetector;

    public string ElementName => SelectedElement?.Name ?? string.Empty;

    public int? ElementAddress => SelectedElement switch
    {
        SbSwitch sw => sw.Address,
        SbDetector detector => detector.FeedbackAddress,
        _ => null
    };

    public string AddressHeader => SelectedElement is SbDetector
        ? "Feedback address"
        : "DCC address (switch)";

    public IReadOnlyList<SignalBoxMultiplexerOption> MultiplexerOptions { get; }

    public IReadOnlyList<SignalArticleOption> MainSignalOptions { get; private set; } = [];

    public IReadOnlyList<SignalArticleOption> DistantSignalOptions { get; private set; } = [];

    public IReadOnlyCollection<SignalAspect> SupportedAspects { get; private set; } =
        Enum.GetValues<SignalAspect>();

    public string? SelectedMultiplexerArticleNumber =>
        (SelectedElement as SbSignal)?.MultiplexerArticleNumber;

    public string? SelectedMainSignalArticleNumber =>
        (SelectedElement as SbSignal)?.MainSignalArticleNumber;

    public string? SelectedDistantSignalArticleNumber =>
        (SelectedElement as SbSignal)?.DistantSignalArticleNumber;

    public int? BaseAddress => (SelectedElement as SbSignal)?.BaseAddress;

    public int? TopSpeedIndicator =>
        ParseSpeedIndicator((SelectedElement as SbSignal)?.TopSpeedIndicator);

    public int? BottomSpeedIndicator =>
        ParseSpeedIndicator((SelectedElement as SbSignal)?.BottomSpeedIndicator);

    public bool IsSpeedIndicatorVisible =>
        string.Equals(
            SelectedMainSignalArticleNumber,
            SpeedIndicatorSignalArticle,
            StringComparison.Ordinal);

    public bool IsAspectAvailable(SignalAspect aspect) =>
        SupportedAspects.Count == 0 || SupportedAspects.Contains(aspect);

    public void SetElementName(string value)
    {
        if (SelectedElement is not { } element ||
            string.Equals(element.Name, value, StringComparison.Ordinal))
        {
            return;
        }

        element.Name = value;
        PublishChange(element, requiresVisualRefresh: true, requiresPersistence: true);
    }

    public void SetElementAddress(int value)
    {
        if (value is < 1 or > MaximumElementAddress)
        {
            return;
        }

        switch (SelectedElement)
        {
            case SbSwitch sw when sw.Address != value:
                sw.Address = value;
                PublishChange(sw, requiresVisualRefresh: false, requiresPersistence: true);
                break;
            case SbDetector detector when detector.FeedbackAddress != value:
                detector.FeedbackAddress = value;
                PublishChange(detector, requiresVisualRefresh: false, requiresPersistence: true);
                break;
        }
    }

    public void SelectMultiplexer(string? articleNumber)
    {
        if (SelectedElement is not SbSignal signal)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(articleNumber))
        {
            ResetMultiplexer(signal);
            return;
        }

        MultiplexerDefinition definition;
        try
        {
            definition = _multiplexerProvider.GetDefinition(articleNumber);
        }
        catch (ArgumentException)
        {
            return;
        }

        signal.MultiplexerArticleNumber = definition.ArticleNumber;
        signal.IsMultiplexed = true;
        RefreshSignalOptions(signal);

        signal.MainSignalArticleNumber = ResolveSelectedArticle(
            signal.MainSignalArticleNumber,
            MainSignalOptions,
            definition.MainSignalArticleNumber);
        signal.DistantSignalArticleNumber = ResolveSelectedArticle(
            signal.DistantSignalArticleNumber,
            DistantSignalOptions,
            definition.DistantSignalArticleNumber);

        RefreshSignalProjection(signal);
        PublishChange(signal, requiresVisualRefresh: true, requiresPersistence: true);
    }

    public void SelectMainSignalArticle(string articleNumber)
    {
        if (SelectedElement is not SbSignal signal ||
            !ContainsArticle(MainSignalOptions, articleNumber) ||
            string.Equals(signal.MainSignalArticleNumber, articleNumber, StringComparison.Ordinal))
        {
            return;
        }

        signal.MainSignalArticleNumber = articleNumber;
        RefreshSignalProjection(signal);
        PublishChange(signal, requiresVisualRefresh: true, requiresPersistence: true);
    }

    public void SelectDistantSignalArticle(string? articleNumber)
    {
        if (SelectedElement is not SbSignal signal)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(articleNumber) &&
            !ContainsArticle(DistantSignalOptions, articleNumber))
        {
            return;
        }

        if (string.Equals(
                signal.DistantSignalArticleNumber,
                articleNumber,
                StringComparison.Ordinal))
        {
            return;
        }

        signal.DistantSignalArticleNumber = articleNumber;
        PublishChange(signal, requiresVisualRefresh: true, requiresPersistence: true);
    }

    public void SetBaseAddress(int value)
    {
        if (SelectedElement is not SbSignal signal ||
            value is < 1 or > MaximumMultiplexerAddress ||
            signal.BaseAddress == value)
        {
            return;
        }

        signal.BaseAddress = value;
        PublishChange(signal, requiresVisualRefresh: false, requiresPersistence: true);
    }

    public void SetTopSpeedIndicator(int? value) =>
        SetSpeedIndicator(value, isTopIndicator: true);

    public void SetBottomSpeedIndicator(int? value) =>
        SetSpeedIndicator(value, isTopIndicator: false);

    [RelayCommand]
    private void Rotate(int rotation)
    {
        if (SelectedElement is not { } element ||
            rotation is not (0 or 90 or 180 or 270) ||
            element.Rotation == rotation)
        {
            return;
        }

        element.Rotation = rotation;
        PublishChange(element, requiresVisualRefresh: true, requiresPersistence: true);
    }

    [RelayCommand]
    private void SetSwitchPosition(SwitchPosition position)
    {
        if (SelectedElement is not SbSwitch sw || sw.SwitchPosition == position)
        {
            return;
        }

        sw.SwitchPosition = position;
        PublishChange(sw, requiresVisualRefresh: true, requiresPersistence: false);
    }

    [RelayCommand]
    private void SetSignalAspect(SignalAspect aspect)
    {
        if (SelectedElement is not SbSignal signal || !IsAspectAvailable(aspect))
        {
            return;
        }

        signal.SignalAspect = aspect;
        var requiresSignalCommand = TryPrepareSignalCommand(signal);
        PublishChange(
            signal,
            requiresVisualRefresh: true,
            requiresPersistence: false,
            requiresSignalCommand);
    }

    [RelayCommand]
    private void DeleteSelectedElement()
    {
        if (SelectedElement is { } element)
        {
            DeletionRequested?.Invoke(this, new SignalBoxElementEventArgs(element));
        }
    }

    partial void OnSelectedElementChanged(SbElement? value)
    {
        if (value is SbSignal signal)
        {
            RefreshSignalProjection(signal);
        }
        else
        {
            MainSignalOptions = [];
            DistantSignalOptions = [];
            SupportedAspects = Enum.GetValues<SignalAspect>();
            NotifySignalProjectionChanged();
        }
    }

    private void ResetMultiplexer(SbSignal signal)
    {
        if (!signal.IsMultiplexed &&
            string.IsNullOrEmpty(signal.MultiplexerArticleNumber) &&
            string.IsNullOrEmpty(signal.MainSignalArticleNumber) &&
            string.IsNullOrEmpty(signal.DistantSignalArticleNumber))
        {
            return;
        }

        signal.MultiplexerArticleNumber = null;
        signal.IsMultiplexed = false;
        signal.MainSignalArticleNumber = null;
        signal.DistantSignalArticleNumber = null;
        MainSignalOptions = [];
        DistantSignalOptions = [];
        SupportedAspects = Enum.GetValues<SignalAspect>();
        NotifySignalProjectionChanged();
        PublishChange(signal, requiresVisualRefresh: true, requiresPersistence: true);
    }

    private void RefreshSignalProjection(SbSignal signal)
    {
        RefreshSignalOptions(signal);
        SupportedAspects = ResolveSupportedAspects(signal);
        NotifySignalProjectionChanged();
    }

    private void RefreshSignalOptions(SbSignal signal)
    {
        if (string.IsNullOrWhiteSpace(signal.MultiplexerArticleNumber))
        {
            MainSignalOptions = [];
            DistantSignalOptions = [];
            return;
        }

        try
        {
            MainSignalOptions = _articleCatalog.GetMainSignalOptions(
                signal.MultiplexerArticleNumber);
            DistantSignalOptions = _articleCatalog.GetDistantSignalOptions(
                signal.MultiplexerArticleNumber);
        }
        catch (ArgumentException)
        {
            MainSignalOptions = [];
            DistantSignalOptions = [];
        }
    }

    private IReadOnlyCollection<SignalAspect> ResolveSupportedAspects(SbSignal signal)
    {
        if (string.IsNullOrWhiteSpace(signal.MultiplexerArticleNumber))
        {
            return Enum.GetValues<SignalAspect>();
        }

        try
        {
            var supported = _multiplexerProvider.GetSupportedAspects(
                signal.MultiplexerArticleNumber,
                signal.MainSignalArticleNumber);
            return supported.Count == 0
                ? Enum.GetValues<SignalAspect>()
                : supported;
        }
        catch (ArgumentException)
        {
            return Enum.GetValues<SignalAspect>();
        }
    }

    private bool TryPrepareSignalCommand(SbSignal signal)
    {
        if (!signal.IsMultiplexed ||
            string.IsNullOrWhiteSpace(signal.MultiplexerArticleNumber) ||
            signal.BaseAddress is <= 0 or > MaximumMultiplexerAddress ||
            signal.BaseAddress % 2 == 0 ||
            !_multiplexerProvider.TryGetMaxAddressOffset(
                signal.MultiplexerArticleNumber,
                signal.MainSignalArticleNumber,
                out var maxOffset) ||
            signal.BaseAddress + maxOffset > MaximumMultiplexerAddress)
        {
            return false;
        }

        try
        {
            if (!_multiplexerProvider.TryGetTurnoutCommand(
                    signal.MultiplexerArticleNumber,
                    signal.MainSignalArticleNumber,
                    signal.SignalAspect,
                    out var command))
            {
                return false;
            }

            signal.ExtendedAccessoryValue = command.Activate ? 1 : 0;
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private void SetSpeedIndicator(int? value, bool isTopIndicator)
    {
        if (SelectedElement is not SbSignal signal ||
            value is < 0 or > 999)
        {
            return;
        }

        var serializedValue = value?.ToString(CultureInfo.InvariantCulture);
        var currentValue = isTopIndicator
            ? signal.TopSpeedIndicator
            : signal.BottomSpeedIndicator;
        if (string.Equals(currentValue, serializedValue, StringComparison.Ordinal))
        {
            return;
        }

        if (isTopIndicator)
        {
            signal.TopSpeedIndicator = serializedValue;
        }
        else
        {
            signal.BottomSpeedIndicator = serializedValue;
        }

        PublishChange(signal, requiresVisualRefresh: true, requiresPersistence: true);
    }

    private void PublishChange(
        SbElement element,
        bool requiresVisualRefresh,
        bool requiresPersistence,
        bool requiresSignalCommand = false)
    {
        OnPropertyChanged(nameof(ElementName));
        OnPropertyChanged(nameof(ElementAddress));
        OnPropertyChanged(nameof(BaseAddress));
        OnPropertyChanged(nameof(TopSpeedIndicator));
        OnPropertyChanged(nameof(BottomSpeedIndicator));
        ElementChanged?.Invoke(
            this,
            new SignalBoxPropertyChangeEventArgs(
                element,
                requiresVisualRefresh,
                requiresPersistence,
                requiresSignalCommand));
    }

    private void NotifySignalProjectionChanged()
    {
        OnPropertyChanged(nameof(MainSignalOptions));
        OnPropertyChanged(nameof(DistantSignalOptions));
        OnPropertyChanged(nameof(SupportedAspects));
        OnPropertyChanged(nameof(SelectedMultiplexerArticleNumber));
        OnPropertyChanged(nameof(SelectedMainSignalArticleNumber));
        OnPropertyChanged(nameof(SelectedDistantSignalArticleNumber));
        OnPropertyChanged(nameof(IsSpeedIndicatorVisible));
        OnPropertyChanged(nameof(BaseAddress));
        OnPropertyChanged(nameof(TopSpeedIndicator));
        OnPropertyChanged(nameof(BottomSpeedIndicator));
    }

    private static bool ContainsArticle(
        IReadOnlyList<SignalArticleOption> options,
        string articleNumber) =>
        options.Any(option =>
            string.Equals(option.ArticleNumber, articleNumber, StringComparison.Ordinal));

    private static string? ResolveSelectedArticle(
        string? selectedArticle,
        IReadOnlyList<SignalArticleOption> options,
        string? fallbackArticle)
    {
        if (!string.IsNullOrWhiteSpace(selectedArticle) &&
            ContainsArticle(options, selectedArticle))
        {
            return selectedArticle;
        }

        if (!string.IsNullOrWhiteSpace(fallbackArticle) &&
            ContainsArticle(options, fallbackArticle))
        {
            return fallbackArticle;
        }

        return options.Count > 0 ? options[0].ArticleNumber : null;
    }

    private static int? ParseSpeedIndicator(string? value) =>
        int.TryParse(value, out var parsed) ? parsed : null;
}

public sealed record SignalBoxMultiplexerOption(
    string ArticleNumber,
    string DisplayName);

public sealed class SignalBoxPropertyChangeEventArgs(
    SbElement element,
    bool requiresVisualRefresh,
    bool requiresPersistence,
    bool requiresSignalCommand) : EventArgs
{
    public SbElement Element { get; } = element;

    public bool RequiresVisualRefresh { get; } = requiresVisualRefresh;

    public bool RequiresPersistence { get; } = requiresPersistence;

    public bool RequiresSignalCommand { get; } = requiresSignalCommand;
}

public sealed class SignalBoxElementEventArgs(SbElement element) : EventArgs
{
    public SbElement Element { get; } = element;
}
