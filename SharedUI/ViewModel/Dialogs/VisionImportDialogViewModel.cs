// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel.Dialogs;

using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;

using Moba.TrackLibrary.PikoA;
using Moba.TrackLibrary.PikoA.Import;

/// <summary>
/// Backs the "Import from screenshot" review dialog. The ViewModel is purely declarative:
/// the dialog shows <see cref="Matches"/> (OCR tokens that matched the PIKO A catalog
/// unambiguously) and <see cref="Unresolved"/> (OCR artifacts the user has to resolve or
/// skip), plus a pixels-per-mm scale. When the user confirms, <see cref="BuildImportList"/>
/// produces the candidates that the caller can feed into
/// <see cref="VisionTrackPlanImporter.Import"/>.
/// </summary>
public sealed partial class VisionImportDialogViewModel : ObservableObject
{
    /// <summary>All catalog entries the user can pick to resolve an unresolved token.</summary>
    public IReadOnlyList<TrackCatalogEntry> AvailableCatalogEntries { get; }

    /// <summary>OCR tokens that matched an entry in the catalog. Each item has its own <c>IsSelected</c> toggle.</summary>
    public ObservableCollection<VisionImportMatchItemViewModel> Matches { get; } = [];

    /// <summary>OCR tokens that did not match anything. Each item carries the user's chosen resolution (or none → skip).</summary>
    public ObservableCollection<VisionImportUnresolvedItemViewModel> Unresolved { get; } = [];

    /// <summary>Image width in pixels (for the scale hint in the UI).</summary>
    public int ImageWidthPixels { get; }

    /// <summary>Image height in pixels (for the scale hint in the UI).</summary>
    public int ImageHeightPixels { get; }

    /// <summary>
    /// User-configurable scale. <c>1.0</c> means 1 pixel = 1 mm and is the default starting
    /// point for AnyRail screenshots. Must be strictly positive.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsValidScale))]
    private double _pixelsPerMillimeter = 1.0;

    /// <summary>True iff the currently entered scale is usable for <see cref="VisionTrackPlanImporter.Import"/>.</summary>
    public bool IsValidScale =>
        PixelsPerMillimeter > 0 && !double.IsNaN(PixelsPerMillimeter) && !double.IsInfinity(PixelsPerMillimeter);

    /// <summary>Summary count shown in the dialog header (kept in sync with the collections).</summary>
    public int SelectedCount => Matches.Count(m => m.IsSelected)
                                + Unresolved.Count(u => u.SelectedResolution is not null);

    /// <summary>Pre-formatted image size label, bound by the dialog header.</summary>
    public string HeaderText => $"Image: {ImageWidthPixels} × {ImageHeightPixels} px";

    /// <summary>Pre-formatted selection summary, bound next to the scale input.</summary>
    public string SummaryText => $"Will import: {SelectedCount}";

    /// <summary>Pre-formatted header for the matches list.</summary>
    public string MatchesHeader => $"Matches ({Matches.Count})";

    /// <summary>Pre-formatted header for the unresolved list.</summary>
    public string UnresolvedHeader => $"Unresolved ({Unresolved.Count})";

    public VisionImportDialogViewModel(
        VisionExtractionResult extraction,
        int imageWidthPixels,
        int imageHeightPixels,
        IReadOnlyList<TrackCatalogEntry>? availableCatalogEntries = null)
    {
        ArgumentNullException.ThrowIfNull(extraction);
        ImageWidthPixels = imageWidthPixels;
        ImageHeightPixels = imageHeightPixels;
        AvailableCatalogEntries = availableCatalogEntries ?? PikoACatalog.All;

        foreach (var m in extraction.Matches)
        {
            var item = new VisionImportMatchItemViewModel(m);
            item.PropertyChanged += OnChildPropertyChanged;
            Matches.Add(item);
        }

        foreach (var u in extraction.Unresolved)
        {
            var item = new VisionImportUnresolvedItemViewModel(u, AvailableCatalogEntries);
            item.PropertyChanged += OnChildPropertyChanged;
            Unresolved.Add(item);
        }
    }

    private void OnChildPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(VisionImportMatchItemViewModel.IsSelected)
            or nameof(VisionImportUnresolvedItemViewModel.SelectedResolution))
        {
            OnPropertyChanged(nameof(SelectedCount));
        }
    }

    /// <summary>
    /// Build the final list of candidates to import based on current user choices.
    /// Matches that were unchecked are dropped; unresolved tokens without a chosen
    /// resolution are skipped.
    /// </summary>
    public IReadOnlyList<VisionTrackCandidate> BuildImportList()
    {
        var list = new List<VisionTrackCandidate>(Matches.Count + Unresolved.Count);

        foreach (var m in Matches)
        {
            if (m.IsSelected)
            {
                list.Add(m.Candidate);
            }
        }

        foreach (var u in Unresolved)
        {
            if (u.SelectedResolution is { } entry)
            {
                list.Add(VisionTrackPlanImporter.ResolveUnresolved(u.Token, entry));
            }
        }

        return list;
    }
}

/// <summary>
/// One row in the "Matches" list: a candidate that the extractor resolved automatically.
/// The user can untick it to exclude from the import.
/// </summary>
public sealed partial class VisionImportMatchItemViewModel : ObservableObject
{
    public VisionTrackCandidate Candidate { get; }

    public string Code => Candidate.CatalogEntry.Code;
    public string DisplayName => Candidate.CatalogEntry.DisplayName;
    public double CenterX => Candidate.CenterX;
    public double CenterY => Candidate.CenterY;
    public string RawText => Candidate.RawText;

    /// <summary>Pre-formatted "(x, y)" label for display; avoids per-double x:Bind noise in XAML.</summary>
    public string PositionText => $"({Candidate.CenterX:F0}, {Candidate.CenterY:F0})";

    /// <summary>Checked by default; user can deselect to skip this candidate during import.</summary>
    [ObservableProperty]
    private bool _isSelected = true;

    public VisionImportMatchItemViewModel(VisionTrackCandidate candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        Candidate = candidate;
    }
}

/// <summary>
/// One row in the "Unresolved" list: an OCR artifact the user must classify or skip.
/// </summary>
public sealed partial class VisionImportUnresolvedItemViewModel : ObservableObject
{
    public VisionUnresolvedToken Token { get; }

    /// <summary>
    /// Catalog entries the user can pick in the per-row dropdown. Exposed per row so the
    /// DataTemplate can bind directly via <c>x:Bind AvailableResolutions</c>.
    /// </summary>
    public IReadOnlyList<TrackCatalogEntry> AvailableResolutions { get; }

    public string RawText => Token.RawText;
    public double CenterX => Token.CenterX;
    public double CenterY => Token.CenterY;

    /// <summary>Pre-formatted "(x, y)" label for display.</summary>
    public string PositionText => $"({Token.CenterX:F0}, {Token.CenterY:F0})";

    /// <summary>Null = skip; otherwise this entry will be imported at the token's position.</summary>
    [ObservableProperty]
    private TrackCatalogEntry? _selectedResolution;

    public VisionImportUnresolvedItemViewModel(
        VisionUnresolvedToken token,
        IReadOnlyList<TrackCatalogEntry> availableResolutions)
    {
        ArgumentNullException.ThrowIfNull(token);
        ArgumentNullException.ThrowIfNull(availableResolutions);
        Token = token;
        AvailableResolutions = availableResolutions;
    }
}
