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
    private bool _isTrainsListExpanded = true;
    private bool _isTrainsLocomotiveLibraryExpanded = true;
    private bool _isTrainsPassengerLibraryExpanded = true;
    private bool _isTrainsGoodsLibraryExpanded = true;
    private bool _isTrainsPropertiesExpanded = true;
    private bool _isProjectListExpanded = true;
    private bool _isWorkflowListExpanded = true;
    private bool _isWorkflowActionsExpanded = true;
    private bool _isWorkflowPropertiesExpanded = true;
    private bool _isSignalBoxToolboxExpanded = true;
    private bool _isSignalBoxPropertiesExpanded = true;
    private bool _isMonitorTrafficExpanded = true;
    private bool _isMonitorActivityLogExpanded = true;
    private bool _isStationsListExpanded = true;
    private bool _isPlatformsListExpanded = true;
    private bool _isStationsPropertiesExpanded = true;

    public bool IsStationsListExpanded
    {
        get => _isStationsListExpanded;
        set
        {
            if (SetProperty(ref _isStationsListExpanded, value))
                PersistLayoutState(layout => layout.StationsPage.IsStationsListExpanded = value);
        }
    }

    public bool IsPlatformsListExpanded
    {
        get => _isPlatformsListExpanded;
        set
        {
            if (SetProperty(ref _isPlatformsListExpanded, value))
                PersistLayoutState(layout => layout.StationsPage.IsPlatformsListExpanded = value);
        }
    }

    public bool IsStationsPropertiesExpanded
    {
        get => _isStationsPropertiesExpanded;
        set
        {
            if (SetProperty(ref _isStationsPropertiesExpanded, value))
                PersistLayoutState(layout => layout.StationsPage.IsPropertiesExpanded = value);
        }
    }

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

    public bool IsTrainsListExpanded
    {
        get => _isTrainsListExpanded;
        set
        {
            if (SetProperty(ref _isTrainsListExpanded, value))
                PersistLayoutState(layout => layout.TrainsPage.IsTrainListExpanded = value);
        }
    }

    public bool IsTrainsLocomotiveLibraryExpanded
    {
        get => _isTrainsLocomotiveLibraryExpanded;
        set
        {
            if (SetProperty(ref _isTrainsLocomotiveLibraryExpanded, value))
                PersistLayoutState(layout => layout.TrainsPage.IsLocomotiveLibraryExpanded = value);
        }
    }

    public bool IsTrainsPassengerLibraryExpanded
    {
        get => _isTrainsPassengerLibraryExpanded;
        set
        {
            if (SetProperty(ref _isTrainsPassengerLibraryExpanded, value))
                PersistLayoutState(layout => layout.TrainsPage.IsPassengerLibraryExpanded = value);
        }
    }

    public bool IsTrainsGoodsLibraryExpanded
    {
        get => _isTrainsGoodsLibraryExpanded;
        set
        {
            if (SetProperty(ref _isTrainsGoodsLibraryExpanded, value))
                PersistLayoutState(layout => layout.TrainsPage.IsGoodsLibraryExpanded = value);
        }
    }

    public bool IsTrainsPropertiesExpanded
    {
        get => _isTrainsPropertiesExpanded;
        set
        {
            if (SetProperty(ref _isTrainsPropertiesExpanded, value))
                PersistLayoutState(layout => layout.TrainsPage.IsPropertiesExpanded = value);
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
        _isTrainsListExpanded = _settings.Layout.TrainsPage.IsTrainListExpanded;
        _isTrainsLocomotiveLibraryExpanded = _settings.Layout.TrainsPage.IsLocomotiveLibraryExpanded;
        _isTrainsPassengerLibraryExpanded = _settings.Layout.TrainsPage.IsPassengerLibraryExpanded;
        _isTrainsGoodsLibraryExpanded = _settings.Layout.TrainsPage.IsGoodsLibraryExpanded;
        _isTrainsPropertiesExpanded = _settings.Layout.TrainsPage.IsPropertiesExpanded;
        _isProjectListExpanded = _settings.Layout.SolutionPage.IsProjectListExpanded;
        _isProjectPropertiesExpanded = _settings.Layout.SolutionPage.IsPropertiesExpanded;
        _isWorkflowListExpanded = _settings.Layout.WorkflowsPage.IsWorkflowListExpanded;
        _isWorkflowActionsExpanded = _settings.Layout.WorkflowsPage.IsActionsExpanded;
        _isWorkflowPropertiesExpanded = _settings.Layout.WorkflowsPage.IsPropertiesExpanded;
        _isSignalBoxToolboxExpanded = _settings.Layout.SignalBoxPage.IsToolboxExpanded;
        _isSignalBoxPropertiesExpanded = _settings.Layout.SignalBoxPage.IsPropertiesExpanded;
        _isStationsListExpanded = _settings.Layout.StationsPage.IsStationsListExpanded;
        _isPlatformsListExpanded = _settings.Layout.StationsPage.IsPlatformsListExpanded;
        _isStationsPropertiesExpanded = _settings.Layout.StationsPage.IsPropertiesExpanded;
        _isMonitorTrafficExpanded = _settings.Layout.MonitorPage.IsTrafficExpanded;
        _isMonitorActivityLogExpanded = _settings.Layout.MonitorPage.IsActivityLogExpanded;
    }

    private void PersistLayoutState(Action<LayoutSettings> apply)
    {
        apply(_settings.Layout);
        PersistSettingsSafely();
    }
}
