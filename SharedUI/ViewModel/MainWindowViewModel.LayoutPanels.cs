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
        _isJourneyListExpanded = _settings.Layout.JourneysPage?.IsJourneyListExpanded ?? true;
        _isStationListExpanded = _settings.Layout.JourneysPage?.IsStationListExpanded ?? true;
        _isCityLibraryVisible = _settings.Layout.JourneysPage?.IsCityLibraryExpanded ?? true;
        _isWorkflowLibraryVisible = _settings.Layout.JourneysPage?.IsWorkflowLibraryExpanded ?? true;
        _isJourneyPropertiesExpanded = _settings.Layout.JourneysPage?.IsJourneyPropertiesExpanded ?? true;
        _isLocomotivesListExpanded = _settings.Layout.LocomotivesPage?.IsListExpanded ?? true;
        _isLocomotivesPropertiesExpanded = _settings.Layout.LocomotivesPage?.IsPropertiesExpanded ?? true;
        _isPassengerWagonListExpanded = _settings.Layout.PassengerWagonPage?.IsListExpanded ?? true;
        _isPassengerWagonPropertiesExpanded = _settings.Layout.PassengerWagonPage?.IsPropertiesExpanded ?? true;
        _isGoodsWagonListExpanded = _settings.Layout.GoodsWagonPage?.IsListExpanded ?? true;
        _isGoodsWagonPropertiesExpanded = _settings.Layout.GoodsWagonPage?.IsPropertiesExpanded ?? true;
        _isProjectListExpanded = _settings.Layout.SolutionPage?.IsProjectListExpanded ?? true;
        _isWorkflowListExpanded = _settings.Layout.WorkflowsPage?.IsWorkflowListExpanded ?? true;
        _isWorkflowActionsExpanded = _settings.Layout.WorkflowsPage?.IsActionsExpanded ?? true;
        _isWorkflowPropertiesExpanded = _settings.Layout.WorkflowsPage?.IsPropertiesExpanded ?? true;
        _isSignalBoxToolboxExpanded = _settings.Layout.SignalBoxPage?.IsToolboxExpanded ?? true;
        _isSignalBoxPropertiesExpanded = _settings.Layout.SignalBoxPage?.IsPropertiesExpanded ?? true;
    }

    private void PersistLayoutState(Action<LayoutSettings> apply)
    {
        apply(_settings.Layout);
        _ = _settingsService?.SaveSettingsAsync(_settings);
    }
}
