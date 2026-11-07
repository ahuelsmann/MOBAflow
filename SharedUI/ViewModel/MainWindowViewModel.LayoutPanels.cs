// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.SharedUI.ViewModel;

using Common.Configuration;
using CommunityToolkit.Mvvm.Input;

public partial class MainWindowViewModel
{
    private bool _isProjectListExpanded = true;
    private bool _isSignalBoxToolboxExpanded = true;
    private bool _isSignalBoxPropertiesExpanded = true;

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
        _isProjectListExpanded = _settings.Layout.SolutionPage.IsProjectListExpanded;
        _isProjectPropertiesExpanded = _settings.Layout.SolutionPage.IsPropertiesExpanded;
        _isSignalBoxToolboxExpanded = _settings.Layout.SignalBoxPage.IsToolboxExpanded;
        _isSignalBoxPropertiesExpanded = _settings.Layout.SignalBoxPage.IsPropertiesExpanded;
    }

    private void PersistLayoutState(Action<LayoutSettings> apply)
    {
        apply(_settings.Layout);
        PersistSettingsSafely();
    }

}
