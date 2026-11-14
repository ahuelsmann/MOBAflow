// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.Configuration;

/// <summary>
/// Layout settings for UI panels and splitters (persisted per user).
/// </summary>
public class LayoutSettings
{
    /// <summary>
    /// Panel position and width settings for the left side panel.
    /// </summary>
    public SidePanelLayoutSettings LeftPanel { get; set; } = new();

    /// <summary>
    /// Panel position and width settings for the right side panel.
    /// </summary>
    public SidePanelLayoutSettings RightPanel { get; set; } = new();

    /// <summary>
    /// Panel position and size settings for the bottom status/controls panel.
    /// </summary>
    public BottomPanelLayoutSettings BottomPanel { get; set; } = new();

    /// <summary>
    /// Tab visibility settings for application views.
    /// </summary>
    public TabVisibilitySettings TabVisibility { get; set; } = new();

    /// <summary>
    /// Generic layout width dictionary to persist resizable grid column widths across pages.
    /// </summary>
    public Dictionary<string, double> ColumnWidths { get; set; } = new();

    /// <summary>
    /// GridSplitter-based layout settings for all pages with resizable columns.
    /// Replaces the old ColumnWidths dictionary with a more structured approach.
    /// </summary>
    public Dictionary<string, PageLayoutSettings> PageLayouts { get; set; } = new();

    public SettingsPageLayoutSettings SettingsPage { get; set; } = new();

    /// <summary>
    /// JourneysPage-specific layout settings (panel visibility).
    /// </summary>
    public JourneysPageLayoutSettings JourneysPage { get; set; } = new();

    public EventManagerPageLayoutSettings EventManagerPage { get; set; } = new();

    public GoodsWagonPageLayoutSettings GoodsWagonPage { get; set; } = new();

    public LocomotivesPageLayoutSettings LocomotivesPage { get; set; } = new();

    public PassengerWagonPageLayoutSettings PassengerWagonPage { get; set; } = new();

    public TrainsPageLayoutSettings TrainsPage { get; set; } = new();

    public SolutionPageLayoutSettings SolutionPage { get; set; } = new();

    public HelpPageLayoutSettings HelpPage { get; set; } = new();

    public MatrixPageLayoutSettings MatrixPage { get; set; } = new();

    public WorkflowsPageLayoutSettings WorkflowsPage { get; set; } = new();

    public SignalBoxPageLayoutSettings SignalBoxPage { get; set; } = new();

    public TrackPlanPageLayoutSettings TrackPlanPage { get; set; } = new();

    public MonitorPageLayoutSettings MonitorPage { get; set; } = new();

    public StationsPageLayoutSettings StationsPage { get; set; } = new();

    public TimetablePageLayoutSettings TimetablePage { get; set; } = new();
}

/// <summary>Persists the relative widths of the Timetable page master-detail columns.</summary>
public class TimetablePageLayoutSettings
{
    /// <summary>Gets or sets the services column star value.</summary>
    public double ServicesColumnStarValue { get; set; } = 2;

    /// <summary>Gets or sets the details column star value.</summary>
    public double DetailsColumnStarValue { get; set; } = 3;
}

/// <summary>
/// Panel position and width settings for the side panels (left/right).
/// </summary>
public class SidePanelLayoutSettings
{
    /// <summary>
    /// Is the panel currently open/visible?
    /// </summary>
    public bool IsOpen { get; set; } = true;

    /// <summary>
    /// Width of the panel in pixels.
    /// </summary>
    public double Width { get; set; } = 300;

    /// <summary>
    /// Persistent layout states for structurally docked panels (e.g., Train Control, Signal Box).
    /// </summary>
    public Dictionary<string, PanelLayoutState> PanelLayoutStates { get; set; } = new();
}

/// <summary>
/// Persistent layout state for a structurally docked panel.
/// </summary>
public abstract class PanelLayoutState
{
    /// <summary>
    /// Is the panel in the collapsed state?
    /// </summary>
    public bool IsCollapsed { get; set; } = false;

    /// <summary>
    /// Additional custom state data as needed by the panel.
    /// </summary>
    public Dictionary<string, object> CustomState { get; set; } = new();
}

