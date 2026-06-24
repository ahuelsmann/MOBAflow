// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.MAUI.View;

using Controls;

using Microsoft.Extensions.DependencyInjection;

using SharedUI.ViewModel;

using MauiView = Microsoft.Maui.Controls.View;

public partial class AppTabHostPage
{
    private const int TabCount = 4;

    private readonly IServiceProvider _serviceProvider;
    private readonly MauiViewModel _mauiViewModel;
    private readonly TrainControlViewModel _trainControlViewModel;
    private CounterPage? _counterPage;
    private SignalBoxPage? _signalBoxPage;
    private EnginePage? _enginePage;
    private ControlPage? _controlPage;
    private readonly MauiView?[] _tabViews = new MauiView?[TabCount];
    private int _activeTabIndex = AppBottomTabBar.CounterTabIndex;
    private bool _initialTabScheduled;

    public AppTabHostPage(
        IServiceProvider serviceProvider,
        MauiViewModel mauiViewModel,
        TrainControlViewModel trainControlViewModel)
    {
        _serviceProvider = serviceProvider;
        _mauiViewModel = mauiViewModel;
        _trainControlViewModel = trainControlViewModel;

        InitializeComponent();
        BindingContext = _mauiViewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        EnsureInitialTabLoaded();
    }

    /// <summary>
    /// Mounts and activates the default Counter tab. Called from splash navigation because
    /// <see cref="OnAppearing" /> is not always raised when <see cref="Window.Page" /> is replaced.
    /// </summary>
    public void EnsureInitialTabLoaded()
    {
        if (_initialTabScheduled)
        {
            return;
        }

        _initialTabScheduled = true;
        ShowTab(AppBottomTabBar.CounterTabIndex);
    }

    private void OnTabSelected(object? sender, int tabIndex) => ShowTab(tabIndex);

    private void ShowTab(int tabIndex)
    {
        if (tabIndex < 0 || tabIndex >= TabCount)
        {
            return;
        }

        DeactivateTabIfNeeded(_activeTabIndex, tabIndex);

        BottomTabBar.SelectedTab = tabIndex;
        MountTabIfNeeded(tabIndex);
        UpdateTabVisibility(tabIndex);
        FinishTabActivation(tabIndex);
    }

    private void DeactivateTabIfNeeded(int previousTabIndex, int nextTabIndex)
    {
        if (previousTabIndex == AppBottomTabBar.ControlTabIndex && nextTabIndex != AppBottomTabBar.ControlTabIndex)
        {
            _controlPage?.DeactivateTab();
            _trainControlViewModel.PauseUpdates();
        }

        if (previousTabIndex == AppBottomTabBar.EnginesTabIndex && nextTabIndex != AppBottomTabBar.EnginesTabIndex)
        {
            _enginePage?.DeactivateTab();
        }

        if (previousTabIndex == AppBottomTabBar.SignalBoxTabIndex && nextTabIndex != AppBottomTabBar.SignalBoxTabIndex)
        {
            _signalBoxPage?.DeactivateTab();
        }

        if (previousTabIndex == AppBottomTabBar.CounterTabIndex && nextTabIndex != AppBottomTabBar.CounterTabIndex)
        {
            _counterPage?.DeactivateTab();
        }
    }

    private void FinishTabActivation(int tabIndex)
    {
        switch (tabIndex)
        {
            case AppBottomTabBar.CounterTabIndex:
                GetCounterPage().ActivateTab();
                break;
            case AppBottomTabBar.SignalBoxTabIndex:
                _signalBoxPage?.ActivateTab();
                break;
            case AppBottomTabBar.EnginesTabIndex:
                GetEnginePage().ActivateTab();
                break;
            case AppBottomTabBar.ControlTabIndex:
                GetControlPage().ActivateTab();
                _trainControlViewModel.ResumeUpdates();
                break;
        }

        UpdateMauiViewModelTabState(tabIndex);
        _activeTabIndex = tabIndex;
    }

    private void UpdateMauiViewModelTabState(int tabIndex)
    {
        _mauiViewModel.SetSignalBoxTabActive(tabIndex == AppBottomTabBar.SignalBoxTabIndex);
        _mauiViewModel.SetControlTabActive(
            tabIndex is AppBottomTabBar.EnginesTabIndex or AppBottomTabBar.ControlTabIndex);

        if (tabIndex == AppBottomTabBar.ControlTabIndex)
        {
            _mauiViewModel.PauseHeavyUpdates();
        }
        else
        {
            _mauiViewModel.ResumeHeavyUpdates();
        }
    }

    private void MountTabIfNeeded(int tabIndex)
    {
        if (_tabViews[tabIndex] is not null)
        {
            return;
        }

        var tabView = tabIndex switch
        {
            AppBottomTabBar.SignalBoxTabIndex => (MauiView)(_signalBoxPage ??= _serviceProvider.GetRequiredService<SignalBoxPage>()),
            AppBottomTabBar.EnginesTabIndex => (MauiView)GetEnginePage(),
            AppBottomTabBar.ControlTabIndex => (MauiView)GetControlPage(),
            _ => (MauiView)GetCounterPage()
        };

        tabView.HorizontalOptions = LayoutOptions.Fill;
        tabView.VerticalOptions = LayoutOptions.Fill;
        tabView.IsVisible = false;

        TabContentGrid.Children.Add(tabView);
        _tabViews[tabIndex] = tabView;
    }

    private void UpdateTabVisibility(int activeTabIndex)
    {
        for (var i = 0; i < TabCount; i++)
        {
            if (_tabViews[i] is { } tabView)
            {
                tabView.IsVisible = i == activeTabIndex;
            }
        }
    }

    private CounterPage GetCounterPage() =>
        _counterPage ??= _serviceProvider.GetRequiredService<CounterPage>();

    private EnginePage GetEnginePage()
    {
        if (_enginePage != null)
        {
            return _enginePage;
        }

        _enginePage = _serviceProvider.GetRequiredService<EnginePage>();
        _enginePage.NavigateToControlTabRequested += OnNavigateToControlTabRequested;
        return _enginePage;
    }

    private ControlPage GetControlPage()
    {
        if (_controlPage != null)
        {
            return _controlPage;
        }

        _controlPage = _serviceProvider.GetRequiredService<ControlPage>();
        _controlPage.NavigateToEnginesTabRequested += OnNavigateToEnginesTabRequested;
        return _controlPage;
    }

    private void OnNavigateToControlTabRequested(object? sender, EventArgs e) =>
        ShowTab(AppBottomTabBar.ControlTabIndex);

    private void OnNavigateToEnginesTabRequested(object? sender, EventArgs e) =>
        ShowTab(AppBottomTabBar.EnginesTabIndex);
}
