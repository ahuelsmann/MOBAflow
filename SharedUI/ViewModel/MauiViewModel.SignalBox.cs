// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Common.Runtime;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Domain;

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
        SignalBoxElements.Clear();
        foreach (var element in elements
                     .OrderBy(e => e.Kind)
                     .ThenBy(e => e.Name, StringComparer.CurrentCultureIgnoreCase)
                     .ThenBy(e => e.X)
                     .ThenBy(e => e.Y))
        {
            var item = new MauiSignalBoxElementViewModel(element);
            item.SignalAspectChanged += OnSignalAspectChanged;
            SignalBoxElements.Add(item);
        }

        OnPropertyChanged(nameof(HasSignalBoxElements));
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

        await _mobaRuntime.SetSignalAspectAsync(item.ElementId, aspect).ConfigureAwait(false);
    }
}

public sealed partial class MauiSignalBoxElementViewModel : ObservableObject
{
    private static readonly IReadOnlyList<SignalAspect> AllSignalAspects = Enum.GetValues<SignalAspect>();
    private bool _isApplyingSnapshot;

    public MauiSignalBoxElementViewModel(SignalBoxElementRuntimeSnapshot snapshot)
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

        _isApplyingSnapshot = true;
        try
        {
            SelectedSignalAspect = snapshot.SignalAspect;
        }
        finally
        {
            _isApplyingSnapshot = false;
        }
    }

    public event EventHandler<MauiSignalBoxElementViewModel>? SignalAspectChanged;

    public Guid ElementId { get; }
    public string Name { get; }
    public SignalBoxElementKind Kind { get; }
    public int X { get; }
    public int Y { get; }
    public int? Address { get; }
    public SwitchPosition? SwitchPosition { get; }
    public SignalSystemType? SignalSystem { get; }
    public IReadOnlyList<SignalAspect> SignalAspectChoices => AllSignalAspects;
    public bool IsSignal => Kind == SignalBoxElementKind.Signal;
    public bool IsSwitch => Kind == SignalBoxElementKind.Switch;
    public string KindText => IsSignal ? "Signal" : "Weiche";
    public string LocationText => $"Planposition [{X},{Y}]";

    public string DetailText => Kind switch
    {
        SignalBoxElementKind.Signal => SignalSystem.HasValue
            ? $"Signalsystem: {SignalSystem.Value}"
            : "Signalsystem nicht gesetzt",
        SignalBoxElementKind.Switch => Address.HasValue
            ? $"DCC-Adresse {Address.Value}, Position {SwitchPosition}"
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