/// <summary>
/// Panel position and size settings for the bottom status/controls panel.
/// </summary>
public class BottomPanelLayoutSettings
{
    /// <summary>
    /// Is the panel currently open/visible?
    /// </summary>
    public bool IsOpen { get; set; } = true;

    /// <summary>
    /// Height of the panel in pixels.
    /// </summary>
    public double Height { get; set; } = 150;
}

/// <summary>
/// Tab visibility settings for application views.
/// </summary>
public class TabVisibilitySettings
{
    /// <summary>
    /// Is the Dashboard tab visible?
    /// </summary>
    public bool IsDashboardTabVisible { get; set; } = true;

    /// <summary>
    /// Is the Solutions tab visible?
    /// </summary>
    public bool IsSolutionsTabVisible { get; set; } = true;

    /// <summary>
    /// Is the Settings tab visible?
    /// </summary>
    public bool IsSettingsTabVisible { get; set; } = true;

    /// <summary>
    /// Is the Journeys tab visible?
    /// </summary>
    public bool IsJourneysTabVisible { get; set; } = true;

    /// <summary>
    /// Is the Workflows tab visible?
    /// </summary>
    public bool IsWorkflowsTabVisible { get; set; } = true;

    /// <summary>
    /// Is the Feedback Points tab visible?
    /// </summary>
    public bool IsFeedbackPointsTabVisible { get; set; } = true;

    /// <summary>
    /// Is the Track Plan Editor tab visible?
    /// </summary>
    public bool IsTrackPlanEditorTabVisible { get; set; } = true;

    /// <summary>
    /// Is the Signal Box tab visible?
    /// </summary>
    public bool IsSignalBoxTabVisible { get; set; } = true;

    /// <summary>
    /// Is the Journey Map tab visible?
    /// </summary>
    public bool IsJourneyMapTabVisible { get; set; } = true;

    /// <summary>
    /// Is the Monitor tab visible?
    /// </summary>
    public bool IsMonitorTabVisible { get; set; } = true;

    /// <summary>
    /// Is the Trains tab visible?
    /// </summary>
    public bool IsTrainsTabVisible { get; set; } = true;

    /// <summary>
    /// Is the Train Control tab visible?
    /// </summary>
    public bool IsTrainControlTabVisible { get; set; } = true;
}

/// <summary>
/// JourneysPage-specific layout settings (panel visibility).
/// </summary>
public class JourneysPageLayoutSettings
{
    public bool IsJourneyListExpanded { get; set; } = true;

    public bool IsStationListExpanded { get; set; } = true;

    /// <summary>
    /// Is City Library panel expanded?
    /// </summary>
    public bool IsCityLibraryExpanded { get; set; } = true;

    public bool IsJourneyPropertiesExpanded { get; set; } = true;

    public double JourneysColumnWidth { get; set; } = 250;

    public double StationsColumnWidth { get; set; } = 250;

    public double CityLibraryColumnWidth { get; set; } = 250;

}

public class EventManagerPageLayoutSettings
{
    public double EventPlanColumnStarValue { get; set; } = 2;
    public double WorkflowLibraryColumnStarValue { get; set; } = 1;
    public double PropertiesColumnStarValue { get; set; } = 1.2;
    public bool IsToolboxExpanded { get; set; } = true;
    public bool IsValuesExpanded { get; set; } = true;
    public bool IsPropertiesExpanded { get; set; } = true;
    public double ToolboxColumnWidth { get; set; } = 220;
    public double ValuesColumnWidth { get; set; } = 260;
    public double PropertiesColumnWidth { get; set; } = 300;
}

public class GoodsWagonPageLayoutSettings
{
    public bool IsListExpanded { get; set; } = true;
    public bool IsPropertiesExpanded { get; set; } = true;
    public double ListColumnWidth { get; set; } = 250;
    public double PropertiesColumnStarValue { get; set; } = 1;
}

public class LocomotivesPageLayoutSettings
{
    public bool IsListExpanded { get; set; } = true;
    public bool IsPropertiesExpanded { get; set; } = true;
    public double ListColumnWidth { get; set; } = 250;
    public double PropertiesColumnStarValue { get; set; } = 1;
}

