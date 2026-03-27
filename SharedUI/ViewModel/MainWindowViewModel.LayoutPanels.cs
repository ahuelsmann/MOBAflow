namespace Moba.SharedUI.ViewModel;

using Common.Configuration;

public partial class MainWindowViewModel
{
    private bool _isJourneyListExpanded = true;
    private bool _isStationListExpanded = true;
    private bool _isJourneyPropertiesExpanded = true;
    private bool _isLocomotivesListExpanded = true;
    private bool _isLocomotivesPropertiesExpanded = true;
    private bool _isPassengerWagonListExpanded = true;
    private bool _isPassengerWagonPropertiesExpanded = true;
    private bool _isGoodsWagonListExpanded = true;
    private bool _isGoodsWagonPropertiesExpanded = true;
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

    public bool IsLocomotivesListExpanded
    {
        get => _isLocomotivesListExpanded;
        set
        {
            if (SetProperty(ref _isLocomotivesListExpanded, value))
                PersistLayoutState(layout => layout.LocomotivesPage.IsListExpanded = value);
        }
    }

    public bool IsLocomotivesPropertiesExpanded
    {
        get => _isLocomotivesPropertiesExpanded;
        set
        {
            if (SetProperty(ref _isLocomotivesPropertiesExpanded, value))
                PersistLayoutState(layout => layout.LocomotivesPage.IsPropertiesExpanded = value);
        }
    }

    public bool IsPassengerWagonListExpanded
    {
        get => _isPassengerWagonListExpanded;
        set
        {
            if (SetProperty(ref _isPassengerWagonListExpanded, value))
                PersistLayoutState(layout => layout.PassengerWagonPage.IsListExpanded = value);
        }
    }

    public bool IsPassengerWagonPropertiesExpanded
    {
        get => _isPassengerWagonPropertiesExpanded;
        set
        {
            if (SetProperty(ref _isPassengerWagonPropertiesExpanded, value))
                PersistLayoutState(layout => layout.PassengerWagonPage.IsPropertiesExpanded = value);
        }
    }

    public bool IsGoodsWagonListExpanded
    {
        get => _isGoodsWagonListExpanded;
        set
        {
            if (SetProperty(ref _isGoodsWagonListExpanded, value))
                PersistLayoutState(layout => layout.GoodsWagonPage.IsListExpanded = value);
        }
    }

    public bool IsGoodsWagonPropertiesExpanded
    {
        get => _isGoodsWagonPropertiesExpanded;
        set
        {
            if (SetProperty(ref _isGoodsWagonPropertiesExpanded, value))
                PersistLayoutState(layout => layout.GoodsWagonPage.IsPropertiesExpanded = value);
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

    private void InitializeLayoutPanelStates()
    {
        _isJourneyListExpanded = _settings.Layout.JourneysPage.IsJourneyListExpanded;
        _isStationListExpanded = _settings.Layout.JourneysPage.IsStationListExpanded;
        _isCityLibraryVisible = _settings.Layout.JourneysPage.IsCityLibraryExpanded;
        _isWorkflowLibraryVisible = _settings.Layout.JourneysPage.IsWorkflowLibraryExpanded;
        _isJourneyPropertiesExpanded = _settings.Layout.JourneysPage.IsJourneyPropertiesExpanded;
        _isLocomotivesListExpanded = _settings.Layout.LocomotivesPage.IsListExpanded;
        _isLocomotivesPropertiesExpanded = _settings.Layout.LocomotivesPage.IsPropertiesExpanded;
        _isPassengerWagonListExpanded = _settings.Layout.PassengerWagonPage.IsListExpanded;
        _isPassengerWagonPropertiesExpanded = _settings.Layout.PassengerWagonPage.IsPropertiesExpanded;
        _isGoodsWagonListExpanded = _settings.Layout.GoodsWagonPage.IsListExpanded;
        _isGoodsWagonPropertiesExpanded = _settings.Layout.GoodsWagonPage.IsPropertiesExpanded;
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
        _ = _settingsService?.SaveSettingsAsync(_settings);
    }
}
