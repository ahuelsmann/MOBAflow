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
    private readonly Dictionary<Guid, SignalAspect> _pendingSignalAspects = new();

    /// <summary>
    /// Gets the active signal-box switches and signals for the mobile SignalBox page.
    /// </summary>
    public ObservableCollection<MauiSignalBoxElementViewModel> SignalBoxElements { get; } = [];

    /// <summary>
    /// Gets a value indicating whether the active runtime project exposes signal-box elements.
    /// </summary>
    public bool HasSignalBoxElements => SignalBoxElements.Count > 0;

    private void RefreshSignalBoxElements(
        IReadOnlyList<SignalBoxElementRuntimeSnapshot> elements,
        bool forceApply = false)
    {
        _uiDispatcher.InvokeOnUiLowPriority(() => RefreshSignalBoxElementsCore(elements, forceApply));
    }

    private void RefreshSignalBoxElementsCore(
        IReadOnlyList<SignalBoxElementRuntimeSnapshot> elements,
        bool forceApply = false)
    {
        if (!forceApply && (_heavyUpdatesPaused || !_signalBoxTabActive))
        {
            if (elements.Count > 0)
            {
                _pendingSignalBoxElements = elements;
            }

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
            // Keep the last known signal-box list when runtime snapshots omit elements
            // (local Z21 telemetry without project, or transient MOBApi hub gaps).
            return;
        }

        var existingById = SignalBoxElements.ToDictionary(item => item.ElementId);
        var targetItems = new List<MauiSignalBoxElementViewModel>(ordered.Count);

        foreach (var snapshot in ordered)
        {
            var resolvedSnapshot = ApplyPendingSignalAspect(snapshot);
            if (existingById.TryGetValue(resolvedSnapshot.ElementId, out var existing))
            {
                existing.ApplySnapshot(resolvedSnapshot);
                targetItems.Add(existing);
                continue;
            }

            var item = new MauiSignalBoxElementViewModel(resolvedSnapshot);
            item.UserSignalAspectSelected += OnUserSignalAspectSelected;
            targetItems.Add(item);
        }

        var targetIds = targetItems.Select(item => item.ElementId).ToHashSet();
        var collectionChanged = false;

        for (var index = SignalBoxElements.Count - 1; index >= 0; index--)
        {
            var existing = SignalBoxElements[index];
            if (targetIds.Contains(existing.ElementId))
            {
                continue;
            }

            existing.UserSignalAspectSelected -= OnUserSignalAspectSelected;
            SignalBoxElements.RemoveAt(index);
            collectionChanged = true;
        }

        var existingIds = SignalBoxElements.Select(item => item.ElementId).ToHashSet();
        foreach (var target in targetItems)
        {
            if (existingIds.Contains(target.ElementId))
            {
                continue;
            }

            SignalBoxElements.Add(target);
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
            item.UserSignalAspectSelected -= OnUserSignalAspectSelected;
        }
    }

    private void OnUserSignalAspectSelected(object? sender, SignalAspect aspect)
    {
        if (sender is not MauiSignalBoxElementViewModel item)
        {
            return;
        }

        SetSignalBoxAspectCommand.Execute((item, aspect));
    }

    [RelayCommand(AllowConcurrentExecutions = true)]
    private async Task SetSignalBoxAspectAsync((MauiSignalBoxElementViewModel Item, SignalAspect Aspect) request)
    {
        var (item, aspect) = request;
        if (!item.IsSignal)
        {
            return;
        }

        await SignalBoxAspectCommandDispatcher
            .DispatchAsync(
                ResolveRuntimeCommandGateway(),
                item.ElementId,
                aspect,
                _pendingSignalAspects)
            .ConfigureAwait(false);
    }

    private IRuntimeCommandGateway ResolveRuntimeCommandGateway() =>
        _mobileRuntimeCoordinator
        ?? _runtimeCommandGateway
        ?? CreateLocalRuntimeCommandGateway();

    private SignalBoxElementRuntimeSnapshot ApplyPendingSignalAspect(SignalBoxElementRuntimeSnapshot snapshot)
    {
        if (snapshot.Kind != SignalBoxElementKind.Signal)
        {
            return snapshot;
        }

        if (!_pendingSignalAspects.TryGetValue(snapshot.ElementId, out var pending))
        {
            return snapshot;
        }

        if (snapshot.SignalAspect == pending)
        {
            _pendingSignalAspects.Remove(snapshot.ElementId);
            return snapshot;
        }

        // MOBAflow runtime snapshots are authoritative while a remote session is active.
        if (_mobileRuntimeCoordinator?.PreferRemoteRuntime == true
            && snapshot.SignalAspect.HasValue)
        {
            _pendingSignalAspects.Remove(snapshot.ElementId);
            return snapshot;
        }

        return snapshot with { SignalAspect = pending };
    }

    private IRuntimeCommandGateway CreateLocalRuntimeCommandGateway() =>
        new LocalRuntimeCommandGateway(_mobaRuntime);
}

public sealed partial class MauiSignalBoxElementViewModel : ObservableObject
{
    private static readonly IReadOnlyList<SignalAspect> AllSignalAspects = Enum.GetValues<SignalAspect>();
    private bool _suppressAspectDispatch;

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

        _suppressAspectDispatch = true;
        try
        {
            SelectedSignalAspect = snapshot.SignalAspect;
        }
        finally
        {
            _suppressAspectDispatch = false;
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

    public event EventHandler<SignalAspect>? UserSignalAspectSelected;

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

    [RelayCommand]
    private void SelectSignalAspect(SignalAspect aspect)
    {
        if (!IsSignal)
        {
            return;
        }

        if (SelectedSignalAspect != aspect)
        {
            SelectedSignalAspect = aspect;
            return;
        }

        UserSignalAspectSelected?.Invoke(this, aspect);
    }

    partial void OnSelectedSignalAspectChanged(SignalAspect? value)
    {
        if (_suppressAspectDispatch || !IsSignal || value is null)
        {
            return;
        }

        UserSignalAspectSelected?.Invoke(this, value.Value);
    }
}