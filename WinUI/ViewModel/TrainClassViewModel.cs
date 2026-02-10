// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.WinUI.ViewModel;

using Backend.Interface;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain;
using System.Collections.ObjectModel;

/// <summary>
/// ViewModel for train class input and selection.
/// Provides input parsing for flexible train class entry (e.g., "110", "BR 110", "br110").
/// </summary>
public partial class TrainClassViewModel : ObservableObject
{
    private readonly ITrainClassParser _parser;

    /// <summary>
    /// User input for train class (e.g., "110", "BR 110", "br110")
    /// </summary>
    [ObservableProperty]
    private string trainClassInput = string.Empty;

    /// <summary>
    /// Currently resolved locomotive series, or null if input is invalid or not yet resolved.
    /// </summary>
    [ObservableProperty]
    private LocomotiveSeries? resolvedSeries;

    /// <summary>
    /// Display text for resolved series name (e.g., "BR 110.3 (Bügelfalte)"), or empty if not resolved.
    /// </summary>
    [ObservableProperty]
    private string resolvedDisplayText = string.Empty;

    /// <summary>
    /// Max speed in km/h for resolved series, or 0 if not resolved.
    /// </summary>
    [ObservableProperty]
    private int vmax;

    /// <summary>
    /// Type of locomotive (e.g., "Elektrolok", "Dampflok", "Triebzug"), or empty if not resolved.
    /// </summary>
    [ObservableProperty]
    private string locomotiveType = string.Empty;

    /// <summary>
    /// Collection of all available locomotive classes for auto-completion or dropdown binding.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<LocomotiveSeries> availableClasses = new();

    /// <summary>
    /// Whether the current input has been resolved successfully.
    /// </summary>
    [ObservableProperty]
    private bool isResolved;

    public TrainClassViewModel(ITrainClassParser parser)
    {
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));

        // Load available classes for auto-completion
        var allClasses = TrainClassLibrary.GetAllClasses();
        foreach (var series in allClasses)
        {
            AvailableClasses.Add(series);
        }
    }

    /// <summary>
    /// Parses and resolves the current input.
    /// Updates ResolvedSeries and display properties if input matches a known class.
    /// </summary>
    [RelayCommand]
    public async Task ResolveInputAsync()
    {
        if (string.IsNullOrWhiteSpace(TrainClassInput))
        {
            ClearResolution();
            return;
        }

        var series = await _parser.ParseAsync(TrainClassInput);

        if (series != null)
        {
            ResolvedSeries = series;
            ResolvedDisplayText = series.Name;
            Vmax = series.Vmax;
            LocomotiveType = series.Type;
            IsResolved = true;
        }
        else
        {
            ClearResolution();
        }
    }

    /// <summary>
    /// Clears the current resolution and resets display properties.
    /// </summary>
    private void ClearResolution()
    {
        ResolvedSeries = null;
        ResolvedDisplayText = string.Empty;
        Vmax = 0;
        LocomotiveType = string.Empty;
        IsResolved = false;
    }

    /// <summary>
    /// Selects a locomotive class from the available list.
    /// </summary>
    /// <param name="series">LocomotiveSeries to select</param>
    [RelayCommand]
    public async Task SelectClassAsync(LocomotiveSeries series)
    {
        if (series == null)
        {
            return;
        }

        TrainClassInput = ExtractClassNumber(series.Name);
        await ResolveInputAsync();
    }

    /// <summary>
    /// Extracts the class number from a full name for re-entry.
    /// Example: "BR 110.3 (Bügelfalte)" → "110"
    /// </summary>
    private static string ExtractClassNumber(string fullName)
    {
        // Extract digits from the beginning
        var digitsOnly = new string(fullName.TakeWhile(char.IsDigit).ToArray());
        return digitsOnly;
    }
}
