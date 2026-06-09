// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Common.Configuration;

/// <summary>
/// Observable layout column widths for all pages. Bound from the UI so that when values
/// are set (from loaded settings or from the resize behavior), the UI is updated via binding.
/// The resize behavior updates this view model during drag and persists to settings on release.
/// </summary>
public sealed class LayoutColumnWidthsViewModel
{
    private const int MaxColumnIndex = 19;

    public LayoutColumnWidthsViewModel()
    {
        JourneysPage = new PageColumnWidthsViewModel();
        SolutionPage = new PageColumnWidthsViewModel();
        TrackPlanPage = new PageColumnWidthsViewModel();
        TrainControlPage = new PageColumnWidthsViewModel();
        WorkflowsPage = new PageColumnWidthsViewModel();
        LocomotivesPage = new PageColumnWidthsViewModel();
        GoodsWagonPage = new PageColumnWidthsViewModel();
        PassengerWagonPage = new PageColumnWidthsViewModel();
        SignalBoxPage = new PageColumnWidthsViewModel();
        MonitorPage = new PageColumnWidthsViewModel();
    }

    public PageColumnWidthsViewModel JourneysPage { get; }
    public PageColumnWidthsViewModel SolutionPage { get; }
    public PageColumnWidthsViewModel TrackPlanPage { get; }
    public PageColumnWidthsViewModel TrainControlPage { get; }
    public PageColumnWidthsViewModel WorkflowsPage { get; }
    public PageColumnWidthsViewModel LocomotivesPage { get; }
    public PageColumnWidthsViewModel GoodsWagonPage { get; }
    public PageColumnWidthsViewModel PassengerWagonPage { get; }
    public PageColumnWidthsViewModel SignalBoxPage { get; }
    public PageColumnWidthsViewModel MonitorPage { get; }

    /// <summary>
    /// Loads column widths from persisted layout settings. Call after settings have been loaded.
    /// Missing keys use page-specific defaults so the UI always has valid values.
    /// </summary>
    public void LoadFrom(LayoutSettings layout)
    {
        LoadPage(JourneysPage, "JourneysPage", layout.ColumnWidths, 250, 0, 250, 0, 0, 0, 0, 0, 400);
        LoadPage(SolutionPage, "SolutionPage", layout.ColumnWidths, 300, 0, 400);
        LoadPage(TrackPlanPage, "TrackPlanPage", layout.ColumnWidths, 180, 0, 240);
        LoadPage(TrainControlPage, "TrainControlPage", layout.ColumnWidths, 260, 0, 180);
        LoadPage(WorkflowsPage, "WorkflowsPage", layout.ColumnWidths, 300, 0, 400, 0, 350);
        LoadPage(
            LocomotivesPage,
            "LocomotivesPage",
            layout.ColumnWidths,
            layout.LocomotivesPage.ListColumnWidth > 0 ? layout.LocomotivesPage.ListColumnWidth : 250,
            0,
            0);
        LoadPage(GoodsWagonPage, "GoodsWagonPage", layout.ColumnWidths, 250, 0, 400);
        LoadPage(PassengerWagonPage, "PassengerWagonPage", layout.ColumnWidths, 250, 0, 400);
        LoadPage(SignalBoxPage, "SignalBoxPage", layout.ColumnWidths, 240, 0, 0, 0, 300);
        LoadPage(MonitorPage, "MonitorPage", layout.ColumnWidths, 350, 400);
    }

    /// <summary>
    /// Sets the width for a column. Used by the resize behavior during drag and when persisting.
    /// The binding source updates and the UI reflects the new value.
    /// </summary>
    public void SetColumnWidth(string pageKey, int columnIndex, double width)
    {
        if (columnIndex < 0 || columnIndex > MaxColumnIndex)
            return;

        var page = GetPage(pageKey);
        if (page != null)
            page[columnIndex] = width;
    }

    /// <summary>
    /// Gets the persisted width for a column. Used when writing back to settings.
    /// </summary>
    public double GetColumnWidth(string pageKey, int columnIndex)
    {
        if (columnIndex < 0 || columnIndex > MaxColumnIndex)
            return 0;

        var page = GetPage(pageKey);
        return page?[columnIndex] ?? 0;
    }

    private static void LoadPage(PageColumnWidthsViewModel page, string keyPrefix,
        Dictionary<string, double> columnWidths, params double[] defaults)
    {
        for (var i = 0; i <= MaxColumnIndex; i++)
        {
            var key = $"{keyPrefix}:{i}";
            var defaultVal = i < defaults.Length ? defaults[i] : 0;
            var value = columnWidths.TryGetValue(key, out var w) && w > 0 ? w : defaultVal;
            if (value > 0)
                page[i] = value;
        }
    }

    private PageColumnWidthsViewModel? GetPage(string pageKey)
    {
        return pageKey switch
        {
            "JourneysPage" => JourneysPage,
            "SolutionPage" => SolutionPage,
            "TrackPlanPage" => TrackPlanPage,
            "TrainControlPage" => TrainControlPage,
            "WorkflowsPage" => WorkflowsPage,
            "LocomotivesPage" => LocomotivesPage,
            "GoodsWagonPage" => GoodsWagonPage,
            "PassengerWagonPage" => PassengerWagonPage,
            "SignalBoxPage" => SignalBoxPage,
            "MonitorPage" => MonitorPage,
            _ => null
        };
    }
}
