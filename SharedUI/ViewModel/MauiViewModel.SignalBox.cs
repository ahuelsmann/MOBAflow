// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Common.Runtime;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Domain;

using Interface;

using Service;

using System.Collections.ObjectModel;

public sealed partial class MauiViewModel
{
    /// <summary>
    /// Gets the active signal-box switches and signals for the mobile SignalBox page.
    /// </summary>
    public ObservableCollection<MauiSignalBoxElementViewModel> SignalBoxElements { get; } = [];

    /// <summary>
    /// Gets a value indicating whether the active runtime project exposes signal-box elements.
    /// </summary>
    public bool HasSignalBoxElements => SignalBoxElements.Count > 0;

    private void RefreshSignalBoxElements(IReadOnlyList<SignalBoxElementRuntimeSnapshot> elements)
    {
        if (_heavyUpdatesPaused || !_signalBoxTabActive)
        {
            _pendingSignalBoxElements = elements;
            return;
        }

        var ordered = elements
            .OrderBy(e => e.Kind)
            .ThenBy(e => e.Name, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(e => e.X)
            .ThenBy(e => e.Y)
            .ToList();

        if (ordered.Count == 0)
        {
            if (SignalBoxElements.Count == 0)
            {
                return;
            }

            DetachAllSignalBoxHandlers();
            SignalBoxElements.Clear();
            OnPropertyChanged(nameof(HasSignalBoxElements));
            return;
        }

        var existingById = SignalBoxElements.ToDictionary(item => item.ElementId);
        var snapshotById = ordered.ToDictionary(item => item.ElementId);

        for (var index = SignalBoxElements.Count - 1; index >= 0; index--)
        {
            var existing = SignalBoxElements[index];
            if (snapshotById.ContainsKey(existing.ElementId))
            {
                continue;
            }

            existing.SignalAspectChanged -= OnSignalAspectChanged;
            SignalBoxElements.RemoveAt(index);
        }

        var collectionChanged = SignalBoxElements.Count != ordered.Count;

        for (var index = 0; index < ordered.Count; index++)
        {
            var snapshot = ordered[index];
            if (existingById.TryGetValue(snapshot.ElementId, out var existing))
            {
                existing.ApplySnapshot(snapshot);
                if (!ReferenceEquals(SignalBoxElements[index], existing))
                {
                    collectionChanged = true;
                }

                continue;
            }

            var item = new MauiSignalBoxElementViewModel(snapshot);
            item.SignalAspectChanged += OnSignalAspectChanged;
            SignalBoxElements.Insert(index, item);
            collectionChanged = true;
        }

        if (collectionChanged)
        {
            OnPropertyChanged(nameof(HasSignalBoxElements));
        }
    }

    private void DetachAllSignalBoxHandlers()
    {
        foreach (var item in SignalBoxElements)
        {
            item.SignalAspectChanged -= OnSignalAspectChanged;
        }
    }

    private void OnSignalAspectChanged(object? sender, MauiSignalBoxElementViewModel item)
    {
        SetSignalBoxAspectCommand.Execute(item);
    }

    [RelayCommand]
    private async Task SetSignalBoxAspectAsync(MauiSignalBoxElementViewModel? item)
    {
        if (item?.SelectedSignalAspect is not { } aspect)
        {
            return;
        }

        if (!IsSessionOperational)
        {
            return;
        }

        await (_runtimeCommandGateway ?? CreateLocalRuntimeCommandGateway())
            .SetSignalAspectAsync(item.ElementId, aspect)
            .ConfigureAwait(false);
    }

    private IRuntimeCommandGateway CreateLocalRuntimeCommandGateway() =>
        new LocalRuntimeCommandGateway(_mobaRuntime);
}

public sealed partial class MauiSignalBoxElementViewModel : ObservableObject
{
    private static readonly IReadOnlyList<SignalAspect> AllSignalAspects = Enum.GetValues<SignalAspect>();
    private bool _isApplyingSnapshot;

    public MauiSignalBoxElementViewModel(SignalBoxElementRuntimeSnapshot snapshot)
    {
        ApplySnapshot(snapshot);
    }

    public void ApplySnapshot(SignalBoxElementRuntimeSnapshot snapshot)
    {
        ElementId = snapshot.ElementId;
        Name = string.IsNullOrWhiteSpace(snapshot.Name)
            ? $"{snapshot.Kind} [{snapshot.X},{snapshot.Y}]"
            : snapshot.Name;
        Kind = snapshot.Kind;
        X = snapshot.X;
        Y = snapshot.Y;
        Address = snapshot.Address;
        SwitchPosition = snapshot.SwitchPosition;
        SignalSystem = snapshot.SignalSystem;
        MainSignalArticleNumber = snapshot.MainSignalArticleNumber ?? string.Empty;
        MultiplexerArticleNumber = snapshot.MultiplexerArticleNumber ?? string.Empty;
        TopSpeedIndicator = snapshot.TopSpeedIndicator ?? string.Empty;
        BottomSpeedIndicator = snapshot.BottomSpeedIndicator ?? string.Empty;

        _isApplyingSnapshot = true;
        try
        {
            SelectedSignalAspect = snapshot.SignalAspect;
        }
        finally
        {
            _isApplyingSnapshot = false;
        }

        OnPropertyChanged(nameof(KindText));
        OnPropertyChanged(nameof(LocationText));
        OnPropertyChanged(nameof(DetailText));
        OnPropertyChanged(nameof(IsSignal));
        OnPropertyChanged(nameof(IsSwitch));
        OnPropertyChanged(nameof(MainSignalArticleNumber));
        OnPropertyChanged(nameof(MultiplexerArticleNumber));
        OnPropertyChanged(nameof(TopSpeedIndicator));
        OnPropertyChanged(nameof(BottomSpeedIndicator));
    }

    public event EventHandler<MauiSignalBoxElementViewModel>? SignalAspectChanged;

    public Guid ElementId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public SignalBoxElementKind Kind { get; private set; }
    public int X { get; private set; }
    public int Y { get; private set; }
    public int? Address { get; private set; }
    public SwitchPosition? SwitchPosition { get; private set; }
    public SignalSystemType? SignalSystem { get; private set; }
    public string MainSignalArticleNumber { get; private set; } = string.Empty;
    public string MultiplexerArticleNumber { get; private set; } = string.Empty;
    public string TopSpeedIndicator { get; private set; } = string.Empty;
    public string BottomSpeedIndicator { get; private set; } = string.Empty;
    public IReadOnlyList<SignalAspect> SignalAspectChoices => AllSignalAspects;
    public bool IsSignal => Kind == SignalBoxElementKind.Signal;
    public bool IsSwitch => Kind == SignalBoxElementKind.Switch;
    public string KindText => IsSignal ? "Signal" : "Switch";
    public string LocationText => $"Plan position [{X},{Y}]";

    public string DetailText => Kind switch
    {
        SignalBoxElementKind.Signal => SignalSystem.HasValue
            ? $"Signal system: {SignalSystem.Value}"
            : "Signal system not set",
        SignalBoxElementKind.Switch => Address.HasValue
            ? $"DCC address {Address.Value}, position {SwitchPosition}"
            : $"Position {SwitchPosition}",
        _ => string.Empty
    };

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedSignalAspectText))]
    private SignalAspect? _selectedSignalAspect;

    public string SelectedSignalAspectText => SelectedSignalAspect?.ToString() ?? "-";

    partial void OnSelectedSignalAspectChanged(SignalAspect? value)
    {
        if (!_isApplyingSnapshot && value.HasValue && IsSignal)
        {
            SignalAspectChanged?.Invoke(this, this);
        }
    }
}