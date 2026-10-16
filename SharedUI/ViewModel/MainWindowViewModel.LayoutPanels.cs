// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Common.Configuration;
using CommunityToolkit.Mvvm.Input;

public partial class MainWindowViewModel
{
    private bool _isJourneyListExpanded = true;
    private bool _isStationListExpanded = true;
    private bool _isJourneyPropertiesExpanded = true;
    private bool _isProjectListExpanded = true;
    private bool _isWorkflowListExpanded = true;
    private bool _isWorkflowActionsExpanded = true;
    private bool _isWorkflowPropertiesExpanded = true;
    private bool _isSignalBoxToolboxExpanded = true;
    private bool _isSignalBoxPropertiesExpanded = true;
    private bool _isMonitorTrafficExpanded = true;
    private bool _isMonitorActivityLogExpanded = true;

    public bool IsMonitorTrafficExpanded
    {
        get => _isMonitorTrafficExpanded;
        set
        {
            if (SetProperty(ref _isMonitorTrafficExpanded, value))
                PersistLayoutState(layout => layout.MonitorPage.IsTrafficExpanded = value);
        }
    }

    public bool IsMonitorActivityLogExpanded
    {
        get => _isMonitorActivityLogExpanded;
        set
        {
            if (SetProperty(ref _isMonitorActivityLogExpanded, value))
                PersistLayoutState(layout => layout.MonitorPage.IsActivityLogExpanded = value);
        }
    }

    public bool IsJourneyListExpanded
    {
        get => _isJourneyListExpanded;
        set
        {
            if (SetProperty(ref _isJourneyListExpanded, value))
                PersistLayoutState(layout => layout.JourneysPage.IsJourneyListExpanded = value);
        }
    }

    public bool IsStationListExpanded
    {
        get => _isStationListExpanded;
        set
        {
            if (SetProperty(ref _isStationListExpanded, value))
                PersistLayoutState(layout => layout.JourneysPage.IsStationListExpanded = value);
        }
    }

    public bool IsJourneyPropertiesExpanded
    {
        get => _isJourneyPropertiesExpanded;
        set
        {
            if (SetProperty(ref _isJourneyPropertiesExpanded, value))
                PersistLayoutState(layout => layout.JourneysPage.IsJourneyPropertiesExpanded = value);
        }
    }

    public bool IsProjectListExpanded
    {
        get => _isProjectListExpanded;
        set
        {
            if (SetProperty(ref _isProjectListExpanded, value))
                PersistLayoutState(layout => layout.SolutionPage.IsProjectListExpanded = value);
        }
    }

    private bool _isProjectPropertiesExpanded = true;
    public bool IsProjectPropertiesExpanded
    {
        get => _isProjectPropertiesExpanded;
        set
        {
            if (SetProperty(ref _isProjectPropertiesExpanded, value))
                PersistLayoutState(layout => layout.SolutionPage.IsPropertiesExpanded = value);
        }
    }

    public bool IsWorkflowListExpanded
    {
        get => _isWorkflowListExpanded;
        set
        {
            if (SetProperty(ref _isWorkflowListExpanded, value))
                PersistLayoutState(layout => layout.WorkflowsPage.IsWorkflowListExpanded = value);
        }
    }

    public bool IsWorkflowActionsExpanded
    {
        get => _isWorkflowActionsExpanded;
        set
        {
            if (SetProperty(ref _isWorkflowActionsExpanded, value))
                PersistLayoutState(layout => layout.WorkflowsPage.IsActionsExpanded = value);
        }
    }

    public bool IsWorkflowPropertiesExpanded
    {
        get => _isWorkflowPropertiesExpanded;
        set
        {
            if (SetProperty(ref _isWorkflowPropertiesExpanded, value))
                PersistLayoutState(layout => layout.WorkflowsPage.IsPropertiesExpanded = value);
        }
    }

    public bool IsSignalBoxToolboxExpanded
    {
        get => _isSignalBoxToolboxExpanded;
        set
        {
            if (SetProperty(ref _isSignalBoxToolboxExpanded, value))
                PersistLayoutState(layout => layout.SignalBoxPage.IsToolboxExpanded = value);
        }
    }

    public bool IsSignalBoxPropertiesExpanded
    {
        get => _isSignalBoxPropertiesExpanded;
        set
        {
            if (SetProperty(ref _isSignalBoxPropertiesExpanded, value))
                PersistLayoutState(layout => layout.SignalBoxPage.IsPropertiesExpanded = value);
        }
    }

    [RelayCommand]
    private void OpenSignalBoxWorkbench() => IsSignalBoxPropertiesExpanded = true;

    private void InitializeLayoutPanelStates()
    {
        _isJourneyListExpanded = _settings.Layout.JourneysPage.IsJourneyListExpanded;
        _isStationListExpanded = _settings.Layout.JourneysPage.IsStationListExpanded;
        _isCityLibraryVisible = _settings.Layout.JourneysPage.IsCityLibraryExpanded;
        _isJourneyPropertiesExpanded = _settings.Layout.JourneysPage.IsJourneyPropertiesExpanded;
        _isProjectListExpanded = _settings.Layout.SolutionPage.IsProjectListExpanded;
        _isProjectPropertiesExpanded = _settings.Layout.SolutionPage.IsPropertiesExpanded;
        _isWorkflowListExpanded = _settings.Layout.WorkflowsPage.IsWorkflowListExpanded;
        _isWorkflowActionsExpanded = _settings.Layout.WorkflowsPage.IsActionsExpanded;
        _isWorkflowPropertiesExpanded = _settings.Layout.WorkflowsPage.IsPropertiesExpanded;
        _isSignalBoxToolboxExpanded = _settings.Layout.SignalBoxPage.IsToolboxExpanded;
        _isSignalBoxPropertiesExpanded = _settings.Layout.SignalBoxPage.IsPropertiesExpanded;
        _isMonitorTrafficExpanded = _settings.Layout.MonitorPage.IsTrafficExpanded;
        _isMonitorActivityLogExpanded = _settings.Layout.MonitorPage.IsActivityLogExpanded;
    }

    private void PersistLayoutState(Action<LayoutSettings> apply)
    {
        apply(_settings.Layout);
        PersistSettingsSafely();
    }

}