public class PassengerWagonPageLayoutSettings
{
    public bool IsListExpanded { get; set; } = true;
    public bool IsPropertiesExpanded { get; set; } = true;
    public double ListColumnWidth { get; set; } = 250;
    public double PropertiesColumnStarValue { get; set; } = 1;
}

public class TrainsPageLayoutSettings
{
    public bool IsTrainListExpanded { get; set; } = true;
    public bool IsLocomotiveLibraryExpanded { get; set; } = true;
    public bool IsPassengerLibraryExpanded { get; set; } = true;
    public bool IsGoodsLibraryExpanded { get; set; } = true;
    public bool IsPropertiesExpanded { get; set; } = true;
    public double TrainListColumnStarValue { get; set; } = 0.9;
    public double LocomotiveLibraryColumnStarValue { get; set; } = 0.8;
    public double PassengerLibraryColumnStarValue { get; set; } = 0.8;
    public double GoodsLibraryColumnStarValue { get; set; } = 0.8;
    public double PropertiesColumnStarValue { get; set; } = 1.9;
}

public class SolutionPageLayoutSettings
{
    public bool IsProjectListExpanded { get; set; } = true;
    public bool IsPropertiesExpanded { get; set; } = true;
    public bool IsZ21FinderExpanded { get; set; } = true;
    public double ProjectListColumnStarValue { get; set; } = 1;
    public double Z21FinderColumnStarValue { get; set; } = 1;
    public double PropertiesColumnStarValue { get; set; } = 2.2;
}

public class HelpPageLayoutSettings
{
    public bool IsNavigationExpanded { get; set; } = true;
    public bool IsDocumentationExpanded { get; set; } = true;
    public double NavigationColumnWidth { get; set; } = 280;
    public double DocumentationColumnStarValue { get; set; } = 1;
}

public class MatrixPageLayoutSettings
{
    public bool IsColorPaletteExpanded { get; set; } = true;
    public bool IsImagesExpanded { get; set; } = true;
    public double ColorPaletteColumnWidth { get; set; } = 320;
    public double ImagesColumnStarValue { get; set; } = 1;
}

public class WorkflowsPageLayoutSettings
{
    public bool IsWorkflowListExpanded { get; set; } = true;
    public bool IsActionsExpanded { get; set; } = true;
    public bool IsPropertiesExpanded { get; set; } = true;
    public double WorkflowListColumnWidth { get; set; } = 200;
    public bool IsGraphViewActive { get; set; } = true;
    public double ActionsColumnStarValue { get; set; } = 2;
    public double PropertiesColumnStarValue { get; set; } = 1;
}

public class SignalBoxPageLayoutSettings
{
    public bool IsToolboxExpanded { get; set; } = true;
    public bool IsPropertiesExpanded { get; set; } = true;
    public double ToolboxColumnWidth { get; set; } = 240;
    public double CanvasColumnStarValue { get; set; } = 3;
    public double PropertiesColumnStarValue { get; set; } = 1;
}

public class TrackPlanPageLayoutSettings
{
    public bool IsToolboxExpanded { get; set; } = true;
    public bool IsPropertiesExpanded { get; set; } = true;
    public bool FitOnLoad { get; set; } = true;
    public double ToolboxColumnWidth { get; set; } = 180;
    public double PropertiesColumnWidth { get; set; } = 240;
    public double PropertiesColumnStarValue { get; set; } = 1;
}

public class MonitorPageLayoutSettings
{
    public bool IsTrafficExpanded { get; set; } = true;
    public bool IsActivityLogExpanded { get; set; } = true;
    public double TrafficColumnStarValue { get; set; } = 1;
    public double ActivityLogColumnStarValue { get; set; } = 2;
}

public class StationsPageLayoutSettings
{
    public bool IsStationsListExpanded { get; set; } = true;
    public bool IsPlatformsListExpanded { get; set; } = true;
    public bool IsWorkflowLibraryExpanded { get; set; } = true;
    public bool IsPropertiesExpanded { get; set; } = true;
    public double StationsColumnWidth { get; set; } = 280;
    public double PlatformsColumnWidth { get; set; } = 280;
    public double WorkflowLibraryColumnWidth { get; set; } = 250;
    public double PropertiesColumnStarValue { get; set; } = 1;
}
